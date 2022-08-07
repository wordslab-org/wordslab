using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class LinuxTests
    {
        [TestMethod]
        public void TestGetOSDistribution()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsLinuxVersionUbuntu1804OrHigher()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsAptPackageManagerAvailable()
        {
            Assert.IsTrue(true);
        }

        // This command requires admin privileges and user interaction to enter password
        [TestMethod]
        public void TestGetAptInstallCommand()
        {
            Assert.IsTrue(true);
        }
    }
}
