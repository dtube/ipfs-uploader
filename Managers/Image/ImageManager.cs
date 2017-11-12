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
                OverlayManager.Overlay(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Overlay.jpg"), fileContainer.SourceFileItem.FilePath, outputPath);
                fileContainer.SetOverlay(outputPath);
                IpfsDaemon.Queue(fileContainer.OverlayFileItem);
            }

            return fileContainer.ProgressToken;
        }
    }
}