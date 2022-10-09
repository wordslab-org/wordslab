using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task T01_TestCreateLocalVM()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmm = new VirtualMachinesManager(storage, configStore);
                Assert.IsNotNull(vmm);

                var vmSpecs = VMRequirements.GetRecommendedVMSpecs(storage);

                var minSpec = vmSpecs.MinimumVMSpec;
                minSpec.Name = "test-blank1";
                var gpus = Compute.GetNvidiaGPUsInfo();
                if (gpus.Count > 0)
                {
                    minSpec.GPUModel = gpus[0].ModelName;
                    minSpec.GPUMemoryGB = gpus[0].MemoryMB / 1024;
                }
                var ui = new TestProcessUI();
                var vm = await vmm.CreateLocalVM(minSpec, ui);
                Assert.IsNotNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));

                minSpec.Name = "test-blank2";
                minSpec.HostHttpIngressPort += 1;
                minSpec.HostHttpsIngressPort += 1;
                minSpec.HostKubernetesPort += 1;
                ui = new TestProcessUI();
                vm = await vmm.CreateLocalVM(minSpec, ui);
                Assert.IsNotNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));
            }
        }

        [TestMethod]
        public async Task T01_TestCreateLocalVM()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmm = new VirtualMachinesManager(storage, configStore);
                Assert.IsNotNull(vmm);

                var vmSpecs = VMRequirements.GetRecommendedVMSpecs(storage);

                var minSpec = vmSpecs.MinimumVMSpec;
                minSpec.Name = "test-blank1";
                var gpus = Compute.GetNvidiaGPUsInfo();
                if (gpus.Count > 0)
                {
                    minSpec.GPUModel = gpus[0].ModelName;
                    minSpec.GPUMemoryGB = gpus[0].MemoryMB / 1024;
                }
                var ui = new TestProcessUI();
                var vm = await vmm.CreateLocalVM(minSpec, ui);
                Assert.IsNotNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));

                minSpec.Name = "test-blank2";
                minSpec.HostHttpIngressPort += 1;
                minSpec.HostHttpsIngressPort += 1;
                minSpec.HostKubernetesPort += 1;
                ui = new TestProcessUI();
                vm = await vmm.CreateLocalVM(minSpec, ui);
                Assert.IsNotNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));
            }
        }

        [TestMethod]
        public void T02_TestListLocalVMs()
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
        public void T03_TestTryFindLocalVM()
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
        public async Task T04_TestDeleteLocalVM()
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
