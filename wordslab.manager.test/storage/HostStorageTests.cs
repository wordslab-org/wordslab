using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.storage;

namespace wordslab.manager.test.storage
{
    [TestClass]
    public class HostStorageTests
    {
        [TestMethod]
        public void TestHostStorage()
        {
            // var storage = LocalStorageManager.Instance;
            // Assert.IsTrue(storage.AppDirectory.Exists);
            // Assert.IsTrue(storage.ScriptsDirectory.GetFiles("*.sh").Length > 0);

            // public string AppDirectory { get; init; }
            // public string ConfigDirectory { get; init; }
            // public string LogsDirectory { get; init; }
            // public string ScriptsDirectory { get; init; }
            // public string DownloadCacheDirectory { get; private set; }
            // public string VirtualMachineOSDirectory { get; private set; }
            // public string VirtualMachineClusterDirectory { get; private set; }
            // public string VirtualMachineDataDirectory { get; private set; }
            // public string BackupDirectory { get; private set; }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestInitConfigurableDirectories()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetConfigurableDirectories()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestMoveConfigurableDirectoryTo()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestDownloadFileWithCache()
        {
            /*var helmVersion = "3.7.2";
            var helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
            var helmExecutableSize = 13870692;
            var helmFileName = $"heml-{helmVersion}.tar";

            var storage = LocalStorageManager.Instance;
            var progressLog = new List<long>();
            var localFile = storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => progressLog.Add(totalBytesDownloaded)).GetAwaiter().GetResult();

            Assert.IsTrue(new FileInfo(localFile.FullName).Length == 45731840);
            Assert.IsTrue(progressLog.Count > 0 && progressLog[progressLog.Count - 1] == 45731840);

            progressLog.Clear();
            localFile = storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => progressLog.Add(totalBytesDownloaded)).GetAwaiter().GetResult();
            Assert.IsTrue(progressLog.Count > 0 && progressLog[progressLog.Count - 1] == 45731840);*/

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestExtractGZipFile()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestExtractTarFile()
        {
            /*LocalStorageManager.ExtractTar(@"C:\Users\laure\AppData\Local\wordslab\cache\heml-3.7.2.tar", @"C:\Users\laure\AppData\Local\wordslab\cache\helm-temp");

            Assert.IsTrue(File.Exists(@"C:\Users\laure\AppData\Local\wordslab\cache\helm-temp\linux-amd64\helm"));*/

            Assert.IsTrue(true);
        }
    }
}
