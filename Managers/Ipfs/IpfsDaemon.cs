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
                            fileItem.IpfsErrorMessage = "Canceled";
                            fileItem.IpfsProgress = null;
                            LogManager.AddIpfsMessage("FileName " + Path.GetFileName(fileItem.FilePath) + " car dernier getProgress a dépassé 20s", "Annulation");
                        }
                        else
                        {
                            // Ipfs add file
                            IpfsAddManager.Add(fileItem);
                        }
                    }
                    catch(Exception ex)
                    {
                        LogManager.AddIpfsMessage(ex.ToString(), "Exception non gérée");
                        fileItem.IpfsErrorMessage = "Exception non gérée";
                    }

                    fileItem.CleanFiles();
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