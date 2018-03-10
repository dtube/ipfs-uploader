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

            if (!IsValidVTT(text))
            {
                fileContainer.SubtitleFileItem.IpfsProcess.SetErrorMessage("Not a valid WEBVTT file", "Not a valid WEBVTT file");
                return fileContainer.ProgressToken;
            } 

            try
            {
                await File.WriteAllTextAsync(fileContainer.SubtitleFileItem.TempFilePath, text);
                fileContainer.SubtitleFileItem.ReplaceOutputPathWithTempPath();
                IpfsDaemon.Instance.Queue(fileContainer.SubtitleFileItem);
            }
            catch(Exception ex)
            {
                LogManager.AddSubtitleMessage(LogLevel.Critical, "Exception non gérée", "Exception", ex);                
                fileContainer.CancelAll("Exception non gérée");
                fileContainer.CleanFilesIfEnd();
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