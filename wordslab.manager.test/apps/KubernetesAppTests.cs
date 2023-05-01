using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using wordslab.manager.apps;
using wordslab.manager.storage;
using wordslab.manager.test.storage;

namespace wordslab.manager.test.apps
{
    [TestClass]
    public class KubernetesAppTests
    {
        [TestMethod]
        public async Task T01_TestImportMetadataFromYamlFileAsync()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var app = await KubernetesApp.ImportMetadataFromYamlFileAsync("test", KubernetesApp.WORDSLAB_NOTEBOOKS_GPU_APP_URL, configStore);
                Assert.IsTrue(!string.IsNullOrEmpty(app.Name));
                Assert.IsTrue(app.ContainerImages.Count == 1);
                Assert.IsTrue(app.Services.Count == 1);
                Assert.IsTrue(app.PersistentVolumes.Count == 2);
                Assert.IsTrue(app.ContainerImages[0].Layers.Count >= 3);

                var existingApp = configStore.TryGetKubernetesApp("test", app.YamlFileHash);
                Assert.IsTrue(!string.IsNullOrEmpty(app.Name));
                Assert.IsTrue(app.ContainerImages.Count == 1);
                Assert.IsTrue(app.Services.Count == 1);
                Assert.IsTrue(app.PersistentVolumes.Count == 2);
                Assert.IsTrue(app.ContainerImages[0].Layers.Count >= 3);
            }
        }
    }
}
