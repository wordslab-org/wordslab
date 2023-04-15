using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Spectre.Console;
using System.Collections.Generic;
using System.Drawing;
using System;
using System.Reflection.PortableExecutable;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using wordslab.manager.web;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using static wordslab.manager.console.host.ConfigInfoCommand;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;
using k8s;

namespace wordslab.manager.os
{
    public interface IVirtualMachineShell
    {
        int ExecuteCommand(string command, string commandArguments = "", int timeoutSec = 10, Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null);
    }

    public class Kubernetes
    {
        public static int ApplyYamlFile(string yamlFileURL, IVirtualMachineShell vmShell)
        {
            throw new NotImplementedException();
        }
    }

    public class KubernetesApp
    {
        public static async Task<KubernetesApp> GetPropertiesFromYamlFileURL(string yamlFileURL)
        {
            var app = new KubernetesApp();
            app.YamlFileContent = await _httpClient.GetStringAsync(yamlFileURL);

            List<object> resources = KubernetesYaml.LoadAllFromString(app.YamlFileContent);
            return app;
        }

        internal static readonly HttpClient _httpClient = new HttpClient();

        public string AppName;

        public string AppTitle;

        public string AppDescription;

        public string YamlFileContent;

        public Dictionary<string,string> FeatureURLs = new Dictionary<string,string>();

        public Dictionary<string,ContainerImage> Images = new Dictionary<string,ContainerImage>();

        public Dictionary<string,int> PersistentVolumes = new Dictionary<string,int>();
    }

    public class ContainerImage
    {
        public string Registry { get; set; }

        public string Repository { get; set; }

        public string Tag { get; set; }

        public ImageManifest Manifest { get; set; }

        public static async Task<ContainerImage> GetManifestFromRegistryAsync(string imageName)
        {
            ContainerImage image = new ContainerImage();

            // Split the image name into its component parts
            string[] parts = imageName.Trim().Split(':');
            var registryNamespaceRepository = parts[0];
            image.Tag = parts.Length==2 ? parts[1] : "latest";

            // Extract the registry URL from the repository name
            parts = registryNamespaceRepository.Split('/');
            image.Registry = parts.Length==3 ? parts[0] : "registry.docker.io";
            if(parts.Length == 1)
            {
                image.Repository = "library/" + parts[0];
            }
            else if(parts.Length == 2)
            {
                image.Repository = parts[0] + "/" + parts[1];
            }
            else if (parts.Length == 3)
            {
                image.Repository = parts[1] + "/" + parts[2];
            }

            // Step 1: Authentication request
            var authServer = image.Registry == "registry.docker.io" ? "auth.docker.io" : image.Registry;
            var authResponse = await KubernetesApp._httpClient.GetFromJsonAsync<Dictionary<string, object>>($"https://{authServer}/token?scope=repository:{image.Repository}:pull&service={image.Registry}");
            var authToken = ((System.Text.Json.JsonElement)authResponse["token"]).GetRawText();

            // Step 2: Manifest request
            var manifestMediaType = "application/vnd.docker.distribution.manifest.v2+json";
            var manifestListMediaType = "application/vnd.docker.distribution.manifest.list.v2+json";
            var manifestRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{image.Registry}/v2/{image.Repository}/manifests/{image.Tag}");
            manifestRequest.Headers.Add("Authorization", $"Bearer {authToken}");
            manifestRequest.Headers.Add("Accept", $"{manifestListMediaType}, {manifestMediaType}");
            var manifestResponse = await KubernetesApp._httpClient.SendAsync(manifestRequest);
            manifestResponse.EnsureSuccessStatusCode();
            var mediaType = manifestResponse.Content.Headers.ContentType.MediaType;
            if (mediaType == manifestMediaType)
            {
                image.Manifest = await manifestResponse.Content.ReadFromJsonAsync<ImageManifest>();
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
                manifestRequest.RequestUri = new Uri($"https://{image.Registry}/v2/{image.Repository}/manifests/{digest}");
                manifestResponse.EnsureSuccessStatusCode();
                mediaType = manifestResponse.Content.Headers.ContentType.MediaType;
                if (mediaType == manifestMediaType)
                {
                    image.Manifest = await manifestResponse.Content.ReadFromJsonAsync<ImageManifest>();
                }
            }
            if(image.Manifest == null)
            {
                throw new NotSupportedException($"Docker image manifest format not supported: {mediaType}");
            }

            // Note: if you want to test downloading a layer
            // wget --header="Authorization: Bearer djE6d29yZHNsYWItb3JnL2xhbWJkYS1zdGFjay1zZXJ2ZXI6MTY4MTE0NDQ2OTcyMzQ3MDc2Mw==" https://ghcr.io/v2/wordslab-org/lambda-stack-server/blobs/sha256:1bdbbbfaf9ee012367f8103c3425fcaa74845576d919bf5f978480dc459c322f

            return image;
        }

