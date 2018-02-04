using System;
using System.Collections.Generic;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public static class EncodeManager
    {
        public static bool AudioVideoCpuEncoding(FileItem fileItem)
        {
            try
            {
                FileItem sourceFile = fileItem.FileContainer.SourceFileItem;
                LogManager.AddEncodingMessage("SourceFilePath " + Path.GetFileName(sourceFile.SourceFilePath) + " -> " + fileItem.VideoSize, "Start AudioVideoCpuEncoding");
                fileItem.AudioVideoCpuEncodeProcess.StartProcessDateTime();

                // Récupérer la durée totale de la vidéo et sa résolution, autorisation encoding
                if(!VideoSourceManager.SuccessAnalyseSource(fileItem, false, fileItem.AudioVideoCpuEncodeProcess))
                {
                    return false;
                }

                string size = GetSize(fileItem.VideoSize, sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value);
                string arguments = $"-y -i {Path.GetFileName(sourceFile.SourceFilePath)} -pixel_format yuv420p -vf scale={size} -vcodec libx264 -acodec aac -strict -2 {Path.GetFileName(fileItem.TempFilePath)}"; //-strict -2 pour forcer aac sur ubuntu

                var ffmpegProcessManager = new FfmpegProcessManager(fileItem, fileItem.AudioVideoCpuEncodeProcess);
                ffmpegProcessManager.StartProcess(arguments, VideoSettings.EncodeTimeout);

                fileItem.SetOutputFilePath(fileItem.TempFilePath);
                LogManager.AddEncodingMessage("OutputFileName " + Path.GetFileName(fileItem.OutputFilePath) + " / FileSize " + fileItem.FileSize + " / Format " +fileItem.VideoSize, "End AudioVideoCpuEncoding");
                fileItem.AudioVideoCpuEncodeProcess.EndProcessDateTime();

                return true;
            }
            catch (Exception ex)
            {
                LogManager.AddEncodingMessage("Video Duration " + fileItem.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.AudioVideoCpuEncodeProcess.Progress + " / Exception : " + ex, "Exception AudioVideoCpuEncoding");
                fileItem.AudioVideoCpuEncodeProcess.SetErrorMessage("Exception");
                TempFileManager.SafeDeleteTempFile(fileItem.TempFilePath);
                return false;
            }
        }

        public static bool AudioCpuEncoding(FileItem fileItem)
        {
            try
            {
                LogManager.AddEncodingMessage("SourceFilePath " + Path.GetFileName(fileItem.SourceFilePath), "Start AudioCpuEncoding");
                fileItem.AudioCpuEncodeProcess.StartProcessDateTime();

                // Récupérer la durée totale de la vidéo et sa résolution, autorisation encoding
                if(!VideoSourceManager.SuccessAnalyseSource(fileItem, false, fileItem.AudioCpuEncodeProcess))
                {
                    return false;
                }

                // encoding audio de la source
                string arguments = $"-y -i {Path.GetFileName(fileItem.SourceFilePath)} -vcodec copy -acodec aac -strict -2 {Path.GetFileName(fileItem.TempFilePath)}";

                var ffmpegProcessManager = new FfmpegProcessManager(fileItem, fileItem.AudioCpuEncodeProcess);
                ffmpegProcessManager.StartProcess(arguments, VideoSettings.EncodeTimeout);

                fileItem.SetVideoAacTempFilePath(fileItem.TempFilePath);
                LogManager.AddEncodingMessage("OutputFileName " + Path.GetFileName(fileItem.VideoAacTempFilePath) + " / FileSize " + fileItem.FileSize + " / Format " +fileItem.VideoSize, "End AudioCpuEncoding");
                fileItem.AudioCpuEncodeProcess.EndProcessDateTime();

                return true;
            }
            catch (Exception ex)
            {
                LogManager.AddEncodingMessage("Video Duration " + fileItem.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.AudioCpuEncodeProcess.Progress + " / Exception : " + ex, "Exception AudioCpuEncoding");
                fileItem.AudioCpuEncodeProcess.SetErrorMessage("Exception");
                TempFileManager.SafeDeleteTempFile(fileItem.TempFilePath);
                return false;
            }
        }

        public static bool VideoGpuEncoding(FileItem fileItem)
        {
            try
            {
                LogManager.AddEncodingMessage("SourceFilePath " + Path.GetFileName(fileItem.VideoAacTempFilePath) + " -> 1:N formats", "Start VideoGpuEncoding");
                fileItem.VideoGpuEncodeProcess.StartProcessDateTime();
                
                // Récupérer la durée totale de la vidéo et sa résolution, autorisation encoding
                if(!VideoSourceManager.SuccessAnalyseSource(fileItem, false, fileItem.VideoGpuEncodeProcess))
                {
                    return false;
                }

                // encoding video 1:N formats
                //string arguments = $"-y -hwaccel cuvid -vcodec h264_cuvid -vsync 0 -i {Path.GetFileName(fileItem.VideoAacTempFilePath)}";
                string arguments = $"-y -i {Path.GetFileName(fileItem.VideoAacTempFilePath)}";
                foreach (var item in fileItem.FileContainer.EncodedFileItems)
                {
                    string size = GetSize(item.VideoSize, fileItem.VideoWidth.Value, fileItem.VideoHeight.Value);
                    string maxRate = GetMaxRate(item.VideoSize);
                    //arguments += $" -pixel_format yuv420p -vf scale_npp={size} -b:v {maxRate} -maxrate {maxRate} -bufsize {maxRate} -vcodec h264_nvenc -acodec copy {Path.GetFileName(item.TempFilePath)}";
                    arguments += $" -pixel_format yuv420p -vf scale={size} -b:v {maxRate} -maxrate {maxRate} -bufsize {maxRate} -vcodec h264_nvenc -acodec copy {Path.GetFileName(item.TempFilePath)}";
                }

                var ffmpegProcessManager = new FfmpegProcessManager(fileItem, fileItem.VideoGpuEncodeProcess);
                ffmpegProcessManager.StartProcess(arguments, VideoSettings.EncodeTimeout);

                foreach (var item in fileItem.FileContainer.EncodedFileItems)
                {
                    item.SetOutputFilePath(item.TempFilePath);
                    LogManager.AddEncodingMessage("OutputFileName " + Path.GetFileName(item.OutputFilePath) + " / FileSize " + item.FileSize + " / Format " + item.VideoSize, "End VideoGpuEncoding");
                }
                fileItem.VideoGpuEncodeProcess.EndProcessDateTime();
                TempFileManager.SafeDeleteTempFile(fileItem.VideoAacTempFilePath);

                return true;
            }
            catch (Exception ex)
            {
                LogManager.AddEncodingMessage("Video Duration " + fileItem.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.VideoGpuEncodeProcess.Progress + " / Exception : " + ex, "Exception VideoGpuEncoding");
                fileItem.VideoGpuEncodeProcess.SetErrorMessage("Exception");
                foreach (FileItem item in fileItem.FileContainer.EncodedFileItems)
                {
                    TempFileManager.SafeDeleteTempFile(item.TempFilePath);
                }
                TempFileManager.SafeDeleteTempFile(fileItem.VideoAacTempFilePath);
                return false;
            }
        }

        private static string GetSize(VideoSize videoSize, int width, int height)
        {
            switch (videoSize)
            {
                case VideoSize.F360p:
                    {
                        Tuple<int, int> finalSize = SizeHelper.GetSize(width, height, 640, 360);
                        return $"{finalSize.Item1}:{finalSize.Item2}";
                    }

                case VideoSize.F480p:
                    {
                        Tuple<int, int> finalSize = SizeHelper.GetSize(width, height, 854, 480);
                        return $"{finalSize.Item1}:{finalSize.Item2}";
                    }

                case VideoSize.F720p:
                    {
                        Tuple<int, int> finalSize = SizeHelper.GetSize(width, height, 1280, 720);
                        return $"{finalSize.Item1}:{finalSize.Item2}";
                    }

                case VideoSize.F1080p:
                    {
                        Tuple<int, int> finalSize = SizeHelper.GetSize(width, height, 1920, 1080);
                        return $"{finalSize.Item1}:{finalSize.Item2}";
                    }

                default:
                    throw new InvalidOperationException("Format non reconnu.");
            }
        }

        private static string GetMaxRate(VideoSize videoSize)
        {
            switch (videoSize)
            {
                case VideoSize.F360p:
                        return "200k";

                case VideoSize.F480p:
                        return "500k";

                case VideoSize.F720p:
                        return "1000k";

                case VideoSize.F1080p:
                        return "1600k";

                default:
                    throw new InvalidOperationException("Format non reconnu.");
            }
        }
    }
}
