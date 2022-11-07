using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.test.vm.wsl
{
    [TestClass]
    public class WslDiskTests
    {
        [TestMethodOnWindows]
        public void T00_DownloadWslImages()
        {
            HostStorage storage = new HostStorage();
            var dt1 = storage.DownloadFileWithCache(WslDisk.alpineImageURL, WslDisk.alpineFileName, gunzip: true);
            var dt2 = storage.DownloadFileWithCache(WslDisk.ubuntuImageURL, WslDisk.ubuntuFileName, gunzip: true);
            var dt3 = storage.DownloadFileWithCache(VirtualMachine.k3sExecutableURL, VirtualMachine.k3sExecutableFileName);
            var dt4 = storage.DownloadFileWithCache(VirtualMachine.k3sImagesURL, VirtualMachine.k3sImagesFileName, gunzip: true);
            var dt5 = storage.DownloadFileWithCache(VirtualMachine.helmExecutableURL, VirtualMachine.helmFileName, gunzip: true);
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
        }

        [TestMethodOnWindows]
        public void T01_TestCreateBlank()
        {
            var storage = new HostStorage();
            var disk = WslDisk.CreateBlank("test-blank", manager.vm.VirtualDiskFunction.Data, storage);
            Assert.IsNotNull(disk);
            Assert.IsTrue(disk.VMName == "test-blank");
            Assert.IsTrue(disk.Function == VirtualDiskFunction.Data);
            Assert.IsTrue(disk.MaxSizeGB == 256);
            Assert.IsTrue(disk.ServiceName == "wordslab-test-blank-data");
            Assert.IsTrue(disk.IsSSD);
            Assert.IsTrue(disk.IsServiceRequired());
            Assert.IsFalse(disk.IsServiceRunnig());
            Assert.IsTrue(disk.StoragePath.EndsWith(@"vm\data\wordslab-test-blank-data\ext4.vhdx"));

            Exception expectedEx = null;
            try
            {
                WslDisk.CreateBlank("test-blank", manager.vm.VirtualDiskFunction.Data, storage);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is ArgumentException);
            Assert.IsTrue(expectedEx.Message.Contains("already exists"));
        }

        [TestMethodOnWindows]
        public void T02_TestTryFindByName()
        {
            var storage = new HostStorage();
            var disk = WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);
            Assert.IsNotNull(disk);
            Assert.IsTrue(disk.VMName == "test-blank");
            Assert.IsTrue(disk.Function == VirtualDiskFunction.Data);
            Assert.IsTrue(disk.MaxSizeGB == 256);
            Assert.IsTrue(disk.ServiceName == "wordslab-test-blank-data");
            Assert.IsTrue(disk.IsSSD);
            Assert.IsTrue(disk.IsServiceRequired());
            Assert.IsFalse(disk.IsServiceRunnig());
            Assert.IsTrue(disk.StoragePath.EndsWith(@"vm\data\wordslab-test-blank-data\ext4.vhdx"));

            disk = WslDisk.TryFindByName("toto", VirtualDiskFunction.Cluster, storage);
            Assert.IsNull(disk);
        }

        [TestMethodOnWindows]
        public void T03_TestStartStopService()
        {
            var storage = new HostStorage();
            var disk = WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);

            if(disk.IsServiceRunnig())
            {
                disk.StopService();
            }
            Assert.IsFalse(disk.IsServiceRunnig());

            disk.StartService();
            Assert.IsTrue(disk.IsServiceRunnig());

            disk.StopService();
            Assert.IsFalse(disk.IsServiceRunnig());
        }

        [TestMethodOnWindows]
        public void T04_TestCreateFromOSImage()
        {
            var storage = new HostStorage();
            var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);

            var disk = WslDisk.CreateFromOSImage("test-blank", osImagePath, storage);
            Assert.IsNotNull(disk);
            Assert.IsTrue(disk.VMName == "test-blank");
            Assert.IsTrue(disk.Function == VirtualDiskFunction.Cluster);
            Assert.IsTrue(disk.MaxSizeGB == 256);
            Assert.IsTrue(disk.ServiceName == "wordslab-test-blank-cluster");
            Assert.IsTrue(disk.IsSSD);
            Assert.IsFalse(disk.IsServiceRequired());
            Assert.IsFalse(disk.IsServiceRunnig());
            Assert.IsTrue(disk.StoragePath.EndsWith(@"vm\cluster\wordslab-test-blank-cluster\ext4.vhdx"));

            Exception expectedEx = null;
            try
            {
                WslDisk.CreateFromOSImage("test-blank", osImagePath, storage);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is ArgumentException);
            Assert.IsTrue(expectedEx.Message.Contains("already exists"));
        }

        [TestMethodOnWindows]
        public void T05_TestInstallNvidiaContainerRuntimeOnOSImage()
        {
            var storage = new HostStorage();

            var clusterDisk = (WslDisk)WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Cluster, storage);
            var installOK = clusterDisk.InstallNvidiaContainerRuntimeOnOSImage(storage);
            Assert.IsTrue(installOK);
        }

        [TestMethodOnWindows]
        public void T06_TestListVMNamesFromOsDisks()
        {
            var storage = new HostStorage();
            var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);
            WslDisk.CreateFromOSImage("test-blank2", osImagePath, storage);

            var vms = WslDisk.ListVMNamesFromClusterDisks(storage);
            Assert.IsTrue(vms.Count == 2);
            Assert.IsTrue(vms[0] == "test-blank");
            Assert.IsTrue(vms[1] == "test-blank2");
        }

        [TestMethodOnWindows]
        [Ignore]
        public void T07_TestResize()
        {
            var storage = new HostStorage();
            var disk = WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);

            // Not yet implemented
            disk.Resize(5);
            Assert.IsTrue(disk.MaxSizeGB == 5);
        }

        [TestMethodOnWindows]
        public void T08_TestDelete()
        {
            var storage = new HostStorage();

            var cluster1 = WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Cluster, storage);
            var cluster2 = WslDisk.TryFindByName("test-blank2", VirtualDiskFunction.Cluster, storage);
            var data = WslDisk.TryFindByName("test-blank", VirtualDiskFunction.Data, storage);

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
