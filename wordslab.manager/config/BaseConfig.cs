namespace wordslab.manager.config
{
    public abstract class BaseConfig
    {
        public BaseConfig()
        {
            DateTimeCreated = DateTime.Now;
            DateTimeUpdated = DateTimeCreated;
        }

        public void RefreshUpdateDateTime()
        {
            DateTimeUpdated = DateTime.Now;
        }

        public DateTime DateTimeCreated { get; private set; }

        public DateTime DateTimeUpdated { get; private set; }
    }
}
