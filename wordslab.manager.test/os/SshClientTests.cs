using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class SshClientTests
    {
        [TestMethod]
        public void T01_TestIsInstalled()
        {
            var installok = SshClient.IsInstalled();
            Assert.IsTrue(installok);
        }

        [TestMethod]
        public void T02_TestInstall()
        {
            var storage = new HostStorage();
            SshClient.Install(storage.ScriptsDirectory, storage.LogsDirectory);

            var installok = SshClient.IsInstalled();
            Assert.IsTrue(installok);
        }

        [TestMethod]
        public void T03_TestGetLinuxInstallCommand()
        {
            var installcommand = SshClient.GetLinuxInstallCommand();
            Assert.IsTrue(installcommand.Length > 10);
        }

        [TestMethod]
        public void T04_TestGetPublicKeyForCurrentUser()
        {
            var publickey = SshClient.GetPublicKeyForCurrentUser();
            Assert.IsTrue(publickey.Length > 10 && publickey.StartsWith("ssh-rsa"));
        }

        [TestMethod]
        public void T05_TestImportKnownHostOnClient()
        {
            // IMPORTANT -- PREREQUISITES --
            
            // Clean ~/.ssh/known_hosts on the local machine before this test

            // Start SSH server : start WSL, then "sudo service ssh start"

            // Generate and get public key on test client : run T04_TestGetPublicKeyForCurrentUser

            // Authorize test client : log into WSL
            // mkdir ~/.ssh
            // chmod 700 ~/.ssh
            // touch ~/.ssh/authorized_keys
            // chmod 600 ~/.ssh/authorized_keys
            // vi ~/.ssh/authorized_keys
            // Then copy test client public key on a new line

            // --- END OF PREREQUISITES ---

            var sshserver = "172.21.218.122";
            var sshport = 22;
            var sshuser = "laurent";

            // Host not yet known
            int exitcode = -1;
            Exception expectedEx = null;
            try
            {
                SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "date", timeoutSec: 2, errorHandler: e => { }, exitCodeHandler: c => exitcode=c);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is TimeoutException);
            
            SshClient.ImportKnownHostOnClient(sshserver);

            // Host known
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "date", timeoutSec: 2, errorHandler: e => { }, exitCodeHandler: c => exitcode=c);
        }

        [TestMethod]
        public void T06_TestExecuteRemoteCommand()
        {
            var sshserver = "172.21.218.122";
            var sshport = 22;
            var sshuser = "laurent";

            // Without parameter
            string output = null;
            string error = null;
            int exitcode = -1;
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "date", timeoutSec: 2, outputHandler: o=>output=o, errorHandler: e=>error=e, exitCodeHandler: c=>exitcode=c);
            Assert.IsTrue(exitcode == 0);
            Assert.IsTrue(output.Length > 15);
            Assert.IsTrue(String.IsNullOrEmpty(error));

            // With parameter
            output = null;
            error = null;
            exitcode = -1;
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "date", "--iso-8601 --utc", timeoutSec: 2, outputHandler: o => output = o, errorHandler: e => error = e, exitCodeHandler: c => exitcode = c);
            Assert.IsTrue(exitcode == 0);
            Assert.IsTrue(output.Length == 12);
            Assert.IsTrue(String.IsNullOrEmpty(error));

            // Bad user
            // > toto@172.21.218.122: Permission denied (publickey).
            output = null;
            error = null;
            exitcode = -1;
            SshClient.ExecuteRemoteCommand("toto", sshserver, sshport, "date", timeoutSec: 2, outputHandler: o => output = o, errorHandler: e => error = e, exitCodeHandler: c => exitcode = c);
            Assert.IsTrue(exitcode == 255);
            Assert.IsTrue(String.IsNullOrEmpty(output));
            Assert.IsTrue(error.Contains("Permission denied"));           

            // Bad server
            // > timeout
            Exception expectedEx = null;
            try
            {
                SshClient.ExecuteRemoteCommand(sshuser, "99.99.99.99", sshport, "date", timeoutSec: 2, outputHandler: o => output = o, errorHandler: e => error = e, exitCodeHandler: c => exitcode = c);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is TimeoutException);

            // Bad port
            // > ssh: connect to host 172.21.218.122 port 23: Connection refused
            output = null;
            error = null;
            exitcode = -1;
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, 23, "date", timeoutSec: 5, outputHandler: o => output = o, errorHandler: e => error = e, exitCodeHandler: c => exitcode = c);
            Assert.IsTrue(exitcode == 255);
            Assert.IsTrue(String.IsNullOrEmpty(output));
            Assert.IsTrue(error.Contains("Connection refused"));

            // Bad command
            // > bash: date2: command not found
            output = null;
            error = null;
            exitcode = -1;
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "date2", timeoutSec: 2, outputHandler: o => output = o, errorHandler: e => error = e, exitCodeHandler: c => exitcode = c);
            Assert.IsTrue(exitcode == 127);
            Assert.IsTrue(String.IsNullOrEmpty(output));
            Assert.IsTrue(error.Contains("command not found"));

            // Command error
            // > date: unrecognized option '--toto'
            output = null;
            error = null;
            exitcode = -1;
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "date", "--toto", timeoutSec: 2, outputHandler: o => output = o, errorHandler: e => error = e, exitCodeHandler: c => exitcode = c);
            Assert.IsTrue(exitcode == 1);
            Assert.IsTrue(String.IsNullOrEmpty(output));
            Assert.IsTrue(error.Contains("option"));
        }

        [TestMethod]
        public void T07_TestCopyFileToRemoteMachine()
        {
            var sshserver = "172.21.218.122";
            var sshport = 22;
            var sshuser = "laurent";

            var file = "appsettings.json";
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            string output = null;
            string error = null;
            int exitcode = -1;            
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "ls", file, timeoutSec: 2, outputHandler: o => output = o, errorHandler: e => error = e, exitCodeHandler: c => exitcode = c);;
            Assert.IsTrue(exitcode == 2);

            SshClient.CopyFileToRemoteMachine(path, sshuser, sshserver, sshport, file);

            exitcode = -1;
            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "ls", file, timeoutSec: 2, exitCodeHandler: c => exitcode = c);
            Assert.IsTrue(exitcode == 0);

            SshClient.ExecuteRemoteCommand(sshuser, sshserver, sshport, "rm", file, timeoutSec: 2);
        }
    }
}
