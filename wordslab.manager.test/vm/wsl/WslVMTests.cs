using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.test.vm.wsl
{
    [TestClass]
    public class WslVMTests
    {
        [TestMethodOnWindows]
        public void T01_TestTryFindByName()
        {
            var storage = new HostStorage();
            var vm = WslVM.TryFindByName("test-blank", storage);
            Assert.IsNull(vm);

            var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);
            WslDisk.CreateFromOSImage("test-blank", osImagePath, storage);

            Exception expectedEx = null;
            try
            {
                WslVM.TryFindByName("test-blank", storage);
            }
            catch (Exception ex)
            {
                expectedEx = ex;
            }
            Assert.IsNotNull(expectedEx);
            Assert.IsTrue(expectedEx is FileNotFoundException);
            Assert.IsTrue(expectedEx.Message.Contains("Could not find virtual disks"));

            WslDisk.CreateBlank("test-blank", manager.vm.VirtualDiskFunction.Cluster, storage);
            WslDisk.CreateBlank("test-blank", manager.vm.VirtualDiskFunction.Data, storage);

            vm = WslVM.TryFindByName("test-blank", storage);
            Assert.IsNotNull(vm);
        }

        [TestMethodOnWindows]
        public void T02_TestListLocalVMs()
        {
            var storage = new HostStorage();
            var vms = WslVM.ListLocalVMs(storage);
            Assert.IsTrue(vms.Count == 1);
            Assert.IsTrue(vms[0].Name == "test-blank");

            var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);
            WslDisk.CreateFromOSImage("test-blank2", osImagePath, storage);
            WslDisk.CreateBlank("test-blank2", manager.vm.VirtualDiskFunction.Cluster, storage);
            WslDisk.CreateBlank("test-blank2", manager.vm.VirtualDiskFunction.Data, storage);

            vms = WslVM.ListLocalVMs(storage);
            Assert.IsTrue(vms.Count == 2);
            Assert.IsTrue(vms[1].Name == "test-blank2");
        }

        [TestMethodOnWindows]
        public void T03_TestIsRunning()
        {
            var storage = new HostStorage();
            var vm = WslVM.TryFindByName("test-blank", storage);
            Assert.IsFalse(vm.IsRunning());
        }

        [TestMethodOnWindows]
        public void T04_TestStart()
        {
            var storage = new HostStorage();
            var vm = WslVM.TryFindByName("test-blank", storage);
            Assert.IsFalse(vm.IsRunning());

            VirtualMachineSpec minSpec, recSpec, maxSpec;
            string minErrMsg, recErrMsg;
            VirtualMachineSpec.GetRecommendedVMSpecs(storage, out minSpec, out recSpec, out maxSpec, out minErrMsg, out recErrMsg);
            vm.Start(minSpec);

            Assert.IsTrue(vm.IsRunning());
        }

        [TestMethod]
        public void T05_TestStop()
        {
            var storage = new HostStorage();
            var vm = WslVM.TryFindByName("test-blank", storage);
            Assert.IsTrue(vm.IsRunning());

            vm.Stop();

            Assert.IsFalse(vm.IsRunning());
        }
    }
}
