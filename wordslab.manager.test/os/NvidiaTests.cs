using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class NvidiaTests
    {
        [TestMethodOnWindowsOrLinux]
        public void T01_TestGetDriverVersion()
        {
            // Windows : 60 ms
            var driverVersion = Nvidia.GetDriverVersion();
            Assert.IsTrue(driverVersion.Major > 0);
            Assert.IsTrue(driverVersion.Minor > 0);
            Assert.IsTrue(driverVersion.Revision == -1);
            Assert.IsTrue(driverVersion.Build == -1); ;
        }

        [TestMethodOnWindows]
        public void T02_TestIsNvidiaDriverForWindows20Sep21OrLater()
        {
            var driverVersion = Nvidia.GetDriverVersion();
            Assert.IsTrue(Nvidia.IsNvidiaDriverForWindows20Sep21OrLater(driverVersion));
        }

        [TestMethodOnWindows]
        public void T03_TestIsNvidiaDriverForWindows16Nov21OrLater()
        {
            var driverVersion = Nvidia.GetDriverVersion();
            Assert.IsTrue(Nvidia.IsNvidiaDriverForWindows16Nov21OrLater(driverVersion));
        }

        [TestMethodOnLinux]
        public void T04_TestIsNvidiaDriverForLinux13Dec21OrLater()
        {
            var driverVersion = Nvidia.GetDriverVersion();
            Assert.IsTrue(Nvidia.IsNvidiaDriverForLinux13Dec21OrLater(driverVersion));
        }

        [TestMethodOnWindows]
        public void T05_TestTryOpenNvidiaUpdateOnWindows()
        {
            Nvidia.TryOpenNvidiaUpdateOnWindows();
        }
    }
}
