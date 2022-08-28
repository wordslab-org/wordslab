using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class ComputeTests
    {
        [TestMethod]
        public void T01_TestGetCPUInfo()
        {
            // Windows : 1300 ms
            var cpuInfo = Compute.GetCPUInfo();
            Assert.IsTrue(cpuInfo.Manufacturer.Length >= 3);
            Assert.IsTrue(cpuInfo.ModelName.Length >= 5);
            Assert.IsTrue(cpuInfo.FeatureFlags.Split(' ').Length > 10);
            Assert.IsTrue(cpuInfo.NumberOfCores >= 1);
            Assert.IsTrue(cpuInfo.NumberOfLogicalProcessors >= 1);
            Assert.IsTrue(cpuInfo.L3CacheSizeKB > 1000);
        }

        [TestMethod]
        public void T02_TestIsCPUVirtualizationAvailable()
        {
            var virt = Compute.IsCPUVirtualizationAvailable(Compute.GetCPUInfo());
            Assert.IsTrue(virt);
        }

        [TestMethod]
        public void T03_TestGetPercentCPUTime()
        {
            // Windows : 600 ms
            var cpu1 = Compute.GetPercentCPUTime();
            Thread.Sleep(100);
            var cpu2 = Compute.GetPercentCPUTime();
            Thread.Sleep(100);
            var cpu3 = Compute.GetPercentCPUTime();
            Assert.IsTrue(cpu1 <= 100 && cpu2 <= 100 && cpu3 <= 100);
            Assert.IsTrue(cpu1 > 0 || cpu2 > 0 || cpu3 > 0);
        }

        [TestMethodOnWindowsOrLinux]
        public void T04_TestGetNvidiaGPUsInfo()
        {
            // Windows : 60 ms
            var gpus = Compute.GetNvidiaGPUsInfo();
            Assert.IsTrue(gpus.Count > 0);
            foreach (var gpu in gpus)
            {
                Assert.IsTrue(gpu.MemoryMB > 1000);
                Assert.IsTrue(gpu.Architecture != Compute.GPUArchitectureInfo.Unknown);
            }
        }

        [TestMethodOnWindowsOrLinux]
        public void T05_TestGetNvidiaGPUsUsage()
        {
            // Windows : 60 ms
            var gpusload = Compute.GetNvidiaGPUsUsage();
            Assert.IsTrue(gpusload.Count > 0);
            foreach (var gpuload in gpusload)
            {
                Assert.IsTrue(gpuload.PercentGPUTime >= 0 && gpuload.PercentGPUTime <=100);
                Assert.IsTrue(gpuload.PercentMemoryTime >= 0 && gpuload.PercentMemoryTime <= 100);
                Assert.IsTrue(gpuload.MemoryFreeMB > 0);
            }
        }
    }
}
