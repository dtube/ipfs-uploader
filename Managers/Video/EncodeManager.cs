using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Uploader.Models;

namespace Uploader.Managers
{
    public static class EncodeManager
    {
        private static FileItem currentFileItem;

        public static bool Encode(FileItem fileItem)
        {
            string newEncodedFilePath = null;

            try
            {
                currentFileItem = fileItem;
                currentFileItem.EncodeProgress = "0.00%";
                totalDurationInSeconds = null;

                string sourceFilePath = currentFileItem.FileContainer.SourceFileItem.FilePath;
                newEncodedFilePath = TempFileManager.GetNewTempFilePath();
                newEncodedFilePath = Path.ChangeExtension(newEncodedFilePath, ".mp4");
                VideoSize videoSize = currentFileItem.VideoSize;

                Debug.WriteLine(Path.GetFileName(sourceFilePath) + " / " + videoSize);

                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "ffmpeg";

                string size;
                if(videoSize == VideoSize.F720p)
                    size = "1280x720";
                else if(videoSize == VideoSize.F480p)
                    size = "720x480";
                else
                    size = size = "720x480";

                //todo améliorer argument format audio ...
                processStartInfo.Arguments = $"-i {sourceFilePath} -s {size} {newEncodedFilePath}";

                processStartInfo.RedirectStandardError = true;

                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                
                using(var process = new Process())
                {
                    process.StartInfo = processStartInfo;

                    process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                    process.Start();

                    process.BeginErrorReadLine();

                    int timeout = 10 * 60 * 60 * 1000; //10h

                    bool success = process.WaitForExit(timeout);
                    if(!success)
                    {
                        throw new InvalidOperationException("Le fichier n'a pas pu être encodé en moins de 10 heures.");
                    }

                    if(process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Le fichier n'a pas pu être encodé, erreur {process.ExitCode}.");
                    }
                }

                currentFileItem.FilePath = newEncodedFilePath;
                currentFileItem.EncodeProgress = "100.00%";

                return true;
            }
            catch(Exception ex)
            {
                currentFileItem.EncodeErrorMessage = ex.Message;

                TempFileManager.SafeDeleteTempFile(newEncodedFilePath);
                return false;
            }
        }

        private static double? totalDurationInSeconds = null;

        private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if(string.IsNullOrWhiteSpace(output))
                return;

            // Get 00:01:42 in "Duration: 00:01:42.11, "
            if(!totalDurationInSeconds.HasValue && output.StartsWith("  Duration: "))
            {
                totalDurationInSeconds = GetDurationInSeconds(output.Substring("  Duration: ".Length, 8));
            }

            if(!totalDurationInSeconds.HasValue)
                return;

            if(currentFileItem.EncodeLastTimeProgressChanged.HasValue && (DateTime.UtcNow - currentFileItem.EncodeLastTimeProgressChanged.Value).TotalMilliseconds < 500)
                return;

            if(!output.Contains(" time="))
                return;

            //toutes les 500ms
            Debug.WriteLine(Path.GetFileName(currentFileItem.FileContainer.SourceFileItem.FilePath) + " : " + output);

            // Get 00:01:42 in " time=00:01:42.08 "
            int durationDone = GetDurationInSeconds(output.Substring(output.LastIndexOf(" time=") + " time=".Length, 8));

            currentFileItem.EncodeProgress = string.Format("{0:N2}%", (durationDone * 100.00 / totalDurationInSeconds.Value)).Replace(',','.');
        }

        private static int GetDurationInSeconds(string durationStr)
        {
            int[] durationTab = durationStr.Split(':').Select(v => Convert.ToInt32(v)).ToArray();
            return durationTab[0] * 3600 + durationTab[1] * 60 + durationTab[2];
        }
    }
}