using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal static class VideoSourceManager
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
                var ffProbeProcessManager = new FfProbeProcessManager(sourceFile);
                ffProbeProcessManager.FillInfo(VideoSettings.Instance.FfProbeTimeout);
            }
            catch(Exception ex)
            {
                LogManager.AddEncodingMessage(LogLevel.Critical, ex.ToString(), "Exception source info");
            }
            
            // Si durée totale de vidéo, largeur hauteur non récupéré, on ne peut pas continuer
            if (!sourceFile.SuccessGetSourceInfo())
            {
                string message = "Error while getting duration, height or width.";
                LogManager.AddEncodingMessage(LogLevel.Error, message + " FileName : " + Path.GetFileName(sourceFile.SourceFilePath), "Error source info");

                if(sourceFile.IpfsProcess == null)
                {
                    sourceFile.AddIpfsProcess(sourceFile.SourceFilePath);
                    IpfsDaemon.Instance.Queue(sourceFile);
                }

                processItem.SetErrorMessage(message);

                return false;
            }

            LogManager.AddEncodingMessage(LogLevel.Information, "SourceVideoDuration " + sourceFile.VideoDuration.Value + " / SourceVideoFileSize " + sourceFile.FileSize, "Info source");

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
    }
}