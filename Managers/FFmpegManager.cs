using System;
using System.Diagnostics;
using System.IO;
using IpfsUploader.Models;

namespace IpfsUploader.Managers
{
    public static class FFmpegManager
    {
        public static bool Encode(FileItem fileItem)
        {
            try
            {
                string sourceFilePath = fileItem.VideoFile.SourceFileItem.FilePath;
                string newEncodedFilePath = TempFileManager.GetNewTempFilePath();
                VideoFormat videoFormat = fileItem.VideoFormat;

                //todo encode
                Debug.WriteLine(Path.GetFileName(sourceFilePath) + " / " + videoFormat);

                fileItem.FilePath = newEncodedFilePath;
                return true;
            }
            catch(Exception ex)
            {
                fileItem.FFmpegErrorMessage = ex.Message;
                return false;
            }
        }
    }
}