using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal class SpriteManager
    {
        public static bool Encode(FileItem fileItem)
        {
            FileItem sourceFile = fileItem.FileContainer.SourceFileItem;
            try
            {
                fileItem.SpriteEncodeProcess.StartProcessDateTime();

                LogManager.AddSpriteMessage(LogLevel.Information, "SourceFilePath " + Path.GetFileName(fileItem.SourceFilePath), "Start Sprite");             

                int nbImages = VideoSettings.Instance.NbSpriteImages;
                int heightSprite = VideoSettings.Instance.HeightSpriteImages;

                // Calculer nb image/s
                //  si < 100s de vidéo -> 1 image/s
                //  sinon (nb secondes de la vidéo / 100) image/s
                string frameRate = "1";
                int duration = sourceFile.VideoDuration.Value;
                if (duration > nbImages)
                {
                    frameRate = $"{nbImages}/{duration}"; //frameRate = inverse de image/s
                }

                int spriteWidth = SizeHelper.GetWidth(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, heightSprite);
                string sizeImageMax = $"scale={spriteWidth}:{heightSprite}";

                // Extract frameRate image/s de la video
                string arguments = $"-y -i {Path.GetFileName(fileItem.SourceFilePath)} -r {frameRate} -vf {sizeImageMax} -f image2 {GetPattern(fileItem.TempFilePath)}";
                var ffmpegProcessManager = new FfmpegProcessManager(fileItem, fileItem.SpriteEncodeProcess);
                ffmpegProcessManager.StartProcess(arguments, VideoSettings.Instance.EncodeGetImagesTimeout);
                string[] files = GetListImageFrom(fileItem.TempFilePath); // récupération des images

                LogManager.AddSpriteMessage(LogLevel.Information, (files.Length - 1) + " images", "Start Combine images");
                bool successSprite = CombineBitmap(files.Skip(files.Length - VideoSettings.Instance.NbSpriteImages).ToArray(), fileItem.TempFilePath); // création du sprite                                
                TempFileManager.SafeDeleteTempFiles(files); // suppression des images
                if(successSprite)
                {
                    fileItem.SetOutputFilePath(fileItem.TempFilePath);
                    LogManager.AddSpriteMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileItem.OutputFilePath) + " / FileSize " + fileItem.FileSize, "End Sprite");
                }
                else
                {
                    LogManager.AddSpriteMessage(LogLevel.Error, "Error while combine images", "Error");
                    fileItem.SpriteEncodeProcess.SetErrorMessage("Error creation sprite while combine images");
                    return false;
                }

                fileItem.SpriteEncodeProcess.EndProcessDateTime();
                return true;
            }
            catch (Exception ex)
            {
                LogManager.AddSpriteMessage(LogLevel.Critical, "Video Duration " + sourceFile.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.SpriteEncodeProcess.Progress + " / Exception : " + ex, "Exception");
                fileItem.SpriteEncodeProcess.SetErrorMessage("Exception");
                string[] files = GetListImageFrom(fileItem.TempFilePath); // récupération des images
                TempFileManager.SafeDeleteTempFiles(files); // suppression des images
                return false;
            }
        }

        private static string GetPattern(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath) + "-%03d.jpeg";
        }

        private static string[] GetListImageFrom(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return new string[0];

            string directoryName = Path.GetDirectoryName(filePath);
            if(directoryName == null)
                return new string[0];

            return Directory.EnumerateFiles(directoryName, Path.GetFileNameWithoutExtension(filePath) + "-*.jpeg").OrderBy(s => s).ToArray();
        }

        private static bool CombineBitmap(string[] filesToCombine, string outputFilePath)
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
                LogManager.AddSpriteMessage(LogLevel.Critical, ex.ToString(), "Exception");
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