using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void T01_TestGetDrivesInfo()
        {
            // Windows : 1.6 sec
            var drives = Storage.GetDrivesInfo();
            Assert.IsTrue(drives.Count >= 2);

            var firstDrive = drives.Values.First();
            Assert.IsTrue(firstDrive.IsSSD);

            var lastDrive = drives.Values.Last();
            Assert.IsTrue(!String.IsNullOrEmpty(lastDrive.DiskId));
            Assert.IsTrue(!String.IsNullOrEmpty(lastDrive.DiskModel));            
            Assert.IsTrue(lastDrive.DiskSizeMB > 0);
            Assert.IsTrue(!String.IsNullOrEmpty(lastDrive.PartitionId));
            Assert.IsTrue(lastDrive.PartitionSizeMB > 0);
            Assert.IsTrue(!String.IsNullOrEmpty(lastDrive.DrivePath));
            Assert.IsTrue(!String.IsNullOrEmpty(lastDrive.VolumeName));
            Assert.IsTrue(lastDrive.TotalSizeMB > 0);
            Assert.IsTrue(lastDrive.FreeSpaceMB > 0);
            Assert.IsTrue(lastDrive.PercentUsedSpace > 0);
        }

        [TestMethod]
        public void T02_TestGetDirectorySizeMB()
        {
            // Windows : 4 sec
            var userFolderSizeMB = Storage.GetDirectorySizeMB(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var appFolderSizeMB = Storage.GetDirectorySizeMB(AppContext.BaseDirectory);

            Assert.IsTrue(userFolderSizeMB > 0);
            Assert.IsTrue(appFolderSizeMB > 0);
            Assert.IsTrue(userFolderSizeMB != appFolderSizeMB);

            // Directory not found
            Exception expectedEx = null;
            try
            {
                Storage.GetDirectorySizeMB(Path.Combine(AppContext.BaseDirectory, "toto"));
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
        }

        [TestMethod]
        public void T03_TestGetDriveInfoFromPath()
        {
            // Windows : 1.6 sec
            var path1 = AppContext.BaseDirectory;
            var drive1 = Storage.GetDriveInfoFromPath(path1);
            Assert.IsNotNull(drive1);
            Assert.IsTrue(path1.StartsWith(drive1.DrivePath));

            var path2 = "d:\\Drivers";
            var drive2 = Storage.GetDriveInfoFromPath(path2);
            Assert.IsNotNull(drive2);
            Assert.IsTrue(path2.StartsWith(drive2.DrivePath, StringComparison.InvariantCultureIgnoreCase)) ;
            Assert.IsTrue(!drive2.Equals(drive1));
        }

        [TestMethod]
        public void T04_TestGetDriveInfoForUserProfile()
        {
            var userdrive = Storage.GetDriveInfoForUserProfile();
            Assert.IsNotNull(userdrive);
        }

        [TestMethod]
        public void T05_TestIsPathOnSSD()
        {
            var onssd = Storage.IsPathOnSSD(AppContext.BaseDirectory);
            Assert.IsTrue(onssd);
        }

        [TestMethod]
        public void T06_TestGetApplicationDirectory()
        {
            var appdir = Storage.GetApplicationDirectory();
            Assert.IsTrue(appdir == AppContext.BaseDirectory);
        }
    }
}
