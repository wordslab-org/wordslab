using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using wordslab.manager.storage;
using wordslab.manager.vm.qemu;
using wordslab.manager.vm;
using System;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.test.vm.qemu
{
    [TestClass]
    public class QemuDiskTests
    {

        [TestMethodOnLinuxOrMacOS]
        public void T00_DownloadQemuImages()
        {
            HostStorage storage = new HostStorage();
            var dt1 = storage.DownloadFileWithCache(QemuDisk.ubuntuImageURL, QemuDisk.ubuntuFileName, gunzip: true);
            var dt2 = storage.DownloadFileWithCache(VirtualMachine.k3sExecutableURL, VirtualMachine.k3sExecutableFileName);
            var dt3 = storage.DownloadFileWithCache(VirtualMachine.k3sImagesURL, VirtualMachine.k3sImagesFileName, gunzip: true);
            var dt4 = storage.DownloadFileWithCache(VirtualMachine.helmExecutableURL, VirtualMachine.helmFileName, gunzip: true);
            var dt5 = storage.DownloadFileWithCache(VirtualMachine.nerdctlBundleURL, VirtualMachine.nerdctlFileName, gunzip: true);
            Task.WaitAll(dt1, dt2, dt3, dt4, dt5);

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

            // Extract nerdctl executable from the downloaded tar file
            var nerdctlExecutablePath = Path.Combine(storage.DownloadCacheDirectory, "nerdctl");
            var buildctlExecutablePath = Path.Combine(storage.DownloadCacheDirectory, "buildctl");
            var buildkitdExecutablePath = Path.Combine(storage.DownloadCacheDirectory, "buildkitd");
            if (!File.Exists(nerdctlExecutablePath) || !File.Exists(buildctlExecutablePath) || !File.Exists(buildkitdExecutablePath))
            {
                var nerdctlTarFile = Path.Combine(storage.DownloadCacheDirectory, VirtualMachine.nerdctlFileName);
                var nerdctlTmpDir = Path.Combine(storage.DownloadCacheDirectory, "nerdctl-temp");
                Directory.CreateDirectory(nerdctlTmpDir);
                HostStorage.ExtractTarFile(nerdctlTarFile, nerdctlTmpDir);
                File.Move(Path.Combine(nerdctlTmpDir, "bin", "nerdctl"), nerdctlExecutablePath);
                File.Move(Path.Combine(nerdctlTmpDir, "bin", "buildctl"), buildctlExecutablePath);
                File.Move(Path.Combine(nerdctlTmpDir, "bin", "buildkitd"), buildkitdExecutablePath);
                Directory.Delete(nerdctlTmpDir, true);
            }
        }

        [TestMethodOnLinuxOrMacOS]
        public void T01_TestCreateBlank()
        {
            var storage = new HostStorage();
            var disk = QemuDisk.CreateBlank("test-blank", 5, storage);
            Assert.IsNotNull(disk);
            Assert.IsTrue(disk.VMName == "test-blank");
            Assert.IsTrue(disk.Function == VirtualDiskFunction.Data);
            Assert.IsTrue(disk.MaxSizeGB == 5);
            Assert.IsTrue(disk.ServiceName == "wordslab-test-blank-data");
            Assert.IsTrue(disk.IsSSD);
            Assert.IsFalse(disk.IsServiceRequired());
            Assert.IsTrue(disk.IsServiceRunnig());
            Assert.IsTrue(disk.StoragePath.EndsWith("vm/data/wordslab-test-blank-data.img"));

            Exception expectedEx = null;
            try
            {
                QemuDisk.CreateBlank("test-blank", 5, storage);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is ArgumentException);
            Assert.IsTrue(expectedEx.Message.Contains("already exists"));
        }

        [TestMethodOnLinuxOrMacOS]
        public void T02_TestTryFindByName()
        {
            var storage = new HostStorage();
            var disk = QemuDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);
            Assert.IsNotNull(disk);
            Assert.IsTrue(disk.VMName == "test-blank");
            Assert.IsTrue(disk.Function == VirtualDiskFunction.Data);
            Assert.IsTrue(disk.MaxSizeGB == 5);
            Assert.IsTrue(disk.ServiceName == "wordslab-test-blank-data");
            Assert.IsTrue(disk.IsSSD);
            Assert.IsFalse(disk.IsServiceRequired());
            Assert.IsTrue(disk.IsServiceRunnig());
            Assert.IsTrue(disk.StoragePath.EndsWith("vm/data/wordslab-test-blank-data.img"));

            disk = QemuDisk.TryFindByName("toto", VirtualDiskFunction.Cluster, storage);
            Assert.IsNull(disk);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T03_TestCreateFromOSImage()
        {
            var storage = new HostStorage();
            var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);

            var disk = QemuDisk.CreateFromOSImage("test-blank", osImagePath, 5, storage);
            Assert.IsNotNull(disk);
            Assert.IsTrue(disk.VMName == "test-blank");
            Assert.IsTrue(disk.Function == VirtualDiskFunction.Cluster);
            Assert.IsTrue(disk.MaxSizeGB == 5);
            Assert.IsTrue(disk.ServiceName == "wordslab-test-blank-cluster");
            Assert.IsTrue(disk.IsSSD);
            Assert.IsFalse(disk.IsServiceRequired());
            Assert.IsTrue(disk.IsServiceRunnig());
            Assert.IsTrue(disk.StoragePath.EndsWith("vm/cluster/wordslab-test-blank-cluster.img"));

            Exception expectedEx = null;
            try
            {
                QemuDisk.CreateFromOSImage("test-blank", osImagePath, 5, storage);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is ArgumentException);
            Assert.IsTrue(expectedEx.Message.Contains("already exists"));
        }

        [TestMethodOnLinuxOrMacOS]
        public void T04_TestListVMNamesFromOsDisks()
        {
            var storage = new HostStorage();
            var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);
            QemuDisk.CreateFromOSImage("test-blank2", osImagePath, 5, storage);

            var vms = QemuDisk.ListVMNamesFromClusterDisks(storage);
            Assert.IsTrue(vms.Count == 2);
            Assert.IsTrue(vms[0] == "test-blank");
            Assert.IsTrue(vms[1] == "test-blank2");
        }

        [TestMethodOnLinuxOrMacOS]
        [Ignore]
        public void T05_TestResize()
        {
            var storage = new HostStorage();
            var disk = QemuDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);

            // Not yet implemented
            disk.Resize(5);
            Assert.IsTrue(disk.MaxSizeGB == 5);
        }

        [TestMethodOnLinuxOrMacOS]
        public void T06_TestDelete()
        {
            var storage = new HostStorage();

            var cluster1 = QemuDisk.TryFindByName("test-blank", VirtualDiskFunction.Cluster, storage);
            var cluster2 = QemuDisk.TryFindByName("test-blank2", VirtualDiskFunction.Cluster, storage);
            var data = QemuDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);

            Assert.IsTrue(Directory.GetFileSystemEntries(storage.VirtualMachineClusterDirectory).Length == 3);
            Assert.IsNotNull(cluster1);
            Assert.IsNotNull(cluster2);
            Assert.IsTrue(Directory.GetFileSystemEntries(storage.VirtualMachineDataDirectory).Length == 1);
            Assert.IsNotNull(data);

            cluster1.Delete();
            cluster2.Delete();
            data.Delete();

            Assert.IsTrue(Directory.GetFileSystemEntries(storage.VirtualMachineClusterDirectory).Length == 1);
            Assert.IsTrue(Directory.GetFileSystemEntries(storage.VirtualMachineDataDirectory).Length == 0);

            cluster1 = WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Cluster, storage);
            cluster2 = WslDisk.TryFindByName("test-blank2", VirtualDiskFunction.Cluster, storage);
            data = WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);

            Assert.IsNull(cluster1);
            Assert.IsNull(cluster2);
            Assert.IsNull(data);
        }
    }
}
