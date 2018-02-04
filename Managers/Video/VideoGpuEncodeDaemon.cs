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
    public class VideoGpuEncodeDaemon : BaseDaemon
    {
        public static VideoGpuEncodeDaemon Instance { get; private set; }

        static VideoGpuEncodeDaemon()
        {
            Instance = new VideoGpuEncodeDaemon();
            Instance.Start(VideoSettings.NbVideoGpuEncodeDaemon);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
            if (!fileItem.VideoGpuEncodeProcess.CanProcess())
            {
                LogManager.AddEncodingMessage("SourceFileName " + Path.GetFileName(fileItem.SourceFilePath) + " car dernier getProgress a dépassé 20s", "Annulation");
                fileItem.VideoGpuEncodeProcess.CancelCascade();                
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
            LogManager.AddEncodingMessage(ex.ToString(), "Exception non gérée");                        
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