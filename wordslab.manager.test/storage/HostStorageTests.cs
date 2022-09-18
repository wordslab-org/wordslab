using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using wordslab.manager.storage;
using wordslab.manager.storage.config;

namespace wordslab.manager.test.storage
{
    [TestClass]
    public class HostStorageTests
    {
        [TestMethod]
        [Ignore]
        public void T00_TestHostStorage()
        {
            // Reset environment
            var storage = new HostStorage();
            storage.DeleteAllDataDirectories();

            // Test host storage
            storage = new HostStorage();

            Assert.IsTrue(Directory.Exists(storage.AppDirectory));
            Assert.AreEqual(storage.AppDirectory, AppContext.BaseDirectory);

            Assert.IsTrue(Directory.Exists(storage.ConfigDirectory));
            Assert.IsTrue(Directory.Exists(storage.LogsDirectory));
            Assert.IsTrue(Directory.Exists(storage.DownloadCacheDirectory));
            Assert.IsTrue(Directory.Exists(storage.VirtualMachineClusterDirectory));
            Assert.IsTrue(Directory.Exists(storage.VirtualMachineDataDirectory));
            Assert.IsTrue(Directory.Exists(storage.BackupDirectory));

            Assert.IsTrue(storage.ConfigDirectory.Contains(storage.AppDirectory));
            Assert.IsTrue(storage.LogsDirectory.Contains(storage.AppDirectory));
            Assert.IsTrue(storage.DownloadCacheDirectory.Contains(storage.AppDirectory));
            Assert.IsTrue(storage.VirtualMachineClusterDirectory.Contains(storage.AppDirectory));
            Assert.IsTrue(storage.VirtualMachineDataDirectory.Contains(storage.AppDirectory));
            Assert.IsTrue(storage.BackupDirectory.Contains(storage.AppDirectory));
        }

        [TestMethod]
        public void T01_TestGetConfigurableDirectories()
        {
            var storage = new HostStorage();
            var dirs = storage.GetConfigurableDirectories();

            Assert.IsTrue(dirs.Count == 5);
            Assert.IsTrue(dirs.Where(dir => dir.Function == HostDirectory.StorageFunction.DownloadCache).First().Path.Contains(storage.AppDirectory));
            Assert.IsTrue(dirs.Where(dir => dir.Function == HostDirectory.StorageFunction.VirtualMachineCluster).First().Path.Contains(storage.AppDirectory));
            Assert.IsTrue(dirs.Where(dir => dir.Function == HostDirectory.StorageFunction.VirtualMachineData).First().Path.Contains(storage.AppDirectory));
            Assert.IsTrue(dirs.Where(dir => dir.Function == HostDirectory.StorageFunction.Backup).First().Path.Contains(storage.AppDirectory));
        }

        [TestMethod]
        public void T02_TestMoveConfigurableDirectoryTo()
        {
            var storage = new HostStorage();

            var subdir = "tmp"; 
            var text = "downloaded content";
            File.WriteAllText(Path.Combine(storage.DownloadCacheDirectory, "test1.txt"), text);
            Directory.CreateDirectory(Path.Combine(storage.DownloadCacheDirectory, subdir));
            File.WriteAllText(Path.Combine(storage.DownloadCacheDirectory, subdir, "test2.txt"), text);

            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T02");
            storage.MoveConfigurableDirectoryTo(HostDirectory.StorageFunction.DownloadCache, workdir);
            try
            {
                var destinationPath = Path.Combine(workdir, "wordslab", "download");
                Assert.AreEqual(storage.DownloadCacheDirectory, destinationPath);
                var file1Path = Path.Combine(destinationPath, "test1.txt");
                var file2Path = Path.Combine(destinationPath, subdir, "test2.txt");
                Assert.IsTrue(File.Exists(file1Path));
                Assert.AreEqual(File.ReadAllText(file1Path), text);
                Assert.IsTrue(File.Exists(file2Path));
                Assert.AreEqual(File.ReadAllText(file2Path), text);
            }
            finally
            {
                Directory.Delete(workdir, true);
            }
        }

        [TestMethod]
        public void T03_TestInitConfigurableDirectories()
        {
            var storage = new HostStorage();

            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T03");
            try
            {
                var path1 = Path.Combine(workdir, "dir1");
                var path2 = Path.Combine(workdir, "dir2");
                var path3 = Path.Combine(workdir, "dir3");
                var path4 = Path.Combine(workdir, "dir4");

                var dirs = new List<HostDirectory>();
                dirs.Add(new HostDirectory(HostDirectory.StorageFunction.DownloadCache, path1));
                dirs.Add(new HostDirectory(HostDirectory.StorageFunction.VirtualMachineCluster, path2));
                dirs.Add(new HostDirectory(HostDirectory.StorageFunction.VirtualMachineData, path3));
                dirs.Add(new HostDirectory(HostDirectory.StorageFunction.Backup, path4));

                storage.InitConfigurableDirectories(dirs);

                Assert.AreEqual(storage.DownloadCacheDirectory, path1);
                Assert.AreEqual(storage.VirtualMachineClusterDirectory, path2);
                Assert.AreEqual(storage.VirtualMachineDataDirectory, path3);
                Assert.AreEqual(storage.BackupDirectory, path4);

                Assert.IsTrue(Directory.Exists(path1));
                Assert.IsTrue(Directory.Exists(path2));
                Assert.IsTrue(Directory.Exists(path3));
                Assert.IsTrue(Directory.Exists(path4));

            }
            finally
            {
                Directory.Delete(workdir, true);
            }
        }                

