using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.installer.infrastructure.commands;

namespace wordslab.installer.test
{
    [TestClass]
    public class CommandsNvidiaTests
    {
        [TestMethod]
        public void TestStatus()
        {
            var status = wsl.status();

            Assert.IsNotNull(status);
            Assert.IsTrue(status.IsInstalled);
            Assert.AreEqual(status.DefaultVersion, 2);
            Assert.IsNotNull(status.DefaultDistribution);
            Assert.IsNotNull(status.LinuxKernelVersion);
            Assert.IsNotNull(status.LastWSLUpdate);
        }
    }
}