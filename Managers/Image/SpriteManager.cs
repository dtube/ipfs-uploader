using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Uploader.Managers
{
    public class SpriteManager
    {
        public static void CombineBitmap(string[] filesToCombine, string outputFilePath)
        {
            //read all images into memory
            var images = new List<Image>();

            try
            {
                int width = 0;
                int height = 0;

                foreach (string imagePath in filesToCombine)
                {
                    //create a Bitmap from the file and add it to the list
                    var image = Image.FromFile(imagePath);

                    //update the size of the final bitmap
                    height += image.Height;
                    width = image.Width > width ? image.Width : width;

                    images.Add(image);
                }

                //create a bitmap to hold the combined image
                using(var finalBitmap = new Bitmap(width, height))
                {
                    //get a graphics object from the image so we can draw on it
                    using (Graphics g = Graphics.FromImage(finalBitmap))
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
    }
}