        [TestMethod]
        public async Task T04_TestDownloadFileWithCache()
        {
            var storage = new HostStorage();

            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T04");
            storage.MoveConfigurableDirectoryTo(HostDirectory.StorageFunction.DownloadCache, workdir);
            try
            {
                // Test regular download
                string k3sVersion = "1.22.5+k3s1";
                string k3sExecutableURL = $"https://github.com/k3s-io/k3s/releases/download/v{k3sVersion}/k3s";
                int k3sExecutableSize = 53473280;
                string k3sExecutableFileName = $"k3s-{k3sVersion}";
                      
                var logs = new List<string>();
                var k3sExecFile = await storage.DownloadFileWithCache(k3sExecutableURL, k3sExecutableFileName,
                                progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => logs.Add($"{totalFileSize}, {totalBytesDownloaded}, {progressPercentage}"));
                Assert.IsTrue(k3sExecFile.Exists);
                Assert.AreEqual(k3sExecFile.Name, k3sExecutableFileName);
                Assert.IsTrue(k3sExecFile.Length == k3sExecutableSize);
                Assert.IsTrue(logs.Count > 10);
                Assert.AreEqual(logs.Last(), $"{k3sExecutableSize}, {k3sExecutableSize}, 100");

                // Test local cache on second download
                logs = new List<string>();
                k3sExecFile = await storage.DownloadFileWithCache(k3sExecutableURL, k3sExecutableFileName,
                                progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => logs.Add($"{totalFileSize}, {totalBytesDownloaded}, {progressPercentage}"));
                Assert.IsTrue(k3sExecFile.Exists);
                Assert.IsTrue(k3sExecFile.Length == k3sExecutableSize);
                Assert.IsTrue(logs.Count == 1);
                Assert.AreEqual(logs.Last(), $"{k3sExecutableSize}, {k3sExecutableSize}, 100");

                // Test download with gunzip
                string helmVersion = "3.7.2";
                string helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
                int helmExecutableDownloadSize = 13870692;
                int helmExecutableDiskSize = 45731840;
                string helmFileName = $"heml-{helmVersion}.tar";

                logs = new List<string>();
                var helmFile = await storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true,
                                progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => logs.Add($"{totalFileSize}, {totalBytesDownloaded}, {progressPercentage}"));
                Assert.IsTrue(helmFile.Exists);
                Assert.AreEqual(helmFile.Name, helmFileName);
                Assert.IsTrue(helmFile.Length == helmExecutableDiskSize);
                Assert.IsTrue(logs.Count > 10);
                Assert.AreEqual(logs.Last(), $"{helmExecutableDownloadSize}, {helmExecutableDownloadSize}, 100");

                // Test local cache on second download
                logs = new List<string>();
                helmFile = await storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true,
                                progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => logs.Add($"{totalFileSize}, {totalBytesDownloaded}, {progressPercentage}"));
                Assert.IsTrue(helmFile.Exists);
                Assert.AreEqual(helmFile.Name, helmFileName);
                Assert.IsTrue(helmFile.Length == helmExecutableDiskSize);
                Assert.IsTrue(logs.Count == 1);
                Assert.AreEqual(logs.Last(), $"{helmExecutableDiskSize}, {helmExecutableDiskSize}, 100");

            }
            finally
            {
                Directory.Delete(workdir, true);
            }
        }

        [TestMethod]
        public async Task T05_TestExtractGZipFile()
        {
            var storage = new HostStorage();

            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T05");
            storage.MoveConfigurableDirectoryTo(HostDirectory.StorageFunction.DownloadCache, workdir);
            try
            {
                // Download Gzip file
                string helmVersion = "3.7.2";
                string helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
                string helmFileName = $"heml-{helmVersion}.tar";
                int helmExecutableDownloadSize = 13870692;
                int helmExecutableDiskSize = 45731840;
                var gzipFileName = helmFileName + ".gz";

                var helmFile = await storage.DownloadFileWithCache(helmExecutableURL, gzipFileName);
                Assert.IsTrue(helmFile.Exists);
                Assert.AreEqual(helmFile.Name, gzipFileName);
                Assert.IsTrue(helmFile.Length == helmExecutableDownloadSize);

                // Extract GZip file
                var extractedFilePath = Path.Combine(storage.DownloadCacheDirectory, helmFileName);
                HostStorage.ExtractGZipFile(helmFile.FullName, extractedFilePath);
                var extractedFile = new FileInfo(extractedFilePath);
                Assert.IsTrue(extractedFile.Exists);
                Assert.IsTrue(extractedFile.Length == helmExecutableDiskSize);
            }
            finally
            {
                Directory.Delete(workdir, true);
            }
        }

        [TestMethod]
        public async Task T06_TestExtractTarFile()
        {
            var storage = new HostStorage();

            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T06");
            storage.MoveConfigurableDirectoryTo(HostDirectory.StorageFunction.DownloadCache, workdir);
            try
            {
                // Download Tar file
                string helmVersion = "3.7.2";
                string helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
                string helmFileName = $"heml-{helmVersion}.tar";
                int helmExecutableDiskSize = 45731840;

                var helmFile = await storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true);
                Assert.IsTrue(helmFile.Exists);
                Assert.IsTrue(helmFile.Length == helmExecutableDiskSize);

                // Extract Tar file
                var outputPath = Path.Combine(storage.DownloadCacheDirectory, "helm");
                HostStorage.ExtractTarFile(helmFile.FullName, outputPath);
                var outputDir = new DirectoryInfo(Path.Combine(outputPath, "linux-amd64"));
                Assert.IsTrue(outputDir.Exists);
                var files = outputDir.EnumerateFiles().ToList();
                Assert.IsTrue(files.Count == 3);
                Assert.IsTrue(files[2].Name == "README.md");
                Assert.IsTrue(files[2].Length == 3367);
            }
            finally
            {
                Directory.Delete(workdir, true);
            }
        }
    }
}
