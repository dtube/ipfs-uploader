namespace Uploader.Managers.Video
{
    public static class VideoSettings
    {
        /// <summary>
        /// milliseconds
        /// </summary>
        public static int EncodeGetOneImageTimeout => 10 * 1000; // 10s max pour extraire une image de la vidéo

        /// <summary>
        /// milliseconds
        /// </summary>
        public static int EncodeGetImagesTimeout => 10 * 60 * 1000; // 10min max pour extraire les images du sprite de la vidéo

        /// <summary>
        /// milliseconds
        /// </summary>
        public static int EncodeTimeout => 30 * 60 * 60 * 1000; // 30h max pour encoder la vidéo

        /// <summary>
        /// minutes
        /// </summary>
        public static int MaxVideoDurationForEncoding => 20 * 60; // 20 minutes max pour encoder une vidéo

        /// <summary>
        /// 
        /// </summary>
        public static int NbSpriteImages => 100;

        /// <summary>
        /// pixels
        /// </summary>
        public static int HeightSpriteImages => 118;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool GpuEncodeMode => false;

        /// <summary>
        /// 
        /// </summary>
        public static int NbSpriteDaemon => 1;

        /// <summary>
        /// 
        /// </summary>
        public static int NbEncodeDaemon => 1;
    }
}