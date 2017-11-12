using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Uploader.Managers;
using Uploader.Models;

namespace Uploader.Daemons
{
    public class EncodeDaemon
    {
        static EncodeDaemon()
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
                    Thread.Sleep(1000);

                    FileItem fileItem;

                    if (!queueFileItems.TryDequeue(out fileItem))
                    {
                        continue;
                    }

                    CurrentPositionInQueue++;

                    // encode video
                    bool success = EncodeManager.Encode(fileItem);

                    if (success)
                    {
                        if (fileItem.ModeSprite)
                        {
                            string[] files = SpriteManager.GetListImageFrom(fileItem.FilePath); // récupération des images
                            string outputPath = TempFileManager.GetNewTempFilePath(); // nom du fichier sprite
                            SpriteManager.CombineBitmap(files, outputPath); // création du sprite
                            TempFileManager.SafeDeleteTempFiles(fileItem.FilePath); // suppression des images
                            fileItem.FilePath = outputPath; // réaffectation chemin sprite
                        }

                        IpfsDaemon.Queue(fileItem);
                    }
                }
            });
        }

        public static void Queue(FileItem fileItem, string messageIpfs)
        {
            queueFileItems.Enqueue(fileItem);
            TotalAddToQueue++;
            fileItem.EncodePositionInQueue = TotalAddToQueue;

            fileItem.EncodeProgress = "Waiting in queue...";
            fileItem.EncodeLastTimeProgressChanged = null;

            fileItem.IpfsProgress = messageIpfs;
            fileItem.IpfsLastTimeProgressChanged = null;
        }
    }
}