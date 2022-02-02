using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using wordslab.installer.localstorage;

namespace wordslab.installer.test
{
    [TestClass]
    public class LocalStorageTest
    {
        [TestMethod]
        public void TestConstructor()
        {
            var storage = LocalStorageManager.Instance;

            Assert.IsTrue(storage.AppDirectory.Exists);
            Assert.IsTrue(storage.ScriptsDirectory.Exists);
            Assert.IsTrue(storage.ConfigDirectory.Exists);
            Assert.IsTrue(storage.LogsDirectory.Exists);
            Assert.IsTrue(storage.DownloadCacheDirectory.Exists);
            Assert.IsTrue(storage.VirtualMachineOSDirectory.Exists);
            Assert.IsTrue(storage.VirtualMachineClusterDirectory.Exists);
            Assert.IsTrue(storage.VirtualMachineDataDirectory.Exists);
            Assert.IsTrue(storage.LocalBackupDirectory.Exists);

            Assert.IsTrue(storage.ScriptsDirectory.GetFiles("*.sh").Length > 0);
        }

        [TestMethod]
        public void TestDownloadFileWithCache()
        {
            var helmVersion = "3.7.2";
            var helmExecutableURL = $"https://get.helm.sh/helm-v{helmVersion}-linux-amd64.tar.gz";
            var helmExecutableSize = 13870692;
            var helmFileName = $"heml-{helmVersion}.tar";

            var storage = LocalStorageManager.Instance;
            var progressLog = new List<long>();
            var localFile = storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => progressLog.Add(totalBytesDownloaded)).GetAwaiter().GetResult();

            Assert.IsTrue(new FileInfo(localFile.FullName).Length == 45731840);
            Assert.IsTrue(progressLog.Count>0 && progressLog[progressLog.Count-1] == 45731840);

            progressLog.Clear();
            localFile = storage.DownloadFileWithCache(helmExecutableURL, helmFileName, gunzip: true, progressCallback: (totalFileSize, totalBytesDownloaded, progressPercentage) => progressLog.Add(totalBytesDownloaded)).GetAwaiter().GetResult();
            Assert.IsTrue(progressLog.Count > 0 && progressLog[progressLog.Count - 1] == 45731840);
        }

        [TestMethod]
        public void TestExtractTar()
        {
            LocalStorageManager.ExtractTar(@"C:\Users\laure\AppData\Local\wordslab\cache\heml-3.7.2.tar", @"C:\Users\laure\AppData\Local\wordslab\cache\helm-temp");

            Assert.IsTrue(File.Exists(@"C:\Users\laure\AppData\Local\wordslab\cache\helm-temp\linux-amd64\helm"));
        }
    }
}
