using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Front
{
    public static class SubtitleManager
    {
        public static async Task<Guid> ComputeSubtitle(string text)
        {
            FileContainer fileContainer = FileContainer.NewSubtitleContainer();
            string outputfilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".vtt");

            if (!IsValidVTT(text)) {
                fileContainer.SubtitleFileItem.IpfsProcess.SetErrorMessage("Not a valid WEBVTT file");
                return fileContainer.ProgressToken;
            } 

            try
            {
                await File.WriteAllTextAsync(outputfilePath, text);
                fileContainer.SubtitleFileItem.SetOutputFilePath(outputfilePath);
                IpfsDaemon.Instance.Queue(fileContainer.SubtitleFileItem);
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(outputfilePath);
                LogManager.AddSubtitleMessage(LogLevel.Critical, ex.ToString(), "Exception");
                return fileContainer.ProgressToken;
            }

            return fileContainer.ProgressToken;
        }

        private static bool IsValidVTT(string text)
        {
            LogManager.AddSubtitleMessage(LogLevel.Debug, text, "DEBUG");
            if (!text.StartsWith("WEBVTT"))
                return false;

            // eventuellement rajouter plus de verifs
            // mais peu d'interet
            
            return true;
        }
    }
}