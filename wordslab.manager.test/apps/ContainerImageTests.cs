using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using wordslab.manager.apps;

namespace wordslab.manager.test.apps
{
    [TestClass]
    public class ContainerImageTests
    {
        [TestMethod]
        public async Task T01_TestContainerImage_GetMetadataFromRegistryAsync()
        {
            /*var redisImage = await ContainerImage.GetMetadataFromRegistryAsync("redis");
            Assert.IsTrue(redisImage.Layers.Count > 3);

            redisImage = await ContainerImage.GetMetadataFromRegistryAsync("redis:7.0.10");
            Assert.IsTrue(redisImage.Layers.Count > 3);

            var cudaImage = await ContainerImage.GetMetadataFromRegistryAsync("nvidia/cuda:12.1.0-base-ubuntu20.04");
            Assert.IsTrue(cudaImage.Layers.Count > 3);

            var jupyterImage = await ContainerImage.GetMetadataFromRegistryAsync("ghcr.io/wordslab-org/jupyter-stack-cuda:jupyterlab-3.6.3-lambda-0.1.13-22.04.2");
            Assert.IsTrue(jupyterImage.Layers.Count > 3);*/
        }
    }
}
