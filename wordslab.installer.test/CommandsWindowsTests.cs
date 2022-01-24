using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.installer.infrastructure.commands;

namespace wordslab.installer.test
{
    [TestClass]
    public class CommandsWindowsTests
    {
        [TestMethod]
        public void TestOSArchitectureX64()
        {
            var result = windows10.IsOSArchitectureX64();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestOSVersion()
        {
            var version = windows10.GetOSVersion();
            Assert.IsNotNull(version);
            Assert.IsTrue(version.Major >= 10);
            Assert.IsTrue(version.Build >= 17000);
        }

        [TestMethod]
        public void TestWindows10Version1903OrHigher()
        {
            var result = windows10.IsWindows10Version1903OrHigher();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestWindows10Version21H2OrHigher()
        {
            var result = windows10.IsWindows10Version21H2OrHigher();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestWindows11Version21HOrHigher()
        {
            var result = windows10.IsWindows11Version21HOrHigher();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestOpenWindowsUpdate()
        {
            windows10.OpenWindowsUpdate();
        }

        [TestMethod]
        public void TestVirtualizationEnabled()
        {
            var result = windows10.IsVirtualizationEnabled();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestWindowsSubsystemForLinuxEnabled()
        {
            var result = windows10.IsWindowsSubsystemForLinuxEnabled();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestEnableWindowsSubsystemForLinux()
        {
            var needsRestart = windows10.EnableWindowsSubsystemForLinux();
            Assert.IsTrue(needsRestart);
        }

        [TestMethod]
        public void TestDisableWindowsSubsystemForLinux()
        {
            var needsRestart = windows10.DisableWindowsSubsystemForLinux();
            Assert.IsTrue(needsRestart);
        }

        [TestMethod]
        public void TestVirtualMachinePlatformEnabled()
        {
            var result = windows10.IsVirtualMachinePlatformEnabled();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestEnableVirtualMachinePlatform()
        {
            var needsRestart = windows10.EnableVirtualMachinePlatform();
            Assert.IsTrue(needsRestart);
        }

        [TestMethod]
        public void TestDisableVirtualMachinePlatform()
        {
            var needsRestart = windows10.DisableVirtualMachinePlatform();
            Assert.IsTrue(needsRestart);
        }

        [TestMethod]
        public void TestShutdownAndRestart()
        {
            windows10.ShutdownAndRestart();
        }
    }
}