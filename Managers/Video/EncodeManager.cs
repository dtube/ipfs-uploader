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

                string sourceFilePath = currentFileItem.FileContainer.SourceFileItem.FilePath;
                newEncodedFilePath = TempFileManager.GetNewTempFilePath();
                newEncodedFilePath = Path.ChangeExtension(newEncodedFilePath, ".mp4");
                VideoSize videoSize = currentFileItem.VideoSize;

                Debug.WriteLine(Path.GetFileName(sourceFilePath) + " / " + videoSize);

                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "ffmpeg";

                processStartInfo.RedirectStandardError = true;
                processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();

                processStartInfo.UseShellExecute = false;
                processStartInfo.ErrorDialog = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // global options
                // -y loglevel error    : overwrite outputfile

                // input File options

                // input File
                // -i {sourceFilePath}  : chemin fichier entrant

                // output File options
                // -b:v 64k -bufsize 64k: video bitrate of the output file to 64 kbit/s
                // -crf 20              : ??
                // -vcodec libx264      : choix codec video libx264 (ou -c:v libx264 ou -codec:v libx264)
                // -r 24                : frame rate 24fps
                // -s {size}            : taille de la video sortante
                // -f image2            : format vidéo sortant

                // -acodec aac          : choix codec audio aac (ou -c:a aac ou -codec:a aac)
                // -ar 44100            : 
                // -ab 128k             : 
                // -ac 2                : number of audio channel

                // output File
                // {newEncodedFilePath} : chemin fichier sortant (foo-%03d.jpeg)

                // Récupérer la durée totale de la vidéo
                if (!currentFileItem.FileContainer.SourceFileItem.VideoDuration.HasValue)
                {
                    processStartInfo.Arguments = $"-i {sourceFilePath}";

                    try
                    {
                        StartProcess(processStartInfo, 30 * 1000); // 30 secondes
                    }
                    catch
                    {}
                }

                // si durée totale de vidéo non récupérer, on ne peut pas continuer
                if ((currentFileItem.FileContainer.SourceFileItem.VideoDuration??0) <= 0)
                {
                    return false;
                }

                int duration = currentFileItem.FileContainer.SourceFileItem.VideoDuration.Value;

                if (currentFileItem.ModeSprite)
                {
                    // calculer nb image/s
                    //  si < 100s de vidéo -> 1 image/s
                    //  sinon (nb secondes de la vidéo / 100) image/s
                    int frameRate = 1;
                    if (duration > 100)
                    {
                        frameRate = duration / 100;
                    }

                    // extract frameRate image/s de la video
                    string pattern = SpriteManager.GetPattern(newEncodedFilePath);
                    processStartInfo.Arguments = $"-y -i {sourceFilePath} -r {frameRate} -f image2 {pattern}";

                    StartProcess(processStartInfo, 2 * 60 * 1000); // 2 minutes
                }
                else
                {
                    string size;
                    if (videoSize == VideoSize.F720p)
                        size = "1280x720";
                    else if (videoSize == VideoSize.F480p)
                        size = "720x480";
                    else
                        throw new InvalidOperationException("le format doit etre défini");

                    processStartInfo.Arguments = $"-y -i {sourceFilePath} -crf 20 -vcodec libx264 -r 24 -s {size} -acodec aac -ar 44100 -ab 128k -ac 2 {newEncodedFilePath}";

                    StartProcess(processStartInfo, 10 * 60 * 60 * 1000); // 10 heures
                }

                currentFileItem.FilePath = newEncodedFilePath;
                currentFileItem.EncodeProgress = "100.00%";

                return true;
            }
            catch (Exception ex)
            {
                currentFileItem.EncodeErrorMessage = ex.Message;

                TempFileManager.SafeDeleteTempFile(newEncodedFilePath);
                return false;
            }
        }

        private static void StartProcess(ProcessStartInfo processStartInfo, int timeout)
        {
            using(var process = Process.Start(processStartInfo))
            {
                process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                process.BeginErrorReadLine();

                bool success = process.WaitForExit(timeout);
                if (!success)
                {
                    throw new InvalidOperationException("Le fichier n'a pas pu être encodé en moins de 10 heures.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Le fichier n'a pas pu être encodé, erreur {process.ExitCode}.");
                }
            }
        }

        private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            Debug.WriteLine(output);
            
            const string durationMarkup = "  Duration: "; // "  Duration: 00:01:42.11"
            const string progressMarkup = " time="; // " time=00:01:42.08"

            // Si on ne connait pas la longueur totale de la vidéo
            if (!currentFileItem.FileContainer.SourceFileItem.VideoDuration.HasValue)
            {
                if (output.StartsWith(durationMarkup))
                    currentFileItem.FileContainer.SourceFileItem.VideoDuration = GetDurationInSeconds(output.Substring(durationMarkup.Length, 8));
                else
                    return;
            }

            // récupérer la progression toutes les 500ms
            if (currentFileItem.EncodeLastTimeProgressChanged.HasValue && (DateTime.UtcNow - currentFileItem.EncodeLastTimeProgressChanged.Value).TotalMilliseconds < 500)
                return;

            if (!output.Contains(progressMarkup))
                return;

            Debug.WriteLine(Path.GetFileName(currentFileItem.FileContainer.SourceFileItem.FilePath) + " : " + output);

            // Récupérer la progression d'encodage avec la durée d'encodage traitée
            int durationDone = GetDurationInSeconds(output.Substring(output.IndexOf(progressMarkup) + progressMarkup.Length, 8));

            currentFileItem.EncodeProgress = string.Format("{0:N2}%", (durationDone * 100.00 / (double) currentFileItem.FileContainer.SourceFileItem.VideoDuration.Value)).Replace(',', '.');
        }

        private static int GetDurationInSeconds(string durationStr)
        {
            int[] durationTab = durationStr.Split(':').Select(v => Convert.ToInt32(v)).ToArray();
            return durationTab[0] * 3600 + durationTab[1] * 60 + durationTab[2];
        }
    }
}