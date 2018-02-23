using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal class FfmpegProcessManager
    {
        private FileItem _fileItem;
        private ProcessItem _processItem;

        public FfmpegProcessManager(FileItem fileItem, ProcessItem processItem)
        {
            if(fileItem == null)
                throw new ArgumentNullException(nameof(fileItem));
            if(processItem == null)
                throw new ArgumentNullException(nameof(processItem));

            _fileItem = fileItem;
            _processItem = processItem;
        }

        public void StartProcess(string arguments, int timeout)
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "ffmpeg";

            processStartInfo.RedirectStandardError = true;
            processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();

            processStartInfo.UseShellExecute = false;
            processStartInfo.ErrorDialog = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            processStartInfo.Arguments = arguments;

            if(_fileItem.TypeFile == TypeFile.SpriteVideo)
                LogManager.AddSpriteMessage(LogLevel.Information, processStartInfo.FileName + " " + processStartInfo.Arguments, "Launch command");
            else
                LogManager.AddEncodingMessage(LogLevel.Information, processStartInfo.FileName + " " + processStartInfo.Arguments, "Launch command");

            using(Process process = Process.Start(processStartInfo))
            {
                process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                process.BeginErrorReadLine();

                bool success = process.WaitForExit(timeout * 1000);
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

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            LogManager.AddEncodingMessage(LogLevel.Debug, output, "DEBUG");
            
            const string progressMarkup = " time=";         // " time=00:01:42.08"

            // Récupérer la progression toutes les 1s
            if (_processItem.LastTimeProgressChanged.HasValue && (DateTime.UtcNow - _processItem.LastTimeProgressChanged.Value).TotalMilliseconds < 1000)
                return;

            if (!output.Contains(progressMarkup) || output.Length < (output.IndexOf(progressMarkup) + progressMarkup.Length + 8))
                return;

            LogManager.AddEncodingMessage(LogLevel.Debug, Path.GetFileName(_fileItem.SourceFilePath) + " : " + output, "DEBUG");

            // Récupérer la progression d'encodage avec la durée d'encodage traitée
            int durationDone = GetDurationInSeconds(output.Substring(output.IndexOf(progressMarkup) + progressMarkup.Length, 8))??0;
            _processItem.SetProgress(string.Format("{0:N2}%", (durationDone * 100.00 / (double) _fileItem.FileContainer.SourceFileItem.VideoDuration.Value)).Replace(',', '.'));
        }

        private static int? GetDurationInSeconds(string durationStr)
        {
            try
            {
                int[] durationTab = durationStr.Split(':').Select(v => Convert.ToInt32(v)).ToArray();
                return durationTab[0] * 3600 + durationTab[1] * 60 + durationTab[2];
            }
            catch
            {
                return null;
            }
        }
    }
}