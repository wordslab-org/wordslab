using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WslTests
    {
        [TestMethod]
        public void TestIsWSL2AlreadyInstalled()
        {
            var wsl2Installed = Wsl.IsWSL2AlreadyInstalled();
            Assert.IsTrue(wsl2Installed);
        }

        [TestMethod]
        public void TestIsWindowsVersionOKForWSL2()
        {
            var windowsOKforWSL2 = Wsl.IsWindowsVersionOKForWSL2();
            Assert.IsTrue(windowsOKforWSL2);
        }

        [TestMethod]
        public void TestGetNvidiaGPUAvailableForWSL2()
        {
            var nvidiaGPUAvailable = Wsl.GetNvidiaGPUAvailableForWSL2();
            Assert.IsTrue(nvidiaGPUAvailable == "GTX 1050 (4 MB)");
        }

        [TestMethod]
        public void TestIsWindowsVersionOKForWSL2WithGPU()
        {
            var windowsOKforWSL2withGPU = Wsl.IsWindowsVersionOKForWSL2WithGPU();
            Assert.IsTrue(windowsOKforWSL2withGPU);
        }

        [TestMethod]
        public void TestIsNvidiaDriverVersionOKForWSL2WithGPU()
        {
            var nvidiaDriverOKforWSL2withGPU = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
            Assert.IsTrue(nvidiaDriverOKforWSL2withGPU);
        }

        [TestMethod]
        public void TestIsLinuxKernelVersionOKForWSL2WithGPU()
        {
            var nvidiaDriverOKforWSL2withGPU = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
            Assert.IsTrue(nvidiaDriverOKforWSL2withGPU);
        }

        [TestMethod]
        public void TestLegacyDownloadAndInstallLinuxKernelUpdatePackage()
        {
            /*LocalStorageManager localStorage = LocalStorageManager.Instance;
            wsl.LegacyDownloadAndInstallLinuxKernelUpdatePackage(localStorage).GetAwaiter().GetResult();
            var status = wsl.status();
            Assert.IsTrue(status.LinuxKernelVersion.Major >= 5);*/
        }

        [TestMethod]
        public void TestUpdateLinuxKernelVersion()
        {
            Wsl.UpdateLinuxKernelVersion();
            var status = Wsl.status();
            Assert.IsTrue(status.LinuxKernelVersion.Major >= 5);
        }

        [TestMethod]
        public void Testexec()
        {
            string output = null;
            Wsl.exec("cat /etc/os-release", "wordslab-os", outputHandler: o => output = o);
            Assert.IsTrue(output.Contains("NAME=\"Ubuntu\""));
        }

        [TestMethod]
        public void TestexecShell()
        {
            string output = null;
            Wsl.execShell("echo $NAME", "wordslab-os", outputHandler: o => output = o);
            Assert.IsTrue(output.StartsWith("YOGA720"));
        }

        [TestMethod]
        public void TestCheckRunningDistribution()
        {
            string distribution;
            string version; ;
            Wsl.CheckRunningDistribution("wordslab-os", out distribution, out version);
            Assert.AreEqual(distribution, "Ubuntu");
            Assert.AreEqual(version, "20.04");
        }

        [TestMethod]
        public void Testinstall()
        {
            var availableDistribs = Wsl.list(online: true);
            var distribution = availableDistribs[6].Distribution;

            var installedDistribs = Wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));

            Wsl.install(distribution);

            installedDistribs = Wsl.list();
            Assert.IsTrue(installedDistribs.Any(d => d.Distribution == distribution));

            Wsl.unregister(distribution);

            installedDistribs = Wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));
        }

        [TestMethod]
        public void TestsetDefaultVersion()
        {
            Wsl.setDefaultVersion(2);
            Assert.AreEqual(2, Wsl.status().DefaultVersion); 
        }

        [TestMethod]
        public void Testshutdown()
        {
            Wsl.exec("echo 0", "wordslab-cluster");
            Wsl.exec("echo 0", "wordslab-data");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 2);

            Wsl.shutdown();

            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 0);
        }

        [TestMethod]
        public void Teststatus()
        {
            var status = Wsl.status();

            Assert.IsNotNull(status);
            Assert.IsTrue(status.IsInstalled);
            Assert.AreEqual(status.DefaultVersion, 2);
            Assert.IsNotNull(status.DefaultDistribution);
            Assert.IsNotNull(status.LinuxKernelVersion);
            Assert.IsNotNull(status.LastWSLUpdate);
        }
        
        [TestMethod]
        public void TestRead_wslconfig()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var config = Wsl.Read_wslconfig();
                Assert.IsTrue(config != null);
            }
        }
                
        [TestMethod]
        public void TestWrite_wslconfig()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var config = Wsl.Read_wslconfig();
                Wsl.Write_wslconfig(config);
                Assert.IsTrue(config.LoadedFromFile);
            }
        }

        [TestMethod]
        public void Testexport()
        {
            Wsl.export("wordslab-cluster", @"c:\tmp\distrotest.tar");
            Assert.IsTrue(File.Exists(@"c:\tmp\distrotest.tar"));
        }

        [TestMethod]
        public void Testimport()
        {
            Wsl.import("wordslab-cluster2", @"c:\tmp", @"c:\tmp\distrotest.tar");
            Wsl.exec("echo 0", "wordslab-cluster2");
        }

        [TestMethod]
        public void Testlist()
        {
            var installedDistribs = Wsl.list();
            Assert.IsTrue(installedDistribs.Count > 0);
            foreach (var distrib in installedDistribs)
            {
                Assert.IsTrue(!String.IsNullOrEmpty(distrib.Distribution));
            }

            var onlineDistribs = Wsl.list(online: true);
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
        public void TestsetDefaultDistribution()
        {
            Assert.IsFalse(Wsl.list().Any(d => d.Distribution == "wordslab -os" && d.IsDefault));
            Wsl.setDefaultDistribution("wordslab-os");
            Assert.IsTrue(Wsl.list().Any(d => d.Distribution == "wordslab-os" && d.IsDefault));
            Wsl.setDefaultDistribution("docker-desktop");
        }

        [TestMethod]
        public void TestsetVersion()
        {
            Wsl.setVersion("wordslab-cluster", 2);
            Assert.IsTrue(Wsl.list().Any(d => d.Distribution == "wordslab-cluster" && d.WslVersion == 2));
        }

        [TestMethod]
        public void Testterminate()
        {
            Wsl.exec("echo 0", "wordslab-cluster");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 1);

            Wsl.terminate("wordslab-cluster");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 0);
        }

        [TestMethod]
        public void Testunregister()
        {
            Assert.IsTrue(Wsl.list().Any(d => d.Distribution == "Debian"));
            Wsl.unregister("Debian");
            Assert.IsFalse(Wsl.list().Any(d => d.Distribution == "Debian"));
        }

        [TestMethod]
        public void TestGetVirtualMachineWorkingSetMB()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var vmMemoryMB = Wsl.GetVirtualMachineWorkingSetMB();
                Assert.IsTrue(vmMemoryMB > 100 && vmMemoryMB < 2000);
            }
        }
    }
}
