using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wordslab.manager.config
{
    public class KubernetesAppSpec
    {
        // Public properties of the application
        [Key]
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        [Key]
        public string Version { get; set; }
        public string Date { get; set; }
        public string HomePage { get; set; }
        [Key]
        public string Source { get; set; }
        public string Author { get; set; }
        public string Licence { get; set; }

        // Raw text of the yaml file
        public string YamlFileContent { get; set; }

        public List<IngressRouteInfo> IngressRoutes { get; } = new List<IngressRouteInfo>();

        public Dictionary<string, ServiceInfo> Services { get; } = new Dictionary<string, ServiceInfo>();

        public Dictionary<string, ContainerImageInfo> ContainerImages { get; } = new Dictionary<string, ContainerImageInfo>();

        public Dictionary<string, ContainerImageLayerInfo> ContainerImagesLayers { get; } = new Dictionary<string, ContainerImageLayerInfo>();

        public Dictionary<string, PersistentVolumeInfo> PersistentVolumes { get; } = new Dictionary<string, PersistentVolumeInfo>();
    }

    [Owned]
    public class IngressRouteInfo
    {
        public string PrefixDefault { get; set; }

        public List<PathInfo> Paths { get; } = new List<PathInfo>();

        public class PathInfo
        {
            public string Path { get; set; }

            public string Title { get; set; }
        }

        internal Dictionary<string, int> ServiceReferences { get; } = new Dictionary<string, int>();
    }

    [Owned]
    public class ServiceInfo
    {
        public string Name { get; set; }

        public string Title { get; set; }

        public int Port { get; set; }

        public HashSet<string> UsedByResourceNames { get; set; } = new HashSet<string>();
    }

    [Owned]
    public class ContainerImageInfo
    {
        public string Registry { get; set; }

        public string Repository { get; set; }

        public string Tag { get; set; }

        /// <summary>
        /// The Config field references a configuration object for a container, by digest. 
        /// This configuration item is a JSON blob that the runtime uses to set up the container.
        /// </summary>
        public LayerInfo Config { get; set; } = new LayerInfo();

        /// <summary>
        /// The layer list is ordered starting from the base image (opposite order of schema1).
        /// </summary>
        public List<LayerInfo> Layers { get; set; } = new List<LayerInfo>();

        /// <summary>
        /// Returns config then layers
        /// </summary>
        public IEnumerable<LayerInfo> ConfigAndLayers()
        {
            yield return Config;
            foreach (var layer in Layers) { yield return layer; }
        }

        public class LayerInfo
        {
            /// <summary>
            /// The MIME type of the referenced object. 
            /// This should generally be application/vnd.docker.image.rootfs.diff.tar.gzip.
            /// </summary>
            public string MediaType { get; set; }

            /// <summary>
            /// The size in bytes of the object. 
            /// This field exists so that a client will have an expected size for the content before validating. 
            /// If the length of the retrieved content does not match the specified length, the content should not be trusted.
            /// </summary>
            public long Size { get; set; }

            /// <summary>
            /// The digest of the content, as defined by the Registry V2 HTTP API Specification.
            /// </summary>
            public string Digest { get; set; }
        }
    }

    [Owned]
    public class ContainerImageLayerInfo
    {
        public ContainerImageLayerInfo(ContainerImageInfo.LayerInfo layer, ContainerImageInfo containerImage)
        {
            Layer = layer;
            UsedByContainerImages.Add(containerImage);
        }

        public ContainerImageInfo.LayerInfo Layer { get; set; }

        public List<ContainerImageInfo> UsedByContainerImages { get; set; } = new List<ContainerImageInfo>();

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

    [Owned]
    public class PersistentVolumeInfo
    {
        public string Name { get; set; }

        public long? StorageRequest { get; set; }

        public long? StorageLimit { get; set; }

        public HashSet<string> UsedByResourceNames { get; set; } = new HashSet<string>();
    }
}
