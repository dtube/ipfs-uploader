namespace Uploader.Core.Managers.Common
{
    public static class CheckManager
    {
        public static bool CheckIpfs()
        {
            var process = new ProcessManager("ipfs", "daemon");
            bool success = process.Launch(1);
            if(!success)
                return false;

            if(process.DataOutput.ToString().Contains("please run: 'ipfs init'"))
            {
                //todo run ipfs init

                    //si process ipfs init en erreur, return false
            }

            return true;
        }

        public static bool CheckFfmpeg()
        {
            var process = new ProcessManager("ffmpeg", "-version");
            bool success = process.Launch(1);
            if(!success)
                return false;

            if(process.DataOutput.ToString().StartsWith("ffmpeg version 3."))
            {
                return true;
            }

            return false;
        }

        public static bool CheckFfprobe()
        {
            var process = new ProcessManager("ffprobe", "-version");
            bool success = process.Launch(1);
            if(!success)
                return false;

            if(process.DataOutput.ToString().StartsWith("ffprobe version 3."))
            {
                return true;
            }

            return false;
        }

        public static bool CheckImageMagickComposite()
        {
            var process = new ProcessManager("C:\\ImageMagick\\composite", "-version");
            bool success = process.Launch(1);
            if(!success)
                return false;

            if(process.DataOutput.ToString().StartsWith("Version: ImageMagick 7."))
            {
                return true;
            }

            return false;
        }

        public static bool CheckImageMagickConvert()
        {
            var process = new ProcessManager("C:\\ImageMagick\\convert", "-version");
            bool success = process.Launch(1);
            if(!success)
                return false;

            if(process.DataOutput.ToString().StartsWith("Version: ImageMagick 7."))
            {
                return true;
            }

            return false;
        }
    }
}