using System;
using System.IO;

using Uploader.Daemons;
using Uploader.Models;

namespace Uploader.Managers
{
    public static class ImageManager
    {
        public static Guid ComputeImage(string sourceFilePath, bool? overlay = null)
        {
            FileContainer fileContainer = FileContainer.NewImageContainer(sourceFilePath);

            // si pas d'option overlay, c'est qu'on veut juste ipfs add l'image
            if (!(overlay??false))
            {
                IpfsDaemon.Queue(fileContainer.SourceFileItem);
            }
            else
            {
                fileContainer.SourceFileItem.IpfsErrorMessage = "ipfs not asked";
                string outputPath = TempFileManager.GetNewTempFilePath();
                bool success = OverlayManager.Overlay(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "overlay.jpeg"), fileContainer.SourceFileItem.FilePath, outputPath);
                if(success)
                {
                    fileContainer.SetOverlay(outputPath);
                    IpfsDaemon.Queue(fileContainer.OverlayFileItem);
                }
            }

            return fileContainer.ProgressToken;
        }
    }
}