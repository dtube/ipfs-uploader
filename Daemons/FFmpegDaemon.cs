using System.Collections.Concurrent;
using System.Threading.Tasks;
using IpfsUploader.Managers;
using IpfsUploader.Models;

namespace IpfsUploader.Daemons
{
    public class FFmpegDaemon
    {
        private static ConcurrentQueue<FileItem> queueFileItems = new ConcurrentQueue<FileItem>();
        
        private static Task daemon = null;

        public static void Start()
        {
            daemon = Task.Run(() =>
            {
                while(true)
                {
                    FileItem fileItem;

                    System.Threading.Thread.Sleep(1000);

                    if(!queueFileItems.TryDequeue(out fileItem))
                    {
                        continue;
                    }

                    // encode video
                    if(FFmpegManager.Encode(fileItem))
                        IpfsDaemon.QueueEncodedFile(fileItem);
                }
            });
        }

        public static void Queue(FileItem fileItem)
        {
            queueFileItems.Enqueue(fileItem);
        }
    }
}