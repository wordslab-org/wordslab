using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class MemoryTests
    {
        [TestMethod]
        public void T01_TestGetMemoryInfo()
        {
            // Windows : 3 ms
            var mem = Memory.GetMemoryInfo();
            Assert.IsTrue(mem.UsedPhysicalMB > 0);
            Assert.IsTrue(mem.FreePhysicalMB > 0);
            Assert.IsTrue(mem.TotalPhysicalMB == mem.UsedPhysicalMB + mem.FreePhysicalMB);
        }
    }
}
