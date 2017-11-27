using System;
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

                    bool success = false;

                    // si le client a pas demandé le progress depuis moins de 20s, lancer l'encoding
                    if((DateTime.UtcNow - fileItem.FileContainer.LastTimeProgressRequested).TotalSeconds <= Settings.MaxGetProgressCanceled)
                    {
                        // encode video
                        success = EncodeManager.Encode(fileItem);
                    }
                    else
                    {
                        fileItem.EncodeErrorMessage = "Canceled";
                        fileItem.EncodeProgress = null;

                        fileItem.IpfsErrorMessage = "Canceled";
                        fileItem.IpfsProgress = null;
                    }

                    if (success)
                    {
                        if (fileItem.ModeSprite)
                        {
                            string[] files = SpriteManager.GetListImageFrom(fileItem.FilePath); // récupération des images
                            string outputPath = System.IO.Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png"); // nom du fichier sprite
                            bool successSprite = SpriteManager.CombineBitmap(files, outputPath); // création du sprite
                            TempFileManager.SafeDeleteTempFiles(files); // suppression des images
                            if(successSprite)
                            {                                
                                fileItem.FilePath = outputPath; // réaffectation chemin sprite
                                IpfsDaemon.Queue(fileItem);
                            }
                        }
                        else
                        {
                            IpfsDaemon.Queue(fileItem);
                        }
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

            fileItem.IpfsProgress = messageIpfs;
        }
    }
}