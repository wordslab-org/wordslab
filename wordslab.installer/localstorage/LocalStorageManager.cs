using System.Runtime.InteropServices;

namespace wordslab.installer.localstorage
{
    public class LocalStorageManager
    {
        public const string APP_NAME = "wordslab";

        public LocalStorageManager()
        {
            // Local storage is located by default under the current user home directory
            var homeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Installation directories are platform-dependent (inspired by rancher-desktop)
            string appPath = null;
            string configPath = null;
            string logsPath = null;
            string downloadCachePath = null;
            string virtualMachineOSPath = null;
            string virtualMachineClusterPath = null;
            string virtualMachineDataPath = null;
            string localBackupPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (appData == null) appData = Path.Combine(homeFolderPath, "AppData", "Roaming");
                var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                if(localAppData == null) localAppData = Path.Combine(homeFolderPath, "AppData", "Local");

                appPath = Path.Combine(appData, APP_NAME);
                configPath = Path.Combine(appData, APP_NAME);
                logsPath = Path.Combine(localAppData, APP_NAME, "logs");
                downloadCachePath = Path.Combine(localAppData, APP_NAME, "cache");
                virtualMachineOSPath = Path.Combine(localAppData, APP_NAME, "vm-os");
                virtualMachineClusterPath = Path.Combine(localAppData, APP_NAME, "vm-cluster");
                virtualMachineDataPath = Path.Combine(localAppData, APP_NAME, "vm-data");
                localBackupPath = Path.Combine(localAppData, APP_NAME, "backup");
            } 
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                appPath = Path.Combine(homeFolderPath, "Library", "Application Support", APP_NAME);
                configPath = Path.Combine(homeFolderPath, "Library", "Preferences", APP_NAME);
                logsPath = Path.Combine(homeFolderPath, "Library", "Logs", APP_NAME);
                downloadCachePath = Path.Combine(homeFolderPath, "Library", "Caches", APP_NAME);
                virtualMachineOSPath = Path.Combine(appPath, "vm-os");
                virtualMachineClusterPath = Path.Combine(appPath, "vm-cluster");
                virtualMachineDataPath = Path.Combine(appPath, "vm-data");
                localBackupPath = Path.Combine(appPath, "backup");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (dataHome == null) dataHome = Path.Combine(homeFolderPath, ".local", "share");
                var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (configHome == null) configHome = Path.Combine(homeFolderPath, ".config");
                var cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                if (cacheHome == null) cacheHome = Path.Combine(homeFolderPath, ".cache");

                appPath = Path.Combine(dataHome, APP_NAME);
                configPath = Path.Combine(configHome, APP_NAME);
                logsPath = Path.Combine(dataHome, APP_NAME, "logs");
                downloadCachePath = Path.Combine(cacheHome, APP_NAME);
                virtualMachineOSPath = Path.Combine(dataHome, APP_NAME, "vm-os");
                virtualMachineClusterPath = Path.Combine(dataHome, APP_NAME, "vm-cluster");
                virtualMachineDataPath = Path.Combine(dataHome, APP_NAME, "vm-data");
                localBackupPath = Path.Combine(dataHome, APP_NAME, "backup");
            }

            // Get or initialize installation directories
            AppDirectory = new DirectoryInfo(appPath);
            if (!AppDirectory.Exists) AppDirectory.Create();

            ConfigDirectory = new DirectoryInfo(configPath);
            if (!ConfigDirectory.Exists) ConfigDirectory.Create();

            LogsDirectory = new DirectoryInfo(logsPath);
            if (!LogsDirectory.Exists) LogsDirectory.Create();

            DownloadCacheDirectory = new DirectoryInfo(downloadCachePath);
            if (!DownloadCacheDirectory.Exists) DownloadCacheDirectory.Create();

            VirtualMachineOSDirectory = new DirectoryInfo(virtualMachineOSPath);
            if (!VirtualMachineOSDirectory.Exists) VirtualMachineOSDirectory.Create();

            VirtualMachineClusterDirectory = new DirectoryInfo(virtualMachineClusterPath);
            if (!VirtualMachineClusterDirectory.Exists) VirtualMachineClusterDirectory.Create();

            VirtualMachineDataDirectory = new DirectoryInfo(virtualMachineDataPath);
            if (!VirtualMachineDataDirectory.Exists) VirtualMachineDataDirectory.Create();

