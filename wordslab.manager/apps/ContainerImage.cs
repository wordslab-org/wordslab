using Microsoft.EntityFrameworkCore;
using wordslab.manager.config;
using wordslab.manager.storage;

namespace wordslab.manager.apps
{
    public static class ContainerImage
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static string NormalizeImageName(string imageName)
        {
            ContainerImageInfo image = new ContainerImageInfo();
            ExtractImageNameParts(imageName, image);
            return image.Name;
        }
        private static void ExtractImageNameParts(string imageName, ContainerImageInfo image)
        {
            // Split the image name into its component parts
            string[] parts = imageName.Trim().Split(':');
            var registryNamespaceRepository = parts[0];
            image.Tag = parts.Length == 2 ? parts[1] : "latest";

            // Extract the registry URL from the repository name
            parts = registryNamespaceRepository.Split('/');
            image.Registry = parts.Length == 3 ? parts[0] : "registry.docker.io";
            if (parts.Length == 1)
            {
                image.Repository = "library/" + parts[0];
            }
            else if (parts.Length == 2)
            {
                image.Repository = parts[0] + "/" + parts[1];
            }
            else if (parts.Length == 3)
            {
                image.Repository = parts[1] + "/" + parts[2];
            }

            // Normalize the image name
            image.Name = $"{image.Registry}/{image.Repository}:{image.Tag}";
        }

        public static async Task<ContainerImageInfo> GetMetadataFromCacheOrFromRegistryAsync(string imageName, ConfigStore configStore)
        {
            // Try to find the container info locally in the config database
            imageName = NormalizeImageName(imageName);
            ContainerImageInfo image = configStore.TryGetContainerImageByName(imageName);
            if(image != null) 
            { 
                return image; 
            }

            // This is a new image, we need to get the data from the repository
            image = new ContainerImageInfo();
            ExtractImageNameParts(imageName, image);

            // Step 1: Authentication request
            var authServer = image.Registry == "registry.docker.io" ? "auth.docker.io" : image.Registry;
            var authResponse = await httpClient.GetFromJsonAsync<Dictionary<string, object>>($"https://{authServer}/token?scope=repository:{image.Repository}:pull&service={image.Registry}");
            var authToken = ((System.Text.Json.JsonElement)authResponse["token"]).GetString();

            // Step 2: Manifest request
            var manifestServer = image.Registry == "registry.docker.io" ? "registry-1.docker.io" : image.Registry;
            var manifestMediaType = "application/vnd.docker.distribution.manifest.v2+json";
            var manifestListMediaType = "application/vnd.docker.distribution.manifest.list.v2+json";
            var manifestRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{manifestServer}/v2/{image.Repository}/manifests/{image.Tag}");
            manifestRequest.Headers.Add("Authorization", $"Bearer {authToken}");
            manifestRequest.Headers.Add("Accept", $"{manifestListMediaType}, {manifestMediaType}");
            var manifestResponse = await httpClient.SendAsync(manifestRequest);
            manifestResponse.EnsureSuccessStatusCode();
            ImageManifest imageManifest = null;
            var mediaType = manifestResponse.Content.Headers.ContentType.MediaType;
            if (mediaType == manifestMediaType)
            {
                imageManifest = await manifestResponse.Content.ReadFromJsonAsync<ImageManifest>();
                image.Digest = manifestResponse.Headers.GetValues("Docker-Content-Digest").FirstOrDefault();
            }
            else if (mediaType == manifestListMediaType)
            {
                var manifestList = await manifestResponse.Content.ReadFromJsonAsync<ImageManifestList>();
                // find the specific version of the image for linux and amd64
                string digest = null;
                foreach (var item in manifestList.manifests)
                {
                    if (item.platform.os == "linux" && item.platform.architecture == "amd64")
                    {
                        digest = item.digest;
                        break;
                    }
                }
                if (digest == null)
                {
                    throw new KeyNotFoundException("Could not find a version of this image for linux and amd64");
                }
                // send a more precise request
                manifestRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{manifestServer}/v2/{image.Repository}/manifests/{digest}");
                manifestRequest.Headers.Add("Authorization", $"Bearer {authToken}");
                manifestRequest.Headers.Add("Accept", $"{manifestMediaType}");
                manifestResponse = await httpClient.SendAsync(manifestRequest);
                manifestResponse.EnsureSuccessStatusCode();
                mediaType = manifestResponse.Content.Headers.ContentType.MediaType;
                if (mediaType == manifestMediaType)
                {
                    imageManifest = await manifestResponse.Content.ReadFromJsonAsync<ImageManifest>();
                    image.Digest = manifestResponse.Headers.GetValues("Docker-Content-Digest").FirstOrDefault();
                }
            }

            // Now that we found a specific digest, try again to find the image in cache
            if(!String.IsNullOrEmpty(image.Digest))
            {
                var cachedImage = configStore.TryGetContainerImageByDigest(image.Digest);
                if(cachedImage != null) { return cachedImage; }
            }

            // Get all the layers metadata
            if (imageManifest == null)
            {
                throw new NotSupportedException($"Docker image manifest format not supported: {mediaType}");
            }
            else
            {
                var configLayer = GetContainerImageLayer(imageManifest.config, configStore);
                image.Layers.Add(configLayer);
                configLayer.UsedByContainerImages.Add(image);

                foreach (var imageLayer in imageManifest.layers)
                {
                    var layer = GetContainerImageLayer(imageLayer, configStore);
                    image.Layers.Add(layer);
                    layer.UsedByContainerImages.Add(image);
                }
            }

            // Save in cache
            if (!configStore.ContainerImages.Any(cachedImage => cachedImage.Digest == image.Digest))
            {
                configStore.AddContainerImage(image);
            }

            // Note: if you want to test downloading a layer
            // wget --header="Authorization: Bearer djE6d29yZHNsYWItb3JnL2xhbWJkYS1zdGFjay1zZXJ2ZXI6MTY4MTE0NDQ2OTcyMzQ3MDc2Mw==" https://ghcr.io/v2/wordslab-org/lambda-stack-server/blobs/sha256:1bdbbbfaf9ee012367f8103c3425fcaa74845576d919bf5f978480dc459c322f

            return image;
        }

