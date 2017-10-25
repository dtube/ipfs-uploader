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
        static IpfsDaemon()
        {
            Start();
        }

        private static ConcurrentQueue<FileItem> queueFileItems = new ConcurrentQueue<FileItem>();

        private static Task daemon = null;

        public static int CurrentPositionInQueue { get; set; }

        public static int TotalAddToQueue { get; set; }

        private static void Start()
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
                    IpfsAddManager.Add(fileItem);

                    // si tout est terminé, supprimer le fichier source
                    if(!fileItem.FileContainer.WorkInProgress())
                    {
                        TempFileManager.SafeDeleteTempFile(fileItem.FileContainer.SourceFileItem.FilePath);
                    }

                    if(!fileItem.IsSource)
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
        }
    }
}