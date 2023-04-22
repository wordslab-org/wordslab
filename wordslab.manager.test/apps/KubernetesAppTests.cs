using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using wordslab.manager.apps;

namespace wordslab.manager.test.apps
{
    [TestClass]
    public class KubernetesAppTests
    {
        [TestMethod]
        public async Task T01_TestKubernetesApp_GetMetadataFromYamlFileAsync()
        {
            var app = await KubernetesApp.GetMetadataFromYamlFileAsync(KubernetesApp.WORDSLAB_NOTEBOOKS_GPU_APP_URL);
            Assert.IsTrue(!string.IsNullOrEmpty(app.Name));
            Assert.IsTrue(app.ContainerImages.Count == 1);
            Assert.IsTrue(app.Services.Values.First().UsedByResourceNames.Any());
            Assert.IsTrue(app.PersistentVolumes.Values.First().UsedByResourceNames.Any());  
        }
    }
}
