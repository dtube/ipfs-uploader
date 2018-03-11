using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Video;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Front
{
    public static class ProgressManager
    {
        public static string Version => GeneralSettings.Instance.Version;

        private static ConcurrentDictionary<Guid, FileContainer> progresses = new ConcurrentDictionary<Guid, FileContainer>();

        internal static void RegisterProgress(FileContainer fileContainer)
        {
            progresses.TryAdd(fileContainer.ProgressToken, fileContainer);

            // Supprimer le suivi progress après 1j
            var now = DateTime.UtcNow;
            var purgeList = progresses.Values.Where(f => (now - f.LastActivityDateTime).TotalHours >= 24).ToList();
            foreach (FileContainer toDelete in purgeList)
            {
                FileContainer thisFileContainer;
                progresses.TryRemove(toDelete.ProgressToken, out thisFileContainer);
            }
        }

        public static dynamic GetErrors()
        {
            return new
            {
                list = progresses.Values
                    .Where(c => c.Error())
                    .Select(c => GetResult(c, true))
                    .ToList()
            };
        }

        public static dynamic GetStats(bool details)
        {
            return details ? GetDetailStats() : GetLightStats();
        }

        public static dynamic GetLightStats()
        {
            return new
                {
                    version = Version,
                    currentWaitingInQueue = GetCurrentWaitingInQueue()
                };
        }

        public static dynamic GetDetailStats()
        {
            try
            {
                var list = progresses.Values.ToList();

                var listVideoEncoded = new List<FileItem>();
                var listSpriteCreated = new List<FileItem>();
                var listIpfsAdded = new List<FileItem>();

                listVideoEncoded.AddRange(list.Select(l => l.SourceFileItem));
                listIpfsAdded.AddRange(list.Select(l => l.SourceFileItem).Where(f => f.IpfsProcess != null));

                var listSpriteFiles = list.Where(l => l.SpriteVideoFileItem != null).Select(l => l.SpriteVideoFileItem).ToList();
                listSpriteCreated.AddRange(listSpriteFiles);
                listIpfsAdded.AddRange(listSpriteFiles);

                var listEncodeFiles = list.Where(l => l.EncodedFileItems != null).SelectMany(l => l.EncodedFileItems).ToList();
                listVideoEncoded.AddRange(listEncodeFiles);
                listIpfsAdded.AddRange(listEncodeFiles.Where(f => f.IpfsProcess != null));

                return new
                {
                    version = Version,
                    currentWaitingInQueue = GetCurrentWaitingInQueue(),

                    Init = GetStatByStep(ProcessStep.Init, listVideoEncoded, listSpriteCreated, listIpfsAdded),
                    Waiting = GetStatByStep(ProcessStep.Waiting, listVideoEncoded, listSpriteCreated, listIpfsAdded),
                    Canceled = GetStatByStep(ProcessStep.Canceled, listVideoEncoded, listSpriteCreated, listIpfsAdded),
                    Started = GetStatByStep(ProcessStep.Started, listVideoEncoded, listSpriteCreated, listIpfsAdded),
                    Error = GetStatByStep(ProcessStep.Error, listVideoEncoded, listSpriteCreated, listIpfsAdded),
                    Success = GetStatByStep(ProcessStep.Success, listVideoEncoded, listSpriteCreated, listIpfsAdded)
                };
            }
            catch(Exception ex)
            {
                return new 
                {
                    version = Version,
                    currentWaitingInQueue = GetCurrentWaitingInQueue(),
                    exception = ex.ToString()
                };
            }
        }

        private static dynamic GetCurrentWaitingInQueue()
        {
                return new
                {
                    audioCpuToEncode = AudioCpuEncodeDaemon.Instance.CurrentWaitingInQueue,
                    videoGpuToEncode = VideoGpuEncodeDaemon.Instance.CurrentWaitingInQueue,
                    audioVideoCpuToEncode = AudioVideoCpuEncodeDaemon.Instance.CurrentWaitingInQueue,
                    spriteToCreate = SpriteDaemon.Instance.CurrentWaitingInQueue,
                    ipfsToAdd = IpfsDaemon.Instance.CurrentWaitingInQueue
                };
        }

        private static dynamic GetStatByStep(ProcessStep step, List<FileItem> listVideoEncoded, List<FileItem> listSpriteCreated, List<FileItem> listIpfsAdded)
        {
            try
            {
                return new
                {
                    audioCpuEncodeLast24h = GetProcessStats(listVideoEncoded.FindAll(f => f.AudioCpuEncodeProcess != null && f.AudioCpuEncodeProcess.CurrentStep == step).Select(f => f.AudioCpuEncodeProcess).ToList()),
                    videoGpuEncodeLast24h = GetProcessStats(listVideoEncoded.FindAll(f => f.VideoGpuEncodeProcess != null && f.VideoGpuEncodeProcess.CurrentStep == step).Select(f => f.VideoGpuEncodeProcess).ToList()),
                    audioVideoCpuEncodeLast24h = GetProcessStats(listVideoEncoded.FindAll(f => f.AudioVideoCpuEncodeProcess != null && f.AudioVideoCpuEncodeProcess.CurrentStep == step).Select(f => f.AudioVideoCpuEncodeProcess).ToList()),
                    spriteCreationLast24h = GetProcessStats(listSpriteCreated.FindAll(f => f.SpriteEncodeProcess != null && f.SpriteEncodeProcess.CurrentStep == step).Select(f => f.SpriteEncodeProcess).ToList()),
                    ipfsAddLast24h = GetProcessStats(listIpfsAdded.FindAll(f => f.IpfsProcess != null && f.IpfsProcess.CurrentStep == step).Select(f => f.IpfsProcess).ToList())
                };
            }
            catch(Exception ex)
            {
                return new
                {
                    exception = ex.ToString()
                };
            }
        }

        private static dynamic GetProcessStats(List<ProcessItem> processItems)
        {
            if(processItems == null || !processItems.Any())
                return null;

            return new
                {
                    nb = processItems.Count,
                    waitingInQueue = GetInfo(processItems
                        .Where(p => p.OriginWaitingPositionInQueue > 0)
                        .Select(p => (long)p.OriginWaitingPositionInQueue)
                        .ToList()),
                    fileSize = GetFileSizeInfo(processItems
                        .Where(p => p.FileItem.FileSize.HasValue)
                        .Select(p => p.FileItem.FileSize.Value)
                        .ToList()),
                    waitingTime = GetTimeInfo(processItems
                        .Where(p => p.WaitingTime.HasValue)
                        .Select(p => p.WaitingTime.Value)
                        .ToList()),
                    processTime = GetTimeInfo(processItems
                        .Where(p => p.ProcessTime.HasValue)
                        .Select(p => p.ProcessTime.Value)
                        .ToList())
                };
        }

        private static dynamic GetProcessWithSourceStats(List<ProcessItem> processItems)
        {
            var stats = GetProcessStats(processItems);
            if(stats == null)
                return null;

            return new
                {
                    nb = stats.nb,
                    waitingInQueue = stats.waitingInQueue,
                    sourceDuration = GetTimeInfo(processItems
                        .Where(p => p.FileItem.FileContainer.SourceFileItem.VideoDuration.HasValue)
                        .Select(p => (long)p.FileItem.FileContainer.SourceFileItem.VideoDuration.Value)
                        .ToList()),
                    sourceFileSize = GetFileSizeInfo(processItems
                        .Where(p => p.FileItem.FileContainer.SourceFileItem.FileSize.HasValue)
                        .Select(p => (long)p.FileItem.FileContainer.SourceFileItem.FileSize.Value)
                        .ToList()),                    
                    fileSize = stats.fileSize,
                    waitingTime = stats.waitingTime,
                    processTime = stats.processTime
                };
        }

        private static dynamic GetInfo(List<long> infos)
        {
            if(!infos.Any())
                return null;
            return new { min = infos.Min(), average = Math.Round(infos.Average(), 2), max = infos.Max() };
        }

        private static dynamic GetTimeInfo(List<long> infos)
        {
            if(!infos.Any())
                return null;
            return new { min = Time(infos.Min()), average = Time((long)infos.Average()), max = Time(infos.Max()) };
        }

        private static dynamic GetFileSizeInfo(List<long> infos)
        {
            if(!infos.Any())
                return null;
            return new { min = Filesize(infos.Min()), average = Filesize((long)infos.Average()), max = Filesize(infos.Max()) };
        }

        private static string Filesize(long octet)
        {
            if(octet < 1 * 1000)
                return octet + " o";

            if(octet < 1 * 1000000)
                return Math.Round(octet/1000d, 2) + " Ko";

            if(octet < 1 * 1000000000)
                return Math.Round(octet/1000000d, 2) + " Mo";

            if(octet < 1 * 1000000000000)
                return Math.Round(octet/1000000000d, 2) + " Go";
            
            return Math.Round(octet/1000000000000d, 2) + " To";
        }

        private static string Time(long seconds)
        {
            if(seconds < 1 * 60)
                return seconds + " second(s)";

            if(seconds < 1 * 3600)
                return Math.Round(seconds/60d, 2) + " minute(s)";

            return Math.Round(seconds/3600d, 2) + " heure(s)";
        }

        public static dynamic GetFileContainerByToken(Guid progressToken)
        {
            FileContainer fileContainer;
            progresses.TryGetValue(progressToken, out fileContainer);

            if(fileContainer != null)
                fileContainer.UpdateLastTimeProgressRequest();
            else
                return null;

            return GetResult(fileContainer);
        }

        public static dynamic GetFileContainerBySourceHash(string sourceHash)
        {
            FileContainer fileContainer = progresses.Values
                .Where(s => s.SourceFileItem.IpfsHash == sourceHash)
                .OrderByDescending(s => s.NumInstance)
                .FirstOrDefault();

            if(fileContainer != null)
                fileContainer.UpdateLastTimeProgressRequest();
            else
                return null;

            return GetResult(fileContainer);
        }

        private static dynamic GetResult(FileContainer fileContainer, bool error = false)
        {
            switch (fileContainer.TypeContainer)
            {
                case TypeContainer.Video:
                    return new
                    {
                        finished = fileContainer.Finished(),
                        debugInfo = error ? DebugInfo(fileContainer) : null,
                        sourceAudioCpuEncoding = AudioCpuEncodeResultJson(fileContainer.SourceFileItem, error),
                        sourceVideoGpuEncoding = VideoGpuEncodeResultJson(fileContainer.SourceFileItem, error),
                        ipfsAddSourceVideo = IpfsResultJson(fileContainer.SourceFileItem, error),
                        sprite = fileContainer.SpriteVideoFileItem == null ? null :
                            new
                            {
                                spriteCreation = SpriteResultJson(fileContainer.SpriteVideoFileItem, error),
                                ipfsAddSprite = IpfsResultJson(fileContainer.SpriteVideoFileItem, error)
                            },
                        encodedVideos = !fileContainer.EncodedFileItems.Any() ? null :
                            fileContainer.EncodedFileItems.Select(e =>
                                    new
                                    {
                                        encode = AudioVideoCpuEncodeResultJson(e, error),
                                        ipfsAddEncodeVideo = IpfsResultJson(e, error)
                                    })
                                .ToArray()
                    };

                case TypeContainer.Image:
                    return new
                    {
                        ipfsAddSource = IpfsResultJson(fileContainer.SnapFileItem, error),
                        ipfsAddOverlay = IpfsResultJson(fileContainer.OverlayFileItem, error)
                    };

                case TypeContainer.Subtitle:
                    return new
                    {
                        ipfsAddSource = IpfsResultJson(fileContainer.SubtitleFileItem, error)
                    };
            }

            LogManager.AddGeneralMessage(LogLevel.Critical, "Type container non géré " + fileContainer.TypeContainer, "Error");
            throw new InvalidOperationException("type container non géré");
        }

        private static dynamic DebugInfo(FileContainer fileContainer)
        {
            string hash = fileContainer?.SourceFileItem.IpfsHash;
            return new
            {
                originFileName = Path.GetFileName(fileContainer.OriginFilePath),
                ipfsUrl = hash == null ? null : "https://ipfs.io/ipfs/" + hash,
                exceptionDetail = fileContainer.ExceptionDetail,
                sourceInfo = SourceInfo(fileContainer.SourceFileItem)
            };
        }

        private static dynamic SourceInfo(FileItem sourceFileItem)
        {
            if (sourceFileItem == null || sourceFileItem.InfoSourceProcess == null)
                return null;

            return new
            {
                sourceFileItem.FileSize,
                sourceFileItem.VideoCodec,
                sourceFileItem.VideoDuration,
                sourceFileItem.VideoWidth,
                sourceFileItem.VideoHeight,
                sourceFileItem.VideoPixelFormat,
                sourceFileItem.VideoFrameRate,
                sourceFileItem.VideoBitRate,
                sourceFileItem.VideoNbFrame,
                sourceFileItem.VideoRotate,
                sourceFileItem.AudioCodec,
                sourceFileItem.InfoSourceProcess.ErrorMessage
            };
        }

        private static dynamic IpfsResultJson(FileItem fileItem, bool error)
        {
            var result = ProcessResultJson(fileItem?.IpfsProcess, error, IpfsDaemon.Instance.CurrentPositionInQueue);
            if(result == null)
                return null;

            return new
            {
                progress = result.progress,
                encodeSize = result.encodeSize,
                lastTimeProgress = result.lastTimeProgress,
                errorMessage = result.errorMessage,
                step = result.step,
                positionInQueue = result.positionInQueue,

                hash = fileItem.IpfsHash,
                fileSize = fileItem.FileSize
            };
        }

        private static dynamic SpriteResultJson(FileItem fileItem, bool error)
        {
            return ProcessResultJson(fileItem?.SpriteEncodeProcess, error, SpriteDaemon.Instance.CurrentPositionInQueue);
        }

        private static dynamic AudioCpuEncodeResultJson(FileItem fileItem, bool error)
        {
            return ProcessResultJson(fileItem?.AudioCpuEncodeProcess, error, AudioCpuEncodeDaemon.Instance.CurrentPositionInQueue);
        }

        private static dynamic AudioVideoCpuEncodeResultJson(FileItem fileItem, bool error)
        {
            return ProcessResultJson(fileItem?.AudioVideoCpuEncodeProcess, error, AudioVideoCpuEncodeDaemon.Instance.CurrentPositionInQueue);
        }

        private static dynamic VideoGpuEncodeResultJson(FileItem fileItem, bool error)
        {
            return ProcessResultJson(fileItem?.VideoGpuEncodeProcess, error, VideoGpuEncodeDaemon.Instance.CurrentPositionInQueue);
        }

        private static dynamic ProcessResultJson(ProcessItem processItem, bool error, int daemonCurrentPositionInQUeue)
        {
            if (processItem == null)
                return null;
            if(error && processItem.CurrentStep != ProcessStep.Error)
                return null;

            return new
            {
                progress = processItem.Progress,
                encodeSize = processItem.FileItem.VideoSize.VideoSizeString(),
                lastTimeProgress = processItem.LastTimeProgressChanged,
                errorMessage = processItem.ErrorMessage,
                step = processItem.CurrentStep.ToString(),
                positionInQueue = Position(processItem, daemonCurrentPositionInQUeue)
            };
        }

        private static string VideoSizeString(this VideoSize videoSize)
        {
            return videoSize == null ? "source" : videoSize.Height.ToString() + "p";
        }

        private static int? Position(ProcessItem processItem, int daemonCurrentPositionInQueue)
        {
            if (processItem.CurrentStep != ProcessStep.Waiting)
                return null;

            return processItem.PositionInQueue - daemonCurrentPositionInQueue;
        }
    }
}