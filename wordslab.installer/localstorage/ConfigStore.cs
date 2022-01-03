using Microsoft.EntityFrameworkCore;
using wordslab.installer.infrastructure;

namespace wordslab.installer.localstorage
{
    public class ConfigStore : DbContext
    {
        private readonly LocalStorageManager localStorageManager;

        public ConfigStore(DbContextOptions<ConfigStore> options, LocalStorageManager localStorageManager)
            : base(options)
        {
            this.localStorageManager = localStorageManager;
        }

        public DbSet<LocalDirectory> LocalDirectories { get; set; }

        public DbSet<MachineConnection> MachineConnections { get; set; }
        public DbSet<CloudAccountConnection> CloudAccountConnections { get; set; }
        public DbSet<K8sClusterConnection> k8SClusterConnections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalDirectory>().ToTable("LocalDirectory");
            modelBuilder.Entity<MachineConnection>().ToTable("MachineConnection");
            modelBuilder.Entity<CloudAccountConnection>().ToTable("CloudAccountConnection");
            modelBuilder.Entity<K8sClusterConnection>().ToTable("K8sClusterConnection");
        }

        internal void Initialize()
        {
            if (LocalDirectories.Any()) { return; }

            var localDirectories = localStorageManager.GetDirectories();
            LocalDirectories.AddRange(localDirectories);
            SaveChanges();
        }
    }
}
