using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Uploader.Managers
{
    public class OverlayManager
    {
        public static void Overlay(string overlayImagePath, string imageToOverlayPath, string outputPath, int? x = null, int? y = null)
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
    }
}