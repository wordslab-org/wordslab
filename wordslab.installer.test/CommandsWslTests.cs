using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using wordslab.installer.infrastructure.commands;

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
            // bool IsNvidiaGPUAvailableForWSL2()
        }

        [TestMethod]
        public void TestWindowsVersionOKForWSL2WithGPU()
        {
            // bool IsWindowsVersionOKForWSL2WithGPU()
        }

        [TestMethod]
        public void TestNvidiaDriverVersionOKForWSL2WithGPU()
        {
            // bool IsNvidiaDriverVersionOKForWSL2WithGPU()
        }

        [TestMethod]
        public void TestLinuxKernelVersionOKForWSL2WithGPU()
        {
            // bool IsLinuxKernelVersionOKForWSL2WithGPU()
        }

        [TestMethod]
        public void TestDownloadAndInstallLinuxKernelUpdatePackage()
        {
            // void DownloadAndInstallLinuxKernelUpdatePackage(LocalStorageManager localStorage)
        }

        [TestMethod]
        public void TestExec()
        {
            // int exec(string commandLine, string distribution = null, string workingDirectory = null, string userName = null, Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        }

        [TestMethod]
        public void TestExecShell()
        {
            // int execShell(string commandLine, string distribution = null, string workingDirectory = null, string userName = null, Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null)
        }

        [TestMethod]
        public void TestCheckRunningDistribution()
        {
            // bool CheckRunningDistribution(out string distribution, out string version)
        }

        [TestMethod]
        public void TestInstall()
        {
            // void install(string distributionName = "Ubuntu")
        }

        [TestMethod]
        public void TestSetDefaultVersion()
        {
            // void setDefaultVersion(int version)
        }

        [TestMethod]
        public void TestShutdown()
        {
            // void shutdown()
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
        public void TestUpdate()
        {
            // void update(bool rollback = false)
        }

        [TestMethod]
        public void TestExport()
        {
            // void export(string distribution, string filename)
        }

        [TestMethod]
        public void TestImport()
        {
            // void import(string distribution, string installPath, string filename, int? version = null)
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
            // void setDefaultDistribution(string distribution)
        }

        [TestMethod]
        public void TestSetVersion()
        {
            // void setVersion(string distribution, int version)
        }

        [TestMethod]
        public void TestTerminate()
        {
            // void terminate(string distribution)
        }

        [TestMethod]
        public void TestUnregister()
        {
            // void unregister(string distribution)
        }

    }
}