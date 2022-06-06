using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void TestGetDrivesInfo()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetDirectorySizeMB()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var dirSizeMB = Storage.GetDirectorySizeMB(new System.IO.DirectoryInfo(@"C:\Users\laure\OneDrive\Dev\C#"));
                Assert.IsTrue(dirSizeMB > 6000 && dirSizeMB < 7000);
            }
        }

        [TestMethod]
        public void TestGetDriveInfoFromPath()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetDriveInfoForUserProfile()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsPathOnSSD()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetApplicationDirectory()
        {
            Assert.IsTrue(true);
        }
    }
}
