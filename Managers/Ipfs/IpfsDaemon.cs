using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Uploader.Managers.Common;
using Uploader.Managers.Front;
using Uploader.Models;

namespace Uploader.Managers.Ipfs
{
    public static class IpfsDaemon
    {
        static IpfsDaemon()
        {
            Start();
        }

        private static ConcurrentQueue<FileItem> queueFileItems = new ConcurrentQueue<FileItem>();

        private static Task daemon = null;

        public static int CurrentPositionInQueue
        {
            get;
            set;
        }

        private static int TotalAddToQueue
        {
            get;
            set;
        }

        private static void Start()
        {
            daemon = Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    FileItem fileItem;

                    if (!queueFileItems.TryDequeue(out fileItem))
                    {
                        continue;
                    }

                    CurrentPositionInQueue++;

                    // Si le client a pas demandé le progress depuis moins de 20s, lancer l'ipfs add
                    if((DateTime.UtcNow - fileItem.FileContainer.LastTimeProgressRequested).TotalSeconds <= FrontSettings.MaxGetProgressCanceled)
                    {
                        // Ipfs add file
                        IpfsAddManager.Add(fileItem);
                    }
                    else
                    {
                        fileItem.IpfsErrorMessage = "Canceled";
                        fileItem.IpfsProgress = null;
                    }

                    // Si tout est terminé, supprimer le fichier source
                    if (!fileItem.FileContainer.WorkInProgress())
                    {
                        TempFileManager.SafeDeleteTempFile(fileItem.FileContainer.SourceFileItem.FilePath);
                    }

                    if (!fileItem.IsSource)
                    {
                        // Supprimer le fichier attaché
                        TempFileManager.SafeDeleteTempFile(fileItem.FilePath);
                    }
                }
            });
        }

        public static void Queue(FileItem fileItem)
        {
            queueFileItems.Enqueue(fileItem);
            TotalAddToQueue++;
            fileItem.IpfsPositionInQueue = TotalAddToQueue;
            fileItem.IpfsProgress = "Waiting in queue...";
        }
    }
}