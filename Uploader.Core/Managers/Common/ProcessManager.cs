using System;
using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Uploader.Core.Managers.Common
{
    public class ProcessManager
    {
        private ProcessStartInfo _processStartInfo;

        private ILogger Logger { get; set; }

        public bool HasTimeout { get; private set; }

        public int ExitCode { get; private set; }

        public StringBuilder DataOutput { get; private set; } = new StringBuilder();

        public StringBuilder ErrorOutput { get; private set; } = new StringBuilder();

        public ProcessManager(string fileName, string arguments, ILogger logger)
        {
            if(string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if(string.IsNullOrWhiteSpace(arguments))
                throw new ArgumentNullException(nameof(arguments));
            if(logger == null)
                throw new ArgumentNullException(nameof(logger));

            Logger = logger;

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
            LogManager.Log(Logger, LogLevel.Information, _processStartInfo.FileName + " " + _processStartInfo.Arguments, "Launch command");

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
                        LogManager.Log(Logger, LogLevel.Error, $"Le process n'a pas pu être exécuté dans le temps imparti.", "Timeout");
                        return false;
                    }

                    if (ExitCode != 0)
                    {
                        LogManager.Log(Logger, LogLevel.Error, $"Le process n'a pas pu être exécuté correctement, erreur {process.ExitCode}.", "Error");
                        return false;
                    }

                    return true;
                }
            }
            catch(Exception ex)
            {
                LogManager.Log(Logger, LogLevel.Critical, $"Exception : Le process n'a pas pu être exécuté correctement : {ex}.", "Exception");
                return false;
            }            
        }

        public bool Launch(int timeout)
        {
            LogManager.Log(Logger, LogLevel.Information, _processStartInfo.FileName + " " + _processStartInfo.Arguments, "Launch command");

            try
            {
                using(Process process = Process.Start(_processStartInfo))
                {
                    bool success = process.WaitForExit(timeout * 1000);

                    DataOutput = DataOutput.Append(process.StandardOutput.ReadToEnd());
                    ErrorOutput = ErrorOutput.Append(process.StandardError.ReadToEnd());

                    LogManager.Log(Logger, LogLevel.Debug, DataOutput.ToString(), "DEBUG");
                    LogManager.Log(Logger, LogLevel.Debug, ErrorOutput.ToString(), "DEBUG");

                    HasTimeout = !success;
                    ExitCode = process.ExitCode;

                    if (HasTimeout)
                    {
                        LogManager.Log(Logger, LogLevel.Error, $"Le process n'a pas pu être exécuté dans le temps imparti.", "Timeout");
                        return false;
                    }

                    if (ExitCode != 0)
                    {
                        LogManager.Log(Logger, LogLevel.Error, $"Le process n'a pas pu être exécuté correctement, erreur {process.ExitCode}.", "Error");
                        return false;
                    }

                    return true;
                }
            }
            catch(Exception ex)
            {
                LogManager.Log(Logger, LogLevel.Critical, $"Exception : Le process n'a pas pu être exécuté correctement : {ex}.", "Exception");
                return false;
            }            
        }

        public bool LaunchWithoutTracking()
        {
            LogManager.Log(Logger, LogLevel.Information, _processStartInfo.FileName + " " + _processStartInfo.Arguments, "Launch command");

            try
            {
                using(Process process = Process.Start(_processStartInfo))
                {
                    return !process.HasExited || process.ExitCode == 0;
                }   
            }
            catch(Exception ex)
            {
                LogManager.Log(Logger, LogLevel.Critical, $"Exception : Le process n'a pas pu être exécuté correctement : {ex}.", "Exception");
                return false;
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            LogManager.Log(Logger, LogLevel.Debug, output, "DEBUG");
            DataOutput.AppendLine(output);
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if (string.IsNullOrWhiteSpace(output))
                return;

            LogManager.Log(Logger, LogLevel.Debug, output, "DEBUG");
            ErrorOutput.AppendLine(output);
        }
    }
}