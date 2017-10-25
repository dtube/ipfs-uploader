using System;
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
            if(overlay??false)
            {
                fileContainer.SourceFileItem.IpfsErrorMessage = null;
                IpfsDaemon.Queue(fileContainer.SourceFileItem);
            }
            else
            {
                string outputPath = TempFileManager.GetNewTempFilePath();
                OverlayManager.Overlay("", outputPath); //todo récupération vrai image
                fileContainer.SetOverlay(outputPath);
                IpfsDaemon.Queue(fileContainer.OverlayFileItem);
            }

            return fileContainer.ProgressToken;
        }
    }
}