        private static ContainerImageLayerInfo GetContainerImageLayer(ImageManifest.Layer layer, ConfigStore configStore)
        {
            ContainerImageLayerInfo imageLayer = configStore.TryGetContainerImageLayer(layer.digest);
            if(imageLayer == null) 
            { 
                imageLayer = new ContainerImageLayerInfo(layer.digest, layer.mediaType, layer.size);
                // Save in cache
                configStore.AddContainerImageLayer(imageLayer);
            }
            return imageLayer;
        }

        // https://docs.docker.com/registry/spec/manifest-v2-2/

        /// <summary>
        /// The manifest list is the “fat manifest” which points to specific image manifests for one or more platforms.
        /// Its use is optional, and relatively few images will use one of these manifests. 
        /// A client will distinguish a manifest list from an image manifest based on the Content-Type returned in the HTTP response.
        /// </summary>
        private class ImageManifestList
        {
            /// <summary>
            /// This field specifies the image manifest schema version as an integer. 
            /// This schema uses the version 2.
            /// </summary>
            public int schemaVersion { get; set; }

            /// <summary>
            /// The MIME type of the manifest list. This should be set to application/vnd.docker.distribution.manifest.list.v2+json.
            /// </summary>
            public string mediaType { get; set; }

            /// <summary>
            /// The manifests field contains a list of manifests for specific platforms.
            /// </summary>
            public ManifestListItem[] manifests { get; set; }

