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

        private static int _finalWidthSnap = 210;
        private static int _finalHeightSnap = 118;

        //private static int _finalWidthOverlay = 640;
        //private static int _finalHeightOverlay = 360;

        public static Guid ComputeOverlay(string sourceFilePath)
        {
            FileContainer fileContainer = FileContainer.NewOverlayContainer(sourceFilePath);           
            FileItem sourceFile = fileContainer.SourceFileItem;

            try
            {
                LogManager.AddOverlayMessage(LogLevel.Information, "SourceFileName " + Path.GetFileName(sourceFile.SourceFilePath), "Start Crop");
                // resize + crop source image                
                string arguments = $"{Path.GetFileName(sourceFile.SourceFilePath)} -resize \"{_finalWidthSnap}x{_finalHeightSnap}^\" -gravity Center -crop {_finalWidthSnap}x{_finalHeightSnap}+0+0 {Path.GetFileName(sourceFile.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "convert"), arguments, LogManager.OverlayLogger);
                bool success = process.Launch(5);
                if(!success)
                {
                    fileContainer.CleanFilesIfEnd();
                    LogManager.AddOverlayMessage(LogLevel.Error, "Erreur convert", "Erreur");
                    return fileContainer.ProgressToken;
                }
                sourceFile.ReplaceOutputPathWithTempPath();
                LogManager.AddOverlayMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(sourceFile.OutputFilePath), "End Crop");
                IpfsDaemon.Instance.Queue(sourceFile);
            }
            catch(Exception ex)
            {
                fileContainer.CleanFilesIfEnd();
                LogManager.AddOverlayMessage(LogLevel.Critical, "Exception non gérée", "Exception", ex);
                return fileContainer.ProgressToken;
            }

            // remplacement de l'image source par l'image de sortie
            sourceFile.SetSourceFilePath(sourceFile.OutputFilePath);

            // changement de la source de OverlayFileItem
            fileContainer.OverlayFileItem.SetSourceFilePath(sourceFile.SourceFilePath);

            try
            {
                LogManager.AddOverlayMessage(LogLevel.Information, "SourceFileName " + Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath), "Start Overlay");
                // watermark source image
                string arguments = $"-gravity NorthEast {_overlayImagePath} {Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath)} {Path.GetFileName(fileContainer.OverlayFileItem.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "composite"), arguments, LogManager.OverlayLogger);
                bool success = process.Launch(5);
                if(!success)
                {
                    fileContainer.CleanFilesIfEnd();
                    LogManager.AddOverlayMessage(LogLevel.Error, "Erreur composite", "Erreur");
                    return fileContainer.ProgressToken;
                }
                fileContainer.OverlayFileItem.ReplaceOutputPathWithTempPath();
                LogManager.AddOverlayMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileContainer.OverlayFileItem.OutputFilePath), "End Overlay");
                IpfsDaemon.Instance.Queue(fileContainer.OverlayFileItem);
            }
            catch(Exception ex)
            {
                fileContainer.CleanFilesIfEnd();
                LogManager.AddOverlayMessage(LogLevel.Critical, "Exception non gérée", "Exception", ex);
                return fileContainer.ProgressToken;
            }

            return fileContainer.ProgressToken;
        }
    }
}