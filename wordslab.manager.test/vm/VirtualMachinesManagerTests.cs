using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.test.storage;
using wordslab.manager.vm;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.test.vm
{
    [TestClass]
    public class VirtualMachinesManagerTests
    {
        [TestMethod]
        public async Task T01_TestConfigureHostMachine()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmm = new VirtualMachinesManager(storage, configStore);
                Assert.IsNotNull(vmm);

                Assert.IsNull(configStore.HostMachineConfig);

                var ui = new TestProcessUI();
                var hostMachineConfig = await vmm.ConfigureHostMachine(ui);

                Assert.IsNotNull(hostMachineConfig);
                Assert.IsTrue(hostMachineConfig.Processors == 6);
            }
        }

        [TestMethod]
        public async Task T02_TestCreateLocalVM()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmm = new VirtualMachinesManager(storage, configStore);
                Assert.IsNotNull(vmm);

                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();

                // Hardware requirements KO
                var vmName = "test-blank";
                var recSpec = vmSpecs.RecommendedVMSpec;
                recSpec.Compute.MemoryGB = 128;
                var config1 = new VirtualMachineConfig(vmName, recSpec,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);

                var ui = new TestProcessUI();
                Exception expectedEx = null;
                try
                {
                    await vmm.CreateLocalVM(config1, ui);
                }
                catch(Exception e)
                {
                    expectedEx = e;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx.Message.Contains("GB memory are requested to create the VM but"));

                var localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 0);

                // Hardware requirements OK
                var maxSpec = vmSpecs.MaximumVMSpecOnThisMachine;
                maxSpec.Compute.Processors = (Compute.GetCPUInfo().NumberOfLogicalProcessors - VMRequirements.MIN_HOST_RESERVED_PROCESSORS) / 2;
                maxSpec.Compute.MemoryGB = ((int)Memory.GetMemoryInfo().TotalPhysicalMB/1024 - VMRequirements.MIN_HOST_RESERVED_MEMORY_GB) / 2;
                var config2 = new VirtualMachineConfig(vmName, maxSpec,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);

                ui = new TestProcessUI();
                var vm1 = await vmm.CreateLocalVM(config2, ui);
                Assert.IsNotNull(vm1);

                vm1.Start();
                Assert.IsTrue(vm1.IsRunning());

                localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 1);

                // Virtual machine already exists
                ui = new TestProcessUI();
                expectedEx = null;
                try
                {
                    var vm = await vmm.CreateLocalVM(config2, ui);
                }
                catch(Exception e)
                {
                    expectedEx = e;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx.Message.Contains("already exists"));

                // 2nd virtual machine - ports conflict
                vmName = "test-blank2";
                var vmSpecs2 = VMRequirements.GetRecommendedVMSpecs();
                var maxSpec2 = vmSpecs2.MaximumVMSpecOnThisMachine;
                maxSpec2.Compute.Processors = (Compute.GetCPUInfo().NumberOfLogicalProcessors - VMRequirements.MIN_HOST_RESERVED_PROCESSORS) / 2;
                maxSpec2.Compute.MemoryGB = ((int)Memory.GetMemoryInfo().TotalPhysicalMB / 1024 - VMRequirements.MIN_HOST_RESERVED_MEMORY_GB) / 2;
                var config3 = new VirtualMachineConfig(vmName, maxSpec2,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);

                ui = new TestProcessUI();
                var vm2 = await vmm.CreateLocalVM(config3, ui);
                Assert.IsNotNull(vm2);

                localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 2);

                expectedEx = null;
                try
                {
                    vm2.Start();
                }
                catch(Exception e)
                {
                    expectedEx= e;
                }
                Assert.IsNotNull(expectedEx);
                Assert.IsTrue(expectedEx.Message.Contains("is already in use"));

                // Stop first vm and try again ...
                Assert.IsTrue(vm1.IsRunning());
                vm1.Stop();
                Assert.IsFalse(vm1.IsRunning());

                Assert.IsFalse(vm2.IsRunning());
                vm2.Start();
                Assert.IsTrue(vm2.IsRunning());
                vm2.Stop();
                Assert.IsFalse(vm2.IsRunning());
            }
        }

        [TestMethod]
        public void T03_TestListLocalVMs()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmm = new VirtualMachinesManager(storage, configStore);
                Assert.IsNotNull(vmm);

                var vms = vmm.ListLocalVMs();

                Assert.IsTrue(vms.Count == 2);
                var vm1 = vms[0];
                var vm2 = vms[1];

                Assert.IsTrue(vm1.Name == "test-blank");
                Assert.IsTrue(vm2.Name == "test-blank2");  

                Assert.IsFalse(vm1.IsRunning());
                Assert.IsFalse(vm2.IsRunning());

                var vmInstance1 = configStore.TryGetLastVirtualMachineInstance(vm1.Name);
                Assert.IsNotNull(vmInstance1);
                var vmInstance2 = configStore.TryGetLastVirtualMachineInstance(vm2.Name);
                Assert.IsNotNull(vmInstance2);

            }
        }

        [TestMethod]
        public void T04_TestTryFindLocalVM()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmm = new VirtualMachinesManager(storage, configStore);
                Assert.IsNotNull(vmm);

                var vm1 = vmm.TryFindLocalVM("test-blank");
                Assert.IsNotNull(vm1);
                if (vm1.IsRunning())
                {
                    vm1.Stop();
                }
                Assert.IsFalse(vm1.IsRunning());

                var portsbefore = Network.GetAllTcpPortsInUse();
                vm1.Start();
                var portsafterStart = Network.GetAllTcpPortsInUse();
                var addedports = portsafterStart.Except(portsbefore);
                vm1.Stop();
                var portsafterStop = Network.GetAllTcpPortsInUse();
                var removedports = portsafterStart.Except(portsafterStop);
                Assert.IsTrue(addedports.Count() == 4);
                Assert.IsTrue(removedports.Count() == 4);

                var vm2 = vmm.TryFindLocalVM("test-blank2");
                Assert.IsNotNull(vm2);
                if (vm2.IsRunning()) 
                {
                    vm2.Stop();
                }
                Assert.IsFalse(vm2.IsRunning());
                vm2.Start();
                Assert.IsTrue(vm2.IsRunning());

                var vm2Instance = configStore.TryGetLastVirtualMachineInstance("test-blank2");
                Assert.IsNotNull(vm2Instance);
                Assert.IsTrue(vm2Instance.State == VirtualMachineState.Running);
                Assert.IsTrue(DateTime.Now.Subtract(vm2Instance.StartTimestamp).TotalSeconds <= 60);
                Assert.IsTrue(vm2Instance.StopTimestamp == null);
                                
                vm2.Stop();
                Assert.IsFalse(vm2.IsRunning());

                vm2Instance = configStore.TryGetLastVirtualMachineInstance("test-blank2");
                Assert.IsNotNull(vm2Instance);
                Assert.IsTrue(vm2Instance.State == VirtualMachineState.Stopped);
                Assert.IsTrue(DateTime.Now.Subtract(vm2Instance.StopTimestamp.Value).TotalSeconds <= 60);

                var vmInstances = configStore.GetVirtualMachineInstancesHistory("test-blank");
                Assert.IsTrue(vmInstances.Count >= 2);
            }
        }

        [TestMethod]
        public async Task T05_TestDeleteLocalVM()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmm = new VirtualMachinesManager(storage, configStore);
                Assert.IsNotNull(vmm);

                var localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 2);

                var ui = new TestProcessUI();
                var vmName = "test-blank";
                var success = await vmm.DeleteLocalVM(vmName, ui);
                Assert.IsTrue(success);
                Assert.IsTrue(ui.Messages.Last().Contains("OK"));

                localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 1);

                var deletedVm = vmm.TryFindLocalVM(vmName);
                Assert.IsNull(deletedVm);
                var deletedConfig = configStore.TryGetVirtualMachineConfig(vmName);
                Assert.IsNull(deletedConfig);
                var deletedInstance = configStore.TryGetLastVirtualMachineInstance(vmName);
                Assert.IsNull(deletedInstance);

                ui = new TestProcessUI();
                vmName = "test-blank2";
                success = await vmm.DeleteLocalVM(vmName, ui);
                Assert.IsTrue(success);
                Assert.IsTrue(ui.Messages.Last().Contains("OK"));

                localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 0);

                deletedVm = vmm.TryFindLocalVM(vmName);
                Assert.IsNull(deletedVm);
                deletedConfig = configStore.TryGetVirtualMachineConfig(vmName);
                Assert.IsNull(deletedConfig);
                deletedInstance = configStore.TryGetLastVirtualMachineInstance(vmName);
                Assert.IsNull(deletedInstance);
            }
        }
    }
}
