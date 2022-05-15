using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace wordslab.manager.os
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
        public static int Run(string command, string arguments="", int timeoutSec=10, bool unicodeEncoding = false, string workingDirectory="", bool mustRunAsAdmin=false,
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
                if (mustRunAsAdmin && !OS.IsRunningAsAdministrator())
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        throw new InvalidOperationException("This operation needs admin privileges. Please run this program as Administrator.");
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
                else { if(!String.IsNullOrEmpty(error)) { throw new InvalidOperationException($"Error while executing command {command} {arguments} : \"{error}\""); } }

                int exitCode = proc.ExitCode;
                if (exitCodeHandler != null) { try { exitCodeHandler(exitCode); } catch (Exception e) { throw new ArgumentException($"Exception occured in exit code handler of command {command}", e); } }
                else { if (exitCode != 0) { throw new InvalidOperationException($"Error while executing command {command} {arguments} : exitcode {exitCode} different of 0 (output=\"{output}\") (error=\"{error}\")"); } }
                
                return exitCode;
            }
        }

        public static int LaunchAndForget(string command, string arguments = "", bool showWindow = true)
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = command;
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.CreateNoWindow = !showWindow;
                try
                {
                    proc.Start();
                    return proc.Id;
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    throw new FileNotFoundException($"Command {command} not found", command, e);
                }
                return -1;
            }
        }

        private static FileInfo GetScriptFile(string scriptsDirectory, string scriptName)
        {
            var scriptPath = Path.Combine(scriptsDirectory, scriptName);
            var scriptFile = new FileInfo(scriptPath);
            if (!scriptFile.Exists)
            {
                throw new FileNotFoundException($"Script file {scriptFile.FullName} not found", scriptFile.FullName);
            }
            return scriptFile;
        }

        public static string GetScriptContent(string scriptsDirectory, string scriptName)
        {
            var scriptFile = GetScriptFile(scriptsDirectory, scriptName);
            using (StreamReader sr = new StreamReader(scriptFile.FullName)) { return sr.ReadToEnd(); }
        }

        public static int ExecuteShellScript(string scriptsDirectory, string scriptName, string scriptArguments, string logsDirectory,
                                             int timeoutSec = 10, bool runAsAdmin = false, bool usePowershell = false, string shellLauncher = null,
                                             Action<string> outputHandler = null, Action<int> exitCodeHandler = null)
        {
            var scriptFile = GetScriptFile(scriptsDirectory, scriptName);

            var logDir = new DirectoryInfo(logsDirectory);
            if (!logDir.Exists)
            {
                try
                {
                    logDir.Create();
                }
                catch (Exception ex)
                {
                    throw new IOException($"Failed to create directory to log script output : {logsDirectory}", ex);
                }
            }
            string logFilePrefix = scriptFile.Name + $".{DateTime.Now.ToString("s").Replace(':','-')}";
            string outputLogFile = Path.Combine(logDir.FullName, logFilePrefix + ".output.txt");

            string redirectOutputSyntax;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (shellLauncher == null)
                {
                    if(usePowershell) { shellLauncher = "powershell.exe"; }
                    else              { shellLauncher = "cmd.exe";        }
                    
                }
                if (String.Equals(shellLauncher, "powershell.exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    redirectOutputSyntax = $"| Tee-Object -FilePath \"{outputLogFile}\"";
                } 
                else
                {
                    redirectOutputSyntax = $"> \"{outputLogFile}\" 2>&1";
                }
            }
            else
            {
                if (shellLauncher == null) shellLauncher = "bash";
                redirectOutputSyntax = $"2>&1 | tee \"{outputLogFile}\"";
            }

            using (Process proc = new Process())
            {
                proc.StartInfo.WorkingDirectory = scriptFile.Directory.FullName;
                proc.StartInfo.FileName = shellLauncher;
                proc.StartInfo.Arguments = $"\"{scriptFile.FullName}\" {scriptArguments} {redirectOutputSyntax}";
                if(shellLauncher == "cmd.exe")
                {
                    proc.StartInfo.Arguments = "/C " + proc.StartInfo.Arguments;
                }
                if (runAsAdmin)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        proc.StartInfo.UseShellExecute = true;
                        proc.StartInfo.Verb = "runas";
                    }
                    else
                    {
                        if (!OS.IsRunningAsAdministrator())
                        {
                            throw new InvalidOperationException("This operation needs admin privileges. Please retry this command with sudo.");
                        }
                    }
                }
                else
                {
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow = true;
                }

                try
                {
                    proc.Start();
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    throw new FileNotFoundException($"Failed to launch script {scriptName}", scriptFile.FullName, e);
                }

                bool exitBeforeTimeout = true;
                try
                {
                    exitBeforeTimeout = proc.WaitForExit(timeoutSec * 1000);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Failed to execute script {scriptName} {scriptArguments}", e);
                }
                if (!exitBeforeTimeout)
                {
                    throw new TimeoutException($"Script {scriptName} {scriptArguments} did not exit before the timeout of {timeoutSec} sec");
                }

                string output = "";
                using(StreamReader srOut = new StreamReader(outputLogFile))
                {
                    output = srOut.ReadToEnd();
                }

                if (outputHandler != null) { try { outputHandler(output); } catch (Exception e) { throw new ArgumentException($"Exception occured in output handler of script {scriptName}", e); } }
                
                int exitCode = proc.ExitCode;
                if (exitCodeHandler != null) { try { exitCodeHandler(exitCode); } catch (Exception e) { throw new ArgumentException($"Exception occured in exit code handler of script {scriptName}", e); } }
                else { if (exitCode != 0) { throw new InvalidOperationException($"Error while executing script {scriptName} {scriptArguments} : exitcode {exitCode} different of 0"); } }

                return exitCode;
            }
        }

        public static CommandOutputParser Output { get { return new CommandOutputParser(); } }
        public static CommandOutputParser Error { get { return new CommandOutputParser(); } }

        // On Linux, commands consist sometimes in reading virtual files (ex: /proc/cpuinfo or /proc/stat) 
        public static string[] TryReadFileLines(string path)
        {
            try
            {
                return File.ReadAllLines(path);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }

    public class CommandOutputParser
    {
        private List<Tuple<Regex, Action<string>>> valueExtractors = new List<Tuple<Regex, Action<string>>>();

        private List<Tuple<Regex?, Regex, Func<Dictionary<string,string>,object>, IList<object>>> listExtractors = new List<Tuple<Regex?, Regex, Func<Dictionary<string,string>,object>, IList<object>>>();

        public CommandOutputParser GetValue(string valueRegex, Action<string> setProperty)
        {
            valueExtractors.Add(Tuple.Create(new Regex(valueRegex, RegexOptions.Multiline), setProperty));
            return this;
        }

        public CommandOutputParser GetList(string headerRegex, string lineRegex, Func<Dictionary<string,string>,object> createObjectFromMatches, IList<object> resultList)
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
                            if (headerRegex != null && headerRegex.IsMatch(line))
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
