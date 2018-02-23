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
    internal class VideoGpuEncodeDaemon : BaseDaemon
    {
        public static VideoGpuEncodeDaemon Instance { get; private set; }

        static VideoGpuEncodeDaemon()
        {
            Instance = new VideoGpuEncodeDaemon();
            Instance.Start(VideoSettings.Instance.NbVideoGpuEncodeDaemon);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
            if (!fileItem.VideoGpuEncodeProcess.CanProcess())
            {
                string message = "FileName " + Path.GetFileName(fileItem.OutputFilePath) + " car le client est déconnecté";
                LogManager.AddEncodingMessage(LogLevel.Warning, message, "Annulation");
                fileItem.VideoGpuEncodeProcess.CancelCascade("Le client est déconnecté.");
                return;
            }

            // encoding videos par GPU
            if (EncodeManager.VideoGpuEncoding(fileItem))
            {                        
                // rechercher le 480p pour le sprite
                var video480p = fileItem.FileContainer.EncodedFileItems.FirstOrDefault(v => v.VideoSize == VideoSize.F480p);
                if(video480p != null && fileItem.FileContainer.SpriteVideoFileItem != null)
                {
                    fileItem.FileContainer.SpriteVideoFileItem.SetSourceFilePath(video480p.OutputFilePath);
                    SpriteDaemon.Instance.Queue(fileItem.FileContainer.SpriteVideoFileItem, "Waiting sprite creation...");
                }

                foreach (var item in fileItem.FileContainer.EncodedFileItems)
                {
                    IpfsDaemon.Instance.Queue(item);
                }
            }         
        }

        protected override void LogException(FileItem fileItem, Exception ex)
        {
            LogManager.AddEncodingMessage(LogLevel.Critical, ex.ToString(), "Exception non gérée");                        
            fileItem.VideoGpuEncodeProcess.SetErrorMessage("Exception non gérée");
        }

        public void Queue(FileItem fileItem, string messageIpfs)
        {
            base.Queue(fileItem, fileItem.VideoGpuEncodeProcess);

            foreach (FileItem item in fileItem.FileContainer.EncodedFileItems)
            {
                item.IpfsProcess.SetProgress(messageIpfs, true);
            }            
        }
    }
}