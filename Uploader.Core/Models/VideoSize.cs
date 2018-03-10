using System;
using System.Collections.Generic;
using System.Linq;

namespace Uploader.Core.Models
{
    internal class VideoSize
    {
        public string UrlTag { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MinSourceHeightForEncoding { get; set; }
        public string MaxRate { get; set; }
        public int QualityOrder { get; set; }
    }

    internal static class VideoSizeFactory
    {
        private static Dictionary<string, VideoSize> _dico = new Dictionary<string, VideoSize>();

        public static void Init(Dictionary<string, VideoSize> dico)
        {
            _dico = dico;
        }

        public static VideoSize GetSize(string urlTag)
        {
            if(!_dico.ContainsKey(urlTag))
                throw new InvalidOperationException("Format non reconnu.");
            
            return _dico[urlTag];
        }
    }
}