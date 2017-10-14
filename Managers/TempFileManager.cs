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

        public static void SafeDeleteTempFile(string sourceFilePath)
        {
            try
            {
                // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                if(System.IO.File.Exists(sourceFilePath))
                    System.IO.File.Delete(sourceFilePath);
            }
            catch {}
        }
    }
}