namespace wordslab.manager.os
{
    public static class SshClient
    {
        public static bool IsInstalled()
        {
            bool isInstalled = false;
            try
            {
                Command.Run("ssh-keygen", "--test", errorHandler: e => { }, exitCodeHandler: exitCode => isInstalled = exitCode == 1);
            }
            catch(Exception e)
            {
                isInstalled = false;
            }
            return isInstalled;
        }

        public static bool Install(string scriptsDirectory, string logsDirectory)
        {
            // On Windows, elevation will be required
            if (OS.IsWindows)
            {
                int exitCode = -1;
                Command.ExecuteShellScript(Path.Combine(scriptsDirectory,"os","Windows"), "install-openssh.ps1", "", logsDirectory, runAsAdmin: true, timeoutSec: 300, exitCodeHandler: c => exitCode = c);

                bool restartNeeded = false;
                if (exitCode == 0) { restartNeeded = false; }
                else if (exitCode == 1) { restartNeeded = true; }
                else { throw new InvalidOperationException($"Failed to install OpenSSH client : see script log file in {logsDirectory}"); }
                return restartNeeded;

            }
            // On Linux, this will work only if wordslab is run as admin
            else if (OS.IsLinux)
            {
                Command.Run("apt", "update -y", mustRunAsAdmin: true);
                Command.Run("apt", "install -y openssh-client", mustRunAsAdmin: true);
                return false;
            }
            // On MacOS: always installed by default
            else
            {
                return false;
            }
        }

        public static string GetLinuxInstallCommand()
        {
            return "sudo apt update -y && sudo apt install -y openssh-client";
        }

        public static string GetPublicKeyForCurrentUser()
        {
            string publicKeyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "id_rsa.pub");
            if (!File.Exists(publicKeyPath))
            {
                Command.Run("ssh-keygen", $"-b 3072 -t rsa -f {publicKeyPath.Substring(0, publicKeyPath.Length-4)} -q -N \"\"");
            }

            string publicKey = File.ReadAllText(publicKeyPath);
            return publicKey;
        }

        public static void ImportKnownHostOnClient(string remoteIpAddress, int remotePort=22)
        {
            string remoteKey = null;
            Command.Run("ssh-keyscan", $"-H -p {remotePort} {remoteIpAddress}", outputHandler: o => remoteKey = o, errorHandler: e => {});
            if(String.IsNullOrEmpty(remoteKey))
            {
                throw new Exception("Failed to get the remote SSH key with ssh-keyscan");
            }

            string knownHostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "known_hosts");
            if(File.Exists(knownHostsPath))
            {
                string content = File.ReadAllText(knownHostsPath);
                if (!content.Contains(remoteKey))
                {
                    File.AppendAllText(knownHostsPath, "\n"+ remoteKey +"\n");
                }
            }
            else
            {
                File.WriteAllText(knownHostsPath, remoteKey);
            }            
        }

        public static void CopyFileToRemoteMachine(string localFilePath, string remoteUser, string remoteIpAddress, int remotePort, string remoteFilePath)
        {
            Command.Run("scp", $"-P {remotePort} {localFilePath} {remoteUser}@{remoteIpAddress}:{remoteFilePath}");
        }

        public static void ExecuteRemoteCommand(string remoteUser, string remoteIpAddress, int remotePort,
                              string remoteCommand, string remoteCommandArguments = "", int timeoutSec = 10, bool unicodeEncoding = false, 
                              Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            if (!String.IsNullOrEmpty(remoteCommandArguments)) remoteCommandArguments = " " + remoteCommandArguments;

            Command.Run("ssh", $"-p {remotePort} {remoteUser}@{remoteIpAddress} {remoteCommand}{remoteCommandArguments}",
                timeoutSec: timeoutSec, unicodeEncoding: unicodeEncoding, 
                outputHandler: outputHandler, errorHandler: errorHandler, exitCodeHandler: exitCodeHandler); 
        }
    }
}
