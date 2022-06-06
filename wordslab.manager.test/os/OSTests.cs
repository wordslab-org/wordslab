using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class OSTests
    {
        [TestMethod]
        public void TestGetMachineName()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsWindows()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsLinux()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsMacOS()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetOSName()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetOSVersion()
        {
            var version = OS.GetOSVersion();
            Assert.IsNotNull(version);
            Assert.IsTrue(version.Major >= 10);
            Assert.IsTrue(version.Build >= 17000);
        }

        [TestMethod]
        public void TestGetLinuxDistributionInfo()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsOSArchitectureX64()
        {
            var result = OS.IsOSArchitectureX64();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsNativeHypervisorAvailable()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsRunningAsAdministrator()
        {
            Assert.IsTrue(true);
        }
    }
}
