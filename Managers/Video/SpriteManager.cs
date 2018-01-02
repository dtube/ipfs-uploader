using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ImageMagick;

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
                
            try
            {
                using (MagickImageCollection images = new MagickImageCollection())
                {
                    foreach (string imagePath in filesToCombine)
                    {
                        // Add the first image
                        MagickImage image = new MagickImage(imagePath);
                        images.Add(image);
                    }

                    // Create a vertical image from images
                    using (IMagickImage result = images.AppendVertically())
                    {
                        result.Format = MagickFormat.Jpeg;
                        result.Quality = 75;

                        // Save the result
                        result.Write(outputFilePath);
                    }
                }

                return true;
            }
            catch(Exception ex)
            {
                LogManager.AddSpriteMessage(ex.ToString(), "Exception");
                return false;
            }
        }
    }
}