using System;
using System.Diagnostics;
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
            Debug.WriteLine("Ffmeg {1} : {0}", message, typeMessage);
            AddMessage("ffmpeg.log", message, typeMessage);
        }

        public static void AddIpfsMessage(string message, string typeMessage)
        {
            Debug.WriteLine("Ipfs {1} : {0}", message, typeMessage);
            AddMessage("ipfs.log", message, typeMessage);
        }

        public static void AddSpriteMessage(string message, string typeMessage)
        {
            Debug.WriteLine("Sprite {1} : {0}", message, typeMessage);
            AddMessage("sprite.log", message, typeMessage);
        }

        public static void AddOverlayMessage(string message, string typeMessage)
        {
            Debug.WriteLine("Overlay {1} : {0}", message, typeMessage);
            AddMessage("overlay.log", message, typeMessage);
        }
    }
}