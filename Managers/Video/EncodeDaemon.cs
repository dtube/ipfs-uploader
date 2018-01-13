using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                        // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
                        if ((DateTime.UtcNow - fileItem.FileContainer.LastTimeProgressRequested).TotalSeconds > FrontSettings.MaxGetProgressCanceled)
                        {
                            fileItem.EncodeErrorMessage = "Canceled";
                            fileItem.EncodeProgress = null;

                            fileItem.IpfsErrorMessage = "Canceled";
                            fileItem.IpfsProgress = null;

                            if(fileItem.TypeFile == TypeFile.SpriteVideo)                            
                                LogManager.AddSpriteMessage("SourceFileName " + Path.GetFileName(fileItem.FileContainer.SourceFileItem.FilePath) + " car dernier getProgress a dépassé 20s", "Annulation");
                            else
                                LogManager.AddEncodingMessage("SourceFileName " + Path.GetFileName(fileItem.FileContainer.SourceFileItem.FilePath) + " car dernier getProgress a dépassé 20s", "Annulation");

                            fileItem.CleanFiles();
                        }
                        else
                        {
                            // encode video
                            if (EncodeManager.Encode(fileItem))
                                IpfsDaemon.Queue(fileItem);
                        }
                    }
                    catch(Exception ex)
                    {
                        if(fileItem.TypeFile == TypeFile.SpriteVideo)
                            LogManager.AddSpriteMessage(ex.ToString(), "Exception non gérée");
                        else
                            LogManager.AddEncodingMessage(ex.ToString(), "Exception non gérée");
                        
                        fileItem.EncodeErrorMessage = "Exception non gérée";
                        fileItem.CleanFiles();
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