using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using wordslab.manager.apps;
using wordslab.manager.storage;
using wordslab.manager.test.storage;

namespace wordslab.manager.test.apps
{
    [TestClass]
    public class ContainerImageTests
    {
        [TestMethod]
        public async Task T01_GetMetadataFromCacheOrFromRegistryAsync()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();
                
                await Check_GetContainerImage("redis", configStore);
                await Check_GetContainerImage("redis:7.0.10", configStore);
                await Check_GetContainerImage("nvidia/cuda:12.1.0-base-ubuntu20.04", configStore);
                await Check_GetContainerImage("ghcr.io/wordslab-org/jupyter-stack-cuda:jupyterlab-3.6.3-lambda-0.1.13-22.04.2", configStore);
            }
        }

        private static async Task Check_GetContainerImage(string imageName, ConfigStore configStore)
        {
            var imageInfo = await ContainerImage.GetMetadataFromCacheOrFromRegistryAsync(imageName, configStore);
            Assert.IsTrue(imageInfo.Layers.Count > 3);

            imageInfo = configStore.TryGetContainerImageByName(ContainerImage.NormalizeImageName(imageName));
            Assert.IsTrue(imageInfo.Layers.Count > 3);

            imageInfo = await ContainerImage.GetMetadataFromCacheOrFromRegistryAsync(imageName, configStore);
            Assert.IsTrue(imageInfo.Layers.Count > 3);
        }
    }
}
