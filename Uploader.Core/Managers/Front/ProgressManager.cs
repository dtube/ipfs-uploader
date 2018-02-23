using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                listIpfsAdded.AddRange(list.Select(l => l.SourceFileItem));

                var listSpriteFiles = list.Where(l => l.SpriteVideoFileItem != null).Select(l => l.SpriteVideoFileItem).ToList();
                listSpriteCreated.AddRange(listSpriteFiles);
                listIpfsAdded.AddRange(listSpriteFiles);

                var listEncodeFiles = list.Where(l => l.EncodedFileItems != null).SelectMany(l => l.EncodedFileItems).ToList();
                listVideoEncoded.AddRange(listEncodeFiles);
                listIpfsAdded.AddRange(listEncodeFiles);

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
            return new
            {
                audioCpuEncodeLast24h = GetAudioCpuEncodeStats(listVideoEncoded.Where(f => f.AudioCpuEncodeProcess != null && f.AudioCpuEncodeProcess.CurrentStep == step).ToList()),
                videoGpuEncodeLast24h = GetVideoGpuEncodeStats(listVideoEncoded.Where(f => f.VideoGpuEncodeProcess != null && f.VideoGpuEncodeProcess.CurrentStep == step).ToList()),
                audioVideoCpuEncodeLast24h = GetAudioVideoCpuEncodeStats(listVideoEncoded.Where(f => f.AudioVideoCpuEncodeProcess != null && f.AudioVideoCpuEncodeProcess.CurrentStep == step).ToList()),
                spriteCreationLast24h = GetSpriteEncodeStats(listSpriteCreated.Where(f => f.SpriteEncodeProcess.CurrentStep == step).ToList()),
                ipfsAddLast24h = GetIpfsStats(listIpfsAdded.Where(f => f.IpfsProcess.CurrentStep == step).ToList())
            };
        }

        private static dynamic GetAudioCpuEncodeStats(List<FileItem> fileItems)
        {
            return new
                {
                    nb = fileItems.Count,
                    waitingInQueue = GetInfo(fileItems
                        .Where(f => f.AudioCpuEncodeProcess.OriginWaitingPositionInQueue > 0)
                        .Select(f => (long)f.AudioCpuEncodeProcess.OriginWaitingPositionInQueue)
                        .ToList()),
                    sourceDuration = GetTimeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.VideoDuration.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.VideoDuration.Value)
                        .ToList()),
                    sourceFileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.FileSize.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.FileSize.Value)
                        .ToList()),                        
                    fileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileSize.HasValue)
                        .Select(f => f.FileSize.Value)
                        .ToList()),
                    waitingTime = GetTimeInfo(fileItems
                        .Where(f => f.AudioCpuEncodeProcess.WaitingTime.HasValue)
                        .Select(f => f.AudioCpuEncodeProcess.WaitingTime.Value)
                        .ToList()),
                    processTime = GetTimeInfo(fileItems
                        .Where(f => f.AudioCpuEncodeProcess.ProcessTime.HasValue)
                        .Select(f => f.AudioCpuEncodeProcess.ProcessTime.Value)
                        .ToList())
                };
        }

        private static dynamic GetVideoGpuEncodeStats(List<FileItem> fileItems)
        {
            return new
                {
                    nb = fileItems.Count,
                    waitingInQueue = GetInfo(fileItems
                        .Where(f => f.VideoGpuEncodeProcess.OriginWaitingPositionInQueue > 0)
                        .Select(f => (long)f.VideoGpuEncodeProcess.OriginWaitingPositionInQueue)
                        .ToList()),
                    sourceDuration = GetTimeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.VideoDuration.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.VideoDuration.Value)
                        .ToList()),
                    sourceFileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.FileSize.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.FileSize.Value)
                        .ToList()),                        
                    fileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileSize.HasValue)
                        .Select(f => f.FileSize.Value)
                        .ToList()),
                    waitingTime = GetTimeInfo(fileItems
                        .Where(f => f.VideoGpuEncodeProcess.WaitingTime.HasValue)
                        .Select(f => f.VideoGpuEncodeProcess.WaitingTime.Value)
                        .ToList()),
                    processTime = GetTimeInfo(fileItems
                        .Where(f => f.VideoGpuEncodeProcess.ProcessTime.HasValue)
                        .Select(f => f.VideoGpuEncodeProcess.ProcessTime.Value)
                        .ToList())
                };
        }

        private static dynamic GetAudioVideoCpuEncodeStats(List<FileItem> fileItems)
        {
            return new
                {
                    nb = fileItems.Count,
                    waitingInQueue = GetInfo(fileItems
                        .Where(f => f.AudioVideoCpuEncodeProcess.OriginWaitingPositionInQueue > 0)
                        .Select(f => (long)f.AudioVideoCpuEncodeProcess.OriginWaitingPositionInQueue)
                        .ToList()),
                    sourceDuration = GetTimeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.VideoDuration.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.VideoDuration.Value)
                        .ToList()),
                    sourceFileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.FileSize.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.FileSize.Value)
                        .ToList()),                        
                    fileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileSize.HasValue)
                        .Select(f => f.FileSize.Value)
                        .ToList()),
                    waitingTime = GetTimeInfo(fileItems
                        .Where(f => f.AudioVideoCpuEncodeProcess.WaitingTime.HasValue)
                        .Select(f => f.AudioVideoCpuEncodeProcess.WaitingTime.Value)
                        .ToList()),
                    processTime = GetTimeInfo(fileItems
                        .Where(f => f.AudioVideoCpuEncodeProcess.ProcessTime.HasValue)
                        .Select(f => f.AudioVideoCpuEncodeProcess.ProcessTime.Value)
                        .ToList())
                };
        }

        private static dynamic GetSpriteEncodeStats(List<FileItem> fileItems)
        {
            return new
                {
                    nb = fileItems.Count,
                    waitingInQueue = GetInfo(fileItems
                        .Where(f => f.SpriteEncodeProcess.OriginWaitingPositionInQueue > 0)
                        .Select(f => (long)f.SpriteEncodeProcess.OriginWaitingPositionInQueue)
                        .ToList()),
                    sourceDuration = GetTimeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.VideoDuration.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.VideoDuration.Value)
                        .ToList()),
                    sourceFileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileContainer.SourceFileItem.FileSize.HasValue)
                        .Select(f => (long)f.FileContainer.SourceFileItem.FileSize.Value)
                        .ToList()),                        
                    fileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileSize.HasValue)
                        .Select(f => f.FileSize.Value)
                        .ToList()),
                    waitingTime = GetTimeInfo(fileItems
                        .Where(f => f.SpriteEncodeProcess.WaitingTime.HasValue)
                        .Select(f => f.SpriteEncodeProcess.WaitingTime.Value)
                        .ToList()),
                    processTime = GetTimeInfo(fileItems
                        .Where(f => f.SpriteEncodeProcess.ProcessTime.HasValue)
                        .Select(f => f.SpriteEncodeProcess.ProcessTime.Value)
                        .ToList())
                };
        }

        private static dynamic GetIpfsStats(List<FileItem> fileItems)
        {
            return new
                {
                    nb = fileItems.Count,
                    waitingInQueue = GetInfo(fileItems
                        .Where(f => f.IpfsProcess.OriginWaitingPositionInQueue > 0)
                        .Select(f => (long)f.IpfsProcess.OriginWaitingPositionInQueue)
                        .ToList()),
                    fileSize = GetFileSizeInfo(fileItems
                        .Where(f => f.FileSize.HasValue)
                        .Select(f => f.FileSize.Value)
                        .ToList()),
                    waitingTime = GetTimeInfo(fileItems
                        .Where(f => f.IpfsProcess.WaitingTime.HasValue)
                        .Select(f => f.IpfsProcess.WaitingTime.Value)
                        .ToList()),
                    processTime = GetTimeInfo(fileItems
                        .Where(f => f.IpfsProcess.ProcessTime.HasValue)
                        .Select(f => f.IpfsProcess.ProcessTime.Value)
                        .ToList())
                };
        }        

        private static dynamic GetInfo(List<long> infos)
        {
            if(!infos.Any())
                return null;
            return new { nb = infos.Count, min = infos.Min(), average = Math.Round(infos.Average(), 2), max = infos.Max() };
        }

        private static dynamic GetTimeInfo(List<long> infos)
        {
            if(!infos.Any())
                return null;
            return new { nb = infos.Count, min = Time(infos.Min()), average = Time((long)infos.Average()), max = Time(infos.Max()) };
        }

        private static dynamic GetFileSizeInfo(List<long> infos)
        {
            if(!infos.Any())
                return null;
            return new { nb = infos.Count, min = Filesize(infos.Min()), average = Filesize((long)infos.Average()), max = Filesize(infos.Max()) };
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

        private static dynamic GetResult(FileContainer fileContainer)
        {
            switch (fileContainer.TypeContainer)
            {
                case TypeContainer.Video:
                    return new
                    {
                        finished = fileContainer.Finished(),
                        sourceAudioCpuEncoding = AudioCpuEncodeResultJson(fileContainer.SourceFileItem),
                        sourceVideoGpuEncoding = VideoGpuEncodeResultJson(fileContainer.SourceFileItem),
                        ipfsAddSourceVideo = IpfsResultJson(fileContainer.SourceFileItem),
                        sprite = fileContainer.SpriteVideoFileItem == null ? null : (new
                        {
                            spriteCreation = SpriteResultJson(fileContainer.SpriteVideoFileItem),
                            ipfsAddSprite = IpfsResultJson(fileContainer.SpriteVideoFileItem)
                        }),
                        encodedVideos = fileContainer.EncodedFileItems
                            .Select(e =>
                                new
                                {
                                    encode = AudioVideoCpuEncodeResultJson(e),
                                    ipfsAddEncodeVideo = IpfsResultJson(e)
                                })
                            .ToArray()
                    };

                case TypeContainer.Overlay:
                    return new
                    {
                        ipfsAddSource = IpfsResultJson(fileContainer.SourceFileItem),
                        ipfsAddOverlay = IpfsResultJson(fileContainer.OverlayFileItem)
                    };

                case TypeContainer.Subtitle:
                    return new
                    {
                        ipfsAddSource = IpfsResultJson(fileContainer.SubtitleFileItem)
                    };
            }

            LogManager.AddGeneralMessage(LogLevel.Critical, "Type container non géré " + fileContainer.TypeContainer, "Exception");
            throw new InvalidOperationException("type container non géré");
        }

        private static dynamic IpfsResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.IpfsProcess == null)
                return null;

            return new
            {
                progress = fileItem.IpfsProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                hash = fileItem.IpfsHash,
                lastTimeProgress = fileItem.IpfsProcess.LastTimeProgressChanged,
                errorMessage = fileItem.IpfsProcess.ErrorMessage,
                step = fileItem.IpfsProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.IpfsProcess, IpfsDaemon.Instance.CurrentPositionInQueue),
                fileSize = fileItem.FileSize
            };
        }

        private static dynamic SpriteResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.SpriteEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.SpriteEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.SpriteEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.SpriteEncodeProcess.ErrorMessage,
                step = fileItem.SpriteEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.SpriteEncodeProcess, SpriteDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private static dynamic AudioCpuEncodeResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.AudioCpuEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.AudioCpuEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.AudioCpuEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.AudioCpuEncodeProcess.ErrorMessage,
                step = fileItem.AudioCpuEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.AudioCpuEncodeProcess, AudioCpuEncodeDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private static dynamic AudioVideoCpuEncodeResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.AudioVideoCpuEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.AudioVideoCpuEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.AudioVideoCpuEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.AudioVideoCpuEncodeProcess.ErrorMessage,
                step = fileItem.AudioVideoCpuEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.AudioVideoCpuEncodeProcess, AudioVideoCpuEncodeDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private static dynamic VideoGpuEncodeResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.VideoGpuEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.VideoGpuEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.VideoGpuEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.VideoGpuEncodeProcess.ErrorMessage,
                step = fileItem.VideoGpuEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.VideoGpuEncodeProcess, VideoGpuEncodeDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private static int? Position(ProcessItem processItem, int daemonCurrentPositionInQueue)
        {
            if (processItem.CurrentStep != ProcessStep.Waiting)
                return null;

            return processItem.PositionInQueue - daemonCurrentPositionInQueue;
        }
    }
}