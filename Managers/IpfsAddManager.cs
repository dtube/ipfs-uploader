using System;
using System.Diagnostics;
using System.IO;
using IpfsUploader.Models;

namespace IpfsUploader.Managers
{
    public static class IpfsManager
    {
        private static FileItem currentFileItem;

        public static void Add(FileItem fileItem)
        {
            try
            {
                currentFileItem = fileItem;

                currentFileItem.IpfsHash = null;

                if(currentFileItem.VideoSize == VideoSize.Source)
                    currentFileItem.IpfsProgress = "0.00%";

                // Send to ipfs and return hash from ipfs
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "ipfs";                
                processStartInfo.Arguments = $"add {currentFileItem.FilePath}";
                
                processStartInfo.RedirectStandardOutput = true;
                if(currentFileItem.VideoSize == VideoSize.Source)
                    processStartInfo.RedirectStandardError = true;

                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                
                using(var process = new Process())
                {
                    process.StartInfo = processStartInfo;

                    process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                    if(currentFileItem.VideoSize == VideoSize.Source)
                        process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                    process.Start();

                    process.BeginOutputReadLine();
                    if(currentFileItem.VideoSize == VideoSize.Source)
                        process.BeginErrorReadLine();

                    int timeout = 5 * 60 * 60 * 1000; //5h

                    bool success = process.WaitForExit(timeout);
                    if(!success)
                    {
                        throw new InvalidOperationException("Le fichier n'a pas pu être envoyé à ipfs en moins de 5 heures.");
                    }

                    if(process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Le fichier n'a pas pu être envoyé à ipfs, erreur {process.ExitCode}.");
                    }
                }

                if(currentFileItem.VideoSize == VideoSize.Source)
                    currentFileItem.IpfsProgress = "100.00%";
            }
            catch(Exception ex)
            {
                currentFileItem.IpfsErrorMessage = ex.Message;
            }
        }

        private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if(string.IsNullOrWhiteSpace(output))
                return;

            if(currentFileItem.IpfsLastTimeProgressChanged.HasValue && (DateTime.UtcNow - currentFileItem.IpfsLastTimeProgressChanged.Value).TotalMilliseconds < 500)
                return;

            Debug.WriteLine(Path.GetFileName(currentFileItem.FilePath) + " : " + output);

            //toutes les 500ms
            string newProgress = output.Substring(output.IndexOf('%') - 6, 7).Trim();

            currentFileItem.IpfsProgress = newProgress;
        }

        private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if(string.IsNullOrWhiteSpace(output))
                return;

            Debug.WriteLine(Path.GetFileName(currentFileItem.FilePath) + " : " + output);

            if(output.StartsWith("added "))
            {
                currentFileItem.IpfsHash = output.Split(' ')[1];
            }
        }
    }
}