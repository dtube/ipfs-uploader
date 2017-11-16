using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Uploader.Managers
{
    public class SpriteManager
    {
        public static bool CombineBitmap(string[] filesToCombine, string outputFilePath)
        {
            //read all images into memory
            var images = new List<Image>();

            try
            {
                int width = 0;
                int height = 0;

                foreach (string imagePath in filesToCombine)
                {
                    // create a Bitmap from the file and add it to the list
                    Image imageSource = Image.FromFile(imagePath);

                    // calculer largeur suivant le ratio de l'image pour garder 118 en hauteur
                    int largeur = imageSource.Width * 118 / imageSource.Height;
                    Image image = ResizeImage(imageSource, largeur, 118);
                    imageSource.Dispose();

                    // update the size of the final bitmap
                    height += image.Height;
                    width = image.Width > width ? image.Width : width;

                    images.Add(image);
                }

                //create a bitmap to hold the combined image
                using(var finalBitmap = new Bitmap(width, height))
                {
                    //get a graphics object from the image so we can draw on it
                    using(Graphics g = Graphics.FromImage(finalBitmap))
                    {
                        //set background color
                        g.Clear(Color.Transparent);

                        // Ajoute les images les unes à la suite des autres verticalement
                        int offset = 0;
                        foreach (Image image in images)
                        {
                            g.DrawImage(image, new Rectangle(0, offset, image.Width, image.Height));
                            offset += image.Height;
                        }

                        //g.Save();
                    }
                    finalBitmap.Save(outputFilePath, ImageFormat.Png);
                }
                return true;
            }
            catch(Exception ex)
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);
                File.AppendAllLines(Path.Combine(logDirectory, "spriteException.log"), new []
                {
                    DateTime.UtcNow.ToString("o") + " " + ex.ToString()
                });

                return false;
            }
            finally
            {
                //clean up memory
                foreach (Image image in images)
                {
                    image.Dispose();
                }
            }
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using(var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using(var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static string GetPattern(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath) + "-%03d.jpeg";
        }

        public static string[] GetListImageFrom(string filePath)
        {
            return Directory.EnumerateFiles(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "*.jpeg").ToArray();
        }
    }
}