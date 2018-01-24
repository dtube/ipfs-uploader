using System;
using System.Linq;

using Uploader.Managers.Ipfs;
using Uploader.Managers.Video;
using Uploader.Models;

namespace Uploader.Managers.Front
{
    public static class VideoManager
    {
        public static Guid ComputeVideo(string sourceFilePath, string videoEncodingFormats, bool? sprite)
        {
            VideoSize[] formats = new VideoSize[0];

            if (!string.IsNullOrWhiteSpace(videoEncodingFormats))
            {
                formats = videoEncodingFormats
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

            FileContainer fileContainer = FileContainer.NewVideoContainer(sourceFilePath, formats);

            IpfsDaemon.Queue(fileContainer.SourceFileItem);

            // si sprite demandé
            if (sprite??false)
            {
                fileContainer.AddSpriteVideo();
                // get images from video
                SpriteDaemon.Queue(fileContainer.SpriteVideoFileItem, "Waiting sprite creation...");
            }

            // si encoding est demandé
            foreach (FileItem file in fileContainer.EncodedFileItems)
            {
                EncodeDaemon.Queue(file, "Waiting encode...");
            }

            return fileContainer.ProgressToken;
        }
    }
}