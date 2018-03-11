namespace Uploader.Core.Managers.Video
{
    internal class VideoSettings
    {
        private VideoSettings(){}

        static VideoSettings()
        {
            Instance = new VideoSettings();
        }

        public static VideoSettings Instance { get; private set; }

        /// <summary>
        /// seconds
        /// </summary>
        public int FfProbeTimeout { get; set; } // 10s max pour extraire les informations de la vidéo

        /// <summary>
        /// seconds
        /// </summary>
        public int EncodeGetImagesTimeout { get; set; } // 10min max pour extraire les images du sprite de la vidéo

        /// <summary>
        /// seconds
        /// </summary>
        public int EncodeTimeout { get; set; } // 30h max pour encoder la vidéo

        /// <summary>
        /// seconds
        /// </summary>
        public int MaxVideoDurationForEncoding { get; set; } // 30 minutes max pour encoder une vidéo

        public int NbSpriteImages { get; set; }

        /// <summary>
        /// pixels
        /// </summary>
        public int HeightSpriteImages { get; set; }

        /// <summary>
        /// encoding audio puis encoding video 1:N formats
        /// </summary>
        public bool GpuEncodeMode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int NbSpriteDaemon { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int NbAudioCpuEncodeDaemon { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int NbVideoGpuEncodeDaemon { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int NbAudioVideoCpuEncodeDaemon { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string AuthorizedQuality { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string NVidiaCard { get; set; }
    }
}