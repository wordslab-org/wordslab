using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using wordslab.manager.storage;
using wordslab.manager.vm;

namespace wordslab.manager.test.vm
{
    [TestClass]
    public class VirtualMachineEndpointTests
    {
        [TestMethod]
        public void T01_TestGetFilePath()
        {
            var storage = new HostStorage();
            var path = VirtualMachineEndpoint.GetEndpointFilePath(storage, "toto");
            
            Assert.IsTrue(path.StartsWith(storage.ConfigDirectory));
            Assert.IsTrue(path.EndsWith("toto.endpoint"));
        }

        [TestMethod]
        public void T02_TestSave()
        {
            var storage = new HostStorage();
            var path = VirtualMachineEndpoint.GetEndpointFilePath(storage, "toto");

            var endpoint = new VirtualMachineEndpoint("toto", "127.0.0.1", 22, 3444, 81, 444, "kubernetes\nconfig");
            endpoint.Save(storage);

            Assert.IsTrue(File.Exists(path));
            Assert.IsTrue(File.ReadAllLines(path).Length == 4);
            Assert.IsTrue(File.Exists(endpoint.KubeconfigPath));
            Assert.IsTrue(File.ReadAllLines(endpoint.KubeconfigPath).Length == 2);
        }

        [TestMethod]
        public void T03_TestLoad()
        {
            var storage = new HostStorage();
            var endpoint = VirtualMachineEndpoint.Load(storage, "toto");

            Assert.IsTrue(endpoint.VMName == "toto");
            Assert.IsTrue(endpoint.IPAddress == "127.0.0.1");
            Assert.IsTrue(endpoint.SSHPort == 22);
            Assert.IsTrue(endpoint.KubernetesPort == 3444);
            Assert.IsTrue(endpoint.HttpIngressPort == 81);
            Assert.IsTrue(endpoint.HttpsIngressPort == 444);
            Assert.IsTrue(endpoint.Kubeconfig == "kubernetes\nconfig");
            Assert.IsTrue(File.Exists(endpoint.KubeconfigPath));
        }

        [TestMethod]
        public void T04_TestDelete()
        {
            var storage = new HostStorage();
            var path = VirtualMachineEndpoint.GetEndpointFilePath(storage, "toto");
            var endpoint = VirtualMachineEndpoint.Load(storage, "toto");

            Assert.IsTrue(File.Exists(path));
            Assert.IsTrue(File.Exists(endpoint.KubeconfigPath));
            endpoint.Delete(storage);
            Assert.IsFalse(File.Exists(path));
            Assert.IsFalse(File.Exists(endpoint.KubeconfigPath));
        }
    }
}
