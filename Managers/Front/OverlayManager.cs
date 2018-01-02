using System;
using System.IO;

using ImageMagick;

using Uploader.Managers.Common;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Front
{
    public static class OverlayManager
    {
        private static string _overlayImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "overlay.png");
        private static int _finalWidth = 210;
        private static int _finalHeight = 118;

        public static Guid ComputeOverlay(string sourceFilePath, int? x = null, int? y = null)
        {
            FileContainer fileContainer = FileContainer.NewOverlayContainer(sourceFilePath);

            ResizeImage(fileContainer);            

            try
            {
                // Read image that needs a watermark
                using (MagickImage sourceImage = new MagickImage(fileContainer.SourceFileItem.FilePath))
                {
                    // Read the watermark that will be put on top of the image
                    using (MagickImage watermark = new MagickImage(_overlayImagePath))
                    {
                        // Si position n'est pas fournie, centrer l'image
                        if (x == null || y == null)
                        {
                            x = (sourceImage.Width / 2) - (watermark.Width / 2);
                            y = (sourceImage.Height / 2) - (watermark.Height / 2);
                        }

                        // draw the watermark at a specific location
                        sourceImage.Composite(watermark, x.Value, y.Value, CompositeOperator.Over);
                    }

                    string outputFilePath = System.IO.Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png");
                    sourceImage.Format = MagickFormat.Png;
                    // Save the result
                    sourceImage.Write(outputFilePath);
                    fileContainer.OverlayFileItem.FilePath = outputFilePath;
                }

                IpfsDaemon.Queue(fileContainer.OverlayFileItem);
            }
            catch(Exception ex)
            {
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
            }

            return fileContainer.ProgressToken;
        }

        public static void ResizeImage(FileContainer fileContainer)
        {
            try
            {
                string oldFilePath = fileContainer.SourceFileItem.FilePath;
                // Read from file
                using (MagickImage sourceImage = new MagickImage(oldFilePath))
                {
                    MagickGeometry size = new MagickGeometry(_finalWidth, _finalHeight);
                    size.IgnoreAspectRatio = false;

                    // video verticale, garder largeur _finalWidth, couper la hauteur
                    if((double)sourceImage.Width / (double)sourceImage.Height < ((double)_finalWidth / (double)_finalHeight))
                    {
                        int hauteur = sourceImage.Width * _finalHeight / _finalWidth;
                        sourceImage.Crop(sourceImage.Width, hauteur);
                    }
                    else // video horizontale, garder hauteur _finalHeight, couper la largeur
                    {
                        int largeur = sourceImage.Height * _finalWidth / _finalHeight;
                        sourceImage.Crop(largeur, sourceImage.Height);
                    }

                    sourceImage.Resize(size);

                    string outputFilePath = System.IO.Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png");
                    sourceImage.Format = MagickFormat.Png;
                    // Save the result
                    sourceImage.Write(outputFilePath);
                    fileContainer.SourceFileItem.FilePath = outputFilePath;
                }

                IpfsDaemon.Queue(fileContainer.SourceFileItem);
                TempFileManager.SafeDeleteTempFile(oldFilePath);
            }
            catch(Exception ex)
            {
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
            }
        }
    }
}