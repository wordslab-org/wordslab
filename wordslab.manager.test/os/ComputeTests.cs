using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class ComputeTests
    {
        [TestMethod]
        public void TestGetCPUInfo()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsCPUVirtualizationAvailable()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetPercentCPUTime()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGetNvidiaGPUsInfo()
        {
            var gpus = Compute.GetNvidiaGPUsInfo();
            Assert.IsTrue(gpus.Count > 0);
            foreach (var gpu in gpus)
            {
                Assert.IsTrue(gpu.MemoryMB > 1000);
                Assert.IsTrue(gpu.Architecture != Compute.GPUArchitectureInfo.Unknown);
            }
        }

        [TestMethod]
        public void TestGetNvidiaGPUsUsage()
        {
            Assert.IsTrue(true);
        }
    }
}
