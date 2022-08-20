using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.vm.qemu;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class QemuTests
    {
        [TestMethodOnLinuxOrMacOS]
        public void T00_DownloadQemuImages()
        {
            HostStorage storage = new HostStorage();
            var dt1 = storage.DownloadFileWithCache(QemuDisk.ubuntuImageURL, QemuDisk.ubuntuFileName);
            var dt2 = storage.DownloadFileWithCache(VirtualMachine.k3sExecutableURL, VirtualMachine.k3sExecutableFileName);
            var dt3 = storage.DownloadFileWithCache(VirtualMachine.k3sImagesURL, VirtualMachine.k3sImagesFileName, gunzip: true);
            var dt4 = storage.DownloadFileWithCache(VirtualMachine.helmExecutableURL, VirtualMachine.helmFileName, gunzip: true);
            Task.WaitAll(dt1, dt2, dt3, dt4);

            // Extract helm executable from the downloaded tar file
            var helmExecutablePath = Path.Combine(storage.DownloadCacheDirectory, "helm");
            if (!File.Exists(helmExecutablePath))
            {
                var helmTarFile = Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.helmFileName);
                var helmTmpDir = Path.Combine(storage.DownloadCacheDirectory, "helm-temp");
                Directory.CreateDirectory(helmTmpDir);
                HostStorage.ExtractTarFile(helmTarFile, helmTmpDir);
                File.Move(Path.Combine(helmTmpDir, "linux-amd64", "helm"), helmExecutablePath);
                Directory.Delete(helmTmpDir, true);
            }
        }

        [TestMethodOnLinuxOrMacOS]
        public void T01_TestIsInstalled()
        {
            var qemuok = Qemu.IsInstalled();
            Assert.IsTrue(qemuok);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T02_TestGetInstalledVersion()
        {
            var qemuversion = Qemu.GetInstalledVersion();
            Assert.IsTrue(qemuversion.Major > 4 && qemuversion.Build > 1);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T03_TestIsCDRomToolInstalled()
        {
            var cdrtoolok = Qemu.IsCDRomToolInstalled();
            Assert.IsTrue(cdrtoolok);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T04_TestIsOsVersionOKForQemu()
        {
            var versionok = Qemu.IsOsVersionOKForQemu();
            Assert.IsTrue(versionok);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T05_TestIsInstalledVersionSupported()
        {
            var versionok = Qemu.IsInstalledVersionSupported();
            Assert.IsTrue(versionok);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T06_TestInstall()
        {
            var qemuversion = Qemu.Install();
            Assert.IsTrue(qemuversion.Major > 4 && qemuversion.Build > 1);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T07_TestInstallCDRomTool()
        {
            var cdrtoolversion = Qemu.Install();
            Assert.IsTrue(cdrtoolversion.Major > 1 && cdrtoolversion.Build > 1);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T08_TestGetLinuxInstallCommand()
        {
            var installcommand = Qemu.GetLinuxInstallCommand();
            Assert.IsTrue(installcommand.Length > 10);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T09_TestCreateVirtualDisk()
        {
            var tmpDir = Path.Combine(AppContext.BaseDirectory, "test-quemu");
            Directory.CreateDirectory(tmpDir);

            var clusterDiskFilePath = Path.Combine(tmpDir, "clusterDisk"); 
            var dataDiskFilePath = Path.Combine(tmpDir, "dataDisk");

            // Path OK
            Qemu.CreateVirtualDisk(clusterDiskFilePath, 1);
            var file = new FileInfo(clusterDiskFilePath);   
            Assert.IsTrue(file.Exists && file.Length > 50000);

            Qemu.CreateVirtualDisk(dataDiskFilePath, 1);

            // Path error
            Exception expectedEx = null;
            try
            {
                Qemu.CreateVirtualDisk(tmpDir+"toto", 1);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
            Assert.IsTrue(expectedEx.Message.Contains("path"));

            Directory.Delete(tmpDir, true);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T10_TestCreateVirtualDiskFromOsImageWithCloudInit()
        {
            var tmpDir = Path.Combine(AppContext.BaseDirectory, "test-quemu");
            Directory.CreateDirectory(tmpDir);

            HostStorage storage = new HostStorage();
            var osImagePath = Path.Combine(storage.DownloadCacheDirectory, QemuDisk.ubuntuFileName);

            var scriptsPath = Path.Combine(storage.DownloadCacheDirectory, "scripts", "linux");
            var metadataFilePath = Path.Combine(scriptsPath, QemuDisk.metadataFile);
            var userdataTemplatePath = Path.Combine(scriptsPath, QemuDisk.userdataFile);

            var osDiskFilePath = Path.Combine(tmpDir, "osDisk");

            // Path OK
            Qemu.CreateVirtualDiskFromOsImageWithCloudInit(osDiskFilePath, osImagePath, metadataFilePath, userdataTemplatePath);
            var file = new FileInfo(osDiskFilePath);
            Assert.IsTrue(file.Exists && file.Length > 50000);

            // Path error
            Exception expectedEx = null;
            try
            {
                Qemu.CreateVirtualDiskFromOsImageWithCloudInit(osDiskFilePath+"2", osImagePath+"toto", metadataFilePath, userdataTemplatePath);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
            Assert.IsTrue(expectedEx.Message.Contains("path"));
        }

        [TestMethodOnLinuxOrMacOS]
        public void T11_TestGetVirtualDiskSizeGB()
        {
            var tmpDir = Path.Combine(AppContext.BaseDirectory, "test-quemu");
            Directory.CreateDirectory(tmpDir);

            var tmpDiskFilePath = Path.Combine(tmpDir, "tmpDisk");

            Qemu.CreateVirtualDisk(tmpDiskFilePath, 2);
            var disksize = Qemu.GetVirtualDiskSizeGB(tmpDiskFilePath);
            Assert.IsTrue(disksize == 2);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T12_TestStartVirtualMachine()
        {
            var tmpDir = Path.Combine(AppContext.BaseDirectory, "test-quemu");
            var osDiskFilePath = Path.Combine(tmpDir, "osDisk");
            var clusterDiskFilePath = Path.Combine(tmpDir, "clusterDisk");
            var dataDiskFilePath = Path.Combine(tmpDir, "dataDisk");

            // Everything ok
            var pid = Qemu.StartVirtualMachine(2, 2, osDiskFilePath, clusterDiskFilePath, dataDiskFilePath);
            var process = Qemu.TryFindVirtualMachineProcess(osDiskFilePath);
            Assert.IsTrue(process.PID == pid);
            Qemu.StopVirtualMachine(pid);

            // Too much processors
            Exception expectedEx = null;
            try
            {
                Qemu.StartVirtualMachine(2000, 2, osDiskFilePath, clusterDiskFilePath, dataDiskFilePath);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is InvalidOperationException);
            Assert.IsTrue(expectedEx.Message.Contains("cpu"));

            // Too much memory
            expectedEx = null;
            try
            {
                Qemu.StartVirtualMachine(2, 2000, osDiskFilePath, clusterDiskFilePath, dataDiskFilePath);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is InvalidOperationException);
            Assert.IsTrue(expectedEx.Message.Contains("memory"));

            // Path error
            expectedEx = null;
            try
            {
                Qemu.StartVirtualMachine(2, 2, osDiskFilePath+"toto", clusterDiskFilePath, dataDiskFilePath);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
            Assert.IsTrue(expectedEx.Message.Contains("path"));
        }

        [TestMethodOnLinuxOrMacOS]
        public void T13_TestTryFindVirtualMachineProcess()
        {
            var tmpDir = Path.Combine(AppContext.BaseDirectory, "test-quemu");
            var osDiskFilePath = Path.Combine(tmpDir, "osDisk");
            var clusterDiskFilePath = Path.Combine(tmpDir, "clusterDisk");
            var dataDiskFilePath = Path.Combine(tmpDir, "dataDisk");

            // VM started
            var pid = Qemu.StartVirtualMachine(2, 2, osDiskFilePath, clusterDiskFilePath, dataDiskFilePath);
            var process = Qemu.TryFindVirtualMachineProcess(osDiskFilePath);
            Assert.IsTrue(process.PID == pid);

            // VM stopped
            Qemu.StopVirtualMachine(pid);
            process = Qemu.TryFindVirtualMachineProcess(osDiskFilePath);
            Assert.IsTrue(process == null);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T14_TestStopVirtualMachine()
        {
            T13_TestTryFindVirtualMachineProcess();
        }

        [TestCleanup]
        public void DeleteTmpDir()
        {
            var tmpDir = Path.Combine(AppContext.BaseDirectory, "test-quemu");
            Directory.Delete(tmpDir, true);
        }
    }
}
