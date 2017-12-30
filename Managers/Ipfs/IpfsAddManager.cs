using System;
using System.Diagnostics;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Models;

namespace Uploader.Managers.Ipfs
{
    public static class IpfsAddManager
    {
        private static FileItem currentFileItem;

        public static void Add(FileItem fileItem)
        {
            try
            {
                currentFileItem = fileItem;

                LogManager.AddIpfsMessage("FileName " + Path.GetFileName(currentFileItem.FilePath), "Start");

                currentFileItem.IpfsHash = null;
                currentFileItem.IpfsProgress = "0.00%";

                // Send to ipfs and return hash from ipfs
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "ipfs";
                processStartInfo.Arguments = $"add {currentFileItem.FilePath}";

                if(IpfsSettings.VideoAndSpriteTriggleDag)                
                    if(currentFileItem.TypeFile == TypeFile.SourceVideo || 
                        currentFileItem.TypeFile == TypeFile.EncodedVideo || 
                        currentFileItem.TypeFile == TypeFile.SpriteVideo)
                        processStartInfo.Arguments = $"add -t {currentFileItem.FilePath}";

                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();

                processStartInfo.UseShellExecute = false;
                processStartInfo.ErrorDialog = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                using(var process = Process.Start(processStartInfo))
                {
                    process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                    process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool success = process.WaitForExit(IpfsSettings.IpfsTimeout);
                    if (!success)
                    {
                        throw new InvalidOperationException("Timeout : Le fichier n'a pas pu être envoyé à ipfs dans le temps imparti.");
                    }

                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Le fichier n'a pas pu être envoyé à ipfs, erreur {process.ExitCode}.");
                    }
                }

                currentFileItem.IpfsProgress = "100.00%";
                LogManager.AddIpfsMessage("Hash " + currentFileItem.IpfsHash + " / FileSize " + currentFileItem.FileSize, "End");
            }
            catch (Exception ex)
            {
                LogManager.AddIpfsMessage("FileSize " + currentFileItem.FileSize + " / Progress " + currentFileItem.IpfsProgress + " / Exception " + ex, "Exception");
                currentFileItem.IpfsErrorMessage = ex.Message;
            }
        }

        private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            // Récupérer la progression toutes les 1s
            if (currentFileItem.IpfsLastTimeProgressChanged.HasValue && (DateTime.UtcNow - currentFileItem.IpfsLastTimeProgressChanged.Value).TotalMilliseconds < 1000)
                return;

            Debug.WriteLine(Path.GetFileName(currentFileItem.FilePath) + " : " + output);

            // Récupérer la progression d'envoi
            string newProgress = output.Substring(output.IndexOf('%') - 6, 7).Trim();
            currentFileItem.IpfsProgress = newProgress;
        }

        private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            Debug.WriteLine(Path.GetFileName(currentFileItem.FilePath) + " : " + output);

            if (output.StartsWith("added "))
            {
                currentFileItem.IpfsHash = output.Split(' ')[1];
            }
        }
    }
}