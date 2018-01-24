using System;
using System.Diagnostics;
using System.IO;

namespace Uploader.Managers.Common
{
    public static class LogManager
    {
        private static object _lockEncodeFile = new object();
        private static object _lockIpfsFile = new object();
        private static object _lockSpriteFile = new object();
        private static object _lockOverlayFile = new object();

        private static void AddMessage(string fileName, string message)
        {
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            try
            {
                File.AppendAllLines(Path.Combine(logDirectory, fileName), new [] { message });
            }
            catch{}
        }

        public static void AddEncodingMessage(string message, string typeMessage)
        {
            string dateTime = DateTime.UtcNow.ToString("o");
            Debug.WriteLine($"#ffmpeg.log {dateTime} [{typeMessage}] {message}");
            lock(_lockEncodeFile)
                AddMessage("ffmpeg.log", $"{dateTime} [{typeMessage}] {message}");
        }

        public static void AddIpfsMessage(string message, string typeMessage)
        {
            string dateTime = DateTime.UtcNow.ToString("o");
            Debug.WriteLine($"#ipfs.log [{typeMessage}] {message}");
            lock(_lockIpfsFile)
                AddMessage("ipfs.log", $"{dateTime} [{typeMessage}] {message}");
        }

        public static void AddSpriteMessage(string message, string typeMessage)
        {
            string dateTime = DateTime.UtcNow.ToString("o");
            Debug.WriteLine($"#sprite.log [{typeMessage}] {message}");
            lock(_lockSpriteFile)
                AddMessage("sprite.log", $"{dateTime} [{typeMessage}] {message}");
        }

        public static void AddOverlayMessage(string message, string typeMessage)
        {
            string dateTime = DateTime.UtcNow.ToString("o");
            Debug.WriteLine($"#overlay.log [{typeMessage}] {message}");
            lock(_lockOverlayFile)
                AddMessage("overlay.log", $"{dateTime} [{typeMessage}] {message}");
        }
    }
}