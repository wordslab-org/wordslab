using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace wordslab.manager.storage
{
    public class HostStorage
    {
        private const string APP = "wordslab";

        private const string CONFIG = "config";
        private const string LOGS = "logs";
        private const string SCRIPTS = "scripts";
        private const string DOWNLOAD = "download";
        private const string VM = "vm";
        private const string VM_CLUSTER = "cluster";
        private const string VM_DATA = "data";
        private const string BACKUP = "backup";

        public string AppDirectory { get; init; }

        public string ConfigDirectory { get; init; }

        public string LogsDirectory { get; init; }

        public string ScriptsDirectory { get; init; }

        public string VirtualMachineClusterDirectory { get; private set; }

        public string DownloadCacheDirectory { get { return Path.Combine(VirtualMachineClusterDirectory, DOWNLOAD); } }

        public string VirtualMachineDataDirectory { get; private set; }

        public string BackupDirectory { get; private set; }

        public HostStorage()
        {
            // The users can choose the base directory where they install wordslab manager
            AppDirectory = AppContext.BaseDirectory;

            // The config, logs, and scripts subdirectories are fixed inside this install directory
            ConfigDirectory = Path.Combine(AppDirectory, CONFIG);
            LogsDirectory = Path.Combine(AppDirectory, LOGS);
            ScriptsDirectory = Path.Combine(AppDirectory, SCRIPTS);

            // The config and logs subdirectories must be created during the first launch
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
            if (!Directory.Exists(LogsDirectory)) Directory.CreateDirectory(LogsDirectory);
            // The scripts directory is included in the installation package

            // The configurable data directories are initialized as direct subdirectories of the install directory
            // The users can then move them to other locations of their choice with MoveConfigurableDirectoryTo()
            VirtualMachineClusterDirectory = Path.Combine(AppDirectory, VM, VM_CLUSTER);
            VirtualMachineDataDirectory = Path.Combine(AppDirectory, VM, VM_DATA);
            BackupDirectory = Path.Combine(AppDirectory, BACKUP);

            // The configurable data directories must also be created during the first launch
            if (!Directory.Exists(VirtualMachineClusterDirectory)) Directory.CreateDirectory(VirtualMachineClusterDirectory);
            // The download cache directory is always a subdirectory of the cluster data directory
            if (!Directory.Exists(DownloadCacheDirectory)) Directory.CreateDirectory(DownloadCacheDirectory);
            if (!Directory.Exists(VirtualMachineDataDirectory)) Directory.CreateDirectory(VirtualMachineDataDirectory);
            if (!Directory.Exists(BackupDirectory)) Directory.CreateDirectory(BackupDirectory);
        }

        // Second initialization - After loading HostMachineConfig from the ConfigStore
        internal void InitConfigurableDirectories(string virtualMachineClusterDirectory, string virtualMachineDataDirectory, string backupDirectory)
        {
            VirtualMachineClusterDirectory = virtualMachineClusterDirectory;
            VirtualMachineDataDirectory = virtualMachineDataDirectory;
            BackupDirectory = backupDirectory;
        }

        internal static string GetSubdirectoryFor(StorageLocation storageLocation)
        {
            switch (storageLocation)
            {
                case StorageLocation.VirtualMachineCluster:
                    return Path.Combine(APP, VM, VM_CLUSTER);
                case StorageLocation.VirtualMachineData:
                    return Path.Combine(APP, VM, VM_DATA);
                case StorageLocation.Backup:
                    return Path.Combine(APP, BACKUP);
            }
            return null;
        }

        // Move configurable directories - Use the public method on ConfigStore to ensure persistence
        internal void MoveConfigurableDirectoryTo(StorageLocation storageLocation, string destinationBaseDir)
        {
            if (!Directory.Exists(destinationBaseDir)) Directory.CreateDirectory(destinationBaseDir);

            string sourcePath = null;
            string destinationPath = Path.Combine(destinationBaseDir, GetSubdirectoryFor(storageLocation));
            switch (storageLocation)
            {                
                case StorageLocation.VirtualMachineCluster:
                    sourcePath = VirtualMachineClusterDirectory;
                    VirtualMachineClusterDirectory = destinationPath;
                    break;
                case StorageLocation.VirtualMachineData:
                    sourcePath = VirtualMachineDataDirectory;
                    VirtualMachineDataDirectory = destinationPath;
                    break;
                case StorageLocation.Backup:
                    sourcePath = BackupDirectory;
                    BackupDirectory = destinationPath;
                    break;
            }

            if (!destinationPath.Contains(sourcePath))
            {
                if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);
                MoveDirectoryTo(sourcePath, destinationPath);
            }
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

        public void ClearLogs()
        {
            if (Directory.Exists(LogsDirectory))
            {
                Directory.Delete(LogsDirectory, true);
                Directory.CreateDirectory(LogsDirectory);
            }
        }

        public void ClearDownloadCache()
        {
            if (Directory.Exists(DownloadCacheDirectory))
            {
                Directory.Delete(DownloadCacheDirectory, true);
                Directory.CreateDirectory(DownloadCacheDirectory);
            }
        }

        /// <summary>
        /// WARNING : very dangerous method - all your data will be permanently lost !!
        /// </summary>
        internal void DeleteAllDataDirectories()
        {
            if (Directory.Exists(ConfigDirectory)) Directory.Delete(ConfigDirectory, true);
            if (Directory.Exists(LogsDirectory)) Directory.Delete(LogsDirectory, true);
            if (Directory.Exists(VirtualMachineClusterDirectory)) Directory.Delete(VirtualMachineClusterDirectory, true);
            if (Directory.Exists(VirtualMachineDataDirectory)) Directory.Delete(VirtualMachineDataDirectory, true);
            if (Directory.Exists(BackupDirectory)) Directory.Delete(BackupDirectory, true);
        }

        public async Task<FileInfo> DownloadFileWithCache(string remoteURL, string localFileName, bool gunzip = false, HttpDownloader.ProgressChangedHandler progressCallback = null)
        {
            var localFileInfo = new FileInfo(Path.Combine(DownloadCacheDirectory, localFileName));
            if (!localFileInfo.Exists)
            {
                using var downloader = new HttpDownloader(remoteURL, localFileInfo.FullName, gunzip);
                if (progressCallback != null)
                {
                    downloader.ProgressChanged += progressCallback;
                }
                await downloader.StartDownload();
                if (progressCallback != null)
                {
                    downloader.ProgressChanged -= progressCallback;
                }
                return new FileInfo(localFileInfo.FullName);
            }
            else
            {
                if (progressCallback != null)
                {
                    progressCallback(localFileInfo.Length, localFileInfo.Length, 100);
                }
                return localFileInfo;
            }
        }

        public static void ExtractGZipFile(string gzipFilePath, string outpuFilePath)
        {
            using FileStream compressedFileStream = File.Open(gzipFilePath, FileMode.Open);
            using FileStream outputFileStream = File.Create(outpuFilePath);
            using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            decompressor.CopyTo(outputFileStream);
        }

        // https://gist.github.com/ForeverZer0/a2cd292bd2f3b5e114956c00bb6e872b
        public static void ExtractTarFile(string tarFilePath, string outputDir)
        {
            using var stream = File.OpenRead(tarFilePath);

            var buffer = new byte[100];
            while (true)
            {
                stream.Read(buffer, 0, 100);
                var name = Encoding.ASCII.GetString(buffer).Trim('\0');
                if (String.IsNullOrWhiteSpace(name))
                    break;
                stream.Seek(24, SeekOrigin.Current);
                stream.Read(buffer, 0, 12);
                var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

                stream.Seek(376L, SeekOrigin.Current);

                var output = Path.Combine(outputDir, name);
                if (!Directory.Exists(Path.GetDirectoryName(output)))
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                if (!name.EndsWith("/", StringComparison.InvariantCulture))
                {
                    using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        var buf = new byte[size];
                        stream.Read(buf, 0, buf.Length);
                        str.Write(buf, 0, buf.Length);
                    }
                }

                var pos = stream.Position;

                var offset = 512 - (pos % 512);
                if (offset == 512)
                    offset = 0;

                stream.Seek(offset, SeekOrigin.Current);
            }
        }       
    }

    public enum StorageLocation
    {
        VirtualMachineCluster,
        VirtualMachineData,
        Backup
    }
}
