using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WslTests
    {
        [TestMethodOnWindows]
        public void T01_Teststatus()
        {
            var status = Wsl.status();

            Assert.IsNotNull(status);
            Assert.IsTrue(status.IsInstalled);
            Assert.AreEqual(status.DefaultVersion, 2);
            Assert.IsNotNull(status.DefaultDistribution);
            Assert.IsNotNull(status.LinuxKernelVersion);
            Assert.IsNotNull(status.LastWSLUpdate);
        }

        [TestMethodOnWindows]
        public void T02_TestIsWindowsVersionOKForWSL2()
        {
            var windowsOKforWSL2 = Wsl.IsWindowsVersionOKForWSL2();
            Assert.IsTrue(windowsOKforWSL2);
        }

        [TestMethodOnWindows]
        public void T03_TestIsWSL2AlreadyInstalled()
        {
            var wsl2Installed = Wsl.IsWSL2AlreadyInstalled();
            Assert.IsTrue(wsl2Installed);
        }

        [TestMethodOnWindows]
        public void T04_TestGetNvidiaGPUAvailableForWSL2()
        {
            var nvidiaGPUAvailable = Wsl.GetNvidiaGPUAvailableForWSL2();
            Assert.IsTrue(nvidiaGPUAvailable.Contains("NVIDIA"));
        }

        [TestMethodOnWindows]
        public void T05_TestIsWindowsVersionOKForWSL2WithGPU()
        {
            var windowsOKforWSL2withGPU = Wsl.IsWindowsVersionOKForWSL2WithGPU();
            Assert.IsTrue(windowsOKforWSL2withGPU);
        }

        [TestMethodOnWindows]
        public void T06_TestIsNvidiaDriverVersionOKForWSL2WithGPU()
        {
            var nvidiaDriverOKforWSL2withGPU = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
            Assert.IsTrue(nvidiaDriverOKforWSL2withGPU);
        }

        [TestMethodOnWindows]
        public void T07_TestIsLinuxKernelVersionOKForWSL2WithGPU()
        {
            var linuxKernelVersionOK = Wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
            Assert.IsTrue(linuxKernelVersionOK);
        }

        [TestMethodOnWindows]
        public void T08_TestUpdateLinuxKernelVersion()
        {
            var storage = new HostStorage();
            Wsl.UpdateLinuxKernelVersion(storage.ScriptsDirectory, storage.LogsDirectory);

            var status = Wsl.status();
            Assert.IsTrue(status.LinuxKernelVersion.Major >= 5);
        }

        [TestMethodOnWindows]
        public void T09_TestsetDefaultVersion()
        {
            Wsl.setDefaultVersion(2);
            Assert.AreEqual(2, Wsl.status().DefaultVersion);
        }

        [TestMethodOnWindows]
        public void T10_Testlist()
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

        [TestMethodOnWindows]
        public void T11_Testinstall()
        {
            var availableDistribs = Wsl.list(online: true);
            var distribution = availableDistribs.Where(d => d.Distribution == "Ubuntu-20.04").First().Distribution;

            var installedDistribs = Wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));

            Wsl.install(distribution);

            installedDistribs = Wsl.list();
            Assert.IsTrue(installedDistribs.Any(d => d.Distribution == distribution));
        }

        [TestMethodOnWindows]
        public void T12_Testunregister()
        {
            var distribution = "Ubuntu-20.04";
            Wsl.unregister(distribution);

            var installedDistribs = Wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));
        }

        [TestMethodOnWindows]
        public void T13_TestsetDefaultDistribution()
        {
            Assert.IsFalse(Wsl.list().Any(d => d.Distribution == "Ubuntu-20.04" && d.IsDefault));
            Wsl.setDefaultDistribution("Ubuntu-20.04");
            Assert.IsTrue(Wsl.list().Any(d => d.Distribution == "Ubuntu-20.04" && d.IsDefault));
            Wsl.setDefaultDistribution("Ubuntu-18.04");
        }

        [TestMethodOnWindows]
        public void T14_Testexec()
        {
            string output = null;
            Wsl.exec("cat /etc/os-release", "Ubuntu-20.04", outputHandler: o => output = o);
            Assert.IsTrue(output.Contains("NAME=\"Ubuntu\""));
        }

        [TestMethodOnWindows]
        public void T15_TestexecShell()
        {
            string output = null;
            Wsl.execShell("echo $HOME", "Ubuntu-20.04", outputHandler: o => output = o);
            Assert.IsTrue(output.StartsWith("/home"));
        }

        [TestMethodOnWindows]
        public void T16_TestCheckRunningDistribution()
        {
            string distribution;
            string version; ;
            Wsl.CheckRunningDistribution("Ubuntu-20.04", out distribution, out version);
            Assert.AreEqual(distribution, "Ubuntu");
            Assert.AreEqual(version, "20.04");
        }

        [TestMethodOnWindows]
        public void T17_Testterminate()
        {
            Wsl.exec("echo 0", "Ubuntu-18.04");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 1);

            Wsl.terminate("Ubuntu-18.04");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 0);
        }

        [TestMethodOnWindows]
        public void T18_Testshutdown()
        {
            Wsl.exec("echo 0", "Ubuntu-18.04");
            Wsl.exec("echo 0", "Ubuntu-20.04");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 2);

            Wsl.shutdown();

            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 0);
        }
        
        [TestMethodOnWindows]
        public void T19_TestRead_wslconfig()
        {
            var config = Wsl.Read_wslconfig();
            Assert.IsTrue(config != null);
            Assert.IsTrue(config.LoadedFromFile);
        }
                
        [TestMethodOnWindows]
        public void T20_TestWrite_wslconfig()
        {
            var config = Wsl.Read_wslconfig();
            config.nestedVirtualization = true;
            Wsl.Write_wslconfig(config);

            var config2 = Wsl.Read_wslconfig();
            Assert.IsTrue(config2.LoadedFromFile);
            Assert.IsTrue(config2.nestedVirtualization);
        }

        [TestMethodOnWindows]
        public void T21_TestsetVersion()
        {
            Wsl.setVersion("Ubuntu-20.04", 2);
            Assert.IsTrue(Wsl.list().Any(d => d.Distribution == "Ubuntu-20.04" && d.WslVersion == 2));
        }

        [TestMethodOnWindows]
        public void T22_Testexport()
        {
            var exportedFile = @"c:\tmp\distrotest.tar";
            if(File.Exists(exportedFile))
            {
                File.Delete(exportedFile);
            }

            Wsl.export("Ubuntu-20.04", exportedFile);
            Assert.IsTrue(File.Exists(exportedFile));
        }

        [TestMethodOnWindows]
        public void T23_Testimport()
        {
            var exportedFile = @"c:\tmp\distrotest.tar";
            if (File.Exists(exportedFile))
            {
                Wsl.import("distrotest", @"c:\tmp", exportedFile);
                File.Delete(exportedFile);

                Wsl.exec("echo 0", "distrotest");
            }

            Wsl.unregister("distrotest");
        }

        [TestMethodOnWindows]
        public void T24_TestGetVirtualMachineWorkingSetMB()
        {
            Wsl.exec("echo 0", "Ubuntu-20.04");

            var vmMemoryMB = Wsl.GetVirtualMachineWorkingSetMB();
            Assert.IsTrue(vmMemoryMB > 100 && vmMemoryMB < 2000);

            Wsl.shutdown();

            vmMemoryMB = Wsl.GetVirtualMachineWorkingSetMB();
            Assert.IsTrue(vmMemoryMB == 0);
        }
    }
}
