using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using wordslab.manager.storage;
using wordslab.manager.storage.config;

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
                ConfigStore.CreateDbIfNotExistsAndInitializeHostStorage(serviceProvider);
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
                Assert.IsTrue(configStore.HostDirectories.Count() == 5);
            }
        }

        [TestMethod]
        public void T01_1_TestMoveHostDirectoryTo()
        {
            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T01");

            // Get config store : access #1
            var serviceCollection = GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();
                Assert.IsTrue(configStore.HostDirectories.Count() == 5);

                Assert.IsTrue(storage.BackupDirectory.StartsWith(storage.AppDirectory));
                var backupPath = configStore.HostDirectories.Where(d => d.Function == HostDirectory.StorageFunction.Backup).First().Path;
                Assert.IsTrue(backupPath.StartsWith(storage.AppDirectory));

                // Move backup directory under working directory
                configStore.MoveHostDirectoryTo(HostDirectory.StorageFunction.Backup, workdir);

                Assert.IsTrue(storage.BackupDirectory.StartsWith(workdir));
                backupPath = configStore.HostDirectories.Where(d => d.Function == HostDirectory.StorageFunction.Backup).First().Path;
                Assert.IsTrue(backupPath.StartsWith(workdir));
            }
        }

        [TestMethod]
        public void T01_2_TestMoveHostDirectoryTo()
        {
            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T01");
            
            // Get config store : access #2
            var serviceCollection2 = GetStorageServices();
            using (var serviceProvider2 = serviceCollection2.BuildServiceProvider())
            {
                var storage2 = serviceProvider2.GetService<HostStorage>();
                var configStore2 = serviceProvider2.GetService<ConfigStore>();
                Assert.IsTrue(configStore2.HostDirectories.Count() == 5);

                Assert.IsTrue(storage2.BackupDirectory.StartsWith(workdir));
                var backupPath2 = configStore2.HostDirectories.Where(d => d.Function == HostDirectory.StorageFunction.Backup).First().Path;
                Assert.IsTrue(backupPath2.StartsWith(workdir));

                // Move backup directory back under app directory
                configStore2.MoveHostDirectoryTo(HostDirectory.StorageFunction.Backup, storage2.AppDirectory);

                Assert.IsTrue(storage2.BackupDirectory.StartsWith(storage2.AppDirectory));
                backupPath2 = configStore2.HostDirectories.Where(d => d.Function == HostDirectory.StorageFunction.Backup).First().Path;
                Assert.IsTrue(backupPath2.StartsWith(storage2.AppDirectory));
            }
        }

        [TestMethod]
        public void T01_3_TestMoveHostDirectoryTo()
        {
            var workdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "wordslab-tmp-T01");

            // Get config store : access #3
            var serviceCollection3 = GetStorageServices();
            using (var serviceProvider3 = serviceCollection3.BuildServiceProvider())
            {
                var storage3 = serviceProvider3.GetService<HostStorage>();
                var configStore3 = serviceProvider3.GetService<ConfigStore>();
                Assert.IsTrue(configStore3.HostDirectories.Count() == 5);

                Assert.IsTrue(storage3.BackupDirectory.StartsWith(storage3.AppDirectory));
                var backupPath3 = configStore3.HostDirectories.Where(d => d.Function == HostDirectory.StorageFunction.Backup).First().Path;
                Assert.IsTrue(backupPath3.StartsWith(storage3.AppDirectory));
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
            var vm1 = new VirtualMachineConfig();

            vm1.Type = VirtualMachineType.Wsl;
            vm1.Name = "vm1";
            vm1.Processors = 2;
            vm1.MemoryGB = 4;
            vm1.GPUModel = "GTX 1050";
            vm1.GPUMemoryGB = 4;
            vm1.VmDiskSizeGB = 1;
            vm1.VmDiskIsSSD = true;
            vm1.ClusterDiskSizeGB = 4;
            vm1.ClusterDiskIsSSD = true;
            vm1.DataDiskSizeGB = 8;
            vm1.DataDiskIsSSD = false;
            vm1.HostSSHPort = 21;
            vm1.HostKubernetesPort = 6443;
            vm1.HostHttpIngressPort = 80;

            return vm1;
        }

        private static VirtualMachineConfig GetVMConfig2()
        {
            var vm1 = new VirtualMachineConfig();

            vm1.Type = VirtualMachineType.Wsl;
            vm1.Name = "vm2";
            vm1.Processors = 4;
            vm1.MemoryGB = 8;
            vm1.GPUModel = null;
            vm1.GPUMemoryGB = 0;
            vm1.VmDiskSizeGB = 2;
            vm1.VmDiskIsSSD = false;
            vm1.ClusterDiskSizeGB = 8;
            vm1.ClusterDiskIsSSD = false;
            vm1.DataDiskSizeGB = 16;
            vm1.DataDiskIsSSD = true;
            vm1.HostSSHPort = 22;
            vm1.HostKubernetesPort = 6444;
            vm1.HostHttpIngressPort = 81;

            return vm1;
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

                var vmRef1 = GetVMConfig1();
                configStore.RemoveVirtualMachineConfig(vmRef1.Name);
                Assert.IsTrue(configStore.VirtualMachines.Count() == 1);

                var vmRef2 = GetVMConfig2();
                configStore.RemoveVirtualMachineConfig(vmRef2.Name);
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
    }
}
