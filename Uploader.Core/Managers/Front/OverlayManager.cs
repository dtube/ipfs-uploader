using System;
using System.IO;

using Microsoft.Extensions.Logging;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Front
{
    public static class ImageManager
    {
        private static string _overlayImagePath = Path.Combine(Directory.GetCurrentDirectory(), "overlay.png");

        private static int _finalWidthSnap = 210;
        private static int _finalHeightSnap = 118;

        private static int _finalWidthOverlay = 640;
        private static int _finalHeightOverlay = 360;

        public static Guid ComputeImage(string sourceFilePath)
        {
            FileContainer fileContainer = FileContainer.NewImageContainer(sourceFilePath);           
            FileItem sourceFile = fileContainer.SourceFileItem;

            ///////////////////////////////////////////////////////////////////////////////////////////////
            /// resize + crop source image
            ///////////////////////////////////////////////////////////////////////////////////////////////

            try
            {
                LogManager.AddImageMessage(LogLevel.Information, "SourceFileName " + Path.GetFileName(sourceFile.SourceFilePath), "Start Resize and Crop source");          
                string arguments = $"{Path.GetFileName(sourceFile.SourceFilePath)} -resize \"{_finalWidthOverlay}x{_finalHeightOverlay}^\" -gravity Center -crop {_finalWidthOverlay}x{_finalHeightOverlay}+0+0 {Path.GetFileName(sourceFile.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "convert"), arguments, LogManager.ImageLogger);
                bool success = process.Launch(5);
                if(!success)
                {
                    LogManager.AddImageMessage(LogLevel.Error, "Erreur convert", "Erreur");
                    fileContainer.CancelAll("Erreur resize and crop source");
                    fileContainer.CleanFilesIfEnd();
                    return fileContainer.ProgressToken;
                }
                sourceFile.ReplaceOutputPathWithTempPath();
                LogManager.AddImageMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(sourceFile.OutputFilePath), "End Resize and Crop source");
            }
            catch(Exception ex)
            {
                LogManager.AddImageMessage(LogLevel.Critical, "Exception non gérée resize crop source", "Exception", ex);
                fileContainer.CancelAll("Exception non gérée");
                fileContainer.CleanFilesIfEnd();
                return fileContainer.ProgressToken;
            }

            // remplacement de l'image source par l'image de sortie
            sourceFile.SetSourceFilePath(sourceFile.OutputFilePath);

            ///////////////////////////////////////////////////////////////////////////////////////////////
            /// Resize snap image
            ///////////////////////////////////////////////////////////////////////////////////////////////

            // changement de la source de SnapFileItem
            fileContainer.SnapFileItem.SetSourceFilePath(sourceFile.SourceFilePath);

            try
            {
                LogManager.AddImageMessage(LogLevel.Information, "SourceFileName " + Path.GetFileName(fileContainer.SnapFileItem.SourceFilePath), "Start Resize Snap");
                string arguments = $"{Path.GetFileName(fileContainer.SnapFileItem.SourceFilePath)} -resize \"{_finalWidthSnap}x{_finalHeightSnap}^\" {Path.GetFileName(fileContainer.SnapFileItem.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "convert"), arguments, LogManager.ImageLogger);
                bool success = process.Launch(5);
                if(!success)
                {
                    LogManager.AddImageMessage(LogLevel.Error, "Erreur snap", "Erreur");
                    fileContainer.CancelAll("Erreur snap");
                    fileContainer.CleanFilesIfEnd();
                    return fileContainer.ProgressToken;
                }
                fileContainer.SnapFileItem.ReplaceOutputPathWithTempPath();
                LogManager.AddImageMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileContainer.SnapFileItem.OutputFilePath), "End Resize Snap");
                IpfsDaemon.Instance.Queue(fileContainer.SnapFileItem);
            }
            catch(Exception ex)
            {
                LogManager.AddImageMessage(LogLevel.Critical, "Exception non gérée snap", "Exception", ex);
                fileContainer.CancelAll("Exception non gérée");
                fileContainer.CleanFilesIfEnd();
                return fileContainer.ProgressToken;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////
            /// Overlay image
            ///////////////////////////////////////////////////////////////////////////////////////////////            

            // changement de la source de OverlayFileItem
            fileContainer.OverlayFileItem.SetSourceFilePath(sourceFile.SourceFilePath);

            try
            {
                LogManager.AddImageMessage(LogLevel.Information, "SourceFileName " + Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath), "Start Overlay");
                string arguments = $"-gravity NorthEast {_overlayImagePath} {Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath)} {Path.GetFileName(fileContainer.OverlayFileItem.TempFilePath)}";
                var process = new ProcessManager(Path.Combine(GeneralSettings.Instance.ImageMagickPath, "composite"), arguments, LogManager.ImageLogger);
                bool success = process.Launch(5);
                if(!success)
                {
                    LogManager.AddImageMessage(LogLevel.Error, "Erreur overlay", "Erreur");
                    fileContainer.CancelAll("Erreur overlay");
                    fileContainer.CleanFilesIfEnd();
                    return fileContainer.ProgressToken;
                }
                fileContainer.OverlayFileItem.ReplaceOutputPathWithTempPath();
                LogManager.AddImageMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileContainer.OverlayFileItem.OutputFilePath), "End Overlay");
                IpfsDaemon.Instance.Queue(fileContainer.OverlayFileItem);
            }
            catch(Exception ex)
            {
                LogManager.AddImageMessage(LogLevel.Critical, "Exception non gérée overlay", "Exception", ex);
                fileContainer.CancelAll("Exception non gérée");
                fileContainer.CleanFilesIfEnd();
                return fileContainer.ProgressToken;
            }

            return fileContainer.ProgressToken;
        }
    }
}