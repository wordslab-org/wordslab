using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using wordslab.manager.storage;
using wordslab.manager.test.storage;
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
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                VirtualMachine vm = null;
                Exception expectedEx = null;
                try
                {
                    vm = WslVM.FindByName("test-blank", configStore, storage);
                }
                catch(Exception e)
                {
                    expectedEx = e;
                }
                Assert.IsNull(vm);
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is FileNotFoundException);
                Assert.IsTrue(expectedEx.Message.Contains("Could not find virtual disks"));

                var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);
                var clusterDisk = WslDisk.CreateFromOSImage("test-blank", osImagePath, storage);
                var dataDisk = WslDisk.CreateBlank("test-blank", manager.vm.VirtualDiskFunction.Data, storage);

                expectedEx = null;
                try
                {
                    vm = WslVM.FindByName("test-blank", configStore, storage);
                }
                catch (Exception ex)
                {
                    expectedEx = ex;
                }
                Assert.IsNull(vm);
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx is Exception);
                Assert.IsTrue(expectedEx.Message.Contains("Could not find a configuration"));

                clusterDisk.Delete();
                dataDisk.Delete();

                // Create a virtual machine here

                vm = WslVM.FindByName("test-blank", configStore, storage);
                Assert.IsNotNull(vm);
            }
        }

        [TestMethodOnWindows]
        public void T02_TestListLocalVMs()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vms = WslVM.ListLocalVMs(configStore, storage);
                Assert.IsTrue(vms.Count == 1);
                Assert.IsTrue(vms[0].Name == "test-blank");

                var osImagePath = Path.Combine(storage.DownloadCacheDirectory, WslDisk.ubuntuFileName);
                WslDisk.CreateFromOSImage("test-blank2", osImagePath, storage);
                WslDisk.CreateBlank("test-blank2", manager.vm.VirtualDiskFunction.Cluster, storage);
                WslDisk.CreateBlank("test-blank2", manager.vm.VirtualDiskFunction.Data, storage);

                vms = WslVM.ListLocalVMs(configStore, storage);
                Assert.IsTrue(vms.Count == 2);
                Assert.IsTrue(vms[1].Name == "test-blank2");
            }
        }

        [TestMethodOnWindows]
        public void T03_TestIsRunning()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vm = WslVM.FindByName("test-blank", configStore, storage);
                Assert.IsFalse(vm.IsRunning());
            }
        }

        [TestMethodOnWindows]
        public void T04_TestStart()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vm = WslVM.FindByName("test-blank", configStore, storage);
                Assert.IsFalse(vm.IsRunning());

                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                var minSpec = vmSpecs.MinimumVMSpec;
                vm.Start(minSpec.Compute, minSpec.GPU);

                Assert.IsTrue(vm.IsRunning());
            }
        }

        [TestMethod]
        public void T05_TestStop()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vm = WslVM.FindByName("test-blank", configStore, storage);
                Assert.IsTrue(vm.IsRunning());

                vm.Stop();

                Assert.IsFalse(vm.IsRunning());
            }
        }
    }
}