        public int DownloadInKubernetesContentStore(IVirtualMachineShell vmShell)
        {
            // Containerd content flow docs: 
            // https://github.com/containerd/containerd/blob/main/docs/content-flow.md

            // Note: if you want to remove unused images for testing
            // k3s crictl rmi --prune

            // Image pull command:
            // ctr image pull ghcr.io/wordslab-org/lambda-stack-cuda:0.1.13-22.04.2
            return vmShell.ExecuteCommand("k3s", $"ctr image pull {Registry}/{Repository}:{Tag}"); 
        }

        public long CheckRemainingBytesToDownload(IVirtualMachineShell vmShell)
        {
            // Download in progress with resume capability:
            // k3s ctr content active
            // REF                                                                             SIZE    AGE
            // layer-sha256:c781ed05e2ff0cdd3c235c9d71270b2ea2adbe86dfb2e728cfc5b12dec98ab65   31.46MB 26 seconds

            // Find on disk while downloading :
            // ls -al /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/ingest/aa86d66378175e273c04b7adbb726b50824a64dc0783c30508ab4361f21c381a/
            // -rw-r--r-- 1 root root 31457280 Apr 15 15:08 data
            // -rw-r--r-- 1 root root       86 Apr 15 15:07 ref
            // -rw -r--r-- 1 root root       29 Apr 15 15:07 startedat
            // -rw -r--r-- 1 root root       10 Apr 15 15:07 total
            // -rw -r--r-- 1 root root       30 Apr 15 15:08 updatedat
            //
            // cat /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/ingest/aa86d66378175e273c04b7adbb726b50824a64dc0783c30508ab4361f21c381a/ref
            // k8s.io/1/layer-sha256:c781ed05e2ff0cdd3c235c9d71270b2ea2adbe86dfb2e728cfc5b12dec98ab65

            string inprogressLayers = "";
            vmShell.ExecuteCommand("k3s", "ctr content active", outputHandler: output => inprogressLayers = output);

            var inprogressLayersDict = new Dictionary<string, long>();
            foreach (var line in inprogressLayers.Split('\n'))
            {
                if(line.StartsWith("layer-"))
                {
                    var parts = line.Split(new char[] { '-', ' ' });
                    var digest = parts[1];
                    var sizeAndUnit = parts[2];
                    var multiply = 1L;
                    if(sizeAndUnit.EndsWith("KB"))
                    {
                        multiply = 1024L;
                    }
                    else if (sizeAndUnit.EndsWith("MB"))
                    {
                        multiply = 1024 * 1024L;
                    }
                    else if (sizeAndUnit.EndsWith("GB"))
                    {
                        multiply = 1024 * 1024 * 1024L;
                    }
                    if(multiply > 1)
                    {
                        sizeAndUnit = sizeAndUnit.Substring(0, sizeAndUnit.Length-2);
                    }
                    var size = (long)(float.Parse(sizeAndUnit) * multiply);
                    inprogressLayersDict.Add(digest, size);
                }
            }

            // Already downloaded layers:
            // k3s ctr content list -q
            // sha256: 0027c958fa3f3976058f1bebc8fa49fc6272528659331074aba57ad24aecc23e
            // sha256:0233247f4c0fad20be467785f7e93fc0213a076a9f0dff630bdec21ec3b6441
            // ...

            // Find on disk after download :
            // ls -al /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/blobs/sha256/fc4a5e1c03378ec8b1824c54bd25943af415adc040cc2071c8324c530a4962fb
            //    -r--r--r-- 1 root root 284407131 ... <== size in manifest

            string downloadedLayers = "";
            vmShell.ExecuteCommand("k3s", "ctr content list -q", outputHandler: output => downloadedLayers = output);

            var downloadedLayersSet = new HashSet<string>();
            foreach (var line in downloadedLayers.Split('\n'))
            {
                downloadedLayersSet.Add(line);
            }

            long remainingBytes = 0;
            foreach(var layer in Manifest.layers)
            {
                if (downloadedLayersSet.Contains(layer.digest))
                { 
                    continue;
                }
                else if(inprogressLayersDict.ContainsKey(layer.digest))
                {
                    remainingBytes += layer.size - inprogressLayersDict[layer.digest];
                }
                else 
                { 
                    remainingBytes += layer.size; 
                }  
            }

            return remainingBytes;
        }

        // https://docs.docker.com/registry/spec/manifest-v2-2/

