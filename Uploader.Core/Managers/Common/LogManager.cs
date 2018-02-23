using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Uploader.Core.Managers.Common
{
    public class LogManager
    {
        private static ILogger _logGeneral;
        private static ILogger _logEncoding;
        private static ILogger _logSprite;
        private static ILogger _logOverlay;
        private static ILogger _logSubtitle;
        private static ILogger _logIpfs;

        public static void Init(ILoggerFactory loggerFactory)
        {
            _logGeneral = loggerFactory.CreateLogger("general");
            _logEncoding = loggerFactory.CreateLogger("ffmpeg");
            _logSprite = loggerFactory.CreateLogger("sprite");
            _logOverlay = loggerFactory.CreateLogger("overlay");
            _logSubtitle = loggerFactory.CreateLogger("subtitle");
            _logIpfs = loggerFactory.CreateLogger("ipfs");
        }

        public static void AddGeneralMessage(LogLevel logLevel, string message, string typeMessage)
        {
            Log(_logGeneral, logLevel, message, typeMessage);
        }

        public static void AddEncodingMessage(LogLevel logLevel, string message, string typeMessage)
        {
            Log(_logEncoding, logLevel, message, typeMessage);
        }

        public static void AddIpfsMessage(LogLevel logLevel, string message, string typeMessage)
        {
            Log(_logIpfs, logLevel, message, typeMessage);
        }

        public static void AddSpriteMessage(LogLevel logLevel, string message, string typeMessage)
        {
            Log(_logSprite, logLevel, message, typeMessage);
        }

        public static void AddOverlayMessage(LogLevel logLevel, string message, string typeMessage)
        {
            Log(_logOverlay, logLevel, message, typeMessage);
        }

        public static void AddSubtitleMessage(LogLevel logLevel, string message, string typeMessage)
        {
            Log(_logSubtitle, logLevel, message, typeMessage);
        }

        private static void Log(ILogger logger, LogLevel logLevel, string message, string typeMessage)
        {
            string formatMessage = $"[{typeMessage}] {message}";
            switch (logLevel)
            {
                case LogLevel.Trace:
                    logger.LogTrace(formatMessage);
                    Trace.WriteLine(formatMessage);
                    break;
                case LogLevel.Debug:
                    logger.LogDebug(formatMessage);
                    Debug.WriteLine(formatMessage);
                    break;
                case LogLevel.Information:
                    logger.LogInformation(formatMessage);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(formatMessage);
                    break;
                case LogLevel.Error:
                    logger.LogError(formatMessage);
                    break;
                case LogLevel.Critical:
                    logger.LogCritical(formatMessage);
                    break;
            }
        }
    }
}