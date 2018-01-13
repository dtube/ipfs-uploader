using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

using Uploader.Managers.Common;

namespace Uploader.Managers.Video
{
    public class SpriteManager
    {
        public static bool CombineBitmap(string[] filesToCombine, string outputFilePath)
        {
            if(filesToCombine == null || filesToCombine.Length == 0)
                return false;
            if(string.IsNullOrWhiteSpace(outputFilePath))
                return false;

            //read all images into memory
            var images = new List<Image>();

            try
            {
                int width = 0;
                int height = 0;

                foreach (string imagePath in filesToCombine)
                {
                    // create a Bitmap from the file and add it to the list
                    Image image = Image.FromFile(imagePath);                  

                    // update the size of the final bitmap
                    height += image.Height;
                    width = image.Width > width ? image.Width : width;

                    images.Add(image);
                }

                //create a bitmap to hold the combined image
                using(var finalBitmap = new Bitmap(width, height))
                {
                    //get a graphics object from the image so we can draw on it
                    using(Graphics graphics = Graphics.FromImage(finalBitmap))
                    {
                        // Ajoute les images les unes à la suite des autres verticalement
                        int offset = 0;
                        foreach (Image image in images)
                        {
                            graphics.DrawImage(image, new Rectangle(0, offset, image.Width, image.Height));
                            offset += image.Height;
                        }
                    }
                    finalBitmap.Save(outputFilePath, ImageFormat.Jpeg);
                }
                return true;
            }
            catch(Exception ex)
            {
                LogManager.AddSpriteMessage(ex.ToString(), "Exception");
                TempFileManager.SafeDeleteTempFile(outputFilePath);
                return false;
            }
            finally
            {
                try
                {
                    //clean up memory
                    foreach (Image image in images)
                    {
                        image.Dispose();
                    }
                }
                catch{}
            }
        }
    }
}