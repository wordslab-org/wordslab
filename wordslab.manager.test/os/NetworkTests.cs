using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Runtime.InteropServices;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class NetworkTests
    {
        [TestMethod]
        public void T01_TestGetIPAddressesAvailable()
        {
            // Windows : 45 ms
            var ipAdressesStatus = Network.GetIPAddressesAvailable();
            Assert.IsTrue(ipAdressesStatus.Count > 0);
            foreach(var status in ipAdressesStatus.Values)
            {
                Assert.IsTrue(status.Address.Split('.').Length == 4);
                Assert.IsTrue(status.NetworkInterfaceName.Length > 0);
                if (status.Address.StartsWith("192."))
                {
                    Assert.IsFalse(status.IsLoopback);
                    Assert.IsTrue(status.IsWireless);
                }
                else if (status.Address.StartsWith("127."))
                {
                    Assert.IsTrue(status.IsLoopback);
                    Assert.IsFalse(status.IsWireless);
                }
                else if (status.Address.StartsWith("172."))
                {
                    Assert.IsFalse(status.IsLoopback);
                    Assert.IsFalse(status.IsWireless);
                }
            }
        }

        [TestMethod]
        public void T02_TestGetTcpPortsInUsePerIPAddress()
        {
            // Windows : 6 ms
            var portsInUse = Network.GetTcpPortsInUsePerIPAddress();
            Assert.IsTrue(portsInUse.Keys.Count > 0);
            Assert.IsTrue(portsInUse["0.0.0.0"].Count > 0);
        }

        [TestMethod]
        public void T03_TestGetAllTcpPortsInUse()
        {
            // Windows : 6 ms
            var ports = Network.GetAllTcpPortsInUse();
            Assert.IsTrue(ports.Count >= 5);
        }

        [TestMethod]
        public void T04_TestGetNextAvailablePort()
        {
            var ports = Network.GetAllTcpPortsInUse();
            var defport = ports.First();
            var next = Network.GetNextAvailablePort(defport, ports);
            Assert.IsTrue(next > defport);
            var next2 = Network.GetNextAvailablePort(next, ports);
            Assert.IsTrue(next2 == next);
        }
    }
}
