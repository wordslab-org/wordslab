using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class OSTests
    {
        [TestMethod]
        public void T01_TestGetMachineName()
        {
            // Windows : 8 ms
            var machine = OS.GetMachineName();
            Assert.IsTrue(!string.IsNullOrEmpty(machine) && machine.Length >= 5);
        }

        [TestMethod]
        public void T02_TestIsWindows()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsTrue(OS.IsWindows);
            }
            else
            {
                Assert.IsFalse(OS.IsWindows);
            }
        }

        [TestMethod]
        public void T03_TestIsLinux()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.IsTrue(OS.IsLinux);
            }
            else
            {
                Assert.IsFalse(OS.IsLinux);
            }
        }

        [TestMethod]
        public void T04_TestIsMacOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.IsTrue(OS.IsMacOS);
            }
            else
            {
                Assert.IsFalse(OS.IsMacOS);
            }
        }

        [TestMethod]
        public void T05_TestGetOSName()
        {
            var os = OS.GetOSName();
            Assert.IsTrue(!string.IsNullOrEmpty(os) && os.Length >= 5);
        }

        [TestMethod]
        public void T06_TestGetOSVersion()
        {
            var version = OS.GetOSVersion();
            Assert.IsNotNull(version);
            Assert.IsTrue(version.Major > 1);
            Assert.IsTrue(version.Build > 1);
        }

        [TestMethodOnLinux]
        public void T07_TestGetLinuxDistributionInfo()
        {
            var distrib = OS.GetLinuxDistributionInfo();
            Assert.IsTrue(!String.IsNullOrEmpty(distrib.Name) && distrib.Name.Length >= 5);
            Assert.IsTrue(distrib.Version != null && distrib.Version.Major > 1 && distrib.Version.Build > 1);
        }

        [TestMethod]
        public void T08_TestIsOSArchitectureX64()
        {
            var archx64 = OS.IsOSArchitectureX64();
            Assert.IsTrue(archx64);
        }

        [TestMethod]
        public void T09_TestIsNativeHypervisorAvailable()
        {
            // Windows : 1700 ms
            var hyperv = OS.IsNativeHypervisorAvailable();
            Assert.IsTrue(hyperv);
        }

        [TestMethod]
        public void T10_TestIsRunningAsAdministrator()
        {
            // Windows : 5 ms
            var admin = OS.IsRunningAsAdministrator();
            Assert.IsFalse(admin);
        }
    }
}
