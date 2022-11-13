using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using wordslab.manager.config;
using wordslab.manager.storage;
using wordslab.manager.test.storage;
using wordslab.manager.vm;

namespace wordslab.manager.test.vm
{
    [TestClass]
    public class LocalVirtualMachineTests
    {
        [TestMethod]
        public async void T01_TestCreateLocalVM()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                VirtualMachinesManager vmm = new VirtualMachinesManager(storage, configStore);
                var ui = new TestProcessUI();

                // Delete the VM if it existed before
                var vmName = "test-blank";
                var vm = vmm.TryFindLocalVM(vmName);
                if (vm != null)
                {
                    vmm.DeleteLocalVM(vmName, ui);
                }

                // Create a new VM
                var vmSpecs = VMRequirements.GetRecommendedVMSpecs();
                var maxSpec = vmSpecs.MaximumVMSpecOnThisMachine;
                var config = new VirtualMachineConfig(vmName, maxSpec,
                    VirtualMachineProvider.Wsl, null, false,
                    false, 0, false, 0, true, 80, false, true, 443, false);                
                vm = await vmm.CreateLocalVM(config, ui);

                // Check initial vm state
                Assert.IsNotNull(vm);
                Assert.IsFalse(vm.IsRunning());

                // Check vm instances
                var vmInstance = configStore.TryGetLastVirtualMachineInstance(vmName);
                Assert.IsTrue(vmInstance == null);
            }
        }

        [TestMethod]
        public void T02_TestStartAndStop()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                // Load the previously created VM
                var vmName = "test-blank";
                VirtualMachinesManager vmm = new VirtualMachinesManager(storage, configStore);
                var vm = vmm.TryFindLocalVM(vmName);
                Assert.IsFalse(vm.IsRunning());
                Assert.IsNotNull(vm);

                // Start without arguments
                var vmInstance = vm.Start();
                Assert.IsTrue(vm.IsRunning());
                Assert.IsNotNull(vmInstance);
                Assert.IsTrue(vmInstance.State== VirtualMachineState.Running);  

                // Check database
                vmInstance = configStore.TryGetLastVirtualMachineInstance(vmName);
                Assert.IsTrue(vmInstance != null);

                // Stop
                vm.Stop();
                Assert.IsFalse(vm.IsRunning());
                Assert.IsNotNull(vmInstance);

                // Check database
                vmInstance = configStore.TryGetLastVirtualMachineInstance(vmName);
                Assert.IsTrue(vmInstance != null);
                Assert.IsTrue(vmInstance.State == VirtualMachineState.Stopped);
            }
        }

        [TestMethod]
        public void T03_TestContainerIsWorking()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                // kubectl run cuda-test --image=nvidia/cuda:11.8.0-base-ubuntu22.04 --image-pull-policy=IfNotPresent --restart=Never --rm --attach -- nvidia-smi --query-gpu=index,gpu_name,memory.total --format=csv,noheader

                throw new NotImplementedException();
            }
        }
    }
}
