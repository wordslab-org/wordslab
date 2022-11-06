using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using wordslab.manager.config;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.test.storage;
using wordslab.manager.vm;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.test.vm.wsl
{
    [TestClass]
    public class WslVMInstallerTests
    {
        [TestMethodOnWindows]
        public async Task T01_TestConfigureHostMachine()
        {
            var storage = new HostStorage();
            var ui = new TestProcessUI();
            var hostMachineConfig = await WslVMInstaller.ConfigureHostMachine(storage, ui);

            Assert.IsNotNull(hostMachineConfig);
            Assert.IsTrue(hostMachineConfig.Processors == 4);
        }

        [TestMethodOnWindows]
        public async Task T02_TestCreateVirtualMachine()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                var vmName = "test-blank";

                // Hardware requirements KO
                var recSpec = vmSpecs.RecommendedVMSpec;
                recSpec.Compute.MemoryGB = 128;
                var config1 = new VirtualMachineConfig(vmName, recSpec,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);

                var ui = new TestProcessUI();
                var vm = await WslVMInstaller.CreateVirtualMachine(config1, configStore, storage, ui);
                Assert.IsNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Not enough physical memory"));

                // Hardware requirements OK
                var minSpec = vmSpecs.MinimumVMSpec;
                var gpus = Compute.GetNvidiaGPUsInfo();
                if (gpus.Count > 0)
                {
                    minSpec.GPU.ModelName = gpus[0].ModelName;
                    minSpec.GPU.MemoryGB = gpus[0].MemoryMB / 1024;
                }
                var config2 = new VirtualMachineConfig(vmName, minSpec,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);

                ui = new TestProcessUI();
                vm = await WslVMInstaller.CreateVirtualMachine(config2, configStore, storage, ui);
                Assert.IsNotNull(vm);
                Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));
            }
        }

        [TestMethodOnWindows]
        public async Task T03_TestDeleteVirtualMachine()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var ui = new TestProcessUI();

                var vmName = "test-blank";
                var success = await WslVMInstaller.DeleteVirtualMachine(vmName, configStore, storage, ui);
                Assert.IsTrue(success);
                Assert.IsTrue(ui.Messages.Last().Contains("OK"));
            }
        }
    }
}
