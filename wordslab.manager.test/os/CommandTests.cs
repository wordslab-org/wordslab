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
            if (OS.IsWindows)
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

                // Return code
                var rc = Command.Run("ipconfig");
                Assert.IsTrue(rc == 0);

                string output = null;
                string error = null;
                int exitcode = -1;
                
                // Handlers
                rc = Command.Run("ipconfig", outputHandler:o => output=o, errorHandler:e => error=e, exitCodeHandler:c => exitcode=c);
                Assert.IsTrue(rc == 0);
                Assert.IsTrue(exitcode == 0);
                Assert.IsTrue(output != null && output.Length > 10);
                Assert.IsTrue(error == null); ;

                // Parameters
                int previousLength = output.Length;
                Command.Run("ipconfig", "/all", outputHandler: o => output = o);
                Assert.IsTrue(output.Length > previousLength);

                // Parameter does not exist
                expectedEx = null;
                try
                {
                    Command.Run("ipconfig", "/toto");
                }
                catch(Exception ex)
                {
                    expectedEx = ex;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is InvalidOperationException);

                exitcode = -1;
                rc = Command.Run("ipconfig", "/toto", exitCodeHandler:c => exitcode = c);
                Assert.IsTrue(rc == 1);
                Assert.IsTrue(exitcode == 1);

                // Working directory
                string outputDir1 = null;
                Command.Run("cmd", "/C dir", outputHandler: o => outputDir1 = o);
                Assert.IsTrue(!string.IsNullOrEmpty(outputDir1));
                
                string outputDir2 = null;
                Command.Run("cmd", "/C dir c:\\Users", outputHandler: o => outputDir2 = o);
                Assert.IsTrue(!string.IsNullOrEmpty(outputDir2));

                string outputDir3 = null;
                Command.Run("cmd", "/C dir", workingDirectory:"c:\\Users", outputHandler: o => outputDir3 = o);
                Assert.IsTrue(!string.IsNullOrEmpty(outputDir3));

                Assert.IsTrue(outputDir1 != outputDir2);
                Assert.IsTrue(outputDir2.Substring(0,outputDir2.Length-30) == outputDir3.Substring(0,outputDir3.Length-30));

                // Working directory does not exist
                expectedEx = null;
                try
                {
                    Command.Run("cmd", "/C dir", workingDirectory: "c:\\toto");
                }
                catch (Exception ex)
                {
                    expectedEx = ex;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is FileNotFoundException);
                Assert.IsTrue(expectedEx.Message.Contains("directory"));

                // Timeout
                Command.Run("timeout", "2", timeoutSec: 3);

                expectedEx = null;
                try
                {
                    Command.Run("timeout", "2", timeoutSec: 1);
                }
                catch (Exception ex)
                {
                    expectedEx = ex;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is TimeoutException);

                // Must run as admin
                expectedEx = null;
                try
                {
                    Command.Run("cmd", "/C dir c:\\Users\\julie", mustRunAsAdmin:true);
                }
                catch (Exception ex)
                {
                    expectedEx = ex;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is InvalidOperationException);
                Assert.IsTrue(expectedEx.Message.Contains("privileges"));

                // Unicode output decoding
                output = null;
                Command.Run("cmd", "/C echo énâtü😀", outputHandler: o => output = o);
                Assert.IsTrue(output == "énâtü??");

                output = null;
                Command.Run("wsl", "--status", outputHandler: o => output = o);
                Assert.IsTrue(output.IndexOf('\0') == 1);

                output = null;
                Command.Run("wsl", "--status", unicodeEncoding:true, outputHandler: o => output = o);
                Assert.IsTrue(output.IndexOf('\0') == -1);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void T02_TestLaunchAndForget()
        {
            if (OS.IsWindows)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                Command.LaunchAndForget("timeout", "2", showWindow: false);
                watch.Stop();
                Assert.IsTrue(watch.ElapsedMilliseconds < 1000);
            }
            else
            {
                throw new NotImplementedException();
            }
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

            // Powershell
            if (OS.IsWindows)
            {
                // Exit code 0 & output
                string output = null;
                var exitCode = Command.ExecuteShellScript(scriptsDir, "timeout.ps1", "1 0", logsDir, outputHandler: o => output = o);
                Assert.IsTrue(exitCode == 0);
                Assert.IsTrue(!String.IsNullOrEmpty(output) && output.Length>20);

                // Exit code 2 & output
                Command.ExecuteShellScript(scriptsDir, "timeout.ps1", "1 2", logsDir,outputHandler: o => output = o, exitCodeHandler: c => exitCode = c);
                Assert.IsTrue(exitCode == 2);
                Assert.IsTrue(!String.IsNullOrEmpty(output) && output.Length > 20);

                // Timeout
                Exception expectedEx = null;
                try
                {
                    Command.ExecuteShellScript(scriptsDir, "timeout.ps1", "2 0", logsDir, timeoutSec: 1);
                }
                catch (Exception ex)
                {
                    expectedEx = ex;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is TimeoutException);

                // No output
                Command.ExecuteShellScript(scriptsDir, "nooutput.ps1", "0", logsDir, outputHandler: o => output = o);
                Assert.IsTrue(output == String.Empty);

                // Script name error
                expectedEx = null;
                try
                {
                    Command.ExecuteShellScript(scriptsDir, "toto.ps1", null, logsDir);
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
                    Command.ExecuteShellScript(scriptsDir, "error.ps1", null, logsDir);
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
                    Command.ExecuteShellScript(scriptsDir, "timeout.ps1", null, logsDir);
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
                    Command.ExecuteShellScript(scriptsDir, "admin.ps1", null, logsDir);
                }
                catch (Exception ex)
                {
                    expectedEx = ex;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is InvalidOperationException);
                Assert.IsTrue(expectedEx.Message.StartsWith("Get-WindowsOptionalFeature"));

                // Run as admin
                Command.ExecuteShellScript(scriptsDir, "admin.ps1", null, logsDir, runAsAdmin: true, outputHandler: o => output=o);
                Assert.IsTrue(output.Contains("Microsoft-Windows-Subsystem-Linux"));
            }
            // Bash
            else
            {
                throw new NotImplementedException();
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