        /// <summary>
        /// The manifest list is the “fat manifest” which points to specific image manifests for one or more platforms.
        /// Its use is optional, and relatively few images will use one of these manifests. 
        /// A client will distinguish a manifest list from an image manifest based on the Content-Type returned in the HTTP response.
        /// </summary>
        public class ImageManifestList
        {
            /// <summary>
            /// This field specifies the image manifest schema version as an integer. 
            /// This schema uses the version 2.
            /// </summary>
            public int schemaVersion;

            /// <summary>
            /// The MIME type of the manifest list. This should be set to application/vnd.docker.distribution.manifest.list.v2+json.
            /// </summary>
            public string mediaType;

            /// <summary>
            /// The manifests field contains a list of manifests for specific platforms.
            /// </summary>
            public ManifestListItem[] manifests;

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
                public string mediaType;

                /// <summary>
                /// The size in bytes of the object. This field exists so that a client will have an expected size for the content before validating.
                /// If the length of the retrieved content does not match the specified length, the content should not be trusted.
                /// </summary>
                public int size;

                /// <summary>
                /// The digest of the content, as defined by the Registry V2 HTTP API Specification.
                /// </summary>
                public string digest;

                /// <summary>
                /// The platform object describes the platform which the image in the manifest runs on. 
                /// </summary>
                public Platform platform;
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
                public string architecture;

                /// <summary>
                /// The os field specifies the operating system, for example linux or windows.
                /// </summary>
                public string os;

                // The optional os.version field specifies the operating system version, for example 10.0.10586.
                // public string os.version;

                // The optional os.features field specifies an array of strings, each listing a required OS feature (for example on Windows win32k).
                // public string[] os.features;

                /// <summary>
                /// The optional variant field specifies a variant of the CPU, for example v6 to specify a particular CPU variant of the ARM CPU.
                /// </summary>
                public string? variant;

                /// <summary>
                /// The optional features field specifies an array of strings, each listing a required CPU feature (for example sse4 or aes).
                /// </summary>
                public string[]? features;
            }
        }

        /// <summary>
        /// The image manifest provides a configuration and a set of layers for a container image.
        /// It’s the direct replacement for the schema-1 manifest.
        /// </summary>
        public class ImageManifest
        {
            /// <summary>
            /// This field specifies the image manifest schema version as an integer.
            /// This schema uses version 2.
            /// </summary>
            public int schemaVersion;

            /// <summary>
            /// The MIME type of the manifest.
            /// This should be set to application/vnd.docker.distribution.manifest.v2+json.
            /// </summary>
            public string mediaType;

            /// <summary>
            /// The config field references a configuration object for a container, by digest. 
            /// This configuration item is a JSON blob that the runtime uses to set up the container.
            /// </summary>
            public Config config;

            /// <summary>
            /// This configuration item is a JSON blob that the runtime uses to set up the container. 
            /// This new schema uses a tweaked version of this configuration to allow image content-addressability on the daemon side.
            /// </summary>
            public class Config
            {
                /// <summary>
                /// The MIME type of the referenced object. 
                /// This should generally be application/vnd.docker.container.image.v1+json.
                /// </summary>
                public string mediaType;

                /// <summary>
                /// The size in bytes of the object. 
                /// This field exists so that a client will have an expected size for the content before validating.
                /// If the length of the retrieved content does not match the specified length, the content should not be trusted.
                /// </summary>
                public int size;

                /// <summary>
                /// The digest of the content, as defined by the Registry V2 HTTP API Specificiation.
                /// </summary>
                public string digest;
            }

            /// <summary>
            /// The layer list is ordered starting from the base image (opposite order of schema1).
            /// </summary>
            public Layer[] layers;

            public class Layer
            {
                /// <summary>
                /// The MIME type of the referenced object. 
                /// This should generally be application/vnd.docker.image.rootfs.diff.tar.gzip.
                /// Layers of type application/vnd.docker.image.rootfs.foreign.diff.tar.gzip may be pulled from a remote location but they should never be pushed.
                /// </summary>
                public string mediaType;

                /// <summary>
                /// The size in bytes of the object. 
                /// This field exists so that a client will have an expected size for the content before validating. 
                /// If the length of the retrieved content does not match the specified length, the content should not be trusted.
                /// </summary>
                public int size;

                /// <summary>
                /// The digest of the content, as defined by the Registry V2 HTTP API Specification.
                /// </summary>
                public string digest;

                /// <summary>
                /// Provides a list of URLs from which the content may be fetched. 
                /// Content must be verified against the digest and size.
                /// This field is optional and uncommon.
                /// </summary>
                public string[]? urls;
            }
        }
    }
}
