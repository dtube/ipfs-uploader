namespace Uploader.Core.Managers.Ipfs
{
    internal class IpfsSettings
    {
        private IpfsSettings(){}

        static IpfsSettings()
        {
            Instance = new IpfsSettings();
        }

        public static IpfsSettings Instance { get; private set; }

        /// <summary>
        /// seconds
        /// 30 * 60 * 60
        /// 30h max pour envoyer un document
        /// </summary>
        public int IpfsTimeout { get; set; }

        public bool VideoAndSpriteTrickleDag { get; set; }

        /// <summary>
        /// si false, la video source ne sera pas envoyé à ipfs
        /// sauf si pas d'encoding video si la vidéo source dépasse
        /// la durée max fixé dans VideoSettings.MaxVideoDurationForEncoding
        /// </summary>
        public bool AddVideoSource { get; set; }

        /// <summary>
        /// si true, on utilise l'option --only-hash de ipfs
        /// ET on ne supprime pas le fichier normal du disque
        /// </summary>
        public bool OnlyHash {get; set;}
    }
}