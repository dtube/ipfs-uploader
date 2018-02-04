namespace Uploader.Managers.Ipfs
{
    public static class IpfsSettings
    {
        /// <summary>
        /// milliseconds
        /// </summary>
        public static int IpfsTimeout => 30 * 60 * 60 * 1000; // 30h max pour envoyer un document

        public static bool VideoAndSpriteTrickleDag => true;

        /// <summary>
        /// si false, la video source ne sera pas envoyé à ipfs
        /// sauf si pas d'encoding video si la vidéo source dépasse
        /// la durée max fixé dans VideoSettings.MaxVideoDurationForEncoding
        /// </summary>
        public static bool AddVideoSource => false;
    }
}