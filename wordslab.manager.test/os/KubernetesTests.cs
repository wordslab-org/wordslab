using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class KubernetesTests
    {
        [TestMethod]
        public async Task T01_TestContainerImage_GetMetadataFromRegistryAsync()
        {
            var redisImage = await ContainerImage.GetMetadataFromRegistryAsync("redis");
            Assert.IsTrue(redisImage.Manifest.layers.Length > 3);

            redisImage = await ContainerImage.GetMetadataFromRegistryAsync("redis:7.0.10");
            Assert.IsTrue(redisImage.Manifest.layers.Length > 3);

            var cudaImage = await ContainerImage.GetMetadataFromRegistryAsync("nvidia/cuda:12.1.0-base-ubuntu20.04");
            Assert.IsTrue(cudaImage.Manifest.layers.Length > 3);

            var jupyterImage = await ContainerImage.GetMetadataFromRegistryAsync("ghcr.io/wordslab-org/jupyter-stack-cuda:jupyterlab-3.6.3-lambda-0.1.13-22.04.2");
            Assert.IsTrue(jupyterImage.Manifest.layers.Length > 3);
        }

        [TestMethod]
        public async Task T02_TestKubernetesApp_GetMetadataFromYamlFileAsync()
        {
            var app = await KubernetesApp.GetMetadataFromYamlFileAsync("https://raw.githubusercontent.com/wordslab-org/wordslab/main/wordslab.manager/images/jupyter-stack/wordslab-notebooks-deployment.yaml");
            Assert.IsTrue(!string.IsNullOrEmpty(app.Name));
        }
    }
}
