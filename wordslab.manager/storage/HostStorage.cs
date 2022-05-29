using System.IO.Compression;
using System.Text;
using wordslab.manager.storage.config;

namespace wordslab.manager.storage
{
    public class HostStorage
    {
        public const string APP_NAME = "wordslab";

        public HostStorage()
        {            
            // Users can choose where they install wordslab manager
            string appPath = AppContext.BaseDirectory;
            string databasePath = Path.Combine(appPath, "db");
            string logsPath = Path.Combine(appPath, "logs");
            string scriptsPath = Path.Combine(appPath, "scripts");

            AppDirectory = appPath;
            ConfigDirectory = databasePath;
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
            LogsDirectory = logsPath;
            if (!Directory.Exists(LogsDirectory)) Directory.CreateDirectory(LogsDirectory);
            ScriptsDirectory = scriptsPath;

            // By default, all local data directories are relative to this install directory
            string downloadCachePath = Path.Combine(appPath, "download");
            string virtualMachineOSPath = Path.Combine(appPath, "vm");
            string virtualMachineClusterPath = Path.Combine(appPath, "vm");
            string virtualMachineDataPath = Path.Combine(appPath, "vm");
            string localBackupPath = Path.Combine(appPath, "backup");
     
            DownloadCacheDirectory = downloadCachePath;
            VirtualMachineOSDirectory = virtualMachineOSPath;
            VirtualMachineClusterDirectory = virtualMachineClusterPath;
            VirtualMachineDataDirectory = virtualMachineDataPath;
            BackupDirectory = localBackupPath;
        }

        public string AppDirectory { get; init; }

        public string ConfigDirectory { get; init; }

        public string LogsDirectory { get; init; }

        public string ScriptsDirectory { get; init; }

        public string DownloadCacheDirectory { get; private set; }        

        public string VirtualMachineOSDirectory { get; private set; }

        public string VirtualMachineClusterDirectory { get; private set; }

        public string VirtualMachineDataDirectory { get; private set; }

        public string BackupDirectory { get; private set; }
        
        public void InitConfigurableDirectories(List<HostDirectory> localDirectories)
        {
            if(localDirectories != null)
            {
                foreach(var dir in localDirectories)
                {
                    switch(dir.Function)
                    {
                        case StorageFunction.DownloadCache:
                            DownloadCacheDirectory = dir.Path;
                            break;
                        case StorageFunction.VirtualMachineOS:
                            VirtualMachineOSDirectory = dir.Path;
                            break;
                        case StorageFunction.VirtualMachineCluster:
                            VirtualMachineClusterDirectory = dir.Path;
                            break;
                        case StorageFunction.VirtualMachineData:
                            VirtualMachineDataDirectory = dir.Path;
                            break;
                        case StorageFunction.LocalBackup:
                            BackupDirectory = dir.Path;
                            break;
                    }
                }
            }

            if (!Directory.Exists(DownloadCacheDirectory)) Directory.CreateDirectory(DownloadCacheDirectory);
            if (!Directory.Exists(VirtualMachineOSDirectory)) Directory.CreateDirectory(VirtualMachineOSDirectory);
            if (!Directory.Exists(VirtualMachineClusterDirectory)) Directory.CreateDirectory(VirtualMachineClusterDirectory);
            if (!Directory.Exists(VirtualMachineDataDirectory)) Directory.CreateDirectory(VirtualMachineDataDirectory);
            if (!Directory.Exists(BackupDirectory)) Directory.CreateDirectory(BackupDirectory);
        }

        public List<HostDirectory> GetConfigurableDirectories()
        {
            var localDirectories = new List<HostDirectory>();
            localDirectories.Add(new HostDirectory(StorageFunction.DownloadCache, DownloadCacheDirectory));
            localDirectories.Add(new HostDirectory(StorageFunction.VirtualMachineOS, VirtualMachineOSDirectory));
            localDirectories.Add(new HostDirectory(StorageFunction.VirtualMachineCluster, VirtualMachineClusterDirectory));
            localDirectories.Add(new HostDirectory(StorageFunction.VirtualMachineData, VirtualMachineDataDirectory));
            localDirectories.Add(new HostDirectory(StorageFunction.LocalBackup, BackupDirectory));
            return localDirectories;
        }

        public void MoveConfigurableDirectoryTo(HostDirectory localDirectory)
        {
            string sourcePath = null;
            string destinationPath = localDirectory.Path;
            switch (localDirectory.Function)
            {
                case StorageFunction.DownloadCache:
                    sourcePath = DownloadCacheDirectory;
                    DownloadCacheDirectory = localDirectory.Path;
                    break;
                case StorageFunction.VirtualMachineOS:
                    sourcePath = VirtualMachineOSDirectory;
                    VirtualMachineOSDirectory = localDirectory.Path;
                    break;
                case StorageFunction.VirtualMachineCluster:
                    sourcePath = VirtualMachineClusterDirectory;
                    VirtualMachineClusterDirectory = localDirectory.Path;
                    break;
                case StorageFunction.VirtualMachineData:
                    sourcePath = VirtualMachineDataDirectory;
                    VirtualMachineDataDirectory = localDirectory.Path;
                    break;
                case StorageFunction.LocalBackup:
                    sourcePath = BackupDirectory;
                    BackupDirectory = localDirectory.Path;
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
            }
            else
            {
                progressCallback(localFileInfo.Length, localFileInfo.Length, 100);
            }
            return localFileInfo;
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

        public enum StorageFunction
        {
            DownloadCache,
            VirtualMachineOS,
            VirtualMachineCluster,
            VirtualMachineData,
            LocalBackup
        }
    }
}
