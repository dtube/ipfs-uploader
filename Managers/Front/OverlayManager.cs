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
        private static string _overlayImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "overlay.jpeg");

        public static void ComputeOverlay(FileContainer fileContainer, int? x = null, int? y = null)
        {
            fileContainer.SourceFileItem.IpfsErrorMessage = "ipfs not asked";
            string outputPath = TempFileManager.GetNewTempFilePath();
            bool success = Combine(_overlayImagePath, fileContainer.SourceFileItem.FilePath, outputPath, x, y);
            if(success)
            {
                fileContainer.SetOverlay(outputPath);
                IpfsDaemon.Queue(fileContainer.OverlayFileItem);
            }
        }

        private static bool Combine(string overlayImagePath, string imageToOverlayPath, string outputPath, int? x = null, int? y = null)
        {
            try
            {
                using(Image overlayImage = Image.FromFile(overlayImagePath))
                {
                    using(Image imageToOverlay = Image.FromFile(imageToOverlayPath))
                    {
                        using(Graphics graphics = Graphics.FromImage(imageToOverlay))
                        {
                            // Si position n'est pas fournie, centrer l'image
                            if (x == null || y == null)
                            {
                                x = (imageToOverlay.Width / 2) - (overlayImage.Width / 2);
                                y = (imageToOverlay.Height / 2) - (overlayImage.Height / 2);
                            }

                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.DrawImage(overlayImage, x.Value, y.Value);
                            //graphics.Save();
                        }

                        imageToOverlay.Save(outputPath, ImageFormat.Png);
                    }
                }

                return true;
            }
            catch(Exception ex)
            {
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
                return false;
            }
        }        
    }
}