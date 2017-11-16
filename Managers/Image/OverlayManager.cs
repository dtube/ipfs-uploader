using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Uploader.Managers
{
    public class OverlayManager
    {
        public static void Overlay(string overlayImagePath, string imageToOverlayPath, string outputPath, int? x = null, int? y = null)
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
            }
            catch(Exception ex)
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);
                File.AppendAllLines(Path.Combine(logDirectory, "imagesException.log"), new []
                {
                    DateTime.UtcNow.ToString("o") + " " + ex.ToString()
                });

                throw;
            }
        }
    }
}