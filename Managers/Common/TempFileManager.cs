using System.IO;

namespace Uploader.Managers.Common
{
    public static class TempFileManager
    {
        private static string _tempDirectoryPath = Path.GetTempPath();

        public static string GetTempDirectory()
        {
            return _tempDirectoryPath;
        }

        public static string GetNewTempFilePath()
        {
            return Path.Combine(_tempDirectoryPath, Path.GetRandomFileName());
        }

        public static void SafeDeleteTempFile(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {}
        }

        public static void SafeDeleteTempFiles(string[] filesPath)
        {
            if(filesPath == null)
                return;

            // suppression des images
            foreach (string filePath in filesPath)
            {
                SafeDeleteTempFile(filePath);
            }
        }
    }
}