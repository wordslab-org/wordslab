using k8s;
using k8s.Models;
using System.Text.Json.Serialization;
using wordslab.manager.config;
using wordslab.manager.storage;
using static wordslab.manager.apps.KubernetesApp.TraefikV1alpha1IngressRoute.Spec.Route;
using static wordslab.manager.config.KubernetesAppSpec;

namespace wordslab.manager.apps
{
    public static class KubernetesApp
    {
        // LABELS mandatory on all resources
        public const string APP_NAME_LABEL = "wordslab.org/app";
        public const string APP_COMPONENT_LABEL = "wordslab.org/component";

        // ANNOTATIONS mandatory only once on the first resource of the file
        public const string APP_NAMESPACE_DEFAULT_ANNOT = "wordslab.org/namespace-default";
        public const string APP_TITLE_ANNOT = "wordslab.org/title";
        public const string APP_DESCRIPTION_ANNOT = "wordslab.org/description";
        public const string APP_VERSION_ANNOT = "wordslab.org/version";
        public const string APP_DATE_ANNOT = "wordslab.org/date";
        public const string APP_HOMEPAGE_ANNOT = "wordslab.org/homepage";
        public const string APP_SOURCE_ANNOT = "wordslab.org/source";
        public const string APP_AUTHOR_ANNOT = "wordslab.org/author";
        public const string APP_LICENSE_ANNOT = "wordslab.org/license";

        // NAMESPACE placeholder: will be replaced by the install namespace
        public const string NAMESPACE_PLACEHOLDER = "$$namespace$$";

        // ANNOTATIONS mandatory on all IngressRoutes
        public const string ROUTE_PATH_TITLE_LABEL = "wordslab.org/route-path-title"; // path1, path2 ... if several needed
        public const string ROUTE_PATH_TITLE_SEPARATOR = "|";

        // Built-in wordslab apps
        public const string WORDSLAB_NOTEBOOKS_GPU_APP_URL = "https://raw.githubusercontent.com/wordslab-org/wordslab/main/wordslab.manager/apps/notebooks/wordslab-notebooks-gpu-app.yaml";
        public const string WORDSLAB_NOTEBOOKS_CPU_APP_URL = "https://raw.githubusercontent.com/wordslab-org/wordslab/main/wordslab.manager/apps/notebooks/wordslab-notebooks-cpu-app.yaml";

        private static Dictionary<string, Type> traefikTypeMap = new Dictionary<string, Type>()
        {
            { "traefik.containo.us/v1alpha1/IngressRoute", typeof(TraefikV1alpha1IngressRoute) }
        };

        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<KubernetesAppInstall> ImportMetadataFromYamlFileAsync(string vmName, string yamlFileURL, ConfigStore configStore)
        {
            var app = new KubernetesAppInstall(vmName, yamlFileURL);
            app.YamlFileContent = await httpClient.GetStringAsync(yamlFileURL);
            app.ComputeHash();

            var existingApp = configStore.TryGetKubernetesApp(vmName, app.YamlFileHash);
            if (existingApp != null)
            {
                throw new InvalidOperationException($"A Kubernetes app install already exists in the virtual machine {vmName} for the yaml file {yamlFileURL}");
            }

            await ParseYamlFileContent(app, isCalledFromImportMetada:true, configStore);
            // Everything OK -> register in database
            app.RemainingDownloadSize = app.ContainerImagesLayers().Sum(layer => layer.Size);
            configStore.AddKubernetesApp(app);
            return app;
        }

