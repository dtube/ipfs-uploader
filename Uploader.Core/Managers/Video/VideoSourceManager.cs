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
                LogManager.AddEncodingMessage(LogLevel.Critical, "Exception non gérée", "Exception source info", ex);
            }
            
            // Si durée totale de vidéo, largeur hauteur non récupéré, on ne peut pas continuer
            if (!sourceFile.SuccessGetSourceInfo())
            {
                string message = "Error while source video information.";
                string longMessage = message + " FileName : " + Path.GetFileName(sourceFile.SourceFilePath);
                processItem.SetErrorMessage(message, longMessage);
                return false;
            }

            LogManager.AddEncodingMessage(LogLevel.Information, "SourceVideoDuration " + sourceFile.VideoDuration.Value + " / SourceVideoFileSize " + sourceFile.FileSize, "Info source");

            return true;
        }
    }
}