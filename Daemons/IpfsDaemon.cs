using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsUploader.Managers;
using IpfsUploader.Models;

namespace IpfsUploader.Daemons
{
    public static class IpfsDaemon
    {
        private static ConcurrentQueue<FileItem> queueFileItems = new ConcurrentQueue<FileItem>();

        private static ConcurrentDictionary<Guid, FileItem> sourceProgresses = new ConcurrentDictionary<Guid, FileItem>();

        private static Task daemon = null;

        public static int CurrentPositionInQueue { get; set; }

        public static int TotalAddToQueue { get; set; }

        public static void Start()
        {
            daemon = Task.Run(() =>
            {
                while(true)
                {
                    Thread.Sleep(1000);

                    FileItem fileItem;

                    if(!queueFileItems.TryDequeue(out fileItem))
                    {
                        continue;
                    }

                    CurrentPositionInQueue++;

                    // Ipfs add file
                    IpfsManager.Add(fileItem);

                    // si tout est terminé, supprimer le fichier source
                    if(!fileItem.FileContainer.WorkInProgress())
                    {
                        TempFileManager.SafeDeleteTempFile(fileItem.FileContainer.SourceFileItem.FilePath);
                    }

                    if(fileItem.IsSource)
                    {
                        // Supprimer le suivi ipfs add progress après 1j
                        Task taskClean = Task.Run(() =>
                        {
                            Guid token = fileItem.IpfsProgressToken;
                            Thread.Sleep(24 * 60 * 60 * 1000); // 1j
                            FileItem thisFileItem;
                            sourceProgresses.TryRemove(token, out thisFileItem);
                        });
                    }
                    else
                    {
                        // Supprimer du fichier attaché
                        TempFileManager.SafeDeleteTempFile(fileItem.FilePath);
                    }                                 
                }
            });
        }

        /// <summary>
        /// Nouveau fichier source à ajouter
        /// </summary>
        /// <param name="fileContainer"></param>
        /// <returns></returns>
        public static void QueueSourceFile(FileContainer fileContainer)
        {
            sourceProgresses.TryAdd(fileContainer.SourceFileItem.IpfsProgressToken, fileContainer.SourceFileItem);
            Queue(fileContainer.SourceFileItem);
        }

        /// <summary>
        /// Fichier à ajouter
        /// </summary>
        /// <param name="attachedFileItem"></param>
        /// <returns></returns>
        public static void QueueAttachedFile(FileItem attachedFileItem)
        {
            Queue(attachedFileItem);
        }

        private static void Queue(FileItem fileItem)
        {
            queueFileItems.Enqueue(fileItem);
            TotalAddToQueue++;
            fileItem.IpfsPositionInQueue = TotalAddToQueue;
        }

        public static FileContainer GetFileContainer(Guid sourceToken)
        {
            FileItem fileItem;
            if(!sourceProgresses.TryGetValue(sourceToken, out fileItem))
            {
                return null;
            }

            return fileItem.FileContainer;
        }

        public static FileContainer GetFileContainer(string sourceHash)
        {
            FileItem fileItem =  sourceProgresses.Values
                .OrderByDescending(s => s.IpfsLastTimeProgressChanged)
                .FirstOrDefault(s => s.IpfsHash == sourceHash);
            
            return fileItem != null ? fileItem.FileContainer : null;
        }
    }
}