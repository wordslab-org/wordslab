using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.test.vm
{
    [TestClass]
    public class VMRequirementsTests
    {
        [TestMethod]
        public void T01_TestGetMinimumVMSpec()
        {
            var minSpec = VMRequirements.GetMinimumVMSpec();
            Assert.IsTrue(minSpec != null);
            Assert.IsTrue(minSpec.Compute.Processors > 0);
            Assert.IsTrue(minSpec.Compute.MemoryGB > 0);
            Assert.IsTrue(minSpec.GPU.ModelName != null);
            Assert.IsTrue(minSpec.GPU.MemoryGB > 0);
            Assert.IsTrue(minSpec.GPU.GPUCount == 1);
            Assert.IsTrue(minSpec.Storage.ClusterDiskSizeGB > 0);
            Assert.IsTrue(minSpec.Storage.DataDiskSizeGB > 0);
        }

        [TestMethod]
        public void T02_TestCheckCPURequirements()
        {
            var minSpec = VMRequirements.GetMinimumVMSpec();
            string error;
            var supported = VMRequirements.CheckCPURequirements(minSpec, Compute.GetCPUInfo(), out error);
            Assert.IsTrue(supported);
            Assert.IsTrue(error == null);
        }

        [TestMethod]
        public void T03_TestCheckMemoryRequirements()
        {
            var minSpec = VMRequirements.GetMinimumVMSpec();
            string error;
            var supported = VMRequirements.CheckMemoryRequirements(minSpec, Memory.GetMemoryInfo(), out error);
            Assert.IsTrue(supported);
            Assert.IsTrue(error == null);
        }

        [TestMethod]
        public void T04_TestCheckStorageRequirements()
        {
            var minSpec = VMRequirements.GetMinimumVMSpec();
            string error;
            var supported = VMRequirements.CheckStorageRequirements(minSpec, Storage.GetDrivesInfo(), out error);
            Assert.IsTrue(supported);
            Assert.IsTrue(error == null);
        }

        [TestMethod]
        public void T05_TestCheckGPURequirements()
        {
            var minSpec = VMRequirements.GetMinimumVMSpec();
            string error;
            var supported = VMRequirements.CheckGPURequirements(minSpec, Compute.GetNvidiaGPUsInfo(), out error);
            Assert.IsTrue(supported);
            Assert.IsTrue(error == null);
        }



        [TestMethod]
        public void T06_TestGetRecommendedVMSpecs()
        {
            var vmSpecs = VMRequirements.GetRecommendedVMSpecs();

            var minSpec = vmSpecs.MinimumVMSpec;
            var minSpecErrors = vmSpecs.MinimunVMSpecErrorMessage;
            Assert.IsTrue(minSpec != null);
            Assert.IsTrue(vmSpecs.MinimumVMSpecIsSupportedOnThisMachine);
            Assert.IsTrue(minSpecErrors == null);
            Assert.IsTrue(minSpec.Compute.Processors > 0);
            Assert.IsTrue(minSpec.Compute.MemoryGB > 0);
            Assert.IsTrue(minSpec.GPU.ModelName != null);
            Assert.IsTrue(minSpec.GPU.MemoryGB > 0);
            Assert.IsTrue(minSpec.GPU.GPUCount == 1);
            Assert.IsTrue(minSpec.Storage.ClusterDiskSizeGB > 0);
            Assert.IsTrue(minSpec.Storage.DataDiskSizeGB > 0);

            var recSpec = vmSpecs.RecommendedVMSpec;
            var recSpecErrors = vmSpecs.RecommendedVMSpecErrorMessage;
            Assert.IsTrue(recSpec != null);
            Assert.IsFalse(vmSpecs.RecommendedVMSpecIsSupportedOnThisMachine);
            Assert.IsTrue(recSpecErrors != null);
            Assert.IsTrue(recSpec.Compute.Processors > 0);
            Assert.IsTrue(recSpec.Compute.MemoryGB > 0);
            Assert.IsTrue(recSpec.GPU.ModelName != null);
            Assert.IsTrue(recSpec.GPU.MemoryGB > 0);
            Assert.IsTrue(recSpec.GPU.GPUCount == 1);
            Assert.IsTrue(recSpec.Storage.ClusterDiskSizeGB > 0);
            Assert.IsTrue(recSpec.Storage.DataDiskSizeGB > 0);

            var maxSpec = vmSpecs.MaximumVMSpecOnThisMachine;
            Assert.IsTrue(maxSpec != null);
            Assert.IsTrue(maxSpec.Compute.Processors > 0);
            Assert.IsTrue(maxSpec.Compute.MemoryGB > 0);
            Assert.IsTrue(maxSpec.GPU.ModelName != null);
            Assert.IsTrue(maxSpec.GPU.MemoryGB > 0);
            Assert.IsTrue(maxSpec.GPU.GPUCount == 1);
            Assert.IsTrue(maxSpec.Storage.ClusterDiskSizeGB > 0);
            Assert.IsTrue(maxSpec.Storage.DataDiskSizeGB > 0);
        }
    }
}
