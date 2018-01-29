using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uploader.Daemons;
using Uploader.Managers.Common;
using Uploader.Managers.Front;
using Uploader.Models;

namespace Uploader.Managers.Ipfs
{
    public class IpfsDaemon : BaseDaemon
    {
        public static IpfsDaemon Instance { get; private set; }

        static IpfsDaemon()
        {
            Instance = new IpfsDaemon();
            Instance.Start(1);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // Si le client a pas demandé le progress depuis moins de 20s, annuler l'opération
            if((DateTime.UtcNow - fileItem.FileContainer.LastTimeProgressRequested).TotalSeconds > FrontSettings.MaxGetProgressCanceled)
            {                            
                LogManager.AddIpfsMessage("FileName " + Path.GetFileName(fileItem.OutputFilePath) + " car dernier getProgress a dépassé 20s", "Annulation");
                fileItem.CancelIpfs();
                return;                
            }

            // Ipfs add file
            IpfsAddManager.Add(fileItem);
            fileItem.CleanFiles();
        }

        protected override void LogException(FileItem fileItem, Exception ex)
        {
            LogManager.AddIpfsMessage(ex.ToString(), "Exception non gérée");
            fileItem.SetIpfsErrorMessage("Exception non gérée");
        }

        public void Queue(FileItem fileItem)
        {
            base.Queue(fileItem, fileItem.IpfsProcess);
        }
    }
}