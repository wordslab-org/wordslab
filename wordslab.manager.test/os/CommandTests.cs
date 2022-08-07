using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
                Assert.IsTrue(error == String.Empty);

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
            Assert.IsTrue(true);
        }
              
        [TestMethod]
        public void T03_TestGetScriptContent()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void T04_TestExecuteShellScript()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void T05_TestCommandOutputParser()
        {
            Assert.IsTrue(true);
        }
                
        [TestMethod]
        public void T06_TestTryReadFileLines()
        {
            Assert.IsTrue(true);
        }
    }
}
