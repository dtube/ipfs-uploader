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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="finalWidth"></param>
        /// <param name="finalHeight"></param>
        /// <returns>largeur, hauteur</returns>
        public static Tuple<int, int> GetSize(double width, double height, double finalWidth, double finalHeight)
        {
            //video verticale, garder hauteur finale, réduire largeur finale
            if(width / height < finalWidth / finalHeight)
                return new Tuple<int, int>(GetWidth(width, height, finalHeight), (int)finalHeight);
            
            // sinon garder largeur finale, réduire hauteur finale
            return new Tuple<int, int>((int)finalWidth, GetHeight(width, height, finalWidth));
        }
        
        public static int GetWidth(double width, double height, double finalHeight)
        {
            return GetPair((int)(finalHeight * width / height));
        }

        public static int GetHeight(double width, double height, double finalWidth)
        {
            return GetPair((int)(finalWidth * height / width));
        }

        private static int GetPair(int number)
        {
            return number % 2 == 0 ? number : number + 1;
        }
    }
}