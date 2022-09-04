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

                var vmSpecs = VirtualMachineSpec.GetRecommendedVMSpecs(storage);

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

                Assert.IsFalse(vm1.IsRunning());
                vm1.Start(new manager.storage.config.VirtualMachineConfig() { });
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
