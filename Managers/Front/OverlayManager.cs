using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

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
                using(Image overlayImage = Image.FromFile(_overlayImagePath))
                {
                    using(Image sourceImage = Image.FromFile(fileContainer.SourceFileItem.FilePath))
                    {
                        using(Graphics graphics = Graphics.FromImage(sourceImage))
                        {
                            // Si position n'est pas fournie, centrer l'image
                            if (x == null || y == null)
                            {
                                x = (sourceImage.Width / 2) - (overlayImage.Width / 2);
                                y = (sourceImage.Height / 2) - (overlayImage.Height / 2);
                            }

                            graphics.DrawImage(overlayImage, x.Value, y.Value);
                        }
                        string outputFilePath = System.IO.Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".jpeg");
                        sourceImage.Save(outputFilePath, ImageFormat.Jpeg);
                        fileContainer.OverlayFileItem.FilePath = outputFilePath;
                    }
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
                using(Image sourceImage = Image.FromFile(oldFilePath))
                {
                    //create a bitmap to hold the new image
                    using(var finalBitmap = new Bitmap(_finalWidth, _finalHeight))
                    {
                        using(Graphics graphics = Graphics.FromImage(finalBitmap))
                        {
                            Rectangle sourceRectangle;
                            Rectangle destRectangle = new Rectangle(0, 0, _finalWidth, _finalHeight);
                            // video verticale, garder largeur _finalWidth, couper la hauteur
                            if((double)sourceImage.Width / (double)sourceImage.Height < ((double)_finalWidth / (double)_finalHeight))
                            {
                                int hauteur = sourceImage.Width * _finalHeight / _finalWidth;
                                int yOffset = (sourceImage.Height - hauteur) / 2;
                                sourceRectangle = new Rectangle(0, yOffset, sourceImage.Width, hauteur);
                            }
                            else // video horizontale, garder hauteur _finalHeight, couper la largeur
                            {
                                int largeur = sourceImage.Height * _finalWidth / _finalHeight;
                                int xOffset = (sourceImage.Width - largeur) / 2;
                                sourceRectangle = new Rectangle(xOffset, 0, largeur, sourceImage.Height);
                            }
                            graphics.DrawImage(sourceImage, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                        }
                        string outputFilePath = System.IO.Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".jpeg");
                        finalBitmap.Save(outputFilePath, ImageFormat.Jpeg);
                        fileContainer.SourceFileItem.FilePath = outputFilePath;
                    }
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