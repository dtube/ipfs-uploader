using System;
using System.Diagnostics;
using System.Text;

namespace Uploader.Core.Managers.Common
{
    public class ProcessManager
    {
        private ProcessStartInfo _processStartInfo;

        public bool HasTimeout { get; private set; }

        public int ExitCode { get; private set; }

        public StringBuilder DataOutput { get; private set; } = new StringBuilder();

        public StringBuilder ErrorOutput { get; private set; } = new StringBuilder();

        public ProcessManager(string fileName, string arguments)
        {
            if(string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if(string.IsNullOrWhiteSpace(arguments))
                throw new ArgumentNullException(nameof(arguments));

            _processStartInfo = new ProcessStartInfo();
            _processStartInfo.FileName = fileName;
            _processStartInfo.Arguments = arguments;

            _processStartInfo.RedirectStandardOutput = true;
            _processStartInfo.RedirectStandardError = true;
            _processStartInfo.WorkingDirectory = TempFileManager.GetTempDirectory();

            _processStartInfo.UseShellExecute = false;
            _processStartInfo.ErrorDialog = false;
            _processStartInfo.CreateNoWindow = true;
            _processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        public bool LaunchAsync(int timeout)
        {
            Debug.WriteLine(_processStartInfo.FileName + " " + _processStartInfo.Arguments, "Launch command");

            try
            {
                using(Process process = Process.Start(_processStartInfo))
                {
                    process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                    process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool success = process.WaitForExit(timeout * 1000);

                    HasTimeout = !success;
                    ExitCode = process.ExitCode;

                    if (HasTimeout)
                    {
                        Debug.WriteLine("Timeout : Le process n'a pas pu être exécuté dans le temps imparti.");
                        //log
                        return false;
                    }

                    if (ExitCode != 0)
                    {
                        Debug.WriteLine($"Error : Le process n'a pas pu être exécuté correctement, erreur {process.ExitCode}.");
                        //log
                        return false;
                    }

                    return true;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Exception : Le process n'a pas pu être exécuté correctement : {ex}.");
                //log
                return false;
            }            
        }

        public bool Launch(int timeout)
        {
            Debug.WriteLine(_processStartInfo.FileName + " " + _processStartInfo.Arguments, "Launch command");

            try
            {
                using(Process process = Process.Start(_processStartInfo))
                {
                    bool success = process.WaitForExit(timeout * 1000);

                    DataOutput = DataOutput.Append(process.StandardOutput.ReadToEnd());
                    ErrorOutput = ErrorOutput.Append(process.StandardError.ReadToEnd());

                    Debug.WriteLine(DataOutput);
                    Debug.WriteLine(ErrorOutput);

                    HasTimeout = !success;
                    ExitCode = process.ExitCode;

                    if (HasTimeout)
                    {
                        Debug.WriteLine("Timeout : Le process n'a pas pu être exécuté dans le temps imparti.");
                        //log
                        return false;
                    }

                    if (ExitCode != 0)
                    {
                        Debug.WriteLine($"Error : Le process n'a pas pu être exécuté correctement, erreur {process.ExitCode}.");
                        //log
                        return false;
                    }

                    return true;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Exception : Le process n'a pas pu être exécuté correctement : {ex}.");
                //log
                return false;
            }            
        }

        public bool LaunchWithoutTracking()
        {
            Debug.WriteLine(_processStartInfo.FileName + " " + _processStartInfo.Arguments, "Launch command");

            try
            {
                using(Process process = Process.Start(_processStartInfo))
                {
                    return !process.HasExited || process.ExitCode == 0;
                }   
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Exception : Le process n'a pas pu être exécuté correctement : {ex}.");
                //log
                return false;
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            Debug.WriteLine(output);
            DataOutput.AppendLine(output);
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            Debug.WriteLine(output);
            ErrorOutput.AppendLine(output);
        }
    }
}