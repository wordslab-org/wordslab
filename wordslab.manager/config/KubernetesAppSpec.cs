using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace wordslab.manager.config
{
    public class KubernetesAppSpec : BaseConfig
    {
        // Public properties of the application
        public string Name { get; set; }
        public string NamespaceDefault { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Date { get; set; }
        public string HomePage { get; set; }
        public string Source { get; set; }
        public string Author { get; set; }
        public string Licence { get; set; }

        // Raw text of the yaml file
        public string YamlFileContent { get; set; }

        public static string ComputeHash(string yamlFileContent)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(yamlFileContent);
                var hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Unique hash of the yaml file content
        public string YamlFileHash { get; set; }

        // Structured view on the contents of the yaml file

        [NotMapped] // Extracted at load time by parsing YamlFileContent
        public List<IngressRouteInfo> IngressRoutes { get; } = new List<IngressRouteInfo>();

        public class IngressRouteInfo
        {
            public bool IsHttp { get; set; } = false;
            public bool IsHttps { get; set; } = false;

            public List<PathInfo> Paths { get; } = new List<PathInfo>();

            public List<Tuple<string,string>> UrlsAndTitles(string vmAddressAndPort, string deploymentNamespace) 
            {
                var urlsAndTitles = new List<Tuple<string,string>>();
                foreach (var pathInfo in Paths) 
                {
                    var url = $"http{(IsHttps ? 's' : null)}://{vmAddressAndPort}/{deploymentNamespace}{pathInfo.Path}";
                    urlsAndTitles.Add(Tuple.Create(url, pathInfo.Title));
                }
                return urlsAndTitles;
            }

            public class PathInfo
            {
                public string Path { get; set; }

                public string Title { get; set; }
            }
        }

        [NotMapped] // Extracted at load time by parsing YamlFileContent
        public Dictionary<string, ServiceInfo> Services { get; } = new Dictionary<string, ServiceInfo>();

        public class ServiceInfo
        {
            public string Name { get; set; }

            public string Title { get; set; }

            public string Description { get; set; }

            public int Port { get; set; }

            public string Url(string deploymentNamespace) { return $"{Name}.{deploymentNamespace}.svc.cluster.local:{Port}"; } 

            public HashSet<string> UsedByResourceNames { get; set; } = new HashSet<string>();
        }

        public List<ContainerImageInfo> ContainerImages { get; } = new List<ContainerImageInfo>();

        public IEnumerable<ContainerImageLayerInfo> ContainerImagesLayers()
        {
            return ContainerImages.SelectMany(image => image.Layers).Distinct();
        }

        [NotMapped] // Extracted at load time by parsing YamlFileContent
        public Dictionary<string, PersistentVolumeInfo> PersistentVolumes { get; } = new Dictionary<string, PersistentVolumeInfo>();

        public class PersistentVolumeInfo
        {
            public string Name { get; set; }

            public string Title { get; set; }

            public string Description { get; set; }

            public long? StorageRequest { get; set; }

            public long? StorageLimit { get; set; }

            public HashSet<string> UsedByResourceNames { get; set; } = new HashSet<string>();
        }
    }
}
