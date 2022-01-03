namespace wordslab.installer.infrastructure
{
    public class K8sClusterConnection
    {
        public int Id { get; private set; }

        public string ClusterName { get; private set; }

        public string ExecEnvironmentName { get; private set; }

        public string KubeConfigFile { get; private set; }

        public string KuberConfigContext { get; private set; }
    }
}
