namespace wordslab.manager.os
{
    public static class SshClient
    {
        public static bool IsInstalled()
        {
            bool isInstalled = false;
            Command.Run("ssh-keygen", "--test", exitCodeHandler: exitCode => isInstalled = exitCode==1);
            return isInstalled;
        }

        public static void Install()
        {
            // On Linux, this will work only if wordslab is run as admin
            if (OS.IsLinux)
            {
                Command.Run("apt", "update -y", mustRunAsAdmin: true);
                Command.Run("apt", "install -y openssh-client", mustRunAsAdmin: true);
            }
            // On Windows, elevation will be required
            else if(OS.IsWindows)
            {
                Command.Run("powershell.exe", "Add-WindowsCapability -Online -Name OpenSSH.Client*", mustRunAsAdmin: true);
            }
            // On MacOS: always installed by default
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

            string displayCommand = null;
            if (OS.IsLinux || OS.IsMacOS)
            {
                displayCommand = "cat";
                
            }
            else if(OS.IsWindows)
            {
                displayCommand = "type"; 
            }                      
            string publicKey = null;
            Command.Run(displayCommand, publicKeyPath, outputHandler: output => publicKey = output);
            return publicKey;
        }

        public static void ImportKnownHostOnLinuxClient(string remoteIpAddress, int remotePort)
        {
            Command.Run("ssh-keyscan", $"-H -p {remotePort} {remoteIpAddress} >> ~/.ssh/known_hosts");
        }

        public static void CopyFileToRemoteMachine(string localFilePath, string remoteUser, string remoteIpAddress, int remotePort, string remoteFilePath)
        {
            Command.Run("scp", $"-P {remotePort} {localFilePath} {remoteUser}@{remoteIpAddress}:{remoteFilePath}");
        }

        public static void ExecuteRemoteCommand(string remoteUser, string remoteIpAddress, int remotePort,
                              string remoteCommand, string remoteCommandArguments = "", int timeoutSec = 10, bool unicodeEncoding = false, 
                              Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        {
            Command.Run("ssh", $"-p {remotePort} {remoteUser}@{remoteIpAddress} '{remoteCommand} {remoteCommandArguments}'",
                timeoutSec: timeoutSec, unicodeEncoding: unicodeEncoding, 
                outputHandler: outputHandler, errorHandler: errorHandler, exitCodeHandler: exitCodeHandler); 
        }
    }
}
