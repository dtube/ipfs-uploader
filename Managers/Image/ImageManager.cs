using System;
using IpfsUploader.Daemons;
using IpfsUploader.Models;

namespace IpfsUploader.Managers
{
    public static class ImageManager
    {
        public static Guid ComputeImage(string sourceFilePath, bool? sprite = null, bool? overlay = null)
        {
            FileContainer fileContainer = FileContainer.NewImageContainer(sourceFilePath);

            // si pas d'option sprite ou overlay, c'est qu'on veut juste ipfs add l'image
            if((sprite??false) && (overlay??false))
            {
                fileContainer.SourceFileItem.IpfsErrorMessage = null;
                IpfsDaemon.Queue(fileContainer.SourceFileItem);
            }
            else
            {
                if(sprite??false)
                {
                    string outputPath = TempFileManager.GetNewTempFilePath();

                    //todo get image from video

                    SpriteManager.CombineBitmap(null, outputPath);
                    fileContainer.SetSprite(outputPath);
                    IpfsDaemon.Queue(fileContainer.SpriteFileItem);
                }

                if(overlay??false)
                {
                    string outputPath = TempFileManager.GetNewTempFilePath();
                    OverlayManager.Overlay("", outputPath);
                    fileContainer.SetOverlay(outputPath);
                    IpfsDaemon.Queue(fileContainer.OverlayFileItem);
                }
            }

            return fileContainer.ProgressToken;
        }
    }
}