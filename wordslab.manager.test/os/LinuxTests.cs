using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class LinuxTests
    {
        [TestMethodOnLinux]
        public void T01_TestGetOSDistribution()
        {
            var distrib = Linux.GetOSDistribution();
            Assert.IsTrue(distrib.Length >= 5);
        }

        [TestMethodOnLinux]
        public void T02_TestIsLinuxVersionUbuntu1804OrHigher()
        {
            var versionok = Linux.IsLinuxVersionUbuntu1804OrHigher();
            Assert.IsTrue(versionok);
        }

        [TestMethodOnLinux]
        public void T03_TestIsAptPackageManagerAvailable()
        {
            var aptok = Linux.IsAptPackageManagerAvailable();
            Assert.IsTrue(aptok);
        }

        // This command requires admin privileges and user interaction to enter password
        [TestMethodOnLinux]
        public void T04_TestGetAptInstallCommand()
        {
            var aptcommand = Linux.GetAptInstallCommand();
            Assert.IsTrue(aptcommand.Length > 10);
        }
    }
}