        public static async Task ParseYamlFileContent(KubernetesAppInstall app, bool isCalledFromImportMetada = false, ConfigStore configStore = null)
        {
            // Used only while parsing the YAML file
            var serviceReferences = new Dictionary<string, HashSet<string>>();
            var pvcReferences = new Dictionary<string, HashSet<string>>();

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
                    throw new FormatException($"In wordslab yaml app files, the labels '{APP_NAME_LABEL}' and '{APP_COMPONENT_LABEL}' are mandatory on all resources");
                }
                if (app.Name == null)
                {
                    app.Name = appName;

                    // First resource of the file must be an ingressroute
                    if (!(resource is TraefikV1alpha1IngressRoute))
                    {
                        throw new FormatException($"In wordslab yaml app files, the first resource declared must be the IngressRoute for the application (here it is {resource.GetType().Name})");
                    }

                    // First resource of the file, read all app properties
                    app.NamespaceDefault = resourceMetadata.GetAnnotation(APP_NAMESPACE_DEFAULT_ANNOT);
                    if (app.NamespaceDefault == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_NAMESPACE_DEFAULT_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.Title = resourceMetadata.GetAnnotation(APP_TITLE_ANNOT);
                    if (app.Title == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_TITLE_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.Description = resourceMetadata.GetAnnotation(APP_DESCRIPTION_ANNOT);
                    if (app.Description == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_DESCRIPTION_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.Version = resourceMetadata.GetAnnotation(APP_VERSION_ANNOT);
                    if (app.Version == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_VERSION_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.Date = resourceMetadata.GetAnnotation(APP_DATE_ANNOT);
                    if (app.Date == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_DATE_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.HomePage = resourceMetadata.GetAnnotation(APP_HOMEPAGE_ANNOT);
                    if (app.HomePage == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_HOMEPAGE_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.Source = resourceMetadata.GetAnnotation(APP_SOURCE_ANNOT);
                    if (app.Source == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_SOURCE_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.Author = resourceMetadata.GetAnnotation(APP_AUTHOR_ANNOT);
                    if (app.Author == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_AUTHOR_ANNOT}' is mandatory on the first resource of the file");
                    }
                    app.Licence = resourceMetadata.GetAnnotation(APP_LICENSE_ANNOT);
                    if (app.Licence == null)
                    {
                        throw new FormatException($"In wordslab yaml app files, the annotation '{APP_LICENSE_ANNOT}' is mandatory on the first resource of the file");
                    }
                }
                else
                {
                    if (appName != app.Name)
                    {
                        throw new FormatException($"Inconsistent application name in '{APP_NAME_LABEL}' labels on different resources: ");
                    }
                }
                switch (resource)
                {
                    // Pod: A pod is the smallest deployable unit in Kubernetes and can contain one or more containers.
                    // The container spec is defined within the spec.containers field of a pod.
                    case V1Pod pod:
                        var resourceName = $"pod/{pod.Name()}";
                        await AddPodSpec(app, resourceName, pod.Spec, pvcReferences, isCalledFromImportMetada, configStore);
                        break;
                    // ReplicationController: A replication controller ensures that a specified number of pod replicas are running at any given time.
                    // The container spec is defined within the spec.template.spec.containers field of a replication controller.
                    case V1ReplicationController replicaCtrl:
                        resourceName = $"replicationcontroller/{replicaCtrl.Name()}";
                        await AddPodSpec(app, resourceName, replicaCtrl.Spec?.Template?.Spec, pvcReferences, isCalledFromImportMetada, configStore);
                        break;
                    // ReplicaSet: A replica set is similar to a replication controller but provides more advanced selectors for managing pods.
                    // The container spec is defined within the spec.template.spec.containers field of a replica set.
                    case V1ReplicaSet replicaSet:
                        resourceName = $"replicaset.apps/{replicaSet.Name()}";
                        await AddPodSpec(app, resourceName, replicaSet.Spec?.Template?.Spec, pvcReferences, isCalledFromImportMetada, configStore);
                        break;
                    // Deployment: A deployment manages the rollout and scaling of a set of replicas.
                    // The container spec is defined within the spec.template.spec.containers field of a deployment.
                    case V1Deployment deployment:
                        resourceName = $"deployment.apps/{deployment.Name()}";
                        await AddPodSpec(app, resourceName, deployment.Spec?.Template?.Spec, pvcReferences, isCalledFromImportMetada, configStore);
                        break;
                    // StatefulSet: A stateful set manages the deployment and scaling of a set of stateful pods.
                    // The container spec is defined within the spec.template.spec.containers field of a stateful set.
                    case V1StatefulSet statefulSet:
                        resourceName = $"statefulset.apps/{statefulSet.Name()}";
                        await AddPodSpec(app, resourceName, statefulSet.Spec?.Template?.Spec, pvcReferences, isCalledFromImportMetada, configStore);
                        break;
                    // DaemonSet: A daemon set ensures that all(or some) nodes run a copy of a pod.
                    // The container spec is defined within the spec.template.spec.containers field of a daemon set.
                    case V1DaemonSet daemonSet:
                        resourceName = $"daemonset.apps/{daemonSet.Name()}";
                        await AddPodSpec(app, resourceName, daemonSet.Spec?.Template?.Spec, pvcReferences, isCalledFromImportMetada, configStore);
                        break;
                    // Job: A job creates one or more pods and ensures that a specified number of them successfully terminate.
                    // The container spec is defined within the spec.template.spec.containers field of a job.
                    case V1Job job:
                        resourceName = $"job.batch/{job.Name()}";
                        await AddPodSpec(app, resourceName, job.Spec?.Template?.Spec, pvcReferences, isCalledFromImportMetada, configStore);
                        break;
                    // CronJob: A cron job creates a job on a repeating schedule.
                    // The container spec is defined within the spec.jobTemplate.spec.template.spec.containers
                    case V1CronJob cronJob:
                        resourceName = $"cronjob.batch/{cronJob.Name()}";
                        await AddPodSpec(app, resourceName, cronJob.Spec?.JobTemplate?.Spec?.Template?.Spec, pvcReferences, isCalledFromImportMetada, configStore);
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
                        long? storageRequest = null;
                        if (pvc.Spec?.Resources?.Requests != null)
                        {
                            storageRequest = pvc.Spec?.Resources?.Requests["storage"]?.ToInt64();
                        }
                        long? storageLimit = null;
                        if (pvc.Spec?.Resources?.Limits != null)
                        {
                            storageLimit = pvc.Spec?.Resources?.Limits["storage"]?.ToInt64();
                        }
                        var persistentVolumeInfo = new PersistentVolumeInfo() { Name = pvcName, StorageRequest = storageRequest, StorageLimit = storageLimit };
                        // Optional title and description if the service is public
                        persistentVolumeInfo.Title = pvc.GetAnnotation(APP_TITLE_ANNOT);
                        persistentVolumeInfo.Description = pvc.GetAnnotation(APP_DESCRIPTION_ANNOT);
                        app.PersistentVolumes.Add(pvcName, persistentVolumeInfo);
                        break;
                    case V1Service service:
                        var serviceType = service.Spec?.Type;
                        if (serviceType != "ClusterIP")
                        {
                            throw new NotSupportedException("In wordslab, Services must use 'type: ClusterIP'");
                        }
                        var port = service.Spec?.Ports?.FirstOrDefault()?.Port;
                        if (port.HasValue)
                        {
                            var serviceName = service.Name();
                            var serviceInfo = new ServiceInfo() { Name = serviceName, Port = port.Value };
                            // Optional title and description if the service is public
                            serviceInfo.Title = service.GetAnnotation(APP_TITLE_ANNOT);
                            serviceInfo.Description = service.GetAnnotation(APP_DESCRIPTION_ANNOT);
                            app.Services.Add(serviceName, serviceInfo);
                        }
                        break;
                    case V1Ingress ingress:
                        throw new NotSupportedException("Ingress resources are not supported in wordslab: please use IngressRoute instead (https://doc.traefik.io/traefik/routing/providers/kubernetes-crd/#kind-ingressroute)");
                    case TraefikV1alpha1IngressRoute ingressRoute:
                        resourceName = $"ingressroute/{ingressRoute.Name()}";
                        // Metadata for all routes
                        var routeInfo = new IngressRouteInfo();
                        AddPathInfo(ingressRoute, routeInfo, "");
                        for (var i = 1; i <= 20; i++)
                        {
                            var pathFound = AddPathInfo(ingressRoute, routeInfo, i.ToString());
                            if (!pathFound) break;
                        }
                        // Entrypoints
                        if (ingressRoute.spec.entryPoints == null || ingressRoute.spec.entryPoints.Length == 0)
                        {
                            throw new FormatException("At least one entrypoint must be specified in IngressResource spec");
                        }
                        var isHttp = false;
                        var isHttps = false;
                        foreach (var entrypoint in ingressRoute.spec?.entryPoints)
                        {
                            if (entrypoint == "web") routeInfo.IsHttp = true;
                            else if (entrypoint == "websecure") routeInfo.IsHttps = true;
                            else throw new FormatException("Only two entrypoints can be specified in IngressResource: 'web' (VM http port) or 'websecure' (VM https port)");
                        }
                        // Routes
                        if (ingressRoute.spec?.routes == null || ingressRoute.spec.routes.Length == 0)
                        {
                            throw new FormatException("At least one route must be specified in IngressResource spec");
                        }
                        foreach (var route in ingressRoute.spec.routes)
                        {
                            if (route.kind != "Rule" || route.match == null || !route.match.StartsWith("PathPrefix(`/$$namespace$$/"))
                            {
                                throw new FormatException("IngressResource routes must be of kind 'Rule' and they match property MUST start with: \"PathPrefix(`/$$namespace$$/\". This is to ensure that the URLs for this application will all stay inside the deployment namespace.");
                            }
                            // Services
                            if (route.services == null || route.services.Length == 0)
                            {
                                throw new FormatException("At least one service must be specified inside IngressResource spec.routes.services");
                            }
                            foreach (var serviceRef in route.services)
                            {
                                HashSet<string> resRef = null;
                                if (serviceReferences.ContainsKey(serviceRef.name))
                                {
                                    resRef = serviceReferences[serviceRef.name];
                                }
                                else
                                {
                                    resRef = new HashSet<string>();
                                    serviceReferences.Add(serviceRef.name, resRef);
                                }
                                resRef.Add(resourceName);
                            }
                        }
                        app.IngressRoutes.Add(routeInfo);
                        break;
                }
            }
            // Check Service references
            foreach (var serviceRef in serviceReferences)
            {
                if (app.Services.ContainsKey(serviceRef.Key))
                {
                    var serviceInfo = app.Services[serviceRef.Key];
                    foreach (var resourceName in serviceRef.Value)
                    {
                        serviceInfo.UsedByResourceNames.Add(resourceName);
                    }
                }
                else
                {
                    throw new FormatException($"Resource '{serviceRef.Value.First()}' references a service named '{serviceRef.Key}' which isn't defined in the Kubernetes yaml file");
                }
            }
            // Check PVC references
            foreach (var pvcRef in pvcReferences)
            {
                if (app.PersistentVolumes.ContainsKey(pvcRef.Key))
                {
                    var pvInfo = app.PersistentVolumes[pvcRef.Key];
                    foreach (var resourceName in pvcRef.Value)
                    {
                        pvInfo.UsedByResourceNames.Add(resourceName);
                    }
                }
                else
                {
                    throw new FormatException($"Resource '{pvcRef.Value.First()}' references a persistent volume claim named '{pvcRef.Key}' which isn't defined in the Kubernetes yaml file");
                }
            }
        }

        private static bool AddPathInfo(TraefikV1alpha1IngressRoute ingressRoute, IngressRouteInfo routeInfo, string suffixNum)
        {
            var pathLabel = ingressRoute.GetLabel(ROUTE_PATH_TITLE_LABEL + suffixNum);
            if (!String.IsNullOrEmpty(pathLabel))
            {
                var pathLabelParts = pathLabel.Split(ROUTE_PATH_TITLE_SEPARATOR);
                if (pathLabelParts.Length == 2)
                {
                    routeInfo.Paths.Add(new IngressRouteInfo.PathInfo() { Path = pathLabelParts[0], Title = pathLabelParts[1] });
                    return true;
                }
                else
                {
                    throw new FormatException($"In wordslab yaml app files, the label {ROUTE_PATH_TITLE_SEPARATOR} must contain two parts: a path and a title, separated by {ROUTE_PATH_TITLE_SEPARATOR}");
                }
            }
            return false;
        }

        private static async Task AddPodSpec(KubernetesAppSpec app, string resourceName, V1PodSpec podSpec, Dictionary<string, HashSet<string>> pvcReferences, bool isCalledFromImportMetada, ConfigStore configStore)
        {
            if (podSpec != null)
            {
                if (podSpec.Containers != null && isCalledFromImportMetada)
                {
                    foreach (var container in podSpec.Containers)
                    {                        
                        if (!String.IsNullOrEmpty(container.Image))
                        {
                            var imageName = ContainerImage.NormalizeImageName(container.Image);
                            // Check if the image reference is specific enough
                            if (imageName.EndsWith(":latest"))
                            {
                                throw new FormatException($"Invalid Docker image reference {container.Image} in resource {resourceName}: a more specific tag is mandatory (for reproducibility)");
                            }
                            ContainerImageInfo containerImage = null;
                            if (!app.ContainerImages.Any(image => image.Name == imageName))
                            {
                                containerImage = await ContainerImage.GetMetadataFromCacheOrFromRegistryAsync(imageName, configStore);
                                app.ContainerImages.Add(containerImage);
                            }
                            else
                            {
                                containerImage = app.ContainerImages.First(image => image.Name == imageName);
                            }
                            // Replace the image reference with the unambiguous digest
                            app.YamlFileContent.Replace(container.Image, containerImage.Name);
                        }
                    }
                }
                if (podSpec.Volumes != null)
                {
                    foreach (var volume in podSpec.Volumes)
                    {
                        if (volume.EmptyDir != null ||
                            volume.ConfigMap != null ||
                            volume.Secret != null ||
                            volume.DownwardAPI != null ||
                            volume.Projected != null)
                        {
                            continue;
                        }

                        var claimName = volume.PersistentVolumeClaim?.ClaimName;
                        if (claimName == null)
                        {
                            throw new NotSupportedException("In wordlabs, Volumes must make a valid reference to a PersistentVolumeClaim or be of type: emptyDir, configMap, secret, downwardApi, projected.");
                        }
                        HashSet<string> references = null;
                        if (!pvcReferences.ContainsKey(claimName))
                        {
                            references = new HashSet<string>();
                            pvcReferences.Add(claimName, references);
                        }
                        else
                        {
                            references = pvcReferences[claimName];
                        }
                        references.Add(resourceName);
                    }
                }
            }
        }

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
    }
}