            LocalBackupDirectory = new DirectoryInfo(localBackupPath);
            if (!LocalBackupDirectory.Exists) LocalBackupDirectory.Create();
        }

        internal List<LocalDirectory> GetDirectories()
        {
            var localDirectories = new List<LocalDirectory>();
            localDirectories.Add(new LocalDirectory(StorageFunction.App, AppDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.Config, ConfigDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.Logs, LogsDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.DownloadCache, DownloadCacheDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.VirtualMachineOS, VirtualMachineOSDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.VirtualMachineCluster, VirtualMachineClusterDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.VirtualMachineData, VirtualMachineDataDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.LocalBackup, LocalBackupDirectory.FullName));
            return localDirectories;
        }

        public DirectoryInfo AppDirectory { get; init; }

        public DirectoryInfo ConfigDirectory { get; init; }

        public DirectoryInfo LogsDirectory { get; private set; }

        public DirectoryInfo DownloadCacheDirectory { get; private set; }

        public DirectoryInfo VirtualMachineOSDirectory { get; private set; }

        public DirectoryInfo VirtualMachineClusterDirectory { get; private set; }

        public DirectoryInfo VirtualMachineDataDirectory { get; private set; }

        public DirectoryInfo LocalBackupDirectory { get; private set; }

        public void MoveLogsDirectoryTo(string destinationPath)
        {
            var sourcePath = LogsDirectory.FullName;
            LogsDirectory = new DirectoryInfo(destinationPath);
            if (!LogsDirectory.Exists) LogsDirectory.Create();
            MoveDirectoryTo(sourcePath, destinationPath);
        }
        public void MoveDownloadCacheDirectoryTo(string destinationPath)
        {
            var sourcePath = DownloadCacheDirectory.FullName;
            DownloadCacheDirectory = new DirectoryInfo(destinationPath);
            if (!DownloadCacheDirectory.Exists) DownloadCacheDirectory.Create();
            MoveDirectoryTo(sourcePath, destinationPath);
        }

        public void MoveVirtualMachineOSDirectoryTo(string destinationPath)
        {
            var sourcePath = VirtualMachineOSDirectory.FullName;
            VirtualMachineOSDirectory = new DirectoryInfo(destinationPath);
            if (!VirtualMachineOSDirectory.Exists) VirtualMachineOSDirectory.Create();
            MoveDirectoryTo(sourcePath, destinationPath);
        }

        public void MoveVirtualMachineClusterDirectoryTo(string destinationPath)
        {
            var sourcePath = VirtualMachineClusterDirectory.FullName;
            VirtualMachineClusterDirectory = new DirectoryInfo(destinationPath);
            if (!VirtualMachineClusterDirectory.Exists) VirtualMachineClusterDirectory.Create();
            MoveDirectoryTo(sourcePath, destinationPath);
        }

        public void MoveVirtualMachineDataDirectoryTo(string destinationPath)
        {
            var sourcePath = VirtualMachineDataDirectory.FullName;
            VirtualMachineDataDirectory = new DirectoryInfo(destinationPath);
            if (!VirtualMachineDataDirectory.Exists) VirtualMachineDataDirectory.Create();
            MoveDirectoryTo(sourcePath, destinationPath);
        }

        public void MoveLocalBackupDirectoryTo(string destinationPath)
        {
            var sourcePath = LocalBackupDirectory.FullName;
            LocalBackupDirectory = new DirectoryInfo(destinationPath);
            if (!LocalBackupDirectory.Exists) LocalBackupDirectory.Create();
            MoveDirectoryTo(sourcePath, destinationPath);
        }

        private void MoveDirectoryTo(string sourcePath, string destinationPath)
        {
            // If the source directory doesn't exist, throw an exception
            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourcePath}");
            }

            // If the destination directory doesn't exist, create it       
            Directory.CreateDirectory(destinationPath);

            // Move the files from the source directory to the destination directory
            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destinationPath, file.Name);
                file.MoveTo(tempPath, true);
            }

            // Repeat recursively the move operation for all subdirectories
            var sourceSubDirs = sourceDir.GetDirectories();
            foreach (var sourceSubDir in sourceSubDirs)
            {
                string destinationSubPath = Path.Combine(destinationPath, sourceSubDir.Name);
                MoveDirectoryTo(sourceSubDir.FullName, destinationSubPath);
            }
        }

        public async Task<FileInfo> DownloadFileWithCache(string remoteURL, string localFileName, HttpDownloader.ProgressChangedHandler progressCallback = null)
        {
            var localFileInfo = new FileInfo(Path.Combine(DownloadCacheDirectory.FullName, localFileName));
            if (!localFileInfo.Exists)
            {
                using var downloader = new HttpDownloader(remoteURL, localFileInfo.FullName);
                if (progressCallback != null)
                {
                    downloader.ProgressChanged += progressCallback;
                }
                await downloader.StartDownload();
                if (progressCallback != null)
                {
                    downloader.ProgressChanged -= progressCallback;
                }
            }
            return localFileInfo;
        }
    }
}
