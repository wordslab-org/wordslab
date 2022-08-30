using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class CommandTests
    {
        [TestMethod]
        public void T01_TestRun()
        {
            // Program does not exists
            Exception expectedEx = null;
            try
            {
                Command.Run("toto");
            }
            catch(Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
            Assert.IsTrue(expectedEx.Message.Contains("Command"));

            var command1 = OS.IsWindows ? "ipconfig" : "date";
            var params1ok = OS.IsWindows ? "/all" : "--help";
            var params1ko = OS.IsWindows ? "/toto" : "--toto";

            // Return code
            var rc = Command.Run(command1);
            Assert.IsTrue(rc == 0);

            string output = null;
            string error = null;
            int exitcode = -1;
            
            // Handlers
            rc = Command.Run(command1, outputHandler:o => output=o, errorHandler:e => error=e, exitCodeHandler:c => exitcode=c);
            Assert.IsTrue(rc == 0);
            Assert.IsTrue(exitcode == 0);
            Assert.IsTrue(output != null && output.Length > 10);
            Assert.IsTrue(error == null);

            // Parameters
            int previousLength = output.Length;
            Command.Run(command1, params1ok, outputHandler: o => output = o);
            Assert.IsTrue(output.Length > previousLength);

            // Parameter does not exist
            expectedEx = null;
            try
            {
                Command.Run(command1, params1ko);
            }
            catch(Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is InvalidOperationException);

            exitcode = -1;
            error = null;
            rc = Command.Run(command1, params1ko, errorHandler: e => error = e,  exitCodeHandler: c => exitcode = c);
            Assert.IsTrue(rc > 0);
            Assert.IsTrue(exitcode > 0);

            var command2 = OS.IsWindows ? "cmd" : "ls";
            var params2_1 = OS.IsWindows ? "/C dir" : ".";
            var params2_2 = OS.IsWindows ? "/C dir c:\\Users" : "/usr";
            var workdirok = OS.IsWindows ? "c:\\Users" : "/usr";
            var workdirko = OS.IsWindows ? "c:\\toto" : "/toto";

            // Working directory
            string outputDir1 = null;
            Command.Run(command2, params2_1, outputHandler: o => outputDir1 = o);
            Assert.IsTrue(!string.IsNullOrEmpty(outputDir1));
            
            string outputDir2 = null;
            Command.Run(command2, params2_2, outputHandler: o => outputDir2 = o);
            Assert.IsTrue(!string.IsNullOrEmpty(outputDir2));

            string outputDir3 = null;
            Command.Run(command2, params2_1, workingDirectory: workdirok, outputHandler: o => outputDir3 = o);
            Assert.IsTrue(!string.IsNullOrEmpty(outputDir3));

            Assert.IsTrue(outputDir1 != outputDir2);
            Assert.IsTrue(outputDir2.Substring(0,outputDir2.Length-30) == outputDir3.Substring(0,outputDir3.Length-30));

            // Working directory does not exist
            expectedEx = null;
            try
            {
                Command.Run(command2, params2_1, workingDirectory: workdirko);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
            Assert.IsTrue(expectedEx.Message.Contains("directory"));

            var command3 = OS.IsWindows ? "timeout" : "sleep";

            // Timeout
            Command.Run(command3, "2", timeoutSec: 3);

            expectedEx = null;
            try
            {
                Command.Run(command3, "2", timeoutSec: 1);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is TimeoutException);

            var params2_admin = OS.IsWindows ? "/C dir c:\\Users\\julie" : "/root";

            // Must run as admin
            expectedEx = null;
            try
            {
                Command.Run(command2, params2_admin, mustRunAsAdmin:true);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is InvalidOperationException);
            Assert.IsTrue(expectedEx.Message.Contains("privileges"));

            var command4 = OS.IsWindows ? "cmd" : "echo";
            var params4 = OS.IsWindows ? "/C echo énâtü😀" : "énâtü😀";

            // Unicode output decoding
            output = null;
            Command.Run(command4, params4, outputHandler: o => output = o);
            if(OS.IsWindows)
            {
                Assert.IsTrue(output == "énâtü??");
            } 
            else
            {
                Assert.IsTrue(output == params4);
            }

            if(OS.IsWindows)
            {
                var command5 = "wsl";
                var params5 = "--status";

                output = null;
                Command.Run(command5, params5, outputHandler: o => output = o);
                Assert.IsTrue(output.IndexOf('\0') == 1);

                output = null;
                Command.Run(command5, params5, unicodeEncoding:true, outputHandler: o => output = o);
                Assert.IsTrue(output.IndexOf('\0') == -1);
            }
        }

        [TestMethod]
        public void T02_TestLaunchAndForget()
        {
            var command = OS.IsWindows ? "timeout" : "sleep";

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Command.LaunchAndForget(command, "2", showWindow: false);
            watch.Stop();
            Assert.IsTrue(watch.ElapsedMilliseconds < 1000);
        }
              
        [TestMethod]
        public void T03_TestGetScriptContent()
        {
            // File found
            var content = Command.GetScriptContent(AppContext.BaseDirectory, "appsettings.json");
            Assert.IsTrue(!String.IsNullOrEmpty(content) && content.Trim().EndsWith("}"));

            // File not found
            Exception expectedEx = null;
            try
            {
                content = Command.GetScriptContent(AppContext.BaseDirectory, "toto");
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
            Assert.IsTrue(expectedEx.Message.Contains(Path.Combine(AppContext.BaseDirectory, "toto")));
        }

        [TestMethod]
        public void T04_TestExecuteShellScript()
        {
            var scriptsDir = Path.Combine(AppContext.BaseDirectory, "os", "scripts");
            var logsDir = Path.Combine(AppContext.BaseDirectory, "test-logs");
            
            var timeoutScript = OS.IsWindows ? "timeout.ps1" : "timeout.sh";
            var noutputScript = OS.IsWindows ? "nooutput.ps1" : "nooutput.sh";
            var totoScript = OS.IsWindows ? "toto.ps1" : "toto.sh";
            var errorScript = OS.IsWindows ? "error.ps1" : "error.sh";
            var adminScript = OS.IsWindows ? "admin.ps1" : "admin.sh";

            // Exit code 0 & output
            string output = null;
            var exitCode = Command.ExecuteShellScript(scriptsDir, timeoutScript, "1 0", logsDir, outputHandler: o => output = o);
            Assert.IsTrue(exitCode == 0);
            Assert.IsTrue(!String.IsNullOrEmpty(output) && output.Length>20);

            // Exit code 2 & output
            Command.ExecuteShellScript(scriptsDir, timeoutScript, "1 2", logsDir,outputHandler: o => output = o, exitCodeHandler: c => exitCode = c);
            Assert.IsTrue(exitCode == 2);
            Assert.IsTrue(!String.IsNullOrEmpty(output) && output.Length > 20);

            // Timeout
            Exception expectedEx = null;
            try
            {
                Command.ExecuteShellScript(scriptsDir, timeoutScript, "2 0", logsDir, timeoutSec: 1);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is TimeoutException);

            // No output
            Command.ExecuteShellScript(scriptsDir, noutputScript, "0", logsDir, outputHandler: o => output = o);
            Assert.IsTrue(output == String.Empty);

            // Script name error
            expectedEx = null;
            try
            {
                Command.ExecuteShellScript(scriptsDir, totoScript, null, logsDir);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);

            // Script execution error
            expectedEx = null;
            try
            {
                Command.ExecuteShellScript(scriptsDir, errorScript, null, logsDir);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is InvalidOperationException);

            // Script missing parameter
            expectedEx = null;
            try
            {
                Command.ExecuteShellScript(scriptsDir, timeoutScript, null, logsDir);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is InvalidOperationException);

            // Not admin
            expectedEx = null;
            try
            {
                Command.ExecuteShellScript(scriptsDir, adminScript, null, logsDir);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is InvalidOperationException);
            if(OS.IsWindows)
            {
                Assert.IsTrue(expectedEx.Message.StartsWith("Get-WindowsOptionalFeature"));
            }

            // Run as admin
            try 
            {                
                Command.ExecuteShellScript(scriptsDir, adminScript, null, logsDir, runAsAdmin: true, outputHandler: o => output=o);
                if(OS.IsWindows) 
                {
                    Assert.IsTrue(output.Contains("Microsoft-Windows-Subsystem-Linux"));
                }
            }
            catch(Exception e)
            {
                if(OS.IsWindows) { throw e; }
                else
                {
                    Assert.IsTrue(e is InvalidOperationException);
                    Assert.IsTrue(e.Message.Contains("sudo"));
                }
            }

            Directory.Delete(logsDir, true);
        }

        [TestMethod]
        public void T05_TestCommandOutputParser()
        {
            var outputsDir = Path.Combine(AppContext.BaseDirectory, "os", "outputs");

            // Single value
            var output = File.ReadAllText(Path.Combine(outputsDir, "values.txt"));
            string result = null;
            var parser = Command.Output.GetValue("Lim. RAM: (\\d+)Mi", r => result = r);
            parser.Run(output);
            Assert.IsTrue(result == "170");

            // Multiple values
            string result2 = null;
            string result3 = null;
            parser = parser.GetValue("Req. CPU: (\\d+)m", r => result2 = r);
            parser = parser.GetValue("Lim. Eph. DISK: (\\d+)Mi", r => result3 = r);
            parser.Run(output);
            Assert.IsTrue(result == "170");
            Assert.IsTrue(result2 == "100");
            Assert.IsTrue(result3 == "768");

            // With one not found
            string result4 = null;
            parser = parser.GetValue("Lim. CPU: (\\d+)", r => result4 = r);
            parser.Run(output);
            Assert.IsTrue(result == "170");
            Assert.IsTrue(result2 == "100");
            Assert.IsTrue(result3 == "768");
            Assert.IsNull(result4);

            // Simple table
            var table = File.ReadAllText(Path.Combine(outputsDir, "table.txt"));
            var results = new List<object>();
            var parsert = Command.Output.GetList(null, "(?<name>[^\\s]+)\\s+(?<ready>[^\\s]+)\\s+(?<status>[^\\s]+)\\s+(?<restarts>\\d+)\\s+(?<age>[^\\s]+)\\s+(?<component>[^\\s]+)\\s*", o => o, results);
            parsert.Run(table);
            Assert.IsTrue(results.Count == 5);
            Assert.IsTrue(((Dictionary<string,string>)results[0]).Count == 7);

            // Multiple tables
            var mtable = File.ReadAllText(Path.Combine(outputsDir, "multitable.txt"));
            var results1 = new List<object>();
            var parserm = Command.Output.GetList("NAME\\s+STATUS", "(?<name>[^\\s]+)\\s+(?<status>[^\\s]+)\\s+(?<roles>[^\\s]+)\\s+(?<age>[^\\s]+)\\s+(?<version>[^\\s]+)\\s+(?<instancetype>[^\\s]+)\\s*", o => o, results1);
            var results2 = new List<object>();
            parserm =parserm.GetList("NAME\\s+READY", "(?<name>[^\\s]+)\\s+(?<ready>[^\\s]+)\\s+(?<status>[^\\s]+)\\s+(?<restarts>[^\\s]+)\\s+(?<age>[^\\s]+)\\s+(?<component>[^\\s]+)\\s*", o => o, results2);
            var results3 = new List<object>();
            parserm = parserm.GetList("NAME\\s+TOTO", "(?<name>[^\\s]+)\\s+(?<ready>[^\\s]+)\\s+(?<status>[^\\s]+)\\s+(?<restarts>[^\\s]+)\\s+(?<age>[^\\s]+)\\s+(?<component>[^\\s]+)\\s*", o => o, results2);
            parserm.Run(mtable);
            Assert.IsTrue(results1.Count == 7);
            var dict1 = (Dictionary<string, string>)results1[0];
            Assert.IsTrue(dict1.Count == 7 && dict1.ContainsKey("roles"));
            Assert.IsTrue(results2.Count == 5);
            var dict2 = (Dictionary<string, string>)results2[0];
            Assert.IsTrue(dict2.Count == 7 && dict2.ContainsKey("component"));
            Assert.IsTrue(results3.Count == 0);
        }
                
        [TestMethod]
        public void T06_TestTryReadFileLines()
        {
            // File found
            var lines = Command.TryReadFileLines(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
            Assert.IsTrue(lines.Length > 0);

            // File not found
            lines = Command.TryReadFileLines(Path.Combine(AppContext.BaseDirectory, "toto"));
            Assert.IsTrue(lines.Length == 0);
        }
    }
}
