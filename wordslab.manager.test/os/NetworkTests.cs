using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class NetworkTests
    {
        [TestMethod]
        public void TestIPAddressesAvailable()
        {
            var ipAdressesStatus = Network.GetIPAddressesAvailable();
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var status = ipAdressesStatus["192.168.1.22"];
                Assert.IsTrue(status.Address == "192.168.1.22");
                Assert.IsFalse(status.IsLoopback);
                Assert.IsTrue(status.NetworkInterfaceName == "Wi-Fi");
                Assert.IsTrue(status.IsWireless);
            }
        }

        [TestMethod]
        public void TestTcpPortsInUse()
        {
            var portsInUse = Network.GetTcpPortsInUse();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsTrue(portsInUse.Keys.Count == 3);
                Assert.IsTrue(portsInUse["0.0.0.0"].Count == 11);
            }
        }
    }
}
