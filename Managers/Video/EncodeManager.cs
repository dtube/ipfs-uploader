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

                if(fileItem.ModeSprite)
                {
                    // todo calculer nb image/s
                    // si < 100s de vidéo -> 1 image/s
                    // sinon (nb secondes de la vidéo / 100) image/s
                    int frameRate = 1;

                    // extract x image/s de la video
                    string pattern = SpriteManager.GetPattern(newEncodedFilePath);
                    processStartInfo.Arguments = $"-y -i {sourceFilePath} -r {frameRate} -f image2 {pattern}";
                }
                else
                {
                    // todo calculer ratio
                    // si ratio > 16/9 => vidéo horizontale : garder largeur mais réduire hauteur
                    // sinon vidéo vertical : garder hauteur mais réduire largeur
                    string size;
                    if(videoSize == VideoSize.F720p)
                        size = "1280x720";
                    else if(videoSize == VideoSize.F480p)
                        size = "720x480";
                    else
                        throw new InvalidOperationException("le format doit etre défini");

                    //processStartInfo.Arguments = $"-i {sourceFilePath} {size} {newEncodedFilePath}";
                    processStartInfo.Arguments = $"-y -i {sourceFilePath} -crf 20 -vcodec libx264 -r 24 -s {size} -acodec aac -ar 44100 -ab 128k -ac 2 {newEncodedFilePath}";
                }

                processStartInfo.RedirectStandardError = true;
                processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();

                processStartInfo.UseShellExecute = false;
                processStartInfo.ErrorDialog = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                
                using(var process = Process.Start(processStartInfo))
                {
                    process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

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