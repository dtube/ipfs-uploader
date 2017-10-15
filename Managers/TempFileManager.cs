using System.IO;

namespace IpfsUploader.Managers
{
    public static class TempFileManager
    {
        private static string _tempDirectoryPath = Path.GetTempPath();

        public static string GetNewTempFilePath()
        {
            return Path.Combine(_tempDirectoryPath, Path.GetRandomFileName());
        }

        public static void SafeDeleteTempFile(string filePath)
        {
            try
            {
                // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                if(File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch {}
        }
    }
}