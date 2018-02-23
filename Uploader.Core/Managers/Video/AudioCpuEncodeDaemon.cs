using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal class AudioCpuEncodeDaemon : BaseDaemon
    {
        public static AudioCpuEncodeDaemon Instance { get; private set; }

        static AudioCpuEncodeDaemon()
        {
            Instance = new AudioCpuEncodeDaemon();
            Instance.Start(VideoSettings.Instance.NbAudioCpuEncodeDaemon);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
            if (!fileItem.AudioCpuEncodeProcess.CanProcess())
            {
                string message = "FileName " + Path.GetFileName(fileItem.OutputFilePath) + " car le client est déconnecté";
                LogManager.AddEncodingMessage(LogLevel.Warning, message, "Annulation");
                fileItem.AudioCpuEncodeProcess.CancelCascade("Le client est déconnecté.");
                return;
            }

            if (EncodeManager.AudioCpuEncoding(fileItem))
            {
                VideoGpuEncodeDaemon.Instance.Queue(fileItem, "waiting video encoding...");
            }
        }

        protected override void LogException(FileItem fileItem, Exception ex)
        {
            LogManager.AddEncodingMessage(LogLevel.Critical, ex.ToString(), "Exception non gérée");                        
            fileItem.AudioCpuEncodeProcess.SetErrorMessage("Exception non gérée");
        }

        public void Queue(FileItem fileItem, string message)
        {
            base.Queue(fileItem, fileItem.AudioCpuEncodeProcess);

            fileItem.VideoGpuEncodeProcess.SetProgress(message, true);
            foreach (FileItem item in fileItem.FileContainer.EncodedFileItems)
            {
                item.IpfsProcess.SetProgress(message, true);
            }            
        }
    }
}