using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Uploader.Managers.Common;
using Uploader.Managers.Front;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Video
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
                    try
                    {
                        Thread.Sleep(1000);

                        FileItem fileItem;

                        if (!queueFileItems.TryDequeue(out fileItem))
                        {
                            continue;
                        }

                        CurrentPositionInQueue++;

                        // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
                        if ((DateTime.UtcNow - fileItem.FileContainer.LastTimeProgressRequested).TotalSeconds > FrontSettings.MaxGetProgressCanceled)
                        {
                            fileItem.EncodeErrorMessage = "Canceled";
                            fileItem.EncodeProgress = null;

                            fileItem.IpfsErrorMessage = "Canceled";
                            fileItem.IpfsProgress = null;

                            continue;
                        }

                        // encode video
                        if (!EncodeManager.Encode(fileItem))
                            continue;

                        switch (fileItem.TypeFile)
                        {
                            case TypeFile.SpriteVideo:
                                {
                                    string[] files = EncodeManager.GetListImageFrom(fileItem.FilePath); // récupération des images
                                    string outputPath = System.IO.Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".jpeg"); // nom du fichier sprite
                                    bool successSprite = SpriteManager.CombineBitmap(files, outputPath); // création du sprite                                
                                    TempFileManager.SafeDeleteTempFiles(files); // suppression des images
                                    if(successSprite)
                                    {                                
                                        fileItem.FilePath = outputPath; // réaffectation chemin sprite
                                        LogManager.AddEncodingMessage("FileSize " + fileItem.FileSize, "End Sprite");
                                        IpfsDaemon.Queue(fileItem);
                                    }
                                    else
                                    {
                                        TempFileManager.SafeDeleteTempFile(outputPath);
                                    }

                                    break;
                                }

                            case TypeFile.EncodedVideo:
                                IpfsDaemon.Queue(fileItem);
                                break;
                                
                            default:
                                throw new InvalidOperationException("type non prévu");
                        }
                    }
                    catch(Exception ex)
                    {
                        LogManager.AddEncodingMessage(ex.ToString(), "Exception non gérée");
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