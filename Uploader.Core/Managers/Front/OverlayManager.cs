using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Front
{
    public static class OverlayManager
    {
        private static string _overlayImagePath = Path.Combine(Directory.GetCurrentDirectory(), "overlay.png");
        private static int _finalWidth = 210;
        private static int _finalHeight = 118;

        public static Guid ComputeOverlay(string sourceFilePath)
        {
            FileContainer fileContainer = FileContainer.NewOverlayContainer(sourceFilePath);           
            var sourceFile = fileContainer.SourceFileItem;

            try
            {
                LogManager.AddOverlayMessage(LogLevel.Information, "SourceFileName " + Path.GetFileName(sourceFile.SourceFilePath), "Start Crop");
                // resize + crop source image                
                string arguments = $"{Path.GetFileName(sourceFile.SourceFilePath)} -resize \"{_finalWidth}x{_finalHeight}^\" -gravity Center -crop {_finalWidth}x{_finalHeight}+0+0 {Path.GetFileName(sourceFile.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "convert"), arguments);
                LogManager.AddOverlayMessage(LogLevel.Information, "convert" + " " + arguments, "Launch command");
                bool success = process.Launch(5);
                if(!success)
                {
                    TempFileManager.SafeDeleteTempFile(sourceFile.SourceFilePath);
                    TempFileManager.SafeDeleteTempFile(sourceFile.TempFilePath);
                    LogManager.AddOverlayMessage(LogLevel.Error, "Erreur convert", "Erreur");
                    return fileContainer.ProgressToken;
                }
                sourceFile.SetOutputFilePath(sourceFile.TempFilePath);
                LogManager.AddOverlayMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(sourceFile.OutputFilePath), "End Crop");
                IpfsDaemon.Instance.Queue(sourceFile);
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(sourceFile.SourceFilePath);
                TempFileManager.SafeDeleteTempFile(sourceFile.TempFilePath);
                LogManager.AddOverlayMessage(LogLevel.Critical, ex.ToString(), "Exception");
                return fileContainer.ProgressToken;
            }

            // remplacement de l'image source
            string oldSourceFilePath = sourceFile.SourceFilePath;
            TempFileManager.SafeDeleteTempFile(oldSourceFilePath);
            sourceFile.SetSourceFilePath(sourceFile.TempFilePath);

            // changement de la source de OverlayFileItem
            fileContainer.OverlayFileItem.SetSourceFilePath(sourceFile.SourceFilePath);

            try
            {
                LogManager.AddOverlayMessage(LogLevel.Information, "SourceFileName " + Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath), "Start Overlay");
                // watermark source image
                string arguments = $"-gravity NorthEast {_overlayImagePath} {Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath)} {Path.GetFileName(fileContainer.OverlayFileItem.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "composite"), arguments);
                LogManager.AddOverlayMessage(LogLevel.Information, "composite" + " " + arguments, "Launch command");
                bool success = process.Launch(5);
                if(!success)
                {
                    TempFileManager.SafeDeleteTempFile(fileContainer.OverlayFileItem.TempFilePath);
                    LogManager.AddOverlayMessage(LogLevel.Error, "Erreur composite", "Erreur");
                    return fileContainer.ProgressToken;
                }
                fileContainer.OverlayFileItem.SetOutputFilePath(fileContainer.OverlayFileItem.TempFilePath);
                LogManager.AddOverlayMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileContainer.OverlayFileItem.OutputFilePath), "End Overlay");
                IpfsDaemon.Instance.Queue(fileContainer.OverlayFileItem);
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(fileContainer.OverlayFileItem.TempFilePath);
                LogManager.AddOverlayMessage(LogLevel.Critical, ex.ToString(), "Exception");
                return fileContainer.ProgressToken;
            }

            return fileContainer.ProgressToken;
        }
    }
}