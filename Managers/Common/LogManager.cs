using System;
using System.IO;

namespace Uploader.Managers.Common
{
    public static class LogManager
    {
        private static void AddMessage(string fileName, string message, string typeMessage)
        {
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            try
            {
                File.AppendAllLines(Path.Combine(logDirectory, fileName), new []
                {
                    DateTime.UtcNow.ToString("o") + " [" + typeMessage + "] " + message
                });
            }
            catch{}
        }

        public static void AddEncodingMessage(string message, string typeMessage)
        {
            AddMessage("ffmpeg.log", message, typeMessage);
        }

        public static void AddIpfsMessage(string message, string typeMessage)
        {
            AddMessage("ipfs.log", message, typeMessage);
        }

        public static void AddSpriteMessage(string message, string typeMessage)
        {
            AddMessage("sprite.log", message, typeMessage);
        }

        public static void AddOverlayMessage(string message, string typeMessage)
        {
            AddMessage("overlay.log", message, typeMessage);
        }
    }
}