            /// <summary>
            /// Fields of an object in the manifests list
            /// </summary>
            public class ManifestListItem
            {
                /// <summary>
                /// The MIME type of the referenced object. 
                /// This will generally be application/vnd.docker.distribution.manifest.v2+json, but it could also be 
                /// application/vnd.docker.distribution.manifest.v1+json if the manifest list references a legacy schema-1 manifest.
                /// </summary>
                public string mediaType { get; set; }

                /// <summary>
                /// The size in bytes of the object. This field exists so that a client will have an expected size for the content before validating.
                /// If the length of the retrieved content does not match the specified length, the content should not be trusted.
                /// </summary>
                public long size { get; set; }

                /// <summary>
                /// The digest of the content, as defined by the Registry V2 HTTP API Specification.
                /// </summary>
                public string digest { get; set; }

                /// <summary>
                /// The platform object describes the platform which the image in the manifest runs on. 
                /// </summary>
                public Platform platform { get; set; }
            }

            /// <summary>
            /// The platform object describes the platform which the image in the manifest runs on. 
            /// A full list of valid operating system and architecture values are listed in the Go language documentation for $GOOS and $GOARCH
            /// </summary>
            public class Platform
            {
                /// <summary>
                /// The architecture field specifies the CPU architecture, for example amd64 or ppc64le.
                /// </summary>
                public string architecture { get; set; }

                /// <summary>
                /// The os field specifies the operating system, for example linux or windows.
                /// </summary>
                public string os { get; set; }

                // The optional os.version field specifies the operating system version, for example 10.0.10586.
                // public string os.version;

                // The optional os.features field specifies an array of strings, each listing a required OS feature (for example on Windows win32k).
                // public string[] os.features;

                // The optional variant field specifies a variant of the CPU, for example v6 to specify a particular CPU variant of the ARM CPU.
                // public string variant;

                // The optional features field specifies an array of strings, each listing a required CPU feature (for example sse4 or aes).
                // public string[] features;
            }
        }

        /// <summary>
        /// The image manifest provides a configuration and a set of layers for a container image.
        /// It’s the direct replacement for the schema-1 manifest.
        /// </summary>
        private class ImageManifest
        {
            /// <summary>
            /// This field specifies the image manifest schema version as an integer.
            /// This schema uses version 2.
            /// </summary>
            public int schemaVersion { get; set; }

            /// <summary>
            /// The MIME type of the manifest.
            /// This should be set to application/vnd.docker.distribution.manifest.v2+json.
            /// </summary>
            public string mediaType { get; set; }

            /// <summary>
            /// The config field references a configuration object for a container, by digest. 
            /// This configuration item is a JSON blob that the runtime uses to set up the container.
            /// </summary>
            public Layer config { get; set; }

            /// <summary>
            /// The layer list is ordered starting from the base image (opposite order of schema1).
            /// </summary>
            public Layer[] layers { get; set; }

            /// <summary>
            /// Returns config then layers
            /// </summary>
            public IEnumerable<Layer> Layers()
            {
                yield return config;
                foreach (var layer in layers) { yield return layer; }
            }

            public class Layer
            {
                /// <summary>
                /// The MIME type of the referenced object. 
                /// This should generally be application/vnd.docker.image.rootfs.diff.tar.gzip.
                /// Layers of type application/vnd.docker.image.rootfs.foreign.diff.tar.gzip may be pulled from a remote location but they should never be pushed.
                /// </summary>
                public string mediaType { get; set; }

                /// <summary>
                /// The size in bytes of the object. 
                /// This field exists so that a client will have an expected size for the content before validating. 
                /// If the length of the retrieved content does not match the specified length, the content should not be trusted.
                /// </summary>
                public long size { get; set; }

                /// <summary>
                /// The digest of the content, as defined by the Registry V2 HTTP API Specification.
                /// </summary>
                public string digest { get; set; }

                // Provides a list of URLs from which the content may be fetched.
                // Content must be verified against the digest and size.
                // This field is optional and uncommon.
                // public string[] urls;
            }
        }
    }
}
