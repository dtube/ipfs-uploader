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

            string oldFilePath = fileContainer.SourceFileItem.FilePath;
            string outputFilePath = null;
            try
            {
                LogManager.AddOverlayMessage("SourceFileName " + Path.GetFileName(oldFilePath), "Start Crop");
                // resize + crop source image                
                outputFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png");
                processStartInfo.FileName = Path.Combine(FrontSettings.ImageMagickPath, "convert");
                processStartInfo.Arguments = $"{Path.GetFileName(fileContainer.SourceFileItem.FilePath)} -resize \"{_finalWidth}x{_finalHeight}^\" -gravity Center -crop {_finalWidth}x{_finalHeight}+0+0 {Path.GetFileName(outputFilePath)}";
                StartProcess(processStartInfo, 5000);
                fileContainer.SourceFileItem.FilePath = outputFilePath;
                LogManager.AddOverlayMessage("OutputFileName " + Path.GetFileName(outputFilePath), "End Crop");
                IpfsDaemon.Queue(fileContainer.SourceFileItem);
            }
            catch(Exception ex)
            {                
                TempFileManager.SafeDeleteTempFile(outputFilePath);
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
                return fileContainer.ProgressToken;
            }
            finally
            {
                TempFileManager.SafeDeleteTempFile(oldFilePath);
            }

            try
            {
                LogManager.AddOverlayMessage("SourceFileName " + Path.GetFileName(fileContainer.SourceFileItem.FilePath), "Start Overlay");
                // watermark source image
                outputFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png");
                processStartInfo.FileName = Path.Combine(FrontSettings.ImageMagickPath, "composite");
                processStartInfo.Arguments = $"-gravity NorthEast {_overlayImagePath} {Path.GetFileName(fileContainer.SourceFileItem.FilePath)} {Path.GetFileName(outputFilePath)}";
                StartProcess(processStartInfo, 5000);
                fileContainer.OverlayFileItem.FilePath = outputFilePath;
                LogManager.AddOverlayMessage("OutputFileName " + Path.GetFileName(outputFilePath), "End Overlay");
                IpfsDaemon.Queue(fileContainer.OverlayFileItem);
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(outputFilePath);
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