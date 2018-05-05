using System.Collections.Generic;
using System.IO;
using System;

using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;

namespace Uploader.Core.Managers.Common
{
    public static class TempFileManager
    {
        private static string _tempDirectoryPath;

        public static string GetTempDirectory()
        {
            if(_tempDirectoryPath == null)
            {
                if (GeneralSettings.Instance.TempFilePath.Length > 0)
                    _tempDirectoryPath = GeneralSettings.Instance.TempFilePath;
                else
                    _tempDirectoryPath = Path.GetTempPath();
            }

            return _tempDirectoryPath;
        }

        public static string GetNewTempFilePath()
        {
            return Path.Combine(GetTempDirectory(), Path.GetRandomFileName());
        }

        public static void SafeDeleteTempFile(string filePath, string hash = "")
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {                
                // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                if (File.Exists(filePath))
                {
                    if (IpfsSettings.Instance.OnlyHash && hash.Length == 46)
                    {
                        File.Move(filePath, Path.Combine(GeneralSettings.Instance.FinalFilePath, hash));
                    }
                    File.Delete(filePath);
                } 
            }
            catch
            {}
        }

        public static void SafeDeleteTempFiles(IList<string> filesPath, string hash = "")
        {
            if(filesPath == null)
                return;

            // suppression des images
            foreach (string filePath in filesPath)
            {
                SafeDeleteTempFile(filePath, hash);
            }
        }

        public static void SafeCopyError(string filePath, string hash)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, Path.Combine(GeneralSettings.Instance.ErrorFilePath, hash));
                } 
            }
            catch
            {}
        }
    }
}