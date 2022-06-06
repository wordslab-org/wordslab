using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WslTests
    {
        [TestMethod]
        public void TestVirtualMachineWorkingSet()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var vmMemoryMB = Wsl.GetVirtualMachineWorkingSetMB();
                Assert.IsTrue(vmMemoryMB > 100 && vmMemoryMB < 2000);
            }
        }

        [TestMethod]
        public void TestReadWslConfig()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var config = Wsl.Read_wslconfig();
                Assert.IsTrue(config != null);
            }
        }

        [TestMethod]
        public void TestWriteWslConfig()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var config = Wsl.Read_wslconfig();
                Wsl.Write_wslconfig(config);
                Assert.IsTrue(config.LoadedFromFile);
            }
        }
    }
}
