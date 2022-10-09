using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.vm;
using wordslab.manager.vm.wsl;

namespace wordslab.manager.test.vm.wsl
{
    [TestClass]
    public class WslVMInstallerTests
    {
        [TestMethodOnWindows]
        public async Task TestInstall()
        {
            var storage = new HostStorage();

           var vmSpecs = VMRequirements.GetRecommendedVMSpecs(storage);

            // Hardware requirements KO
            var recSpec = vmSpecs.RecommendedVMSpec;
            recSpec.Name = "test-blank";
            recSpec.Compute.MemoryGB = 128;
            var ui = new TestProcessUI();
            var vm = await WslVMInstaller.Install(recSpec, storage, ui);
            Assert.IsNull(vm);
            Assert.IsTrue(ui.Messages.Last().Contains("Not enough physical memory"));

            // Hardware requirements OK
            var minSpec = vmSpecs.MinimumVMSpec;
            minSpec.Name = "test-blank";
            var gpus = Compute.GetNvidiaGPUsInfo();
            if (gpus.Count > 0)
            {
                minSpec.GPUModel = gpus[0].ModelName;
                minSpec.GPUMemoryGB = gpus[0].MemoryMB / 1024;
            }
            ui = new TestProcessUI();
            vm = await WslVMInstaller.Install(minSpec, storage, ui);
            Assert.IsNotNull(vm);
            Assert.IsTrue(ui.Messages.Last().Contains("Virtual machine started"));
        }

        [TestMethodOnWindows]
        public async Task TestUninstall()
        {
            var storage = new HostStorage();
            var ui = new TestProcessUI();

            var vmName = "test-blank";
            var success = await WslVMInstaller.Uninstall(vmName, storage, ui);
            Assert.IsTrue(success);
            Assert.IsTrue(ui.Messages.Last().Contains("OK"));
        }
    }
}
