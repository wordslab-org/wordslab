using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wordslab.manager.apps;
using wordslab.manager.os;
using wordslab.manager.storage;
using wordslab.manager.test.storage;
using wordslab.manager.vm;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class KubernetesTests
    {
        [TestMethod]
        public async Task T01_TestDownloadImageInContentStore()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmManager = new VirtualMachinesManager(storage, configStore);
                var vm = vmManager.TryFindLocalVM("test");

                var containerImageInfo = await ContainerImage.GetMetadataFromCacheOrFromRegistryAsync("ghcr.io/wordslab-org/lambda-stack-server:22.04.2", configStore);

                var result = Kubernetes.DownloadImageInContentStore(containerImageInfo, vm);
                Assert.IsTrue(result == 0);
            }
        }

        [TestMethod]
        public async Task T02_TestCheckImageBytesToDownload()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmManager = new VirtualMachinesManager(storage, configStore);
                var vm = vmManager.TryFindLocalVM("test");

                var containerImageInfo = await ContainerImage.GetMetadataFromCacheOrFromRegistryAsync("ghcr.io/wordslab-org/lambda-stack-server:22.04.2", configStore);

                var remainingSize = Kubernetes.CheckImageBytesToDownload(containerImageInfo, vm);
                Assert.IsTrue(remainingSize == 0);
            }
        }

        [TestMethod]
        public async Task T03_TestDeleteImageFromContentStore()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmManager = new VirtualMachinesManager(storage, configStore);
                var vm = vmManager.TryFindLocalVM("test");

                var containerImageInfo = await ContainerImage.GetMetadataFromCacheOrFromRegistryAsync("ghcr.io/wordslab-org/lambda-stack-server:22.04.2", configStore);

                var imageFoundAndDeleted = Kubernetes.DeleteImageFromContentStore(containerImageInfo, vm);
                Assert.IsTrue(imageFoundAndDeleted);

                var remainingSize = Kubernetes.CheckImageBytesToDownload(containerImageInfo, vm);
                Assert.IsTrue(remainingSize > 250000000);
            }
        }

        [TestMethod]
        public async Task T04_DownloadImageInContentStoreWithProgress()
        {
            var serviceCollection = ConfigStoreTests.GetStorageServices();
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var storage = serviceProvider.GetService<HostStorage>();
                var configStore = serviceProvider.GetService<ConfigStore>();

                var vmManager = new VirtualMachinesManager(storage, configStore);
                var vm = vmManager.TryFindLocalVM("test");

                var containerImageInfo = await ContainerImage.GetMetadataFromCacheOrFromRegistryAsync("ghcr.io/wordslab-org/lambda-stack-server:22.04.2", configStore);

                var messages = new List<string>();
                await Kubernetes.DownloadImageInContentStoreWithProgress(containerImageInfo, vm, (totalDownloadSize, totalBytesDownloaded, progressPercentage) => messages.Add($"{DateTime.Now} - Downloaded {totalBytesDownloaded}/{totalDownloadSize} bytes ({progressPercentage}%)"));               
                Assert.IsTrue(messages.Count > 10);
            }
        }
        
    }
}
