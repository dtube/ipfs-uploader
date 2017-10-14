using System;
using System.Collections.Concurrent;
using System.Linq;
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

        public static int NbAddSourceDone { get; set; }

        public static void Start()
        {
            daemon = Task.Run(() =>
            {
                while(true)
                {
                    System.Threading.Thread.Sleep(1000);

                    FileItem fileItem;

                    if(!queueFileItems.TryDequeue(out fileItem))
                    {
                        continue;
                    }

                    IpfsAddManager.Add(fileItem);

                    if(fileItem.VideoFormat != VideoFormat.Source)
                    {
                        TempFileManager.SafeDeleteTempFile(fileItem.FilePath);
                        continue;
                    }
                    
                    NbAddSourceDone++;

                    VideoFile videoFile = fileItem.VideoFile;

                    if(videoFile.EncodedFileItems.Any())
                    {
                        foreach (FileItem file in videoFile.EncodedFileItems)
                        {   
                            //FFmpegDaemon.Queue(file);
                        }

                        //SteemDaemon.Queue(videoFile);
                    }

                    //supprimer le suivi ipfs add progress après 1h
                    Task taskClean = Task.Run(() =>
                    {
                        Guid token = fileItem.IpfsAddProgressToken;
                        System.Threading.Thread.Sleep(60 * 60 * 1000); // 1h
                        FileItem thisFileItem;
                        sourceProgresses.TryRemove(token, out thisFileItem);
                    });                                   
                }
            });
        }

        /// <summary>
        /// Nouvelle video source à ajouter
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public static Guid QueueSourceFile(string sourceFilePath, params VideoFormat[] videoFormats)
        {
            var videoFile = new VideoFile(sourceFilePath, videoFormats);

            sourceProgresses.TryAdd(videoFile.SourceFileItem.IpfsAddProgressToken, videoFile.SourceFileItem);
            Queue(videoFile.SourceFileItem);

            return videoFile.SourceFileItem.IpfsAddProgressToken;
        }

        /// <summary>
        /// Video encodée à ajouter
        /// </summary>
        /// <param name="encodedFileItem"></param>
        /// <returns></returns>
        public static void QueueEncodedFile(FileItem encodedFileItem)
        {
            Queue(encodedFileItem);
        }

        private static void Queue(FileItem fileItem)
        {
            queueFileItems.Enqueue(fileItem);
        }

        public static FileItem GetFileItem(Guid sourceToken)
        {
            FileItem fileItem;
            if(!sourceProgresses.TryGetValue(sourceToken, out fileItem))
            {
                return null;
            }

            return fileItem;
        }
    }
}