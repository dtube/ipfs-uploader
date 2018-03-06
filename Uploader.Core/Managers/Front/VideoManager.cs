using System;
using System.Collections.Generic;
using System.Linq;

using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Video;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Front
{
    public static class VideoManager
    {
        public static Guid ComputeVideo(string originFilePath, string videoEncodingFormats, bool? sprite)
        {
            FileContainer fileContainer = FileContainer.NewVideoContainer(originFilePath);
            FileItem sourceFile = fileContainer.SourceFileItem;

            // Récupérer la durée totale de la vidéo et sa résolution, autorisation encoding
            bool successGetSourceInfo = VideoSourceManager.SuccessAnalyseSource(sourceFile, sourceFile.InfoSourceProcess);

            if(successGetSourceInfo && !sourceFile.HasReachMaxVideoDurationForEncoding())
            {
                VideoSize[] requestFormats = GetVideoSizes(videoEncodingFormats);
                VideoSize[] authorizedFormats = GetVideoSizes(VideoSettings.Instance.AuthorizedQuality);
                IList<VideoSize> formats = requestFormats
                    .Intersect(authorizedFormats)
                    .OrderBy(v => v.QualityOrder)
                    .ToList();

                // suppression des formats à encoder avec une qualité/bitrate/nbframe/resolution... supérieure
                foreach (VideoSize videoSize in formats.ToList())
                {
                    if(sourceFile.VideoHeight <= videoSize.Height)
                        formats.Remove(videoSize);
                }            

                if(formats.Any())
                    fileContainer.AddEncodedVideo(formats);

                // si ipfs add source demandé mais pas d'encoding à faire...
                if(IpfsSettings.Instance.AddVideoSource || !fileContainer.EncodedFileItems.Any())
                {
                    sourceFile.AddIpfsProcess(sourceFile.SourceFilePath);
                    IpfsDaemon.Instance.Queue(sourceFile);
                }

                // si sprite demandé
                if (sprite??false)
                {                
                    fileContainer.AddSprite();

                    // si pas d'encoding à faire... déclencher le sprite maintenant avec la source
                    if(!fileContainer.EncodedFileItems.Any())
                    {
                        SpriteDaemon.Instance.Queue(fileContainer.SpriteVideoFileItem, "Waiting sprite creation...");
                    }
                }
            }

            if(fileContainer.EncodedFileItems.Any())
            {
                if (VideoSettings.Instance.GpuEncodeMode)
                {
                    // encoding audio de la source puis ça sera encoding videos Gpu
                    AudioCpuEncodeDaemon.Instance.Queue(sourceFile, "waiting audio encoding...");
                }
                else
                {
                    // si encoding est demandé, et gpuMode -> encodingAudio
                    foreach (FileItem file in fileContainer.EncodedFileItems)
                    {
                        AudioVideoCpuEncodeDaemon.Instance.Queue(file, "Waiting encode...");
                    }
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
                .Select(v => VideoSizeFactory.GetSize(v))
                .ToArray();            
        }
    }
}