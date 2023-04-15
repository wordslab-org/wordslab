using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class KubernetesTests
    {
        [TestMethod]
        public async Task T01_TestContainerImageInit()
        {
            var redisImage = await ContainerImage.GetManifestFromRegistryAsync("redis");
            Assert.IsTrue(redisImage.Registry == "docker.io");
        }
    }
}
