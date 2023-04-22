using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.test.storage
{
    [TestClass]
    public class ConfigStoreTests
    {
        internal static IServiceCollection GetStorageServices()
        {
            // Initialize dependency injection
            var serviceCollection = new ServiceCollection();

            // Initialize local storage and register local storage manager
            var hostStorage = new HostStorage();
            serviceCollection.AddSingleton(hostStorage);

            // Configure database connection and register an Entity Framework Core database context factory
            var databasePath = Path.Combine(hostStorage.ConfigDirectory, "wordslab-config.db");
            serviceCollection.AddDbContextFactory<ConfigStore>(options => options.UseSqlite($"Data Source={databasePath}"));

            // Create the database if it doesn't exist and initialize the host storage directories
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                ConfigStore.CreateOrUpdateDbSchemaAndInitializeHostStorage(serviceProvider);
            }

            return serviceCollection;
        }

        [TestMethod]
        [Ignore]
        public void T00_TestCreateDbIfNotExists()
        {
            // Reset environment
            var hostStorage = new HostStorage();
            hostStorage.DeleteAllDataDirectories();

            // Check that database file was properly deleted
            var databasePath = Path.Combine(hostStorage.ConfigDirectory, "wordslab-config.db");
            Assert.IsFalse(File.Exists(databasePath));

            // Initialize dependency injection for storage services
            var serviceCollection = GetStorageServices();

            // Test database file creation
            Assert.IsTrue(File.Exists(databasePath));

            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                // Test dependency injection
                var configStore = serviceProvider.GetService<ConfigStore>();

                // Test database content
                Assert.IsNotNull(configStore);
                Assert.IsTrue(configStore.HostMachineConfig == null);

                // Intialize host machine config
                var machineConfig = new HostMachineConfig(OS.GetMachineName(), hostStorage,
                    2, 4, true, 5, 10, 20, 21, 6443, 80, false, 443, true);
                configStore.InitializeHostMachineConfig(machineConfig);

                // Test host machine config
                machineConfig = configStore.HostMachineConfig;
                Assert.IsTrue(machineConfig != null);
                Assert.IsTrue(machineConfig.Processors == 2);
                Assert.IsTrue(machineConfig.MemoryGB == 4);
                Assert.IsTrue(machineConfig.CanUseGPUs == true);
                Assert.IsTrue(machineConfig.VirtualMachineClusterPath == hostStorage.VirtualMachineClusterDirectory);
                Assert.IsTrue(machineConfig.VirtualMachineClusterSizeGB == 5);
                Assert.IsTrue(machineConfig.VirtualMachineDataPath == hostStorage.VirtualMachineDataDirectory);
                Assert.IsTrue(machineConfig.VirtualMachineDataSizeGB == 10);
                Assert.IsTrue(machineConfig.BackupPath == hostStorage.BackupDirectory);
                Assert.IsTrue(machineConfig.BackupSizeGB == 20);
                Assert.IsTrue(machineConfig.SSHPort == 21);
                Assert.IsTrue(machineConfig.KubernetesPort == 6443);
                Assert.IsTrue(machineConfig.HttpPort == 80);
                Assert.IsTrue(machineConfig.CanExposeHttpOnLAN == false);
                Assert.IsTrue(machineConfig.HttpsPort == 443);
                Assert.IsTrue(machineConfig.CanExposeHttpsOnLAN == true);
            }
        }

        [TestMethod]
        public void T01_1_TestMoveHostStorageLocationTo()
        {
            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T01");

            // Get config store : access #1
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();
                var machineConfig = configStore.HostMachineConfig;
                Assert.IsTrue(machineConfig != null);

                Assert.IsTrue(storage.BackupDirectory.StartsWith(storage.AppDirectory));
                Assert.IsTrue(machineConfig.BackupPath.StartsWith(storage.AppDirectory));

                // Move backup directory under working directory
                configStore.MoveHostStorageLocationTo(StorageLocation.Backup, workdir);

                Assert.IsTrue(storage.BackupDirectory.StartsWith(workdir));
                Assert.IsTrue(machineConfig.BackupPath.StartsWith(workdir));
            }
        }

        [TestMethod]
        public void T01_2_TestMoveHostStorageLocationTo()
        {
            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T01");
            
            // Get config store : access #2
            var serviceCollection2 = GetStorageServices();
            using (var serviceProvider2 = serviceCollection2.BuildServiceProvider())
            {
                var storage2 = serviceProvider2.GetService<HostStorage>();
                var configStore2 = serviceProvider2.GetService<ConfigStore>();
                var machineConfig2 = configStore2.HostMachineConfig;
                Assert.IsTrue(machineConfig2 != null);

                Assert.IsTrue(storage2.BackupDirectory.StartsWith(workdir));
                Assert.IsTrue(machineConfig2.BackupPath.StartsWith(workdir));

                // Move backup directory back under app directory
                configStore2.MoveHostStorageLocationTo(StorageLocation.Backup, storage2.AppDirectory);

                Assert.IsTrue(storage2.BackupDirectory.StartsWith(storage2.AppDirectory));
                Assert.IsTrue(machineConfig2.BackupPath.StartsWith(storage2.AppDirectory));
            }
        }

        [TestMethod]
        public void T01_3_TestMoveHostStorageLocationTo()
        {
            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T01");

            // Get config store : access #3
            var serviceCollection3 = GetStorageServices();
            using (var serviceProvider3 = serviceCollection3.BuildServiceProvider())
            {
                var storage3 = serviceProvider3.GetService<HostStorage>();
                var configStore3 = serviceProvider3.GetService<ConfigStore>();
                var machineConfig3 = configStore3.HostMachineConfig;
                Assert.IsTrue(machineConfig3 != null);

                Assert.IsTrue(storage3.BackupDirectory.StartsWith(storage3.AppDirectory));
                Assert.IsTrue(machineConfig3.BackupPath.StartsWith(storage3.AppDirectory));
            }

            Directory.Delete(workdir, true);
        }         

        [TestMethod]
        public void T02_TestAddVirtualMachineConfig()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachines.Count() == 0);

                var vm1 = GetVMConfig1();
                configStore.AddVirtualMachineConfig(vm1);

                var vm2 = GetVMConfig2();
                configStore.AddVirtualMachineConfig(vm2);

                Assert.IsTrue(configStore.VirtualMachines.Count() == 2);
            }
        }

        private static VirtualMachineConfig GetVMConfig1()
        {
            var vmSpec1 = new VirtualMachineSpec();
            vmSpec1.Compute.Processors = 2;
            vmSpec1.Compute.MemoryGB = 4;
            vmSpec1.GPU.GPUCount = 1;
            vmSpec1.GPU.ModelName = "GTX 1050";
            vmSpec1.GPU.MemoryGB = 4;
            vmSpec1.Storage.ClusterDiskSizeGB = 5;
            vmSpec1.Storage.ClusterDiskIsSSD = true;
            vmSpec1.Storage.DataDiskSizeGB = 8;
            vmSpec1.Storage.DataDiskIsSSD = false; 

            var vm1 = new VirtualMachineConfig("vm1",
                vmSpec1, VirtualMachineProvider.Wsl, "WslVm", false,
                false, 0, false, 0, true, 8080, false, true, 8433, true);

            return vm1;
        }

        private static VirtualMachineConfig GetVMConfig2()
        {
            var vmSpec2 = new VirtualMachineSpec();
            vmSpec2.Compute.Processors = 4;
            vmSpec2.Compute.MemoryGB = 8;
            vmSpec2.Storage.ClusterDiskSizeGB = 8;
            vmSpec2.Storage.ClusterDiskIsSSD = false;
            vmSpec2.Storage.DataDiskSizeGB = 16;
            vmSpec2.Storage.DataDiskIsSSD = true;
            vmSpec2.Network.SSHPort = 22;
            vmSpec2.Network.KubernetesPort = 6444;
            vmSpec2.Network.HttpIngressPort = 81;
            vmSpec2.Network.HttpsIngressPort = 444;

            var vm2 = new VirtualMachineConfig("vm2",
                vmSpec2, VirtualMachineProvider.Qemu, "QemuVm", true,
                true, 8021, true, 8443, true, 80, true, false, 0, false);

            return vm2;
        }

        [TestMethod]
        public void T03_TestTryGetVirtualMachineConfig()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachines.Count() == 2);
                
                var vmRef1 = GetVMConfig1();
                var vm1 = configStore.TryGetVirtualMachineConfig(vmRef1.Name);
                Assert.IsTrue(vm1 != null && vm1.Equals(vmRef1));

                var vmRef2 = GetVMConfig2();
                var vm2 = configStore.TryGetVirtualMachineConfig(vmRef2.Name);
                Assert.IsTrue(vm2 != null && vm2.Equals(vmRef2));

                var vm3 = configStore.TryGetVirtualMachineConfig("vm3");
                Assert.IsTrue(vm3 == null);
            }
        }

        [TestMethod]
        public void T04_1_TestRemoveVirtualMachineConfig()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachines.Count() == 2);

                configStore.RemoveVirtualMachineConfig("vm1");
                Assert.IsTrue(configStore.VirtualMachines.Count() == 1);

                configStore.RemoveVirtualMachineConfig("vm2");
                Assert.IsTrue(configStore.VirtualMachines.Count() == 0); ;
            }
        }

        [TestMethod]
        public void T04_2_TestRemoveVirtualMachineConfig()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachines.Count() == 0);
            }
        }

        [TestMethod]
        public void T05_TestAddVirtualMachineInstance()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachines.Count() == 0);

                var vm1 = GetVMConfig1();
                configStore.AddVirtualMachineConfig(vm1);
                var vm2 = GetVMConfig2();
                configStore.AddVirtualMachineConfig(vm2);
                Assert.IsTrue(configStore.VirtualMachines.Count() == 2);

                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 0);

                var instance1_1 = GetVMInstance1(configStore, true);                
                configStore.AddVirtualMachineInstance(instance1_1);
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 1);

                var instance1_2 = GetVMInstance1(configStore, false);
                configStore.AddVirtualMachineInstance(instance1_2);
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 2);
                Assert.AreNotEqual(instance1_1.StartTimestamp, instance1_2.StartTimestamp);

                var instance2 = GetVMInstance2(configStore);
                configStore.AddVirtualMachineInstance(instance2);
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 3);
            }
        }

        private static VirtualMachineInstance GetVMInstance1(ConfigStore configStore, bool isFirst)
        {
            var vmConfig1 = configStore.TryGetVirtualMachineConfig("vm1");
            var vmInstance1 = new VirtualMachineInstance(vmConfig1.Name, vmConfig1, null, null, null);
            if(isFirst)
            {
                vmInstance1.Started(31, "127.0.0.11", "kubeconfig content1", "Everything OK1");
                vmInstance1.Stopped();
                vmInstance1.StartTimestamp = new DateTime(2022, 10, 8, 12, 10, 55);
                vmInstance1.StopTimestamp = new DateTime(2022, 10, 8, 15, 2, 21);
            }
            else
            {
                vmInstance1.Started(17, "127.0.0.12", "kubeconfig content2", "Everything OK2");
                vmInstance1.StartTimestamp = new DateTime(2022, 10, 9, 9, 44, 0);
            }
            return vmInstance1;
        }

        private static VirtualMachineInstance GetVMInstance2(ConfigStore configStore)
        {
            var vmConfig2 = configStore.TryGetVirtualMachineConfig("vm2");
            var vmInstance2 = new VirtualMachineInstance(vmConfig2.Name, vmConfig2, 
                new ComputeSpec() { Processors = 1, MemoryGB = 1 }, new GPUSpec() { GPUCount = 2, ModelName = "RTX 3090", MemoryGB = 24 }, new List<string>(new string[] { "One message", "Two messages" }));
            vmInstance2.Started(45, "127.0.0.13", "kubeconfig content3", "Everything OK3");
            vmInstance2.StartTimestamp = new DateTime(2022, 10, 10, 1, 15, 54);            
            return vmInstance2;
        }

        [TestMethod]
        public void T06_TestVirtualMachineInstances()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 3);
            }
        }

        [TestMethod]
        public void T07_TestTryGetVirtualMachineInstance()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 3);

                var instanceRef1 = GetVMInstance1(configStore, false);
                var instance1 = configStore.TryGetLastVirtualMachineInstance(instanceRef1.Name);
                Assert.IsTrue(instance1 != null && instance1.Equals(instanceRef1));

                var instanceRef2 = GetVMInstance2(configStore);
                var instance2 = configStore.TryGetLastVirtualMachineInstance(instanceRef2.Name);
                Assert.IsTrue(instance2 != null && instance2.Equals(instanceRef2));

                var instance3 = configStore.TryGetLastVirtualMachineInstance("vm3");
                Assert.IsTrue(instance3 == null);
            }
        }

        [TestMethod]
        public void T08_1_TestRemoveVirtualMachineConfigWithInstances()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachines.Count() == 2);
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 3);

                var vmRef1 = GetVMConfig1();
                configStore.RemoveVirtualMachineConfig(vmRef1.Name);
                Assert.IsTrue(configStore.VirtualMachines.Count() == 1);
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 1);

                var vmRef2 = GetVMConfig2();
                configStore.RemoveVirtualMachineConfig(vmRef2.Name);
                Assert.IsTrue(configStore.VirtualMachines.Count() == 0);
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 0);
            }
        }

        [TestMethod]
        public void T08_2_TestRemoveVirtualMachineConfigWithInstances()
        {
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.VirtualMachines.Count() == 0);
                Assert.IsTrue(configStore.VirtualMachineInstances.Count() == 0);
            }
        }
    }
}
