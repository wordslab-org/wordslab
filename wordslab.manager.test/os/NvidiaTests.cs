using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class NvidiaTests
    {
        [TestMethod]
        public void TestGetDriverVersion()
        {
            var driverVersion = Nvidia.GetDriverVersion();
            Assert.IsTrue(driverVersion.Major > 0);
            Assert.IsTrue(driverVersion.Minor > 0);
            Assert.IsTrue(driverVersion.Revision == -1);
            Assert.IsTrue(driverVersion.Build == -1); ;

            if (OS.IsWindows)
            {
                Assert.IsTrue(Nvidia.IsNvidiaDriverForWindows20Sep21OrLater(driverVersion));
                Assert.IsTrue(Nvidia.IsNvidiaDriverForWindows16Nov21OrLater(driverVersion));
            }
            if(OS.IsLinux)
            {
                Assert.IsTrue(Nvidia.IsNvidiaDriverForLinux13Dec21OrLater(driverVersion));
            }
        }

        [TestMethod]
        public void TestIsNvidiaDriverForWindows20Sep21OrLater()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsNvidiaDriverForWindows16Nov21OrLater()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsNvidiaDriverForLinux13Dec21OrLater()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestTryOpenNvidiaUpdateOnWindows()
        {
            Nvidia.TryOpenNvidiaUpdateOnWindows();
        }
    }
}
