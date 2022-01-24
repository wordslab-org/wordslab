using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.installer.infrastructure.commands;

namespace wordslab.installer.test
{
    [TestClass]
    public class CommandsNvidiaTests
    {
        [TestMethod]
        public void TestDriverVersion()
        {
            var driverVersion = nvidia.GetDriverVersion();
            Assert.IsTrue(driverVersion.Major > 0);
            Assert.IsTrue(driverVersion.Minor > 0);
            Assert.IsTrue(driverVersion.Revision == -1);
            Assert.IsTrue(driverVersion.Build == -1); ;

            Assert.IsTrue(nvidia.IsNvidiaDriver20Sep21OrLater(driverVersion));
            Assert.IsTrue(nvidia.IsNvidiaDriver16Nov21OrLater(driverVersion));
        }

        [TestMethod]
        public void TestOpenNvidiaUpdate()
        {
            nvidia.TryOpenNvidiaUpdate();
        }

        [TestMethod]
        public void TestGPUInfo()
        {
            var gpus = nvidia.GetNvidiaGPUs();
            Assert.IsTrue(gpus.Count > 0);
            foreach (var gpu in gpus)
            {
                Assert.IsTrue(gpu.MemoryMB > 1000);
                Assert.IsTrue(gpu.Architecture != nvidia.GPUArchitectureInfo.Unknown);
            }
        }
    }
}