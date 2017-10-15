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

                    // si tout terminer, supprimer fichier source
                    if(!fileItem.VideoFile.WorkInProgress())
                    {
                        TempFileManager.SafeDeleteTempFile(fileItem.VideoFile.SourceFileItem.FilePath);
                    }

                    if(fileItem.VideoSize == VideoSize.Source)
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
                        // Supprimer video encodé
                        TempFileManager.SafeDeleteTempFile(fileItem.FilePath);
                    }                                 
                }
            });
        }

        /// <summary>
        /// Nouvelle video source à ajouter
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public static FileItem QueueSourceFile(string sourceFilePath, params VideoSize[] videoSizes)
        {
            var videoFile = new VideoFile(sourceFilePath, videoSizes);

            sourceProgresses.TryAdd(videoFile.SourceFileItem.IpfsProgressToken, videoFile.SourceFileItem);
            Queue(videoFile.SourceFileItem);

            return videoFile.SourceFileItem;
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
            TotalAddToQueue++;
            fileItem.IpfsPositionInQueue = TotalAddToQueue;
        }

        public static VideoFile GetVideoFile(Guid sourceToken)
        {
            FileItem fileItem;
            if(!sourceProgresses.TryGetValue(sourceToken, out fileItem))
            {
                return null;
            }

            return fileItem.VideoFile;
        }

        public static VideoFile GetVideoFile(string sourceHash)
        {
            FileItem fileItem =  sourceProgresses.Values
                .OrderByDescending(s => s.IpfsLastTimeProgressChanged)
                .FirstOrDefault(s => s.IpfsHash == sourceHash);
            
            return fileItem != null ? fileItem.VideoFile : null;
        }
    }
}