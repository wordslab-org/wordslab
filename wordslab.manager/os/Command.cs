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
        public static int Run(string command, string arguments="", int timeoutSec=10, bool killAfterTimeout=false, bool unicodeEncoding = false, string workingDirectory="", bool mustRunAsAdmin=false,
                              Action<string> outputHandler=null, Action<string> errorHandler=null, Action<int> exitCodeHandler=null)
        {
            using (Process process = new Process())
            {
                // Set process start info
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                if (unicodeEncoding) process.StartInfo.StandardOutputEncoding = Encoding.Unicode;
                process.StartInfo.RedirectStandardOutput = true;
                if (unicodeEncoding) process.StartInfo.StandardErrorEncoding = Encoding.Unicode;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WorkingDirectory = workingDirectory;
                if(!String.IsNullOrEmpty(workingDirectory) && !Directory.Exists(workingDirectory))
                {
                    throw new FileNotFoundException($"Working directory {workingDirectory} not found", workingDirectory);
                }
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

                // Start process in an asynchronous way
                bool isStarted = true;
                string output = null;
                var outputCloseEvent = new TaskCompletionSource<bool>();
                string error = null;
                var errorCloseEvent = new TaskCompletionSource<bool>();
                try
                {
                    // To be able to use a timeout, we need to read the standard output and error streams asynchronously
                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        if (e.Data == null) { outputCloseEvent.SetResult(true); }
                        else { if (output == null) { output = e.Data; } else { if (!String.IsNullOrEmpty(e.Data)) { output += Environment.NewLine + e.Data; } } }
                    });
                    process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => 
                    {
                        if (e.Data == null) { errorCloseEvent.SetResult(true); }
                        else { if (error == null) { error = e.Data; } else { if (!String.IsNullOrEmpty(e.Data)) { error += Environment.NewLine + e.Data; } } }
                    });
                    isStarted = process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                } 
                catch(System.ComponentModel.Win32Exception e)
                {
                    if (e.NativeErrorCode == 267)
                    {
                        throw new FileNotFoundException($"Working directory {workingDirectory} not found", workingDirectory, e);
                    }
                    else
                    {
                        throw new FileNotFoundException($"Command {command} not found", command, e);
                    }
                }

                // Wait for command exit and standard streams closing - until timeout
                if (isStarted)
                {
                    // Creat subtask to wait for process exit
                    Task<bool> waitForExitTask = Task.Run(() => process.WaitForExit(timeoutSec * 1000));
                    
                    // Create master task to wait for process exit AND closing all output streams
                    var processTask = Task.WhenAll(waitForExitTask, outputCloseEvent.Task, errorCloseEvent.Task);

                    // Execute all tasks
                    bool exitBeforeTimeout = true;
                    try
                    {
                        exitBeforeTimeout = Task.WhenAny(Task.Delay(timeoutSec * 1000), processTask).Result == processTask && waitForExitTask.Result;
                    }
                    catch (AggregateException ae)
                    {
                        throw new InvalidOperationException($"Failed to execute command {command} {arguments}", ae.Flatten().InnerExceptions.FirstOrDefault());
                    }

                    // Handle timeout
                    if (!exitBeforeTimeout)
                    {
                        if (killAfterTimeout)
                        {
                            try
                            {
                                // Kill hung process
                                process.Kill();
                            }
                            catch { /* ignored */ }
                        }
                        throw new TimeoutException($"Command {command} {arguments} did not exit before the timeout of {timeoutSec} sec");
                    }
                }
                                
                // Handle return value and standard streams output

                if(outputHandler != null) { try { outputHandler(output); } catch (Exception e) { throw new ArgumentException($"Exception occured in output handler of command {command}", e); } }
                if(errorHandler != null) { try { errorHandler(error); } catch (Exception e) { throw new ArgumentException($"Exception occured in error handler of command {command}", e); } }
                else { if(!String.IsNullOrEmpty(error)) { throw new InvalidOperationException($"Error while executing command {command} {arguments} : \"{error}\""); } }

                int exitCode = process.ExitCode;
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
                proc.StartInfo.UseShellExecute = showWindow;
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
                                             int timeoutSec = 10, bool runAsAdmin = false,
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
            
            using (Process process = new Process())
            {
                process.StartInfo.WorkingDirectory = scriptFile.Directory.FullName;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.FileName = "powershell.exe"; 
                    var scriptCommand = $"\"{scriptFile.FullName}\" {scriptArguments}";
                    var redirectOutputSyntax = $"2>&1 | Out-File -FilePath \"{outputLogFile}\" -Encoding 'oem'";
                    process.StartInfo.Arguments = $"-ExecutionPolicy Bypass {scriptCommand} {redirectOutputSyntax}; exit $LASTEXITCODE";
                }
                else
                {
                    process.StartInfo.FileName = "/bin/bash";
                    var scriptCommand = $"\"{scriptFile.FullName}\" {scriptArguments}";
                    var redirectOutputSyntax = $"2>&1 | tee \"{outputLogFile}\"";
                    process.StartInfo.ArgumentList.Add("-c");
                    process.StartInfo.ArgumentList.Add($"set -o pipefail; {scriptCommand} {redirectOutputSyntax}");                
                }                
                if (runAsAdmin)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.StartInfo.Verb = "runas";
                    }
                    else
                    {
                        if (!OS.IsRunningAsAdministrator())
                        {
                            throw new InvalidOperationException("This operation needs admin privileges. Please retry this command with sudo.");
                        }
                    }
                }

                try
                {
                    process.Start();
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    throw new FileNotFoundException($"Failed to launch script {scriptName}", scriptFile.FullName, e);
                }

                bool exitBeforeTimeout = true;
                try
                {
                    exitBeforeTimeout = process.WaitForExit(timeoutSec * 1000);
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
                if (File.Exists(outputLogFile))
                {
                    using (StreamReader srOut = new StreamReader(outputLogFile))
                    {
                        output = srOut.ReadToEnd();
                    }
                }
                if (OS.IsWindows && !String.IsNullOrEmpty(output))
                {
                    if (output.Contains("FullyQualifiedErrorId"))
                    {
                        throw new InvalidOperationException(output);
                    }
                }

                if (outputHandler != null) { try { outputHandler(output); } catch (Exception e) { throw new ArgumentException($"Exception occured in output handler of script {scriptName}", e); } }
                
                int exitCode = process.ExitCode;                

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
                    Regex lineRegex = null;
                    Func<Dictionary<string, string>, object> createObjectFromMatches = null;
                    IList<object> resultList = null;

                    // Default extractor
                    foreach (var listExtractor in listExtractors)
                    {
                        var headerRegex = listExtractor.Item1;
                        if (headerRegex == null)
                        {
                            lineRegex = listExtractor.Item2;
                            createObjectFromMatches = listExtractor.Item3;
                            resultList = listExtractor.Item4;
                            break;
                        }
                    }

                    // Scan all lines
                    var lines = output.Split(new char[]{'\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines) 
                    {
                        var isHeaderLine = false;
                        foreach (var listExtractor in listExtractors)
                        {
                            var headerRegex = listExtractor.Item1;   
                            if (headerRegex != null && headerRegex.IsMatch(line))
                            {
                                lineRegex = listExtractor.Item2;
                                createObjectFromMatches = listExtractor.Item3;
                                resultList = listExtractor.Item4;
                                isHeaderLine = true;
                                break;
                            }
                        }
                        if (!isHeaderLine && lineRegex != null)
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
