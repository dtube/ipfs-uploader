using System;
using System.Diagnostics;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Front
{
    public static class SubtitleManager
    {
        private static int _maxLength = 262144; // 256KB

        public static Guid ComputeSubtitle(string text)
        {
            FileContainer fileContainer = FileContainer.NewSubtitleContainer();
            string filePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".vtt");

            if (!isValidVTT(text))
                return fileContainer.ProgressToken;

            try
            {
                System.IO.File.WriteAllText(filePath, text);
                fileContainer.SubtitleFileItem.FilePath = filePath;
                IpfsDaemon.Queue(fileContainer.SubtitleFileItem);
            }
            catch(Exception ex)
            {                
                LogManager.AddSubtitleMessage(ex.ToString(), "Exception");
                return fileContainer.ProgressToken;
            }
            finally
            {
                TempFileManager.SafeDeleteTempFile(filePath);
            }

            return fileContainer.ProgressToken;
        }

        private static bool isValidVTT(string text)
        {
            Debug.WriteLine(text);
            if (!text.StartsWith("VTT"))
                return false;

            // eventuellement rajouter plus de verifs
            // mais peu d'interet
            
            return true;
        }
    }
}