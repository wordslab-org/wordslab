using Microsoft.EntityFrameworkCore;

namespace wordslab.manager.storage
{
    public class ConfigStore : DbContext
    {
        private readonly StorageManager storageManager;

        public ConfigStore(DbContextOptions<ConfigStore> options, StorageManager storageManager)
            : base(options)
        {
            this.storageManager = storageManager;
        }

        public DbSet<LocalDirectory> LocalDirectories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalDirectory>().ToTable("LocalDirectory");
        }

        internal void Initialize()
        {
            if (LocalDirectories.Any()) { return; }

            var localDirectories = storageManager.GetConfigurableDirectories();
            LocalDirectories.AddRange(localDirectories);
            SaveChanges();
        }

        public static void CreateDbIfNotExists(IServiceProvider hostServiceProvider)
        {
            using (var scope = hostServiceProvider.CreateScope())
            {
                var servicesInScope = scope.ServiceProvider;
                try
                {
                    var dbContextFactory = servicesInScope.GetRequiredService<IDbContextFactory<ConfigStore>>();
                    using var configStore = dbContextFactory.CreateDbContext();
                    configStore.Database.EnsureCreated();
                    configStore.Initialize();
                }
                catch (Exception ex)
                {
                    var logger = servicesInScope.GetRequiredService<ILogger<ConfigStore>>();
                    logger.LogError(ex, "An error occurred creating the config database.");
                }
            }
        }
    }
}
