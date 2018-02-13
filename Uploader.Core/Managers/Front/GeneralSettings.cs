namespace Uploader.Core.Managers.Front
{
    public class GeneralSettings
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

    }
}
