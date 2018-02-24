using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal class SpriteDaemon : BaseDaemon
    {
        public static SpriteDaemon Instance { get; private set; }

        static SpriteDaemon()
        {
            Instance = new SpriteDaemon();
            Instance.Start(VideoSettings.Instance.NbSpriteDaemon);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
            if (!fileItem.SpriteEncodeProcess.CanProcess())
            {
                string message = "FileName " + Path.GetFileName(fileItem.OutputFilePath) + " car le client est déconnecté";
                fileItem.SpriteEncodeProcess.CancelCascade("Le client est déconnecté.", message);
                return;
            }

            // sprite creation video
            if (SpriteManager.Encode(fileItem))
                IpfsDaemon.Instance.Queue(fileItem);
        }

        protected override void LogException(FileItem fileItem, Exception ex)
        {
            fileItem.SpriteEncodeProcess.SetErrorMessage("Exception non gérée", "Exception non gérée", ex);
        }

        public void Queue(FileItem fileItem, string messageIpfs)
        {
            base.Queue(fileItem, fileItem.SpriteEncodeProcess);

            fileItem.IpfsProcess.SetProgress(messageIpfs, true);
        }
    }
}