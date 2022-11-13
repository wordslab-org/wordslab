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
                var config2 = new VirtualMachineConfig(vmName, maxSpec,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);

                ui = new TestProcessUI();
                var vm = await vmm.CreateLocalVM(config2, ui);
                Assert.IsNotNull(vm);

                vm.Start();
                Assert.IsTrue(vm.IsRunning());

                localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 1);

                // Virtual machine already exists
                ui = new TestProcessUI();
                vm = await vmm.CreateLocalVM(config2, ui);
                Assert.IsNull(vm);
                vm.Start();
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));

                // 2nd virtual machine - ports conflict
                vmName = "test-blank2";
                var minSpec = vmSpecs.MaximumVMSpecOnThisMachine;
                var config3 = new VirtualMachineConfig(vmName, minSpec,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);

                ui = new TestProcessUI();
                vm = await vmm.CreateLocalVM(config3, ui);
                Assert.IsNotNull(vm);
                vm.Start();
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));

                localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 2);
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

                Assert.IsTrue(vm1.Name == "test-blank1");
                Assert.IsTrue(vm2.Name == "test-blank2");                
                
                var portsbefore = Network.GetAllTcpPortsInUse();
                vm1.Start();
                var portsafter = Network.GetAllTcpPortsInUse();
                var newports = portsafter.Except(portsbefore);

                portsbefore = Network.GetAllTcpPortsInUse();
                vm2.Start();
                portsafter = Network.GetAllTcpPortsInUse();
                newports = portsafter.Except(portsbefore);

                Assert.IsTrue(vm1.IsRunning());
                Assert.IsTrue(vm2.IsRunning());
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

                var vm1 = vmm.TryFindLocalVM("test-blank1");
                Assert.IsNotNull(vm1);
                Assert.IsTrue(vm1.IsRunning());
                vm1.Stop();
                Assert.IsFalse(vm1.IsRunning());

                var vm2 = vmm.TryFindLocalVM("test-blank2");
                Assert.IsNotNull(vm2);
                Assert.IsTrue(vm2.IsRunning());
                vm2.Stop();
                Assert.IsFalse(vm2.IsRunning());
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

                ui = new TestProcessUI();
                vmName = "test-blank2";
                success = await vmm.DeleteLocalVM(vmName, ui);
                Assert.IsTrue(success);
                Assert.IsTrue(ui.Messages.Last().Contains("OK"));

                localvms = vmm.ListLocalVMs();
                Assert.IsTrue(localvms.Count == 0);
            }
        }
    }
}
