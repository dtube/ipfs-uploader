using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uploader.Daemons;
using Uploader.Managers.Common;
using Uploader.Managers.Front;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public class AudioCpuEncodeDaemon : BaseDaemon
    {
        public static AudioCpuEncodeDaemon Instance { get; private set; }

        static AudioCpuEncodeDaemon()
        {
            Instance = new AudioCpuEncodeDaemon();
            Instance.Start(VideoSettings.NbAudioCpuEncodeDaemon);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
            if (!fileItem.AudioCpuEncodeProcess.CanProcess())
            {
                LogManager.AddEncodingMessage("SourceFileName " + Path.GetFileName(fileItem.SourceFilePath) + " car dernier getProgress a dépassé 20s", "Annulation");
                fileItem.AudioCpuEncodeProcess.CancelCascade();
                return;

            }

            if (EncodeManager.AudioCpuEncoding(fileItem))
            {
                VideoGpuEncodeDaemon.Instance.Queue(fileItem, "waiting video encoding...");
            }
        }

        protected override void LogException(FileItem fileItem, Exception ex)
        {
            LogManager.AddEncodingMessage(ex.ToString(), "Exception non gérée");                        
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