using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.storage.config;
using wordslab.manager.vm;

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

        public void AddVirtualMachineConfig(VirtualMachineConfig vmConfig)
        {
            VirtualMachines.Add(vmConfig);
            SaveChanges();
        }

        public VirtualMachineConfig TryGetVirtualMachineConfig(string vmName)
        {
            return VirtualMachines.Where(vm => vm.Name == vmName).FirstOrDefault();
        }

        public void RemoveVirtualMachineConfig(string vmName)
        {
            var vmConfig = VirtualMachines.Where(vmConfig => vmConfig.Name == vmName).FirstOrDefault();
            if (vmConfig != null)
            {
                VirtualMachines.Remove(vmConfig);
                SaveChanges();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HostDirectory>().ToTable("HostDirectory");
            modelBuilder.Entity<VirtualMachineConfig>().ToTable("VirtualMachine");
        }

        private void InitializeHostStorage()
        {
            if (HostDirectories.Any())
            {
                hostStorage.InitConfigurableDirectories(HostDirectories);
            }
            else
            {
                var localDirectories = hostStorage.GetConfigurableDirectories();
                HostDirectories.AddRange(localDirectories);
                SaveChanges();
            }
        }

        // EF Core is not compatible with assembly trimming by default
        [DynamicDependency(DynamicallyAccessedMemberTypes.All,  typeof(DateOnly))]
        public static void CreateDbIfNotExistsAndInitializeHostStorage(IServiceProvider hostServiceProvider)
        {
            using (var scope = hostServiceProvider.CreateScope())
            {
                var servicesInScope = scope.ServiceProvider;
                try
                {
                    var dbContextFactory = servicesInScope.GetRequiredService<IDbContextFactory<ConfigStore>>();
                    using var configStore = dbContextFactory.CreateDbContext();
                    configStore.Database.EnsureCreated();
                    configStore.InitializeHostStorage();
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
