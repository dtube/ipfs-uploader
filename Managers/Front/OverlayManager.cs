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

        public static Guid ComputeOverlay(string sourceFilePath, int? x = null, int? y = null)
        {
            FileContainer fileContainer = FileContainer.NewOverlayContainer(sourceFilePath);

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();
            processStartInfo.UseShellExecute = false;
            processStartInfo.ErrorDialog = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                // resize + crop source image
                string oldFilePath = fileContainer.SourceFileItem.FilePath;
                string outputFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png");
                processStartInfo.FileName = Path.Combine(FrontSettings.ImageMagickPath, "convert");
                processStartInfo.Arguments = $"{Path.GetFileName(fileContainer.SourceFileItem.FilePath)} -resize \"{_finalWidth}x{_finalHeight}^\" -gravity center -crop {_finalWidth}x{_finalHeight}+0+0 {Path.GetFileName(outputFilePath)}";
                StartProcess(processStartInfo, 5000);
                fileContainer.SourceFileItem.FilePath = outputFilePath;
                IpfsDaemon.Queue(fileContainer.SourceFileItem);
                TempFileManager.SafeDeleteTempFile(oldFilePath);

                // watermark source image
                outputFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png");
                processStartInfo.FileName = Path.Combine(FrontSettings.ImageMagickPath, "composite");
                processStartInfo.Arguments = $"-gravity Center {_overlayImagePath} {Path.GetFileName(fileContainer.SourceFileItem.FilePath)} {Path.GetFileName(outputFilePath)}";
                StartProcess(processStartInfo, 5000);
                fileContainer.OverlayFileItem.FilePath = outputFilePath;
                IpfsDaemon.Queue(fileContainer.OverlayFileItem);
            }
            catch(Exception ex)
            {
                LogManager.AddOverlayMessage(ex.ToString(), "Exception");
            }

            return fileContainer.ProgressToken;
        }

        private static void StartProcess(ProcessStartInfo processStartInfo, int timeout)
        {
            Debug.WriteLine("===> Launch : " + processStartInfo.FileName + " " + processStartInfo.Arguments);
            using(var process = Process.Start(processStartInfo))
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