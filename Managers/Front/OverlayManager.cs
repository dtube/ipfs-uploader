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

        public static Guid ComputeOverlay(string sourceFilePath, int? x = null, int? y = null)
        {
            FileContainer fileContainer = FileContainer.NewOverlayContainer(sourceFilePath);

            try
            {
                // TODO resize source 210*118, si non 16/9 crop image, save in jpeg

                using(Image overlayImage = Image.FromFile(_overlayImagePath)) // TODO tester avec png transparent
                {
                    using(Image imageToOverlay = Image.FromFile(fileContainer.SourceFileItem.FilePath))
                    {
                        using(Graphics graphics = Graphics.FromImage(imageToOverlay))
                        {
                            // Si position n'est pas fournie, centrer l'image
                            if (x == null || y == null)
                            {
                                x = (imageToOverlay.Width / 2) - (overlayImage.Width / 2);
                                y = (imageToOverlay.Height / 2) - (overlayImage.Height / 2);
                            }

                            graphics.DrawImage(overlayImage, x.Value, y.Value);
                        }

                        string outputPath = System.IO.Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".jpeg");
                        imageToOverlay.Save(outputPath, ImageFormat.Jpeg);
                        fileContainer.OverlayFileItem.FilePath = outputPath;
                    }
                }

                IpfsDaemon.Queue(fileContainer.OverlayFileItem);
                IpfsDaemon.Queue(fileContainer.SourceFileItem);
            }
            catch(Exception ex)
            {
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
            }

            return fileContainer.ProgressToken;
        }
    }
}