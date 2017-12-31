namespace Uploader.Managers.Ipfs
{
    public static class IpfsSettings
    {
        /// <summary>
        /// milliseconds
        /// </summary>
        public static int IpfsTimeout => 30 * 60 * 60 * 1000; // 30h max pour envoyer un document

        public static bool VideoAndSpriteTrickleDag => true;
    }
}