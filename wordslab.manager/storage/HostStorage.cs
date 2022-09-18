using System.IO.Compression;
using System.Text;
using wordslab.manager.storage.config;

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
        private const string BACKUP = "backup";

        public string AppDirectory { get; init; }

        public string ConfigDirectory { get; init; }

        public string LogsDirectory { get; init; }

        public string ScriptsDirectory { get; init; }

        public string DownloadCacheDirectory { get; private set; }

        public string VirtualMachineClusterDirectory { get; private set; }

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

            // The data directories are initialized as direct subdirectories of the install directory
            // The users can then move them to other locations of their choice with MoveConfigurableDirectoryTo()
            DownloadCacheDirectory = Path.Combine(AppDirectory, DOWNLOAD);
            VirtualMachineClusterDirectory = Path.Combine(AppDirectory, VM);
            VirtualMachineDataDirectory = Path.Combine(AppDirectory, VM);
            BackupDirectory = Path.Combine(AppDirectory, BACKUP);

            // The data directories must be created during the first launch
            EnsureConfigurableDirectoriesExist();
        }

        // First initialization
        private void EnsureConfigurableDirectoriesExist()
        {
            if (!Directory.Exists(DownloadCacheDirectory)) Directory.CreateDirectory(DownloadCacheDirectory);
            if (!Directory.Exists(VirtualMachineClusterDirectory)) Directory.CreateDirectory(VirtualMachineClusterDirectory);
            if (!Directory.Exists(VirtualMachineDataDirectory)) Directory.CreateDirectory(VirtualMachineDataDirectory);
            if (!Directory.Exists(BackupDirectory)) Directory.CreateDirectory(BackupDirectory);
        }

        // Second initialization - Interaction with ConfigStore
        internal void InitConfigurableDirectories(IEnumerable<HostDirectory> localDirectories)
        {
            if (localDirectories != null)
            {
                foreach (var dir in localDirectories)
                {
                    switch (dir.Function)
                    {
                        case HostDirectory.StorageFunction.DownloadCache:
                            DownloadCacheDirectory = dir.Path;
                            break;
                        case HostDirectory.StorageFunction.VirtualMachineCluster:
                            VirtualMachineClusterDirectory = dir.Path;
                            break;
                        case HostDirectory.StorageFunction.VirtualMachineData:
                            VirtualMachineDataDirectory = dir.Path;
                            break;
                        case HostDirectory.StorageFunction.Backup:
                            BackupDirectory = dir.Path;
                            break;
                    }
                }
            }

            EnsureConfigurableDirectoriesExist();
        }

        // Persistence - Interaction with ConfigStore
        internal List<HostDirectory> GetConfigurableDirectories()
        {
            var localDirectories = new List<HostDirectory>();
            localDirectories.Add(new HostDirectory(HostDirectory.StorageFunction.DownloadCache, DownloadCacheDirectory));
            localDirectories.Add(new HostDirectory(HostDirectory.StorageFunction.VirtualMachineCluster, VirtualMachineClusterDirectory));
            localDirectories.Add(new HostDirectory(HostDirectory.StorageFunction.VirtualMachineData, VirtualMachineDataDirectory));
            localDirectories.Add(new HostDirectory(HostDirectory.StorageFunction.Backup, BackupDirectory));
            return localDirectories;
        }

        // Move configurable directories - Use the public method on ConfigStore to ensure persistence
        internal void MoveConfigurableDirectoryTo(HostDirectory.StorageFunction storageFunction, string destinationBaseDir)
        {
            string sourcePath = null;
            string destinationPath = null;

            switch (storageFunction)
            {
                case HostDirectory.StorageFunction.DownloadCache:
                    sourcePath = DownloadCacheDirectory;
                    destinationPath = Path.Combine(destinationBaseDir, APP, DOWNLOAD);
                    DownloadCacheDirectory = destinationPath;
                    break;
                case HostDirectory.StorageFunction.VirtualMachineCluster:
                    sourcePath = VirtualMachineClusterDirectory;
                    destinationPath = Path.Combine(destinationBaseDir, APP, VM);
                    VirtualMachineClusterDirectory = destinationPath;
                    break;
                case HostDirectory.StorageFunction.VirtualMachineData:
                    sourcePath = VirtualMachineDataDirectory;
                    destinationPath = Path.Combine(destinationBaseDir, APP, VM);
                    VirtualMachineDataDirectory = destinationPath;
                    break;
                case HostDirectory.StorageFunction.Backup:
                    sourcePath = BackupDirectory;
                    destinationPath = Path.Combine(destinationBaseDir, APP, BACKUP);
                    BackupDirectory = destinationPath;
                    break;
            }
            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);

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

        /// <summary>
        /// WARNING : very dangerous method - all your data will be permanently lost !!
        /// </summary>
        public void DeleteAllDataDirectories()
        {
            if (Directory.Exists(ConfigDirectory)) Directory.Delete(ConfigDirectory, true);
            if (Directory.Exists(LogsDirectory)) Directory.Delete(LogsDirectory, true);
            if (Directory.Exists(DownloadCacheDirectory)) Directory.Delete(DownloadCacheDirectory, true);
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
}
