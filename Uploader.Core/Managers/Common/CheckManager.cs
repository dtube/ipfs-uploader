namespace Uploader.Core.Managers.Common
{
    public static class CheckManager
    {
        public static bool CheckAndLaunchIpfsDaemon()
        {
            //IPFS_PATH=~/monDossierIPFS/ ipfs init
            //todo comment changer sous windows le chemin des fichiers ipfs

            var process0 = new ProcessManager("ipfs", "version");
            bool success0 = process0.Launch(2);
            if(!success0)
            {
                return false;
            }
            if(!process0.DataOutput.ToString().Contains("ipfs version 0.4."))
            {
                return false;
            }

            var process1 = new ProcessManager("ipfs", "stats bw");
            bool success1 = process1.Launch(5);
            if(success1)
                return true; //le process ipfs est déjà lancé
            
            bool mustStart = false;
            if(process1.ErrorOutput.ToString().Contains("please run: 'ipfs init'"))
            {
                var process2 = new ProcessManager("ipfs", "init");
                bool success2 = process2.Launch(10);
                if(!success2)
                {
                    return false; // echec ipfs init
                }
                if(!process2.DataOutput.ToString().Contains("generating 2048-bit RSA keypair...done"))
                {
                    return false;  // echec ipfs init
                }
                mustStart = true;            
            }
            if(mustStart || process1.ErrorOutput.ToString().Contains("Error: This command must be run in online mode. Try"))
            {
                var process3 = new ProcessManager("ipfs", "daemon");
                return process3.LaunchWithoutTracking();  // echec ipfs daemon
            }

            return false;
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