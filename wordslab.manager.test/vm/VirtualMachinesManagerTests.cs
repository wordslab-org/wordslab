using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.test.storage;
using wordslab.manager.vm;

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

                var userWantsGPU = true;
                var ui = new TestProcessUI();
                var machineConfig = await vmm.ConfigureHostMachine(userWantsGPU, ui);
                Assert.IsNotNull(machineConfig);

                Assert.IsNotNull(configStore.HostMachineConfig);
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

                var minSpec = VMRequirements.GetMinimumVMSpec();
                var gpus = Compute.GetNvidiaGPUsInfo();
                if (gpus.Count > 0)
                {
                    minSpec.GPU.ModelName = gpus[0].ModelName;
                    minSpec.GPU.MemoryGB = gpus[0].MemoryMB / 1024;
                }
                var minConfig = new VirtualMachineConfig("test-blank1", minSpec);

                var ui = new TestProcessUI();
                var vm = await vmm.CreateLocalVM(minConfig, ui);
                Assert.IsNotNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));

                minConfig.Name = "test-blank2";
                // minConfig.HostHttpIngressPort += 1;
                // minConfig.HostHttpsIngressPort += 1;
                // minConfig.HostKubernetesPort += 1;
                ui = new TestProcessUI();
                vm = await vmm.CreateLocalVM(minConfig, ui);
                Assert.IsNotNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));
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

                var ui = new TestProcessUI();
                var vmName = "test-blank1";
                var success = await vmm.DeleteLocalVM(vmName, ui);
                Assert.IsTrue(success);
                Assert.IsTrue(ui.Messages.Last().Contains("OK"));

                ui = new TestProcessUI();
                vmName = "test-blank2";
                success = await vmm.DeleteLocalVM(vmName, ui);
                Assert.IsTrue(success);
                Assert.IsTrue(ui.Messages.Last().Contains("OK"));
            }
        }
    }
}
