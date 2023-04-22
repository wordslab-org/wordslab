using k8s;
using k8s.Models;
using System.Text.Json.Serialization;
using wordslab.manager.config;

namespace wordslab.manager.apps
{
    public static class KubernetesApp
    {
        // Mandatory on all resources
        public const string APP_NAME_LABEL = "app.wordslab.org/name";
        public const string APP_COMPONENT_LABEL = "app.wordslab.org/component";

        // Mandatory only once on the first resource of the file
        public const string APP_TITLE_LABEL = "app.wordslab.org/title";
        public const string APP_DESCRIPTION_LABEL = "app.wordslab.org/description";
        public const string APP_VERSION_LABEL = "app.wordslab.org/version";
        public const string APP_DATE_LABEL = "app.wordslab.org/date";
        public const string APP_HOMEPAGE_LABEL = "app.wordslab.org/homepage";
        public const string APP_SOURCE_LABEL = "app.wordslab.org/source";
        public const string APP_AUTHOR_LABEL = "app.wordslab.org/author";
        public const string APP_LICENSE_LABEL = "app.wordslab.org/license";

        // Mandatory on all IngressRoutes
        public const string ROUTE_PREFIX_PLACEHOLDER = "app.wordslab.org/route/prefix";
        public const string ROUTE_PREFIX_DELIMITER = "$$";
        public const string ROUTE_PATH_TITLE_LABEL = "app.wordslab.org/route/path+title"; // path1, path2 ... if several needed
        public const string ROUTE_PATH_TITLE_SEPARATOR = "||";

        // Built-in wordslab apps
        public const string WORDSLAB_NOTEBOOKS_GPU_APP_URL = "https://raw.githubusercontent.com/wordslab-org/wordslab/main/wordslab.manager/apps/notebooks/wordslab-notebooks-gpu-app.yaml";
        public const string WORDSLAB_NOTEBOOKS_CPU_APP_URL = "https://raw.githubusercontent.com/wordslab-org/wordslab/main/wordslab.manager/apps/notebooks/wordslab-notebooks-cpu-app.yaml";

