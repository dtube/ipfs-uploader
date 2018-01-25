using System;
using System.Collections.Concurrent;
using System.IO;
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

        public static int TotalAddToQueue
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
                    FileItem fileItem = null;
                    try
                    {
                        Thread.Sleep(1000);

                        fileItem = null;

                        if (!queueFileItems.TryDequeue(out fileItem))
                        {
                            continue;
                        }

                        CurrentPositionInQueue++;

                        // Si le client a pas demandé le progress depuis moins de 20s, annuler l'opération
                        if((DateTime.UtcNow - fileItem.FileContainer.LastTimeProgressRequested).TotalSeconds > FrontSettings.MaxGetProgressCanceled)
                        {                            
                            LogManager.AddIpfsMessage("FileName " + Path.GetFileName(fileItem.FilePath) + " car dernier getProgress a dépassé 20s", "Annulation");
                            fileItem.CancelIpfs();
                        }
                        else
                        {
                            // Ipfs add file
                            IpfsAddManager.Add(fileItem);
                            fileItem.CleanFiles();
                        }
                    }
                    catch(Exception ex)
                    {
                        LogManager.AddIpfsMessage(ex.ToString(), "Exception non gérée");
                        fileItem.SetIpfsErrorMessage("Exception non gérée");
                    }
                }
            });
        }

        public static void Queue(FileItem fileItem)
        {
            queueFileItems.Enqueue(fileItem);
            TotalAddToQueue++;
            fileItem.IpfsProcess.SavePositionInQueue(TotalAddToQueue, CurrentPositionInQueue);
            fileItem.IpfsProcess.SetProgress("Waiting in queue...", true);
        }
    }
}