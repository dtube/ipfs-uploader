using System;
using System.Collections.Generic;
using System.Linq;

namespace Uploader.Core.Models
{
    internal abstract class VideoSize
    {
        public abstract int Width { get; }
        public abstract int Height { get; }

        public abstract int MinSourceHeightForEncoding { get; }
        public abstract string MaxRate { get; }
        public abstract int QualityOrder { get; }
    }

    internal class VideoSize240 : VideoSize
    {
        public override int Width => 426;
        public override int Height => 240;
        public override int MinSourceHeightForEncoding => 120;
        public override string MaxRate => "100k";
        public override int QualityOrder => 1;
    }

    internal class VideoSize360 : VideoSize
    {
        public override int Width => 640;
        public override int Height => 360;
        public override int MinSourceHeightForEncoding => 300;
        public override string MaxRate => "200k";
        public override int QualityOrder => 2;
    }

    internal class VideoSize480 : VideoSize
    {
        public override int Width => 854;
        public override int Height => 480;
        public override int MinSourceHeightForEncoding => 360;
        public override string MaxRate => "500k";
        public override int QualityOrder => 3;
    }

    internal class VideoSize720 : VideoSize
    {
        public override int Width => 1280;
        public override int Height => 720;
        public override int MinSourceHeightForEncoding => 600;
        public override string MaxRate => "1000k";
        public override int QualityOrder => 4;
    }

    internal class VideoSize1080 : VideoSize
    {
        public override int Width => 1920;
        public override int Height => 1080;
        public override int MinSourceHeightForEncoding => 900;
        public override string MaxRate => "1600k";
        public override int QualityOrder => 5;
    }

    internal static class VideoSizeFactory
    {
        private static Dictionary<string, VideoSize> _dico = new Dictionary<string, VideoSize>();

        static VideoSizeFactory()
        {
            _dico.Add("240p", new VideoSize240());
            //_dico.Add("360p", new VideoSize360());
            _dico.Add("480p", new VideoSize480());
            _dico.Add("720p", new VideoSize720());
            //_dico.Add("1080p", new VideoSize1080());
        }

        public static VideoSize GetSize(string urlTag)
        {
            if(!_dico.ContainsKey(urlTag))
                throw new InvalidOperationException("Format non reconnu.");
            
            return _dico[urlTag];
        }
    }
}