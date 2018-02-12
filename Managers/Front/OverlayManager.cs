using System;
using System.Diagnostics;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Front
{
    public static class OverlayManager
    {
        private static string _overlayImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "overlay.png");
        private static int _finalWidth = 210;
        private static int _finalHeight = 118;

        public static Guid ComputeOverlay(string sourceFilePath)
        {
            FileContainer fileContainer = FileContainer.NewOverlayContainer(sourceFilePath);

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();
            processStartInfo.UseShellExecute = false;
            processStartInfo.ErrorDialog = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            
            var sourceFile = fileContainer.SourceFileItem;
            try
            {
                LogManager.AddOverlayMessage("SourceFileName " + Path.GetFileName(sourceFile.SourceFilePath), "Start Crop");
                // resize + crop source image                
                processStartInfo.FileName = Path.Combine(GeneralSettings.Instance.ImageMagickPath, "convert");
                processStartInfo.Arguments = $"{Path.GetFileName(sourceFile.SourceFilePath)} -resize \"{_finalWidth}x{_finalHeight}^\" -gravity Center -crop {_finalWidth}x{_finalHeight}+0+0 {Path.GetFileName(sourceFile.TempFilePath)}";
                StartProcess(processStartInfo, 5000);                
                sourceFile.SetOutputFilePath(sourceFile.TempFilePath);
                LogManager.AddOverlayMessage("OutputFileName " + Path.GetFileName(sourceFile.OutputFilePath), "End Crop");
                IpfsDaemon.Instance.Queue(sourceFile);
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(sourceFile.SourceFilePath);
                TempFileManager.SafeDeleteTempFile(sourceFile.TempFilePath);
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
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
                LogManager.AddOverlayMessage("SourceFileName " + Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath), "Start Overlay");
                // watermark source image
                processStartInfo.FileName = Path.Combine(GeneralSettings.Instance.ImageMagickPath, "composite");
                processStartInfo.Arguments = $"-gravity NorthEast {_overlayImagePath} {Path.GetFileName(fileContainer.OverlayFileItem.SourceFilePath)} {Path.GetFileName(fileContainer.OverlayFileItem.TempFilePath)}";
                StartProcess(processStartInfo, 5000);
                fileContainer.OverlayFileItem.SetOutputFilePath(fileContainer.OverlayFileItem.TempFilePath);
                LogManager.AddOverlayMessage("OutputFileName " + Path.GetFileName(fileContainer.OverlayFileItem.OutputFilePath), "End Overlay");
                IpfsDaemon.Instance.Queue(fileContainer.OverlayFileItem);
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(fileContainer.OverlayFileItem.TempFilePath);
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
                return fileContainer.ProgressToken;
            }

            return fileContainer.ProgressToken;
        }

        private static void StartProcess(ProcessStartInfo processStartInfo, int timeout)
        {
            LogManager.AddOverlayMessage(processStartInfo.FileName + " " + processStartInfo.Arguments, "Launch command");
            using(Process process = Process.Start(processStartInfo))
            {
                bool success = process.WaitForExit(timeout);
                if (!success)
                {
                    throw new InvalidOperationException("Timeout : Le fichier n'a pas pu être encodé dans le temps imparti.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Le fichier n'a pas pu être encodé, erreur {process.ExitCode}.");
                }
            }
        }
    }
}