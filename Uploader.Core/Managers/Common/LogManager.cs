using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Uploader.Core.Managers.Common
{
    public static class LogManager
    {
        public static void Init(ILoggerFactory loggerFactory)
        {
            GeneralLogger = loggerFactory.CreateLogger("general");
            FfmpegLogger = loggerFactory.CreateLogger("ffmpeg");
            SpriteLogger = loggerFactory.CreateLogger("sprite");
            ImageLogger = loggerFactory.CreateLogger("image");
            SubtitleLogger = loggerFactory.CreateLogger("subtitle");
            IpfsLogger = loggerFactory.CreateLogger("ipfs");
        }

        public static ILogger GeneralLogger { get; private set; }
        public static ILogger FfmpegLogger { get; private set; }
        public static ILogger SpriteLogger { get; private set; }
        public static ILogger IpfsLogger { get; private set; }
        public static ILogger ImageLogger { get; private set; }
        public static ILogger SubtitleLogger { get; private set; }

        public static void AddGeneralMessage(LogLevel logLevel, string message, string typeMessage, Exception exception = null)
        {
            Log(GeneralLogger, logLevel, message, typeMessage, exception);
        }

        public static void AddEncodingMessage(LogLevel logLevel, string message, string typeMessage, Exception exception = null)
        {
            Log(FfmpegLogger, logLevel, message, typeMessage, exception);
        }

        public static void AddIpfsMessage(LogLevel logLevel, string message, string typeMessage, Exception exception = null)
        {
            Log(IpfsLogger, logLevel, message, typeMessage, exception);
        }

        public static void AddSpriteMessage(LogLevel logLevel, string message, string typeMessage, Exception exception = null)
        {
            Log(SpriteLogger, logLevel, message, typeMessage, exception);
        }

        public static void AddImageMessage(LogLevel logLevel, string message, string typeMessage, Exception exception = null)
        {
            Log(ImageLogger, logLevel, message, typeMessage, exception);
        }

        public static void AddSubtitleMessage(LogLevel logLevel, string message, string typeMessage, Exception exception = null)
        {
            Log(SubtitleLogger, logLevel, message, typeMessage, exception);
        }

        public static void Log(ILogger logger, LogLevel logLevel, string message, string typeMessage, Exception exception = null)
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
                    if(exception == null)
                        logger.LogCritical(formatMessage);
                    else
                        logger.LogCritical(exception, formatMessage);
                    break;
            }
        }
    }
}