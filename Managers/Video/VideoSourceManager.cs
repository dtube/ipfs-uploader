using System;
using System.Drawing;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public static class VideoSourceManager
    {
        public static bool SuccessAnalyseSource(FileItem sourceFile, bool spriteMode, ProcessItem processItem)
        {
            if(sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));
            if(sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));
            if(!sourceFile.IsSource)
                throw new ArgumentException("Doit être le fichier source", nameof(sourceFile));

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
                            var ffmpegProcessManager = new FfmpegProcessManager(sourceFile, sourceFile.InfoSourceProcess);
                            string argumentsImage = $"-y -i {Path.GetFileName(sourceFile.SourceFilePath)} -vf fps=1 -vframes 1 {Path.GetFileName(imageOutputPath)}";
                            ffmpegProcessManager.StartProcess(argumentsImage, VideoSettings.EncodeGetOneImageTimeout);

                            using(Image image = Image.FromFile(imageOutputPath))
                            {
                                sourceFile.VideoWidth = image.Width;
                                sourceFile.VideoHeight = image.Height;
                            }
                        }
                        catch(Exception ex)
                        {
                            Log(ex.ToString(), "Exception source info", spriteMode);
                            sourceFile.VideoDuration = -1; //pour ne pas essayer de le recalculer sur une demande de video à encoder
                        }

                        TempFileManager.SafeDeleteTempFile(imageOutputPath);
                    }
                }
            }
            
            // Si durée totale de vidéo, largeur hauteur non récupéré, on ne peut pas continuer
            if (!sourceFile.SuccessGetSourceInfo())
            {
                string message = "Error while getting duration, height or width.";
                Log(message + " FileName : " + Path.GetFileName(sourceFile.SourceFilePath), "Error source info", spriteMode);

                if(!spriteMode && sourceFile.IpfsProcess == null)
                {
                    sourceFile.AddIpfsProcess(sourceFile.SourceFilePath);
                    IpfsDaemon.Instance.Queue(sourceFile);
                }

                processItem.SetErrorMessage(message);

                return false;
            }

            int duration = sourceFile.VideoDuration.Value;

            Log("SourceVideoDuration " + duration + " / SourceVideoFileSize " + sourceFile.FileSize, "Info source", spriteMode);

            // Désactivation encoding et sprite si dépassement de la durée maximale
            if(sourceFile.HasReachMaxVideoDurationForEncoding())
            {
                if(!spriteMode && sourceFile.IpfsProcess == null)
                {
                    sourceFile.AddIpfsProcess(sourceFile.SourceFilePath);
                    IpfsDaemon.Instance.Queue(sourceFile);
                }

                processItem.CancelCascade("Dépassement de la durée limite de la vidéo atteinte.");

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