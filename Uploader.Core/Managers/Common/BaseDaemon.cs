using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Video;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Common
{
    internal abstract class BaseDaemon
    {
        private ConcurrentQueue<FileItem> queueFileItems = new ConcurrentQueue<FileItem>();

        private List<Task> daemons = new List<Task>();

        public int CurrentPositionInQueue
        {
            get;
            private set;
        }

        public int TotalAddToQueue
        {
            get;
            private set;
        }

        protected void Start(int parralelTask)
        {
            for (int i = 0; i < parralelTask; i++)
            {
                Task daemon = Task.Run(() =>
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

                            ProcessItem(fileItem);
                        }
                        catch(Exception ex)
                        {
                            LogException(fileItem, ex);
                        }

                        // Nettoyer les fichiers au besoin
                        fileItem.FileContainer.CleanFilesIfEnd();
                    }
                });
                daemons.Add(daemon);
            }
        }

        protected abstract void ProcessItem(FileItem fileItem);

        protected abstract void LogException(FileItem fileItem, Exception ex);

        protected void Queue(FileItem fileItem, ProcessItem processItem)
        {
            queueFileItems.Enqueue(fileItem);
            TotalAddToQueue++;

            processItem.SavePositionInQueue(TotalAddToQueue, CurrentPositionInQueue);
            processItem.SetProgress("Waiting in queue...", true);
        }

        public int CurrentWaitingInQueue => TotalAddToQueue - CurrentPositionInQueue;
    }
}