using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}
