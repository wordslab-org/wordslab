namespace wordslab.installer.infrastructure
{
    public class WordslabVMResources
    {
        // CPU consumption is between 0 and 1000 x logical CPUs
        // Memory and storage properties are measured in MB

        // everything else
        public int OSStorage;

        // /etc/rancher
        // /var/lib => expect the specific paths below
        // /var/log/rancher
        public int ClusterStateStorage;

        // 164M    /var/lib/rancher/k3s/data => busybox distrib with containerd and ipsec
        // 482M    /var/lib/rancher/k3s/agent/containerd/io.containerd.snapshotter.v1.overlayfs/snapshots
        // 471M    /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/blobs
        // 471M    /var/lib/rancher/k3s/agent/images
        public int TotalImageStorage;

        // /var/lib/rancher/k3s/agent/containerd/io.containerd.runtime.v2.task/k8s.io
        // /run/k3s/containerd/io.containerd.runtime.v2.task/k8s.io
        public int TotalContainerStorage;

        // /var/volume/rancher
        public int TotalVolumeStorage;

        public List<ImageResources> ImageResources;
    }

    public class ImageResources
    {
        public string Origin;
        public string Name;

        public int ImageStorage;

        public List<LayerResources> LayerResources;
    }

    public class LayerResources
    {
        public string Origin;
        public string Command;

        public int ContentStorage;
        public int LayerStorage;

        public string ContentPath;
        public string LayerPath;
    }
}
