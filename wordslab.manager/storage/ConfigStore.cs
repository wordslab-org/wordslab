using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.storage.config;

namespace wordslab.manager.storage
{
    public class ConfigStore : DbContext
    {
        private readonly HostStorage hostStorage;

        public ConfigStore(DbContextOptions<ConfigStore> options, HostStorage hostStorage)
            : base(options)
        {
            this.hostStorage = hostStorage;
        }

        public DbSet<HostDirectory> HostDirectories { get; set; }

        public DbSet<VirtualMachineConfig> VirtualMachines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HostDirectory>().ToTable("HostDirectory");
            modelBuilder.Entity<VirtualMachineConfig>().ToTable("VirtualMachine");
        }

        internal void Initialize()
        {
            if (HostDirectories.Any()) { return; }

            var localDirectories = hostStorage.GetConfigurableDirectories();
            HostDirectories.AddRange(localDirectories);
            SaveChanges();
        }

        // EF Core is not compatible with assembly trimming by default
        [DynamicDependency(DynamicallyAccessedMemberTypes.All,  typeof(DateOnly))]
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
