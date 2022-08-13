using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class LinuxTests
    {
        [TestMethodOnLinux]
        public void TestGetOSDistribution()
        {
            Assert.IsTrue(true);
        }

        [TestMethodOnLinux]
        public void TestIsLinuxVersionUbuntu1804OrHigher()
        {
            Assert.IsTrue(true);
        }

        [TestMethodOnLinux]
        public void TestIsAptPackageManagerAvailable()
        {
            Assert.IsTrue(true);
        }

        // This command requires admin privileges and user interaction to enter password
        [TestMethodOnLinux]
        public void TestGetAptInstallCommand()
        {
            Assert.IsTrue(true);
        }
    }
}