        private static Dictionary<string, Type> traefikTypeMap = new Dictionary<string, Type>()
        {
            { "traefik.containo.us/v1alpha1/IngressRoute", typeof(TraefikV1alpha1IngressRoute) }
        };

        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<KubernetesAppSpec> GetMetadataFromYamlFileAsync(string yamlFileURL)
        {
            var app = new KubernetesAppSpec();
            app.YamlFileContent = await httpClient.GetStringAsync(yamlFileURL);
            app.ComputeHash();


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
                    throw new FormatException("In wordslab yaml app files, the labels 'app.wordslab.org/name' and 'app.wordslab.org/component' are mandatory on all resources");
                }
                if (app.Name == null)
                {
                    app.Name = appName;

                    // First resource of the file, read all app properties
                    app.Title = resourceMetadata.GetLabel(APP_TITLE_LABEL);
                    if (app.Title == null)
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
                    if (appName != app.Name)
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
                        await AddPodSpec(app, resourceName, pod.Spec, pvcReferences);
                        break;
                    // ReplicationController: A replication controller ensures that a specified number of pod replicas are running at any given time.
                    // The container spec is defined within the spec.template.spec.containers field of a replication controller.
                    case V1ReplicationController replicaCtrl:
                        resourceName = $"replicationcontroller/{replicaCtrl.Name()}";
                        await AddPodSpec(app, resourceName, replicaCtrl.Spec?.Template?.Spec, pvcReferences);
                        break;
                    // ReplicaSet: A replica set is similar to a replication controller but provides more advanced selectors for managing pods.
                    // The container spec is defined within the spec.template.spec.containers field of a replica set.
                    case V1ReplicaSet replicaSet:
                        resourceName = $"replicaset.apps/{replicaSet.Name()}";
                        await AddPodSpec(app, resourceName, replicaSet.Spec?.Template?.Spec, pvcReferences);
                        break;
                    // Deployment: A deployment manages the rollout and scaling of a set of replicas.
                    // The container spec is defined within the spec.template.spec.containers field of a deployment.
                    case V1Deployment deployment:
                        resourceName = $"deployment.apps/{deployment.Name()}";
                        await AddPodSpec(app, resourceName, deployment.Spec?.Template?.Spec, pvcReferences);
                        break;
                    // StatefulSet: A stateful set manages the deployment and scaling of a set of stateful pods.
                    // The container spec is defined within the spec.template.spec.containers field of a stateful set.
                    case V1StatefulSet statefulSet:
                        resourceName = $"statefulset.apps/{statefulSet.Name()}";
                        await AddPodSpec(app, resourceName, statefulSet.Spec?.Template?.Spec, pvcReferences);
                        break;
                    // DaemonSet: A daemon set ensures that all(or some) nodes run a copy of a pod.
                    // The container spec is defined within the spec.template.spec.containers field of a daemon set.
                    case V1DaemonSet daemonSet:
                        resourceName = $"daemonset.apps/{daemonSet.Name()}";
                        await AddPodSpec(app, resourceName, daemonSet.Spec?.Template?.Spec, pvcReferences);
                        break;
                    // Job: A job creates one or more pods and ensures that a specified number of them successfully terminate.
                    // The container spec is defined within the spec.template.spec.containers field of a job.
                    case V1Job job:
                        resourceName = $"job.batch/{job.Name()}";
                        await AddPodSpec(app, resourceName, job.Spec?.Template?.Spec, pvcReferences);
                        break;
                    // CronJob: A cron job creates a job on a repeating schedule.
                    // The container spec is defined within the spec.jobTemplate.spec.template.spec.containers
                    case V1CronJob cronJob:
                        resourceName = $"cronjob.batch/{cronJob.Name()}";
                        await AddPodSpec(app, resourceName, cronJob.Spec?.JobTemplate?.Spec?.Template?.Spec, pvcReferences);
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
                        app.PersistentVolumes.Add(pvcName, new PersistentVolumeInfo() { Name = pvcName, StorageRequest = storageRequest, StorageLimit = storageLimit });
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
                            var serviceInfo = new ServiceInfo() { Name = serviceName, Port = port.Value, Title = service.GetLabel(APP_TITLE_LABEL) };
                            app.Services.Add(serviceName, serviceInfo);
                        }
                        break;
                    case V1Ingress ingress:
                        throw new NotSupportedException("Igress resources are not supported in wordslab: please use IngressRoute instead (https://doc.traefik.io/traefik/routing/providers/kubernetes-crd/#kind-ingressroute)");
                    case TraefikV1alpha1IngressRoute ingressRoute:
                        resourceName = $"ingressroute/{ingressRoute.Name()}";
                        var routeInfo = new IngressRouteInfo();
                        routeInfo.PrefixDefault = ingressRoute.GetLabel(ROUTE_PREFIX_PLACEHOLDER);
                        if (routeInfo.PrefixDefault == null)
                        {
                            throw new FormatException($"In wordslab yaml app files, the label '{ROUTE_PREFIX_PLACEHOLDER}' is mandatory on all IngressRoutes");
                        }
                        if (!routeInfo.PrefixDefault.StartsWith(ROUTE_PREFIX_DELIMITER) || !routeInfo.PrefixDefault.EndsWith(ROUTE_PREFIX_DELIMITER))
                        {
                            throw new FormatException($"In wordslab yaml app files, the label '{ROUTE_PREFIX_PLACEHOLDER}' must contain a placeholder which starts and ends with {ROUTE_PREFIX_DELIMITER}");
                        }
                        routeInfo.PrefixDefault = routeInfo.PrefixDefault.Substring(2, routeInfo.PrefixDefault.Length - 4);
                        AddPathInfo(ingressRoute, routeInfo, "");
                        app.IngressRoutes.Add(routeInfo);
                        for (var i = 1; i <= 20; i++)
                        {
                            var pathFound = AddPathInfo(ingressRoute, routeInfo, i.ToString());
                            if (!pathFound) break;
                        }
                        if (ingressRoute.spec?.routes != null)
                        {
                            foreach (var route in ingressRoute.spec?.routes)
                            {
                                if (route.services != null)
                                {
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
                            }
                        }
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
            return app;
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

        private static async Task AddPodSpec(KubernetesAppSpec app, string resourceName, V1PodSpec podSpec, Dictionary<string, HashSet<string>> pvcReferences)
        {
            if (podSpec != null)
            {
                if (podSpec.Containers != null)
                {
                    foreach (var container in podSpec.Containers)
                    {
                        var imageName = container.Image;
                        if (!String.IsNullOrEmpty(imageName))
                        {
                            ContainerImageInfo containerImage = null;
                            if (!app.ContainerImages.ContainsKey(imageName))
                            {
                                containerImage = await ContainerImage.GetMetadataFromRegistryAsync(imageName);
                                app.ContainerImages.Add(imageName, containerImage);
                            }
                            else
                            {
                                containerImage = app.ContainerImages[imageName];
                            }
                            foreach (var layer in containerImage.ConfigAndLayers())
                            {
                                if (!app.ContainerImagesLayers.ContainsKey(layer.Digest))
                                {
                                    app.ContainerImagesLayers.Add(layer.Digest, new ContainerImageLayerInfo(layer, containerImage));
                                }
                                else
                                {
                                    app.ContainerImagesLayers[layer.Digest].UsedByContainerImages.Add(containerImage);
                                }
                                app.ContainerImagesLayers[layer.Digest].UsedByResourceNames.Add(resourceName);
                            }
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
