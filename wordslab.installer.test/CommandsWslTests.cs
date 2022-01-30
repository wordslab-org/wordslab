using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using wordslab.installer.infrastructure.commands;
using wordslab.installer.localstorage;

namespace wordslab.installer.test
{
    [TestClass]
    public class CommandsWslTests
    {
        [TestMethod]
        public void TestAlreadyInstalled()
        {
            var wsl2Installed = wsl.IsWSL2AlreadyInstalled();
            Assert.IsTrue(wsl2Installed);
        }

        [TestMethod]
        public void TestWindowsVersionOKForWSL2()
        {
            var windowsOKforWSL2 = wsl.IsWindowsVersionOKForWSL2();
            Assert.IsTrue(windowsOKforWSL2);
        }

        [TestMethod]
        public void TestVirtualizationEnabled()
        {
            var virtEnabled = wsl.IsVirtualizationEnabled();
            Assert.IsTrue(virtEnabled);
        }

        [TestMethod]
        public void TestNvidiaGPUAvailableForWSL2()
        {
            var nvidiaGPUAvailable = wsl.GetNvidiaGPUAvailableForWSL2();
            Assert.IsTrue(nvidiaGPUAvailable == "GTX 1050 (4 MB)");
        }

        [TestMethod]
        public void TestWindowsVersionOKForWSL2WithGPU()
        {
            var windowsOKforWSL2withGPU = wsl.IsWindowsVersionOKForWSL2WithGPU();
            Assert.IsTrue(windowsOKforWSL2withGPU);
        }

        [TestMethod]
        public void TestNvidiaDriverVersionOKForWSL2WithGPU()
        {
            var nvidiaDriverOKforWSL2withGPU = wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
            Assert.IsTrue(nvidiaDriverOKforWSL2withGPU);
        }

        [TestMethod]
        public void TestLinuxKernelVersionOKForWSL2WithGPU()
        {
            var linuxKernelOKforWSL2withGPU = wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
            Assert.IsTrue(linuxKernelOKforWSL2withGPU);
        }

        [TestMethod]
        public void TestLegacyDownloadAndInstallLinuxKernelUpdatePackage()
        {
            LocalStorageManager localStorage = new LocalStorageManager();
            wsl.LegacyDownloadAndInstallLinuxKernelUpdatePackage(localStorage).GetAwaiter().GetResult();
            var status = wsl.status();
            Assert.IsTrue(status.LinuxKernelVersion.Major >= 5);
        }

        [TestMethod]
        public void TestUpdateLinuxKernelVersion()
        {
            wsl.UpdateLinuxKernelVersion();
            var status = wsl.status();
            Assert.IsTrue(status.LinuxKernelVersion.Major >= 5);
        }

        [TestMethod]
        public void TestExec()
        {
            string output = null;
            wsl.exec("cat /etc/os-release", "wordslab-os", outputHandler: o => output = o);
            Assert.IsTrue(output.Contains("NAME=\"Ubuntu\""));
        }

        [TestMethod]
        public void TestExecShell()
        {
            string output = null;
            wsl.execShell("echo $NAME", "wordslab-os", outputHandler: o => output = o);
            Assert.IsTrue(output.StartsWith("YOGA720"));
        }

        [TestMethod]
        public void TestCheckRunningDistribution()
        {
            string distribution;
            string version; ;
            wsl.CheckRunningDistribution("wordslab-os", out distribution, out version);
            Assert.AreEqual(distribution, "Ubuntu");
            Assert.AreEqual(version, "20.04");
        }

        [TestMethod]
        public void TestInstall()
        {
            var availableDistribs = wsl.list(online: true);
            var distribution = availableDistribs[6].Distribution;
            
            var installedDistribs = wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));

            wsl.install(distribution);
            
            installedDistribs = wsl.list();
            Assert.IsTrue(installedDistribs.Any(d => d.Distribution == distribution));

            wsl.unregister(distribution);

            installedDistribs = wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));
        }

        [TestMethod]
        public void TestSetDefaultVersion()
        {
            wsl.setDefaultVersion(2);
            Assert.AreEqual(2, wsl.status().DefaultVersion);
        }

        [TestMethod]
        public void TestShutdown()
        {
            wsl.exec("echo 0", "wordslab-cluster");
            wsl.exec("echo 0", "wordslab-data");
            Assert.IsTrue(wsl.list().Where(d => d.IsRunning).Count() == 2);

            wsl.shutdown();

            Assert.IsTrue(wsl.list().Where(d => d.IsRunning).Count() == 0);

        }


        [TestMethod]
        public void TestStatus()
        {
            var status = wsl.status();

            Assert.IsNotNull(status);
            Assert.IsTrue(status.IsInstalled);
            Assert.AreEqual(status.DefaultVersion, 2);
            Assert.IsNotNull(status.DefaultDistribution);
            Assert.IsNotNull(status.LinuxKernelVersion);
            Assert.IsNotNull(status.LastWSLUpdate);
        }

        [TestMethod]
        public void TestExport()
        {
            wsl.export("wordslab-cluster", @"c:\tmp\distrotest.tar");
            Assert.IsTrue(File.Exists(@"c:\tmp\distrotest.tar"));
        }

        [TestMethod]
        public void TestImport()
        {
            wsl.import("wordslab-cluster2", @"c:\tmp", @"c:\tmp\distrotest.tar");
            wsl.exec("echo 0", "wordslab-cluster2");
        }

        [TestMethod]
        public void TestList()
        {
            var installedDistribs = wsl.list();
            Assert.IsTrue(installedDistribs.Count > 0);
            foreach(var distrib in installedDistribs)
            {
                Assert.IsTrue(!String.IsNullOrEmpty(distrib.Distribution));
            }

            var onlineDistribs = wsl.list(online: true);
            Assert.IsTrue(onlineDistribs.Count > 0);
            foreach (var distrib in onlineDistribs)
            {
                Assert.IsFalse(distrib.IsDefault);
                Assert.IsFalse(distrib.IsRunning);
                Assert.IsTrue(!String.IsNullOrEmpty(distrib.Distribution));
                Assert.IsTrue(!String.IsNullOrEmpty(distrib.OnlineFriendlyName));
            }
        }

        [TestMethod]
        public void TestDefaultDistribution()
        {
            Assert.IsFalse(wsl.list().Any(d => d.Distribution == "wordslab -os" && d.IsDefault));
            wsl.setDefaultDistribution("wordslab-os");
            Assert.IsTrue(wsl.list().Any(d => d.Distribution == "wordslab-os" && d.IsDefault));
            wsl.setDefaultDistribution("docker-desktop");
        }

        [TestMethod]
        public void TestSetVersion()
        {
            wsl.setVersion("wordslab-cluster", 2);
            Assert.IsTrue(wsl.list().Any(d => d.Distribution == "wordslab-cluster" && d.WslVersion == 2));
        }

        [TestMethod]
        public void TestTerminate()
        {
            wsl.exec("echo 0", "wordslab-cluster");
            Assert.IsTrue(wsl.list().Where(d => d.IsRunning).Count() == 1);

            wsl.terminate("wordslab-cluster");
            Assert.IsTrue(wsl.list().Where(d => d.IsRunning).Count() == 0);
        }

        [TestMethod]
        public void TestUnregister()
        {
            Assert.IsTrue(wsl.list().Any(d => d.Distribution == "Debian"));
            wsl.unregister("Debian");
            Assert.IsFalse(wsl.list().Any(d => d.Distribution == "Debian"));
        }

    }
}