using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Front;
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
                IList<string> files = GetListImageFrom(fileItem.TempFilePath); // récupération des images

                LogManager.AddSpriteMessage(LogLevel.Information, (files.Count - 1) + " images", "Start Combine images");


                // garder que les 100 dernières images pour éliminer les premières (1 ou 2 en réalité)
                int skip = files.Count > VideoSettings.Instance.NbSpriteImages
                    ? files.Count - VideoSettings.Instance.NbSpriteImages
                    : 0;
                var list = new StringBuilder();
                foreach (string imagePath in files.Skip(skip))
                {
                    if(list.Length > 0)
                        list.Append(" ");

                    list.Append(Path.GetFileName(imagePath));
                }

                arguments = $"-mode concatenate -tile 1x {list} {Path.GetFileName(fileItem.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "montage"), arguments, LogManager.SpriteLogger);
                bool successSprite = process.Launch(5);
                TempFileManager.SafeDeleteTempFiles(files); // suppression des images
                if (!successSprite)
                {
                    fileItem.SpriteEncodeProcess.SetErrorMessage("Error while combine images", "Error creation sprite while combine images");
                    return false;
                }

                fileItem.ReplaceOutputPathWithTempPath();
                LogManager.AddSpriteMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileItem.OutputFilePath) + " / FileSize " + fileItem.FileSize, "End Sprite");
                fileItem.SpriteEncodeProcess.EndProcessDateTime();
                return true;
            }
            catch (Exception ex)
            {
                string message = "Video Duration " + sourceFile.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.SpriteEncodeProcess.Progress;
                fileItem.SpriteEncodeProcess.SetErrorMessage("Exception non gérée", message, ex);
                IList<string> files = GetListImageFrom(fileItem.TempFilePath); // récupération des images
                TempFileManager.SafeDeleteTempFiles(files); // suppression des images
                return false;
            }
        }

        private static string GetPattern(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath) + "-%03d.jpeg";
        }

        private static IList<string> GetListImageFrom(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return new List<string>();

            string directoryName = Path.GetDirectoryName(filePath);
            if(directoryName == null)
                return new List<string>();

            return Directory.EnumerateFiles(directoryName, Path.GetFileNameWithoutExtension(filePath) + "-*.jpeg").OrderBy(s => s).ToList();
        }
    }
}