using System;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public static class VideoSourceManager
    {
        public static bool SuccessAnalyseSource(FileItem sourceFile, ProcessItem processItem)
        {
            if(sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));
            if(sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));
            if(!sourceFile.IsSource)
                throw new ArgumentException("Doit être le fichier source", nameof(sourceFile));

            // Récupérer la durée totale de la vidéo et sa résolution
            try
            {
                var ffmpegProcessManager = new FfProbeProcessManager(sourceFile);
                ffmpegProcessManager.StartProcess(VideoSettings.EncodeGetOneImageTimeout);
            }
            catch(Exception ex)
            {
                Log(ex.ToString(), "Exception source info");
            }
            
            // Si durée totale de vidéo, largeur hauteur non récupéré, on ne peut pas continuer
            if (!sourceFile.SuccessGetSourceInfo())
            {
                string message = "Error while getting duration, height or width.";
                Log(message + " FileName : " + Path.GetFileName(sourceFile.SourceFilePath), "Error source info");

                if(sourceFile.IpfsProcess == null)
                {
                    sourceFile.AddIpfsProcess(sourceFile.SourceFilePath);
                    IpfsDaemon.Instance.Queue(sourceFile);
                }

                processItem.SetErrorMessage(message);

                return false;
            }

            Log("SourceVideoDuration " + sourceFile.VideoDuration.Value + " / SourceVideoFileSize " + sourceFile.FileSize, "Info source");

            // Désactivation encoding et sprite si dépassement de la durée maximale
            if(sourceFile.HasReachMaxVideoDurationForEncoding())
            {
                if(sourceFile.IpfsProcess == null)
                {
                    sourceFile.AddIpfsProcess(sourceFile.SourceFilePath);
                    IpfsDaemon.Instance.Queue(sourceFile);
                }

                processItem.CancelCascade("Dépassement de la durée limite de la vidéo atteinte.");

                return false;
            }

            return true;
        }

        private static void Log(string message, string typeMessage)
        {
            LogManager.AddEncodingMessage(message, typeMessage);
        }
    }
}