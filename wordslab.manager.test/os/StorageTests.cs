using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void TestDrivesStatus()
        {
            var drivesStatus = Storage.GetDrivesStatus();
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsTrue(drivesStatus.Count == 2);
                var status = drivesStatus["C:\\"];
                Assert.IsTrue(status.FreeSpaceMB > 80000 && status.FreeSpaceMB < 90000);
                Assert.IsFalse(status.IsNetworkDrive);
                Assert.IsTrue(status.Label == "Windows");
                Assert.IsTrue(status.PercentUsedSpace > 80 && status.PercentUsedSpace < 90);
                Assert.IsTrue(status.TotalSizeMB / 1024 == 450);
            }
        }

        [TestMethod]
        public void TestDirectorySize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var dirSizeMB = Storage.GetDirectorySizeMB(new System.IO.DirectoryInfo(@"C:\Users\laure\OneDrive\Dev\C#"));
                Assert.IsTrue(dirSizeMB > 6000 && dirSizeMB < 7000);
            }
        }
    }
}
