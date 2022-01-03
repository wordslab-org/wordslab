namespace wordslab.installer.infrastructure
{
    public class CloudAccountConnection
    {
        public int Id { get; private set; }

        public CloudProvider Provider { get; private set; }

        public string AccountName { get; private set; }

        public string CredentialsFile { get; private set; }
    }

    public enum CloudProvider
    {
        GoogleCloud
    }
}
