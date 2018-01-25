using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Uploader.Managers.Common;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public class FfmpegProcessManager
    {
        private FileItem _fileItem;

        public FfmpegProcessManager(FileItem fileItem)
        {
            _fileItem = fileItem;
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
                LogManager.AddSpriteMessage(processStartInfo.FileName + " " + processStartInfo.Arguments, "Launch command");
            else
                LogManager.AddEncodingMessage(processStartInfo.FileName + " " + processStartInfo.Arguments, "Launch command");

            using(Process process = Process.Start(processStartInfo))
            {
                process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                process.BeginErrorReadLine();

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

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            Debug.WriteLine(output);
            
            const string durationMarkup = "  Duration: ";   // "  Duration: 00:01:42.11"
            const string progressMarkup = " time=";         // " time=00:01:42.08"

            // Si on ne connait pas la longueur totale de la vidéo
            if (!_fileItem.FileContainer.SourceFileItem.VideoDuration.HasValue)
            {
                if (output.StartsWith(durationMarkup) && output.Length >= durationMarkup.Length + 8)
                    _fileItem.FileContainer.SourceFileItem.VideoDuration = GetDurationInSeconds(output.Substring(durationMarkup.Length, 8));
                else
                    return;
            }

            // Récupérer la progression toutes les 1s
            if (_fileItem.EncodeProcess.LastTimeProgressChanged.HasValue && (DateTime.UtcNow - _fileItem.EncodeProcess.LastTimeProgressChanged.Value).TotalMilliseconds < 1000)
                return;

            if (!output.Contains(progressMarkup) || output.Length < (output.IndexOf(progressMarkup) + progressMarkup.Length + 8))
                return;

            Debug.WriteLine(Path.GetFileName(_fileItem.FileContainer.SourceFileItem.FilePath) + " : " + output);

            // Récupérer la progression d'encodage avec la durée d'encodage traitée
            int durationDone = GetDurationInSeconds(output.Substring(output.IndexOf(progressMarkup) + progressMarkup.Length, 8))??0;
            _fileItem.EncodeProcess.SetProgress(string.Format("{0:N2}%", (durationDone * 100.00 / (double) _fileItem.FileContainer.SourceFileItem.VideoDuration.Value)).Replace(',', '.'));
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