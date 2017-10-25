using System.IO;

namespace Uploader.Managers
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
            try
            {
                // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                if(File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch {}
        }

        public static void SafeDeleteTempFiles(string filePath)
        {
            try
            {
                // suppression des images temporaires s'ils sont présent
                foreach(string filePath2 in SpriteManager.GetListImageFrom(filePath))
                {
                    File.Delete(filePath2);
                }
            }
            catch {}
        }
    }
}