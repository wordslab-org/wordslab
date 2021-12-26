using System.Runtime.InteropServices;

namespace wordslab.installer.localstorage
{
    public class LocalStorageManager
    {
        public LocalStorageManager(string downloadCachePath = null, string virtualMachineOSPath = null, string virtualMachineDataPath = null, string localBackupPath = null)
        {
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            ConfigDirectory = new DirectoryInfo(Path.Combine(homeDirectory, ".wordslab"));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (downloadCachePath == null) { }
                if (virtualMachineOSPath == null) { }
                if (virtualMachineDataPath == null) { }
                if (localBackupPath == null) { }
            } 
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

            }

            DownloadCacheDirectory = new DirectoryInfo(downloadCachePath);
            if (!DownloadCacheDirectory.Exists) DownloadCacheDirectory.Create();

            VirtualMachineOSDirectory = new DirectoryInfo(virtualMachineOSPath);
            if (!VirtualMachineOSDirectory.Exists) VirtualMachineOSDirectory.Create();

            VirtualMachineDataDirectory = new DirectoryInfo(virtualMachineDataPath);
            if (!VirtualMachineDataDirectory.Exists) VirtualMachineDataDirectory.Create();

            LocalBackupDirectory = new DirectoryInfo(localBackupPath);
            if (!LocalBackupDirectory.Exists) LocalBackupDirectory.Create();
        }

        public DirectoryInfo ConfigDirectory { get; }

        public DirectoryInfo DownloadCacheDirectory { get; }

        public DirectoryInfo VirtualMachineOSDirectory { get; }

        public DirectoryInfo VirtualMachineDataDirectory { get; }

        public DirectoryInfo LocalBackupDirectory { get; }

        public FileInfo DownloadFileWithCache(string remoteURL, string localFileName, HttpDownloader.ProgressChangedHandler progressCallback = null)
        {
            var localFileInfo = new FileInfo(Path.Combine(DownloadCacheDirectory.FullName, localFileName));
            if (!localFileInfo.Exists)
            {
                var downloader = new HttpDownloader(remoteURL, localFileInfo.FullName);
                if (progressCallback != null)
                {
                    downloader.ProgressChanged += progressCallback;
                }
            }
            return localFileInfo;
        }
    }
}
