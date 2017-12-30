using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

using Uploader.Managers.Common;
using Uploader.Models;

namespace Uploader.Managers.Video
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

                FileItem sourceFile = currentFileItem.FileContainer.SourceFileItem;
                string sourceFilePath = sourceFile.FilePath;
                newEncodedFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".mp4");
                LogManager.AddEncodingMessage("FileName " + Path.GetFileName(newEncodedFilePath), "Start");
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

                // Récupérer la durée totale de la vidéo et sa résolution
                if (!sourceFile.VideoDuration.HasValue)
                {
                    string imageOutput = System.IO.Path.ChangeExtension(sourceFilePath, ".jpeg");
                    processStartInfo.Arguments = $"-y -i {sourceFilePath} -vf fps=1 -vframes 1 {imageOutput}";

                    StartProcess(processStartInfo, VideoSettings.EncodeGetOneImageTimeout);

                    using(Image image = Image.FromFile(imageOutput))
                    {
                        sourceFile.VideoWidth = image.Width;
                        sourceFile.VideoHeight = image.Height;
                    }
                    TempFileManager.SafeDeleteTempFile(imageOutput);
                }
                
                // Si durée totale de vidéo, largeur hauteur non récupéré, on ne peut pas continuer
                if ((sourceFile.VideoDuration??0) <= 0)
                    return false;                
                if ((sourceFile.VideoHeight??0) <= 0)
                    return false;
                if ((sourceFile.VideoHeight??0) <= 0)
                    return false;

                int duration = sourceFile.VideoDuration.Value;

                // Désactivation encoding et sprite si dépassement de la durée maximale
                if(duration > VideoSettings.MaxVideoDurationForEncoding)
                {
                    currentFileItem.EncodeErrorMessage = "Disable because duration reach the max limit.";
                    currentFileItem.FileContainer.EncodedFileItems.Clear();
                    currentFileItem.FileContainer.DeleteSpriteVideo();
                    return false;
                }

                switch (currentFileItem.TypeFile)
                {
                    case TypeFile.SpriteVideo:
                        {
                            int nbImages = VideoSettings.NbSpriteImages;
                            int heightSprite = VideoSettings.HeightSpriteImages;

                            // Calculer nb image/s
                            //  si < 100s de vidéo -> 1 image/s
                            //  sinon (nb secondes de la vidéo / 100) image/s
                            string frameRate = "1";
                            if (duration > nbImages)
                            {
                                frameRate = $"{nbImages}/{duration}";
                            }

                            int spriteWidth = GetWidth(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, heightSprite);
                            string sizeImageMax = $"scale={spriteWidth}:{heightSprite}";

                            // Extract frameRate image/s de la video
                            string pattern = GetPattern(newEncodedFilePath);
                            processStartInfo.Arguments = $"-y -i {sourceFilePath} -r {frameRate} -vf \"{sizeImageMax}\" -f image2 {pattern}";

                            StartProcess(processStartInfo, VideoSettings.EncodeGetImagesTimeout);
                            break;
                        }

                    case TypeFile.EncodedVideo:
                        {
                            string size;
                            switch (videoSize)
                            {
                                case VideoSize.F360p:
                                    {
                                        Tuple<int, int> finalSize = GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 640, 360);
                                        size = $"scale={finalSize.Item1}:{finalSize.Item2}";
                                        break;
                                    }

                                case VideoSize.F480p:
                                    {
                                        Tuple<int, int> finalSize = GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 854, 480);
                                        size = $"scale={finalSize.Item1}:{finalSize.Item2}";
                                        break;
                                    }

                                case VideoSize.F720p:
                                    {
                                        Tuple<int, int> finalSize = GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 1280, 720);
                                        size = $"scale={finalSize.Item1}:{finalSize.Item2}";
                                        break;
                                    }

                                case VideoSize.F1080p:
                                    {
                                        Tuple<int, int> finalSize = GetSize(sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value, 1920, 1080);
                                        size = $"scale={finalSize.Item1}:{finalSize.Item2}";
                                        break;
                                    }

                                default:
                                    throw new InvalidOperationException("Format non reconnu.");
                            }

                            string formatEncode = "libx264";
                            string maxRate = string.Empty;
                            if(VideoSettings.GpuEncodeMode)
                            {
                                formatEncode = "h264_nvenc";
                                switch (videoSize)
                                {
                                    case VideoSize.F360p:
                                        maxRate = "200k";
                                        break;
                                    case VideoSize.F480p:
                                        maxRate = "500k";
                                        break;
                                    case VideoSize.F720p:
                                        maxRate = "1000k";
                                        break;
                                    case VideoSize.F1080p:
                                        maxRate = "1600k";
                                        break;

                                    default:
                                        throw new InvalidOperationException("Format non reconnu.");
                                }
                            }

                            processStartInfo.Arguments = $"-y -i {sourceFilePath} -vcodec {formatEncode} -vf \"{size}\" -b:v {maxRate} -maxrate {maxRate} -bufsize {maxRate} -acodec aac {newEncodedFilePath}";

                            StartProcess(processStartInfo, VideoSettings.EncodeTimeout);
                            break;
                        }

                    default:
                        throw new InvalidOperationException("type non prévu");
                }

                currentFileItem.FilePath = newEncodedFilePath;
                currentFileItem.EncodeProgress = "100.00%";
                switch (currentFileItem.TypeFile)
                {
                    case TypeFile.SpriteVideo:
                        LogManager.AddEncodingMessage("Video Duration " + duration + " / SourceVideoFileSize " + currentFileItem.FileContainer.SourceFileItem.FileSize, "End Extract Images");
                        break;

                    case TypeFile.EncodedVideo:
                        LogManager.AddEncodingMessage("Video Duration " + duration + " / FileSize " + currentFileItem.FileSize + " / Format " + videoSize, "End Encoding");
                        break;

                    default:
                        throw new InvalidOperationException("type non prévu");
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.AddEncodingMessage("Video Duration " + currentFileItem.VideoDuration + " / FileSize " + currentFileItem.FileSize + " / Progress " + currentFileItem.EncodeProgress + " / Exception : " + ex, "Exception");
                currentFileItem.EncodeErrorMessage = ex.Message;

                TempFileManager.SafeDeleteTempFile(newEncodedFilePath);

                if(currentFileItem.VideoSize != VideoSize.Source)
                    TempFileManager.SafeDeleteTempFile(currentFileItem.FilePath);

                if (currentFileItem.TypeFile == TypeFile.SpriteVideo)
                {
                    string[] files = EncodeManager.GetListImageFrom(newEncodedFilePath); // récupération des images
                    TempFileManager.SafeDeleteTempFiles(files); // suppression des images
                }

                return false;
            }
        }

        private static void StartProcess(ProcessStartInfo processStartInfo, int timeout)
        {
            Debug.WriteLine("===> Launch : " + processStartInfo.FileName + " " + processStartInfo.Arguments);
            using(var process = Process.Start(processStartInfo))
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

        private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            Debug.WriteLine(output);
            
            const string durationMarkup = "  Duration: ";   // "  Duration: 00:01:42.11"
            const string progressMarkup = " time=";         // " time=00:01:42.08"

            // Si on ne connait pas la longueur totale de la vidéo
            if (!currentFileItem.FileContainer.SourceFileItem.VideoDuration.HasValue)
            {
                if (output.StartsWith(durationMarkup) && output.Length >= durationMarkup.Length + 8)
                    currentFileItem.FileContainer.SourceFileItem.VideoDuration = GetDurationInSeconds(output.Substring(durationMarkup.Length, 8));
                else
                    return;
            }

            // Récupérer la progression toutes les 1s
            if (currentFileItem.EncodeLastTimeProgressChanged.HasValue && (DateTime.UtcNow - currentFileItem.EncodeLastTimeProgressChanged.Value).TotalMilliseconds < 1000)
                return;

            if (!output.Contains(progressMarkup) || output.Length < (output.IndexOf(progressMarkup) + progressMarkup.Length + 8))
                return;

            Debug.WriteLine(Path.GetFileName(currentFileItem.FileContainer.SourceFileItem.FilePath) + " : " + output);

            // Récupérer la progression d'encodage avec la durée d'encodage traitée
            int durationDone = GetDurationInSeconds(output.Substring(output.IndexOf(progressMarkup) + progressMarkup.Length, 8))??0;
            currentFileItem.EncodeProgress = string.Format("{0:N2}%", (durationDone * 100.00 / (double) currentFileItem.FileContainer.SourceFileItem.VideoDuration.Value)).Replace(',', '.');
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

        private static string GetPattern(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath) + "-%03d.jpeg";
        }

        public static string[] GetListImageFrom(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return new string[0];

            string directoryName = Path.GetDirectoryName(filePath);
            if(directoryName == null)
                return new string[0];

            return Directory.EnumerateFiles(directoryName, Path.GetFileNameWithoutExtension(filePath) + "-*.jpeg").OrderBy(s => s).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="finalWidth"></param>
        /// <param name="finalHeight"></param>
        /// <returns>largeur, hauteur</returns>
        private static Tuple<int, int> GetSize(double width, double height, double finalWidth, double finalHeight)
        {
            //video verticale, garder hauteur finale, réduire largeur finale
            if(width / height < finalWidth / finalHeight)
                return new Tuple<int, int>(GetWidth(width, height, finalHeight), (int)finalHeight);
            
            // sinon garder largeur finale, réduire hauteur finale
            return new Tuple<int, int>((int)finalWidth, GetHeight(width, height, finalWidth));
        }
        
        private static int GetWidth(double width, double height, double finalHeight)
        {
            return GetPair((int)(finalHeight * width / height));
        }

        private static int GetHeight(double width, double height, double finalWidth)
        {
            return GetPair((int)(finalWidth * height / width));
        }

        private static int GetPair(int number)
        {
            return number % 2 == 0 ? number : number + 1;
        }
    }
}