using System;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public static class EncodeManager
    {
        public static bool Encode(FileItem fileItem)
        {
            string newEncodedFilePath = null;

            try
            {
                fileItem.EncodeProcess.StartProcessDateTime();

                FileItem sourceFile = fileItem.FileContainer.SourceFileItem;
                string sourceFilePath = sourceFile.FilePath;
                VideoSize videoSize = fileItem.VideoSize;
                LogManager.AddEncodingMessage("SourceFilePath " + Path.GetFileName(sourceFilePath) + " -> " + videoSize, "Start");
   
                // Récupérer la durée totale de la vidéo et sa résolution, autorisation encoding
                if(!VideoSourceManager.CheckAndAnalyseSource(fileItem, false))
                    return false;
                
                string size;
                string maxRate = string.Empty;
                switch (videoSize)
                {
                    case VideoSize.F360p:
                        {
                            maxRate = "200k";
                            Tuple<int, int> finalSize = SizeHelper.GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 640, 360);
                            size = $"{finalSize.Item1}:{finalSize.Item2}";
                            break;
                        }

                    case VideoSize.F480p:
                        {
                            maxRate = "500k";
                            Tuple<int, int> finalSize = SizeHelper.GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 854, 480);
                            size = $"{finalSize.Item1}:{finalSize.Item2}";
                            break;
                        }

                    case VideoSize.F720p:
                        {
                            maxRate = "1000k";
                            Tuple<int, int> finalSize = SizeHelper.GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 1280, 720);
                            size = $"{finalSize.Item1}:{finalSize.Item2}";
                            break;
                        }

                    case VideoSize.F1080p:
                        {
                            maxRate = "1600k";
                            Tuple<int, int> finalSize = SizeHelper.GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 1920, 1080);
                            size = $"{finalSize.Item1}:{finalSize.Item2}";
                            break;
                        }

                    default:
                        throw new InvalidOperationException("Format non reconnu.");
                }

                newEncodedFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".mp4");
                string arguments;                
                if(VideoSettings.GpuEncodeMode)
                    arguments = $"-y -hwaccel cuvid -vcodec h264_cuvid -vsync 0 -i {Path.GetFileName(sourceFilePath)} -pixel_format yuv420p -vf scale_npp={size} -b:v {maxRate} -maxrate {maxRate} -bufsize {maxRate} -vcodec h264_nvenc -acodec aac -strict -2 {Path.GetFileName(newEncodedFilePath)}";
                else
                    arguments = $"-y -i {Path.GetFileName(sourceFilePath)} -pixel_format yuv420p -vf scale={size} -vcodec libx264 -acodec aac -strict -2 {Path.GetFileName(newEncodedFilePath)}"; //-strict -2 pour forcer aac sur ubuntu

                var ffmpegProcessManager = new FfmpegProcessManager(fileItem);
                ffmpegProcessManager.StartProcess(arguments, VideoSettings.EncodeTimeout);

                fileItem.FilePath = newEncodedFilePath;
                LogManager.AddEncodingMessage("OutputFileName " + Path.GetFileName(newEncodedFilePath) + " / FileSize " + fileItem.FileSize + " / Format " + videoSize, "End Encoding");

                fileItem.EncodeProcess.EndProcessDateTime();
                return true;
            }
            catch (Exception ex)
            {
                LogManager.AddEncodingMessage("Video Duration " + fileItem.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.EncodeProcess.Progress + " / Exception : " + ex, "Exception");
                fileItem.SetEncodeErrorMessage("Exception");
                TempFileManager.SafeDeleteTempFile(newEncodedFilePath);
                return false;
            }
        }
    }
}
