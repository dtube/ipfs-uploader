namespace Uploader.Core.Managers.Front
{
    internal class GeneralSettings
    {
        private GeneralSettings(){}

        static GeneralSettings()
        {
            Instance = new GeneralSettings();
        }

        public static GeneralSettings Instance { get; private set; }

        /// <summary>
        /// seconds
        /// </summary>
        public int MaxGetProgressCanceled { get; set; }


        public string ImageMagickPath { get; set; }

        public string TempFilePath { get; set; }

        public string FinalFilePath { get; set; }

        public string ErrorFilePath { get; set; }

        public string Version { get; set; }
    }
}
