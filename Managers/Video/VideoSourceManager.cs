using System;
using System.Drawing;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public static class VideoSourceManager
    {
        public static bool CheckAndAnalyseSource(FileItem fileItem, bool spriteMode)
        {
            FileItem sourceFile = fileItem.FileContainer.SourceFileItem;
            // Récupérer la durée totale de la vidéo et sa résolution
            if (!sourceFile.VideoDuration.HasValue)
            {
                lock(sourceFile)
                {
                    if (!sourceFile.VideoDuration.HasValue)
                    {
                        string imageOutputPath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".jpeg");

                        try
                        {
                            var ffmpegProcessManager = new FfmpegProcessManager(fileItem);
                            string argumentsImage = $"-y -i {Path.GetFileName(sourceFile.FilePath)} -vf fps=1 -vframes 1 {Path.GetFileName(imageOutputPath)}";
                            ffmpegProcessManager.StartProcess(argumentsImage, VideoSettings.EncodeGetOneImageTimeout);

                            using(Image image = Image.FromFile(imageOutputPath))
                            {
                                sourceFile.VideoWidth = image.Width;
                                sourceFile.VideoHeight = image.Height;
                            }
                        }
                        catch(Exception ex)
                        {
                            Log(ex.ToString(), "Exception", spriteMode);
                            sourceFile.VideoDuration = -1; //pour ne pas essayer de le recalculer sur une demande de video à encoder
                        }

                        TempFileManager.SafeDeleteTempFile(imageOutputPath);
                    }
                }
            }
            
            // Si durée totale de vidéo, largeur hauteur non récupéré, on ne peut pas continuer
            if ((sourceFile.VideoDuration??0) <= 0 || (sourceFile.VideoWidth??0) <= 0 || (sourceFile.VideoHeight??0) <= 0)
            {
                Log("Error while getting duration, height or width. FileName : " + Path.GetFileName(sourceFile.FilePath), "Error", spriteMode);                
                fileItem.SetEncodeErrorMessage("Error while getting duration, height or width.");
                return false;
            }

            int duration = sourceFile.VideoDuration.Value;

            Log("SourceVideoDuration " + duration + " / SourceVideoFileSize " + fileItem.FileContainer.SourceFileItem.FileSize, "Info source", spriteMode);

            // Désactivation encoding et sprite si dépassement de la durée maximale
            if(duration > VideoSettings.MaxVideoDurationForEncoding)
            {
                if(spriteMode)
                    fileItem.FileContainer.DeleteSpriteVideo();
                else
                    fileItem.FileContainer.EncodedFileItems.Clear();

                fileItem.SetEncodeErrorMessage("Disable because duration reach the max limit.");
                return false;
            }

            return true;
        }

        private static void Log(string message, string typeMessage, bool spriteMode)
        {
            if(spriteMode)
                LogManager.AddSpriteMessage(message, typeMessage);
            else
                LogManager.AddEncodingMessage(message, typeMessage);
        }
    }
}