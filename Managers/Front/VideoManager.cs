using System;
using System.Linq;

using Uploader.Managers.Ipfs;
using Uploader.Managers.Video;
using Uploader.Models;

namespace Uploader.Managers.Front
{
    public static class VideoManager
    {
        public static Guid ComputeVideo(string originFilePath, string videoEncodingFormats, bool? sprite)
        {
            VideoSize[] requestFormats = GetVideoSizes(videoEncodingFormats);
            VideoSize[] authorizedFormats = GetVideoSizes(VideoSettings.AuthorizedQuality);
            VideoSize[] formats = requestFormats.Intersect(authorizedFormats).ToArray();

            FileContainer fileContainer = FileContainer.NewVideoContainer(originFilePath, sprite??false, formats);

            if(IpfsSettings.AddVideoSource)
            {
                IpfsDaemon.Instance.Queue(fileContainer.SourceFileItem);
            }

            // Récupérer la durée totale de la vidéo et sa résolution, autorisation encoding
            if(!VideoSourceManager.SuccessAnalyseSource(fileContainer.SourceFileItem, fileContainer.SourceFileItem.InfoSourceProcess))
            {
                return fileContainer.ProgressToken;
            }

            if(VideoSettings.GpuEncodeMode)
            {
                // encoding audio de la source puis ça sera encoding videos Gpu
                AudioCpuEncodeDaemon.Instance.Queue(fileContainer.SourceFileItem, "waiting audio encoding...");
            }
            else
            {
                // si encoding est demandé, et gpuMode -> encodingAudio
                foreach (FileItem file in fileContainer.EncodedFileItems)
                {
                    AudioVideoCpuEncodeDaemon.Instance.Queue(file, "Waiting encode...");
                }
            }

            return fileContainer.ProgressToken;
        }

        private static VideoSize[] GetVideoSizes(string videoEncodingFormats)
        {
            if (string.IsNullOrWhiteSpace(videoEncodingFormats))
                return new VideoSize[0];

            return videoEncodingFormats
                .Split(',')
                .Select(v =>
                {
                    switch (v)
                    {
                        case "360p":
                            return VideoSize.F360p;
                        case "480p":
                            return VideoSize.F480p;
                        case "720p":
                            return VideoSize.F720p;
                        case "1080p":
                            return VideoSize.F1080p;
                        default:
                            throw new InvalidOperationException("Format non reconnu.");
                    }
                })
                .ToArray();            
        }
    }
}