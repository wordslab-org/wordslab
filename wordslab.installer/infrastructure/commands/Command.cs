using System.Diagnostics;
using System.Text;

namespace wordslab.installer.infrastructure.commands
{
    /// <summary>
    /// Executes a command line and collects console output, error, exit code 
    /// </summary>
    // Exceptions :
    // - FileNotFoundException      : command not found
    // - InvalidOperationException  : failed to execute command
    // - TimeoutException           : command did not exit before the timeout
    // - ArgumentException          : exception occured in output handler, error handler, exit code handler
    public static class Command
    {
        public static int Run(string command, string arguments="", int timeoutSec=10, bool unicodeEncoding = true, string workingDirectory="",
                              Action<string> outputHandler=null, Action<string> errorHandler=null, Action<int> exitCodeHandler=null)
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = command;
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                if (unicodeEncoding) proc.StartInfo.StandardOutputEncoding = Encoding.Unicode;
                proc.StartInfo.RedirectStandardOutput = true;
                if (unicodeEncoding) proc.StartInfo.StandardErrorEncoding = Encoding.Unicode;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.WorkingDirectory = workingDirectory;

                string output = null;
                string error = null;
                try
                {
                    // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.
                    proc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => { error += e.Data; });
                    proc.Start();  
                    proc.BeginErrorReadLine();
                    output = proc.StandardOutput.ReadToEnd();
                } 
                catch(System.ComponentModel.Win32Exception e)
                {
                    throw new FileNotFoundException($"Command {command} not found", command, e);
                }

                bool exitBeforeTimeout = true;
                try
                {
                    exitBeforeTimeout = proc.WaitForExit(timeoutSec * 1000);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Failed to execute command {command} {arguments}", e);
                }
                if (!exitBeforeTimeout)
                {
                    throw new TimeoutException($"Command {command} {arguments} did not exit before the timeout of {timeoutSec} sec");
                }
                                
                if(outputHandler != null) { try { outputHandler(output); } catch (Exception e) { throw new ArgumentException($"Exception occured in output handler of command {command}", e); } }
                if(errorHandler != null) { try { errorHandler(error); } catch (Exception e) { throw new ArgumentException($"Exception occured in error handler of command {command}", e); } }
                else { if(!String.IsNullOrEmpty(error)) { throw new InvalidOperationException($"Error while executing command {command} {arguments} : error output \"{error}\""); } }

                int exitCode = proc.ExitCode;
                if (exitCodeHandler != null) { try { exitCodeHandler(exitCode); } catch (Exception e) { throw new ArgumentException($"Exception occured in exit code handler of command {command}", e); } }
                else { if (exitCode != 0) { throw new InvalidOperationException($"Error while executing command {command} {arguments} : exitcode {exitCode} different of 0"); } }
                
                return exitCode;
            }
        }
    }
}
