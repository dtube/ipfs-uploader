using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Uploader.Managers.Ipfs;
using Uploader.Managers.Video;
using Uploader.Models;

namespace Uploader.Managers.Front
{
    public static class ProgressManager
    {
        private static ConcurrentDictionary<Guid, FileContainer> progresses = new ConcurrentDictionary<Guid, FileContainer>();

        public static void RegisterProgress(FileContainer fileContainer)
        {
            progresses.TryAdd(fileContainer.ProgressToken, fileContainer);

            // Supprimer le suivi progress après 1j
            Task taskClean = Task.Run(() =>
            {
                Thread.Sleep(24 * 60 * 60 * 1000); // 1j
                FileContainer thisFileContainer;
                progresses.TryRemove(fileContainer.ProgressToken, out thisFileContainer);
            });
        }

        public static dynamic GetStats()
        {
            try
            {
                var list = progresses.Values.ToList().FindAll(l => !l.WorkInProgress()).ToList();

                var listVideoEncoded = new List<FileItem>();
                var listSpriteCreated = new List<FileItem>();
                var listIpfsAdded = new List<FileItem>();

                foreach (FileContainer fileContainer in list)
                {
                    if(fileContainer.SourceFileItem.IpfsProcess.CurrentStep == ProcessStep.Success)
                        listIpfsAdded.Add(fileContainer.SourceFileItem);

                    if(fileContainer.SpriteVideoFileItem != null)
                    {
                        FileItem fileItem = fileContainer.SpriteVideoFileItem;
                        if(fileItem.EncodeProcess.CurrentStep == ProcessStep.Success)
                            listSpriteCreated.Add(fileItem);

                        if(fileItem.IpfsProcess.CurrentStep == ProcessStep.Success)
                            listIpfsAdded.Add(fileItem);
                    }

                    if(fileContainer.EncodedFileItems != null)
                    {
                        foreach (FileItem fileItem in fileContainer.EncodedFileItems)
                        {
                            if(fileItem.EncodeProcess.CurrentStep == ProcessStep.Success)
                                listVideoEncoded.Add(fileItem);

                            if(fileItem.IpfsProcess.CurrentStep == ProcessStep.Success)
                                listIpfsAdded.Add(fileItem);
                        }
                    }
                }

                return new
                {
                    currentWaitingInQueue = GetCurrentWaitingInQueue(),

                    videoEncodedLast24h = GetEncodeStats(listVideoEncoded),
                    striteCreatedLast24h = GetEncodeStats(listSpriteCreated),
                    ipfsAddedLast24h = GetIpfsStats(listIpfsAdded)
                };
            }
            catch
            {
                return new { currentWaitingInQueue = GetCurrentWaitingInQueue() };
            }
        }

        private static dynamic GetCurrentWaitingInQueue()
        {
                return new
                {
                    videoToEncodeInQueue = EncodeDaemon.TotalAddToQueue - EncodeDaemon.CurrentPositionInQueue,
                    spriteToCreateInQueue = SpriteDaemon.TotalAddToQueue - SpriteDaemon.CurrentPositionInQueue,
                    ipfsToAddInQueue = IpfsDaemon.TotalAddToQueue - IpfsDaemon.CurrentPositionInQueue,
                    version = "0.6.6",
                };
        }

        private static dynamic GetEncodeStats(List<FileItem> fileItems)
        {
            return new
                {
                    nbSuccess = fileItems.Count,
                    waitingInQueue = GetInfo(fileItems.Select(f => (long)f.EncodeProcess.OriginWaitingPositionInQueue).ToList()),
                    fileSize = GetInfo(fileItems.Select(f => f.FileSize.Value).ToList()),
                    videoDuration = GetInfo(fileItems.Select(f => (long)f.FileContainer.SourceFileItem.VideoDuration.Value).ToList()),
                    waitingTime = GetInfo(fileItems.Select(f => (long)f.EncodeProcess.WaitingTime.Value).ToList()),
                    processTime = GetInfo(fileItems.Select(f => (long)f.EncodeProcess.ProcessTime.Value).ToList())
                };
        }

        private static dynamic GetIpfsStats(List<FileItem> fileItems)
        {
            return new
                {
                    nbSuccess = fileItems.Count,
                    waitingInQueue = GetInfo(fileItems.Select(f => (long)f.IpfsProcess.OriginWaitingPositionInQueue).ToList()),
                    fileSize = GetInfo(fileItems.Select(f => f.FileSize.Value).ToList()),
                    waitingTime = GetInfo(fileItems.Select(f => (long)f.IpfsProcess.WaitingTime.Value).ToList()),
                    processTime = GetInfo(fileItems.Select(f => (long)f.IpfsProcess.ProcessTime.Value).ToList())
                };
        }        

        private static dynamic GetInfo(List<long> infos)
        {
            if(!infos.Any())
                return null;
            return new { min = infos.Min(), average = infos.Average().ToString("0.00"), max = infos.Max() };
        }

        public static FileContainer GetFileContainerByToken(Guid progressToken)
        {
            FileContainer fileContainer;
            progresses.TryGetValue(progressToken, out fileContainer);

            if(fileContainer != null)
                fileContainer.LastTimeProgressRequested = DateTime.UtcNow;

            return fileContainer;
        }

        public static FileContainer GetFileContainerBySourceHash(string sourceHash)
        {
            FileContainer fileContainer = progresses.Values
                .Where(s => s.SourceFileItem.IpfsHash == sourceHash)
                .OrderByDescending(s => s.NumInstance)
                .FirstOrDefault();

            if(fileContainer != null)
                fileContainer.LastTimeProgressRequested = DateTime.UtcNow;
            
            return fileContainer;
        }

        public static FileContainer GetFileContainerByChildHash(string hash)
        {
            FileContainer fileContainer = progresses.Values.Where(s =>
                    s?.OverlayFileItem?.IpfsHash == hash ||
                    s?.SpriteVideoFileItem?.IpfsHash == hash ||
                    (s?.EncodedFileItems.Any(v => v.IpfsHash == hash)??false)
                )
                .OrderByDescending(s => s?.NumInstance)
                .FirstOrDefault();

            if(fileContainer != null)
                fileContainer.LastTimeProgressRequested = DateTime.UtcNow;
            
            return fileContainer;
        }
    }
}