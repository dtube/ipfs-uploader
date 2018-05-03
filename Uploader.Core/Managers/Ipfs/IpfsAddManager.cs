using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Ipfs
{
    internal static class IpfsAddManager
    {
        private static FileItem currentFileItem;

        public static void Add(FileItem fileItem)
        {
            try
            {
                currentFileItem = fileItem;

                LogManager.AddIpfsMessage(LogLevel.Information, "FileName " + Path.GetFileName(currentFileItem.OutputFilePath), "Start");

                currentFileItem.IpfsHash = null;
                currentFileItem.IpfsProcess.StartProcessDateTime();

                // Send to ipfs and return hash from ipfs
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "ipfs";
                if (IpfsSettings.Instance.OnlyHash)
                    processStartInfo.Arguments = $"add --only-hash {Path.GetFileName(currentFileItem.OutputFilePath)}";
                else
                    processStartInfo.Arguments = $"add {Path.GetFileName(currentFileItem.OutputFilePath)}";

                if(IpfsSettings.Instance.VideoAndSpriteTrickleDag)                
                    if(currentFileItem.TypeFile == TypeFile.SourceVideo || 
                        currentFileItem.TypeFile == TypeFile.EncodedVideo || 
                        currentFileItem.TypeFile == TypeFile.SpriteVideo)
                        processStartInfo.Arguments = $"add -t {Path.GetFileName(currentFileItem.OutputFilePath)}";

                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();

                processStartInfo.UseShellExecute = false;
                processStartInfo.ErrorDialog = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                LogManager.AddIpfsMessage(LogLevel.Information, processStartInfo.FileName + " " + processStartInfo.Arguments, "Launch command");
                using(Process process = Process.Start(processStartInfo))
                {
                    process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                    process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool success = process.WaitForExit(IpfsSettings.Instance.IpfsTimeout * 1000);
                    if (!success)
                    {
                        throw new InvalidOperationException("Timeout : Le fichier n'a pas pu être envoyé à ipfs dans le temps imparti.");
                    }

                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Le fichier n'a pas pu être envoyé à ipfs, erreur {process.ExitCode}.");
                    }
                }

                currentFileItem.IpfsProcess.EndProcessDateTime();
                LogManager.AddIpfsMessage(LogLevel.Information, "Hash " + currentFileItem.IpfsHash + " / FileSize " + currentFileItem.FileSize, "End");
            }
            catch (Exception ex)
            {
                string message = "FileSize " + currentFileItem.FileSize + " / Progress " + currentFileItem.IpfsProcess.Progress;
                currentFileItem.IpfsProcess.SetErrorMessage("Exception non gérée", message, ex);
            }
        }

        private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            // Récupérer la progression toutes les 1s
            if (currentFileItem.IpfsProcess.LastTimeProgressChanged.HasValue && (DateTime.UtcNow - currentFileItem.IpfsProcess.LastTimeProgressChanged.Value).TotalMilliseconds < 1000)
                return;

            LogManager.AddIpfsMessage(LogLevel.Debug, Path.GetFileName(currentFileItem.OutputFilePath) + " : " + output, "DEBUG");

            // Récupérer la progression d'envoi, ex : 98.45%
            int startIndex = output.IndexOf('%') - 6;
            if(startIndex >= 0 && output.Length >= startIndex + 7)
            {
                string newProgress = output.Substring(startIndex, 7).Trim();
                currentFileItem.IpfsProcess.SetProgress(newProgress);
            }
        }

        private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            LogManager.AddIpfsMessage(LogLevel.Debug, Path.GetFileName(currentFileItem.OutputFilePath) + " : " + output, "DEBUG");

            if (output.StartsWith("added "))
            {
                currentFileItem.IpfsHash = output.Split(' ')[1];
            }
        }
    }
}