using System.Collections.Concurrent;
using System.Threading.Tasks;
using IpfsUploader.Managers;
using IpfsUploader.Models;

namespace IpfsUploader.Daemons
{
    public class SteemDaemon
    {
        private static ConcurrentQueue<VideoFile> queueVideoFiles = new ConcurrentQueue<VideoFile>();
        
        private static Task daemon = null;

        public static void Start()
        {
            daemon = Task.Run(() =>
            {
                while(true)
                {
                    VideoFile videoFile;

                    System.Threading.Thread.Sleep(1000);

                    if(!queueVideoFiles.TryDequeue(out videoFile))
                    {
                        continue;
                    }

                    if(videoFile.WorkInProgress())
                    {
                        Queue(videoFile); //remettre dans la queue;
                        continue;
                    }

                    // maj steem
                    SteemManager.Update(videoFile);

                    // suppr sourceFile
                    TempFileManager.SafeDeleteTempFile(videoFile.SourceFileItem.FilePath);
                }
            });
        }

        public static void Queue(VideoFile videoFile)
        {
            queueVideoFiles.Enqueue(videoFile);
        }
    }
}