using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

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
        public static int Run(string command, string arguments="", int timeoutSec=10, bool unicodeEncoding = true, string workingDirectory="", bool runAsAdmin=false,
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
                if (!IsRunningAsAdministrator())
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        proc.StartInfo.Verb = "runas";
                    } 
                    else
                    {
                        throw new InvalidOperationException("This operation needs admin privileges. Please retry this command with sudo.");
                    }
                }

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

        public static CommandOutputParser Output { get { return new CommandOutputParser(); } }


        [DllImport("libc")]
        private static extern uint geteuid();

        public static bool IsRunningAsAdministrator()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
            {
                return geteuid() == 0;
            }
        }
    }

    public class CommandOutputParser
    {
        private List<Tuple<Regex, Action<string>>> valueExtractors = new List<Tuple<Regex, Action<string>>>();

        private List<Tuple<Regex?, Regex, Func<Dictionary<string,string>,object>, List<object>>> listExtractors = new List<Tuple<Regex?, Regex, Func<Dictionary<string,string>,object>, List<object>>>();

        public CommandOutputParser GetValue(string valueRegex, Action<string> setProperty)
        {
            valueExtractors.Add(Tuple.Create(new Regex(valueRegex, RegexOptions.Multiline), setProperty));
            return this;
        }

        public CommandOutputParser GetList(string headerRegex, string lineRegex, Func<Dictionary<string,string>,object> createObjectFromMatches, List<object> resultList)
        {
            listExtractors.Add(Tuple.Create(headerRegex==null?null:new Regex(headerRegex), new Regex(lineRegex), createObjectFromMatches, resultList));
            return this;
        }

        public void Run(string output)
        {
            if (!String.IsNullOrEmpty(output)) {
                foreach(var valueExtractor in valueExtractors)
                {
                    var valueRegex = valueExtractor.Item1;
                    var setProperty = valueExtractor.Item2;
                    var match = valueRegex.Match(output);
                    if (match.Success)
                    {
                        setProperty(match.Groups[1].Value);
                    }
                }
                if (listExtractors.Count > 0)
                {
                    int activeHeaderIndex = -1;
                    var lines = output.Split(new char[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines) 
                    {
                        for (var i = 0; i < listExtractors.Count; i++)
                        {
                            var listExtractor = listExtractors[i];
                            var headerRegex = listExtractor.Item1;
                            var lineRegex = listExtractor.Item2;
                            var createObjectFromMatches = listExtractor.Item3;
                            var resultList = listExtractor.Item4;
                            if (headerRegex != null || headerRegex.IsMatch(line))
                            {
                                activeHeaderIndex = i;
                            }
                            else if (headerRegex == null || i == activeHeaderIndex)
                            {
                                var match = lineRegex.Match(line);
                                if(match.Success)
                                {
                                    var values = new Dictionary<string,string>();
                                    foreach(Group group in match.Groups)
                                    {
                                        values.Add(group.Name, group.Value);
                                    }
                                    var obj = createObjectFromMatches(values);
                                    resultList.Add(obj);
                                }
                            } 
                        }
                    }
                }
            }
        }
    }
}
