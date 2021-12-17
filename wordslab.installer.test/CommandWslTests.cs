using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.installer.infrastructure.commands;

namespace wordslab.installer.test
{
    [TestClass]
    public class CommandWslTests
    {
        [TestMethod]
        public void TestStatus()
        {
            Assert.IsTrue(wsl.status());
        }
    }
}