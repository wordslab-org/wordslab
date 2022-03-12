using System.IO.Compression;
using System.Text;

namespace wordslab.manager.storage
{
    public class StorageManager
    {
        public const string APP_NAME = "wordslab";

        public StorageManager()
        {            
            // Users can choose where they install wordslab manager
            string appPath = AppContext.BaseDirectory;
            string databasePath = Path.Combine(appPath, "db");
            string logsPath = Path.Combine(appPath, "logs");
            string scriptsPath = Path.Combine(appPath, "scripts");

            AppDirectory = new DirectoryInfo(appPath);
            ConfigDirectory = new DirectoryInfo(databasePath);
            if (!ConfigDirectory.Exists) ConfigDirectory.Create();
            LogsDirectory = new DirectoryInfo(logsPath);
            if (!LogsDirectory.Exists) LogsDirectory.Create();
            ScriptsDirectory = new DirectoryInfo(scriptsPath);

            // By default, all local data directories are relative to this install directory
            string downloadCachePath = Path.Combine(appPath, "download");
            string virtualMachineOSPath = Path.Combine(appPath, "vm");
            string virtualMachineClusterPath = Path.Combine(appPath, "vm");
            string virtualMachineDataPath = Path.Combine(appPath, "vm");
            string localBackupPath = Path.Combine(appPath, "backup");
     
            DownloadCacheDirectory = new DirectoryInfo(downloadCachePath);
            VirtualMachineOSDirectory = new DirectoryInfo(virtualMachineOSPath);
            VirtualMachineClusterDirectory = new DirectoryInfo(virtualMachineClusterPath);
            VirtualMachineDataDirectory = new DirectoryInfo(virtualMachineDataPath);
            BackupDirectory = new DirectoryInfo(localBackupPath);
        }

        public DirectoryInfo AppDirectory { get; init; }

        public DirectoryInfo ConfigDirectory { get; init; }

        public DirectoryInfo LogsDirectory { get; init; }

        public DirectoryInfo ScriptsDirectory { get; init; }

        public DirectoryInfo DownloadCacheDirectory { get; private set; }        

        public DirectoryInfo VirtualMachineOSDirectory { get; private set; }

        public DirectoryInfo VirtualMachineClusterDirectory { get; private set; }

        public DirectoryInfo VirtualMachineDataDirectory { get; private set; }

        public DirectoryInfo BackupDirectory { get; private set; }
        
        public void InitConfigurableDirectories(List<LocalDirectory> localDirectories)
        {
            if(localDirectories != null)
            {
                foreach(var dir in localDirectories)
                {
                    switch(dir.Function)
                    {
                        case StorageFunction.DownloadCache:
                            DownloadCacheDirectory = new DirectoryInfo(dir.Path);
                            break;
                        case StorageFunction.VirtualMachineOS:
                            VirtualMachineOSDirectory = new DirectoryInfo(dir.Path);
                            break;
                        case StorageFunction.VirtualMachineCluster:
                            VirtualMachineClusterDirectory = new DirectoryInfo(dir.Path);
                            break;
                        case StorageFunction.VirtualMachineData:
                            VirtualMachineDataDirectory = new DirectoryInfo(dir.Path);
                            break;
                        case StorageFunction.LocalBackup:
                            BackupDirectory = new DirectoryInfo(dir.Path);
                            break;
                    }
                }
            }

            if (!DownloadCacheDirectory.Exists) DownloadCacheDirectory.Create();
            if (!VirtualMachineOSDirectory.Exists) VirtualMachineOSDirectory.Create();
            if (!VirtualMachineClusterDirectory.Exists) VirtualMachineClusterDirectory.Create();
            if (!VirtualMachineDataDirectory.Exists) VirtualMachineDataDirectory.Create();
            if (!BackupDirectory.Exists) BackupDirectory.Create();
        }

        public List<LocalDirectory> GetConfigurableDirectories()
        {
            var localDirectories = new List<LocalDirectory>();
            localDirectories.Add(new LocalDirectory(StorageFunction.DownloadCache, DownloadCacheDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.VirtualMachineOS, VirtualMachineOSDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.VirtualMachineCluster, VirtualMachineClusterDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.VirtualMachineData, VirtualMachineDataDirectory.FullName));
            localDirectories.Add(new LocalDirectory(StorageFunction.LocalBackup, BackupDirectory.FullName));
            return localDirectories;
        }

        public void MoveConfigurableDirectoryTo(LocalDirectory localDirectory)
        {
            string sourcePath = null;
            string destinationPath = localDirectory.Path;
            switch (localDirectory.Function)
            {
                case StorageFunction.DownloadCache:
                    sourcePath = DownloadCacheDirectory.FullName;
                    DownloadCacheDirectory = new DirectoryInfo(localDirectory.Path);
                    break;
                case StorageFunction.VirtualMachineOS:
                    sourcePath = VirtualMachineOSDirectory.FullName;
                    VirtualMachineOSDirectory = new DirectoryInfo(localDirectory.Path);
                    break;
                case StorageFunction.VirtualMachineCluster:
                    sourcePath = VirtualMachineClusterDirectory.FullName;
                    VirtualMachineClusterDirectory = new DirectoryInfo(localDirectory.Path);
                    break;
                case StorageFunction.VirtualMachineData:
                    sourcePath = VirtualMachineDataDirectory.FullName;
                    VirtualMachineDataDirectory = new DirectoryInfo(localDirectory.Path);
                    break;
                case StorageFunction.LocalBackup:
                    sourcePath = BackupDirectory.FullName;
                    BackupDirectory = new DirectoryInfo(localDirectory.Path);
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
            var localFileInfo = new FileInfo(Path.Combine(DownloadCacheDirectory.FullName, localFileName));
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
    }
}
