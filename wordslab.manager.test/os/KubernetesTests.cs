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
            Assert.IsTrue(redisImage.Manifest.layers.Length > 3);

            redisImage = await ContainerImage.GetManifestFromRegistryAsync("redis:7.0.10");
            Assert.IsTrue(redisImage.Manifest.layers.Length > 3);

            var cudaImage = await ContainerImage.GetManifestFromRegistryAsync("nvidia/cuda:12.1.0-base-ubuntu20.04");
            Assert.IsTrue(cudaImage.Manifest.layers.Length > 3);

            var jupyterImage = await ContainerImage.GetManifestFromRegistryAsync("ghcr.io/wordslab-org/jupyter-stack-cuda:jupyterlab-3.6.3-lambda-0.1.13-22.04.2");
            Assert.IsTrue(jupyterImage.Manifest.layers.Length > 3);
        }
    }
}
