using k8s;
using k8s.Models;
using System.Text.Json.Serialization;

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
        // Mandatory on all resources
        public const string APP_NAME_LABEL          = "app.wordslab.org/name";
        public const string APP_COMPONENT_LABEL     = "app.wordslab.org/component";
        // Mandatory only once on the first resource of the file
        public const string APP_TITLE_LABEL         = "app.wordslab.org/title";
        public const string APP_DESCRIPTION_LABEL   = "app.wordslab.org/description";
        public const string APP_VERSION_LABEL       = "app.wordslab.org/version";
        public const string APP_DATE_LABEL          = "app.wordslab.org/date";
        public const string APP_HOMEPAGE_LABEL      = "app.wordslab.org/homepage";
        public const string APP_SOURCE_LABEL        = "app.wordslab.org/source";
        public const string APP_AUTHOR_LABEL        = "app.wordslab.org/author";
        public const string APP_LICENSE_LABEL       = "app.wordslab.org/license";
        // Mandatory on all IngressRoutes
        public const string ROUTE_PREFIX_PLACEHOLDER    = "app.wordslab.org/route/prefix";
        public const string ROUTE_PREFIX_DELIMITER      = "$$";
        public const string ROUTE_PATH_TITLE_LABEL      = "app.wordslab.org/route/path";// path1, path2 ... if several needed
        public const string ROUTE_PATH_TITLE_SEPARATOR  = "||";

        internal static readonly HttpClient _httpClient = new HttpClient();
     
        private static Dictionary<string, Type> traefikTypeMap = new Dictionary<string, Type>()
        {
            { "traefik.containo.us/v1alpha1/IngressRoute", typeof(TraefikV1alpha1IngressRoute) }
        };

        public static async Task<KubernetesApp> GetMetadataFromYamlFileAsync(string yamlFileURL)
        {
            var app = new KubernetesApp();
            app.YamlFileContent = await _httpClient.GetStringAsync(yamlFileURL);

            var resources = KubernetesYaml.LoadAllFromString(app.YamlFileContent, traefikTypeMap);
            foreach (var resource in resources)
            {
                var resourceMetadata = (IMetadata<V1ObjectMeta>)resource;
                if (resourceMetadata.Namespace() != null)
                {
                    throw new FormatException("In wordslab yaml app files, explicit namespaces are not allowed: all resources will be inserted in the same user-defined namespace at install time");
                }
                var appName = resourceMetadata.GetLabel(APP_NAME_LABEL);
                var appComponent = resourceMetadata.GetLabel(APP_COMPONENT_LABEL);
                if (appName == null || appComponent == null)
                {
                    throw new FormatException("In wordslab yaml app files, the labels 'app.wordslab.org/name' and 'app.wordslab.org/component' are mandatory on all resources");
                }
                if(app.Name == null)
                {
                    app.Name = appName;

                    // First resource of the file, read all app properties
                    app.Title = resourceMetadata.GetLabel(APP_TITLE_LABEL);
                    if(app.Title == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_TITLE_LABEL}' is mandatory on the first resource of the file");
                    }
                    app.Description = resourceMetadata.GetLabel(APP_DESCRIPTION_LABEL);
                    if (app.Description == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_DESCRIPTION_LABEL}' is mandatory on the first resource of the file");
                    }
                    app.Version = resourceMetadata.GetLabel(APP_VERSION_LABEL);
                    if (app.Version == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_VERSION_LABEL}' is mandatory on the first resource of the file");
                    }
                    app.Date = resourceMetadata.GetLabel(APP_DATE_LABEL);
                    if (app.Date == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_DATE_LABEL}' is mandatory on the first resource of the file");
                    }
                    app.HomePage = resourceMetadata.GetLabel(APP_HOMEPAGE_LABEL);
                    if (app.HomePage == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_HOMEPAGE_LABEL}' is mandatory on the first resource of the file");
                    }
                    app.Source = resourceMetadata.GetLabel(APP_SOURCE_LABEL);
                    if (app.Source == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_SOURCE_LABEL}' is mandatory on the first resource of the file");
                    }
                    app.Author = resourceMetadata.GetLabel(APP_AUTHOR_LABEL);
                    if (app.Author == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_AUTHOR_LABEL}' is mandatory on the first resource of the file");
                    }
                    app.Licence = resourceMetadata.GetLabel(APP_LICENSE_LABEL);
                    if (app.Licence == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the label '{APP_LICENSE_LABEL}' is mandatory on the first resource of the file");
                    }
                }
                else
                {
                    if(appName != app.Name)
                    {
                        throw new FormatException($"Inconsistent application name in 'app.wordslab.org/name' labels on different resources: ");
                    }
                }
                switch (resource)
                {
                    // Pod: A pod is the smallest deployable unit in Kubernetes and can contain one or more containers.
                    // The container spec is defined within the spec.containers field of a pod.
                    case V1Pod pod:
                        var resourceName = $"pod/{pod.Name()}";
                        await app.AddPodSpec(resourceName, pod.Spec);
                        break;
                    // ReplicationController: A replication controller ensures that a specified number of pod replicas are running at any given time.
                    // The container spec is defined within the spec.template.spec.containers field of a replication controller.
                    case V1ReplicationController replicaCtrl:
                        resourceName = $"replicationcontroller/{replicaCtrl.Name()}";
                        await app.AddPodSpec(resourceName, replicaCtrl.Spec?.Template?.Spec);
                        break;
                    // ReplicaSet: A replica set is similar to a replication controller but provides more advanced selectors for managing pods.
                    // The container spec is defined within the spec.template.spec.containers field of a replica set.
                    case V1ReplicaSet replicaSet:
                        resourceName = $"replicaset.apps/{replicaSet.Name()}";
                        await app.AddPodSpec(resourceName, replicaSet.Spec?.Template?.Spec);
                        break;
                    // Deployment: A deployment manages the rollout and scaling of a set of replicas.
                    // The container spec is defined within the spec.template.spec.containers field of a deployment.
                    case V1Deployment deployment:
                        resourceName = $"deployment.apps/{deployment.Name()}";
                        await app.AddPodSpec(resourceName, deployment.Spec?.Template?.Spec);
                        break;
                    // StatefulSet: A stateful set manages the deployment and scaling of a set of stateful pods.
                    // The container spec is defined within the spec.template.spec.containers field of a stateful set.
                    case V1StatefulSet statefulSet:
                        resourceName = $"statefulset.apps/{statefulSet.Name()}";
                        await app.AddPodSpec(resourceName, statefulSet.Spec?.Template?.Spec);
                        break;
                    // DaemonSet: A daemon set ensures that all(or some) nodes run a copy of a pod.
                    // The container spec is defined within the spec.template.spec.containers field of a daemon set.
                    case V1DaemonSet daemonSet:
                        resourceName = $"daemonset.apps/{daemonSet.Name()}";
                        await app.AddPodSpec(resourceName, daemonSet.Spec?.Template?.Spec);
                        break;
                    // Job: A job creates one or more pods and ensures that a specified number of them successfully terminate.
                    // The container spec is defined within the spec.template.spec.containers field of a job.
                    case V1Job job:
                        resourceName = $"job.batch/{job.Name()}";
                        await app.AddPodSpec(resourceName, job.Spec?.Template?.Spec);
                        break;
                    // CronJob: A cron job creates a job on a repeating schedule.
                    // The container spec is defined within the spec.jobTemplate.spec.template.spec.containers
                    case V1CronJob cronJob:
                        resourceName = $"cronjob.batch/{cronJob.Name()}";
                        await app.AddPodSpec(resourceName, cronJob.Spec?.JobTemplate?.Spec?.Template?.Spec);
                        break;
                    case V1PersistentVolume pv:
                        throw new NotSupportedException("PersistentVolumes are not supported in wordslab: please use a PersistentVolumeClaim with 'storageClassName: local-path'");
                    case V1PersistentVolumeClaim pvc:
                        var storageClassName = pvc.Spec?.StorageClassName;
                        if (storageClassName != "local-path")
                        {
                            throw new NotSupportedException("In wordslab, PersistentVolumeClaims must use 'storageClassName: local-path'");
                        }
                        var pvcName = pvc.Name();
                        var storageRequest = pvc.Spec?.Resources?.Requests["storage"]?.ToInt64();
                        var storageLimit = pvc.Spec?.Resources?.Limits["storage"]?.ToInt64();
                        app.PersistentVolumes.Add(pvcName, new PersistentVolumeInfo() { Name=pvcName, StorageRequest = storageRequest, StorageLimit = storageLimit });
                        break;
                    case V1Service service:
                        var serviceType = service.Spec?.Type;
                        if(serviceType != "ClusterIP")
                        {
                            throw new NotSupportedException("In wordslab, Services must use 'type: ClusterIP'");
                        }
                        var port = service.Spec?.Ports?.FirstOrDefault()?.Port;
                        if (port.HasValue)
                        {
                            app.Services.Add(service.Name(), port.Value);
                        }
                        break;
                    case V1Ingress ingress:
                        throw new NotSupportedException("Igress resources are not supported in wordslab: please use IngressRoute instead (https://doc.traefik.io/traefik/routing/providers/kubernetes-crd/#kind-ingressroute)");
                    case TraefikV1alpha1IngressRoute ingressRoute:
                        var routeInfo = new IngressRouteInfo();
                        routeInfo.PrefixDefault = ingressRoute.GetLabel(ROUTE_PREFIX_PLACEHOLDER);
                        if (routeInfo.PrefixDefault == null)
                        {
                            throw new FormatException($"In wordslab yaml app files, the label '{ROUTE_PREFIX_PLACEHOLDER}' is mandatory on all IngressRoutes");
                        }
                        app.IngressRoutes.Add(routeInfo);
                        break;
                }
            }
            // Check PVC references

            // Check Service references

            return app;
        }

        private async Task AddPodSpec(string resourceName, V1PodSpec podSpec)
        {
            if (podSpec == null)
            {
                if (podSpec.Containers != null)
                {
                    foreach (var container in podSpec.Containers)
                    {
                        var imageName = container.Image;
                        if (!String.IsNullOrEmpty(imageName))
                        {
                            ContainerImage containerImage = null;
                            if (!ContainerImages.ContainsKey(imageName))
                            {
                                containerImage = await ContainerImage.GetMetadataFromRegistryAsync(imageName);
                                ContainerImages.Add(imageName, containerImage);
                            }
                            else
                            {
                                containerImage = ContainerImages[imageName];
                            }
                            foreach (var layer in containerImage.Manifest.Layers())
                            {
                                if (!ContainerImagesLayers.ContainsKey(layer.digest))
                                {
                                    ContainerImagesLayers.Add(layer.digest, new ContainerImageLayerInfo(layer, containerImage));
                                }
                                else
                                {
                                    ContainerImagesLayers[layer.digest].UsedByContainerImages.Add(containerImage);
                                }
                                ContainerImagesLayers[layer.digest].UsedByResourceNames.Add(resourceName);
                            }
                        }
                    }
                }
                if(podSpec.Volumes != null)
                {
                    foreach(var volume in podSpec.Volumes)
                    {
                        if( volume.EmptyDir != null ||
                            volume.ConfigMap != null || 
                            volume.Secret != null ||
                            volume.DownwardAPI != null ||
                            volume.Projected != null)
                        {
                            continue;
                        }

                        var claimName = volume.PersistentVolumeClaim?.ClaimName;
                        if(claimName == null)
                        {
                            throw new NotSupportedException("In wordlabs, Volumes must make a valid reference to a PersistentVolumeClaim or be of type: emptyDir, configMap, secret, downwardApi, projected.");
                        }
                        HashSet<string> references = null;
                        if(!PVCReferences.ContainsKey(claimName))
                        {
                            references = new HashSet<string>();
                            PVCReferences.Add(claimName, references);
                        }
                        else
                        {
                            references = PVCReferences[claimName];
                        }
                        references.Add(resourceName);
                    }
                }
            }
        }

        // Public properties of the application
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Date { get; set; }
        public string HomePage { get; set; }
        public string Source { get; set; }
        public string Author { get; set; }
        public string Licence { get; set; }

        // Raw text of the yaml file
        internal string YamlFileContent;

        public List<IngressRouteInfo> IngressRoutes { get; } = new List<IngressRouteInfo>();

        public Dictionary<string, ContainerImage> ContainerImages { get; } = new Dictionary<string, ContainerImage>();

        public Dictionary<string, ContainerImageLayerInfo> ContainerImagesLayers { get; } = new Dictionary<string, ContainerImageLayerInfo>();

        public Dictionary<string, PersistentVolumeInfo> PersistentVolumes { get; } = new Dictionary<string, PersistentVolumeInfo>();

        // Used only while parsing the YAML file
        private Dictionary<string, int> Services = new Dictionary<string, int>();
        private Dictionary<string, HashSet<string>> ServiceReferences = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, HashSet<string>> PVCReferences = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// IngressRoute is the CRD implementation of a Traefik HTTP Router.
        /// </summary>
        public class TraefikV1alpha1IngressRoute : IKubernetesObject<V1ObjectMeta>
        {
            // https://doc.traefik.io/traefik/reference/dynamic-configuration/kubernetes-crd/


            /// <summary>
            /// APIVersion defines the versioned schema of this representation of an object. 
            /// Servers should convert recognized schemas to the latest internal value, and may reject unrecognized values.
            /// More info: 
            /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#resources
            /// </summary>
            [JsonPropertyName("apiVersion")]
            public string ApiVersion { get; set; }

            /// <summary>
            /// Kind is a string value representing the REST resource this object represents.
            /// Servers may infer this from the endpoint the client submits requests to. 
            /// Cannot be updated.In CamelCase. More info: 
            /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#types-kinds
            /// </summary>
            [JsonPropertyName("kind")]
            public string Kind { get; set; }

            /// <summary>
            /// Standard object's metadata. More info:
            /// https://git.k8s.io/community/contributors/devel/sig-architecture/api-conventions.md#metadata
            /// </summary>
            [JsonPropertyName("metadata")]
            public V1ObjectMeta Metadata { get; set; }

            /// <summary>
            /// IngressRouteSpec defines the desired state of IngressRoute.
            /// </summary>
            public Spec spec { get; set; }

            /// <summary>
            /// IngressRouteSpec defines the desired state of IngressRoute.
            /// </summary>
            public class Spec
            {
                /// <summary>
                /// EntryPoints defines the list of entry point names to bind to.
                /// Entry points have to be configured in the static configuration.
                /// More info: https://doc.traefik.io/traefik/v2.9/routing/entrypoints/
                /// Default: all.
                /// </summary>
                public string[] entryPoints { get; set; }

                /// <summary>
                /// Routes defines the list of routes.
                /// </summary>
                public Route[] routes { get; set; }

                /// <summary>
                /// Route holds the HTTP route configuration.
                /// </summary>
                public class Route
                {
                    /// <summary>
                    /// Kind defines the kind of the route. 
                    /// Rule is the only supported kind.
                    /// </summary>
                    public string kind { get; set; }

                    /// <summary>
                    /// Match defines the router's rule. 
                    /// More info: https://doc.traefik.io/traefik/v2.9/routing/routers/#rule
                    /// </summary>
                    public string match { get; set; }

                    /// <summary>
                    /// Services defines the list of Service. 
                    /// It can contain any combination of TraefikService and/or reference to a Kubernetes Service.
                    /// </summary>
                    public Service[] services { get; set; }

                    /// <summary>
                    /// Service defines an upstream HTTP service to proxy traffic to.
                    /// </summary>
                    public class Service
                    {
                        /// <summary>
                        /// Kind defines the kind of the Service.
                        /// enum:
                        /// - Service
                        /// - TraefikService
                        /// </summary>
                        public string kind { get; set; }

                        /// <summary>
                        /// Name defines the name of the referenced Kubernetes Service or TraefikService.
                        /// The differentiation between the two is specified in the Kind field.
                        /// </summary>
                        public string name { get; set; }

                        /// <summary>
                        ///  Namespace defines the namespace of the referenced Kubernetes Service or TraefikService.
                        /// </summary>
                        [JsonPropertyName("namespace")]
                        public string Namespace { get; set; }

                        /// <summary>
                        /// Port defines the port of a Kubernetes Service.
                        /// This can be a reference to a named port.
                        /// </summary>
                        public int port { get; set; }
                    }
                }
            }
        }

        public class IngressRouteInfo
        {
            public string PrefixDefault { get; set; }

            public List<PathInfo> Paths { get; } = new List<PathInfo>();

            public class PathInfo
            {
                public string Path { get; }
                public string Title { get; set; }
            }

            internal Dictionary<string, int> ServiceReferences { get; } = new Dictionary<string, int>();
        }

        public class ContainerImageLayerInfo
        {
            public ContainerImageLayerInfo(ContainerImage.ImageManifest.Layer layer, ContainerImage containerImage)
            {
                Layer = layer;
                UsedByContainerImages.Add(containerImage);
            }

            public ContainerImage.ImageManifest.Layer Layer { get; set; }

            public List<ContainerImage> UsedByContainerImages { get; set; } = new List<ContainerImage>();

            public HashSet<string> UsedByResourceNames { get; set; } = new HashSet<string>();

            public LayerDownloadStatus DownloadStatus { get; set; }

            public long NeedToDownloadSize { get; set; }

            public enum LayerDownloadStatus
            {
                NeedToDownload,
                DownloadInProgress,
                DownloadedInContentStore
            }
        }

        public class PersistentVolumeInfo
        {
            public string Name { get; set; }

            public long? StorageRequest { get; set; }

            public long? StorageLimit { get; set; }

            public HashSet<string> UsedByResourceNames { get; set; } = new HashSet<string>();
        }
    }   
    

    public class ContainerImage
    {
        public string Registry { get; set; }

        public string Repository { get; set; }

        public string Tag { get; set; }

        public ImageManifest Manifest { get; set; }

        public static async Task<ContainerImage> GetMetadataFromRegistryAsync(string imageName)
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
            var authToken = ((System.Text.Json.JsonElement)authResponse["token"]).GetString();

            // Step 2: Manifest request
            var manifestServer = image.Registry == "registry.docker.io" ? "registry-1.docker.io" : image.Registry;
            var manifestMediaType = "application/vnd.docker.distribution.manifest.v2+json";
            var manifestListMediaType = "application/vnd.docker.distribution.manifest.list.v2+json";
            var manifestRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{manifestServer}/v2/{image.Repository}/manifests/{image.Tag}");
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
                manifestRequest = new HttpRequestMessage(HttpMethod.Get, $"https://{manifestServer}/v2/{image.Repository}/manifests/{digest}");
                manifestRequest.Headers.Add("Authorization", $"Bearer {authToken}");
                manifestRequest.Headers.Add("Accept", $"{manifestMediaType}");
                manifestResponse = await KubernetesApp._httpClient.SendAsync(manifestRequest);
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
        public class ImageManifest
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
                foreach(var layer in layers) { yield return layer; }
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
