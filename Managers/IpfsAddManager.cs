using System;
using System.Diagnostics;
using System.IO;
using IpfsUploader.Models;

namespace IpfsUploader.Managers
{
    public static class IpfsAddManager
    {
        private static FileItem currentFileItem;

        public static void Add(FileItem fileItem)
        {
            try
            {
                currentFileItem = fileItem;

                currentFileItem.IpfsHash = null;

                if(currentFileItem.VideoFormat == VideoFormat.Source)
                    currentFileItem.IpfsAddProgress = "0.00%";

                // Send to ipfs and return hash from ipfs
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "ipfs";
                processStartInfo.Arguments = $"add {currentFileItem.FilePath}";
                processStartInfo.RedirectStandardOutput = true;
                if(currentFileItem.VideoFormat == VideoFormat.Source)
                    processStartInfo.RedirectStandardError = true;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                
                using(Process process = new Process())
                {
                    process.StartInfo = processStartInfo;

                    process.OutputDataReceived += new DataReceivedEventHandler(IpfsOutputDataReceived);
                    if(currentFileItem.VideoFormat == VideoFormat.Source)
                        process.ErrorDataReceived += new DataReceivedEventHandler(IpfsErrorDataReceived);

                    process.Start();

                    process.BeginOutputReadLine();
                    if(currentFileItem.VideoFormat == VideoFormat.Source)
                        process.BeginErrorReadLine();

                    int timeout = 60 * 60 * 1000; //1h

                    bool success = process.WaitForExit(timeout);
                    if(!success)
                    {
                        throw new InvalidOperationException("Le fichier n'a pas pu être envoyé à ipfs en moins de 1 heure.");
                    }

                    if(process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"Le fichier n'a pas pu être envoyé à ipfs, erreur {process.ExitCode}.");
                    }
                }

                if(currentFileItem.VideoFormat == VideoFormat.Source)
                    currentFileItem.IpfsAddProgress = "100.00%";
            }
            catch(Exception ex)
            {
                currentFileItem.IpfsAddErrorMessage = ex.Message;
            }
        }

        private static void IpfsErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if(string.IsNullOrWhiteSpace(output))
                return;

            if(currentFileItem.IpfsAddLastTimeProgressChanged != null 
                && (DateTime.UtcNow - currentFileItem.IpfsAddLastTimeProgressChanged.Value).TotalMilliseconds < 500)
                return;

            Debug.WriteLine(Path.GetFileName(currentFileItem.FilePath) + " : " + output);

            //toutes les 500ms
            string newProgress = output.Substring(output.IndexOf('%') - 6, 7).Trim();

            currentFileItem.IpfsAddProgress = newProgress;
        }

        private static void IpfsOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if(string.IsNullOrWhiteSpace(output))
                return;

            if(output.StartsWith("added "))
            {
                currentFileItem.IpfsHash = output.Split(' ')[1];
            }
        }
    }
}