using System;
using System.Linq;
using Uploader.Daemons;
using Uploader.Models;

namespace Uploader.Managers
{
    public static class VideoManager
    {
        public static Guid ComputeVideo(string sourceFilePath, string videoEncodingFormats, bool? sprite)
        {
            VideoSize[] formats = new VideoSize[0];
            
            if(!string.IsNullOrWhiteSpace(videoEncodingFormats))
            {
                formats = videoEncodingFormats
                            .Split(',')
                            .Select(v => 
                            {
                                switch(v)
                                {
                                    case "720p": return VideoSize.F720p;
                                    case "480p": return VideoSize.F480p;
                                    default: return VideoSize.F720p;
                                }
                            })
                            .ToArray();
            }

            FileContainer fileContainer = FileContainer.NewVideoContainer(sourceFilePath, formats);

            IpfsDaemon.Queue(fileContainer.SourceFileItem);

            // si encoding est demandé
            foreach (FileItem file in fileContainer.EncodedFileItems)
            {   
                EncodeDaemon.Queue(file);
            }

            // si sprite demandé
            if(sprite??false)
            {
                fileContainer.SetSpriteVideo();
                // get images from video
                EncodeDaemon.Queue(fileContainer.SpriteVideoFileItem);
            }

            return fileContainer.ProgressToken;
        }
    }
}