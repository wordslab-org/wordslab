using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace wordslab.manager.config
{
    public class ContainerImageInfo
    {
        [Key]
        public string Name { get; set; }

        public string Registry { get; set; }

        public string Repository { get; set; }

        public string Tag { get; set; }

        /// <summary>
        /// The Config field references a configuration object for a container, by digest. 
        /// This configuration item is a JSON blob that the runtime uses to set up the container.
        /// </summary>
        [NotMapped]
        public ContainerImageLayerInfo Config { get { return Layers[0]; } }

        /// <summary>
        /// Index   [0]   : The config field.
        /// Indexes [1-n] : The layers list is ordered starting from the base image.
        /// </summary>
        public List<ContainerImageLayerInfo> Layers { get; set; } = new List<ContainerImageLayerInfo>();

        public List<KubernetesAppInstall> UsedByKubernetesApps { get; set; } = new List<KubernetesAppInstall>();
    }

    public class ContainerImageLayerInfo
    {
        public ContainerImageLayerInfo() { }

        public ContainerImageLayerInfo(string digest, string mediaType, long size)
        {
            Digest = digest;
            MediaType = mediaType;
            Size = size;
        }

        /// <summary>
        /// The digest of the content, as defined by the Registry V2 HTTP API Specification.
        /// </summary>
        [Key]
        public string Digest { get; set; }

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

        public List<ContainerImageInfo> UsedByContainerImages { get; set; } = new List<ContainerImageInfo>();
    }
}
