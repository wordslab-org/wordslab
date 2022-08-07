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

        /// <summary>
        /// Moves the specified host directory and all its contents under a new base directory,
        /// then saves the new location in the configuration database.
        /// </summary>
        public void MoveHostDirectoryTo(HostDirectory.StorageFunction storageFunction, string destinationBaseDir)
        {
            hostStorage.MoveConfigurableDirectoryTo(storageFunction, destinationBaseDir);

            var oldHostDirectory = HostDirectories.Where(d => d.Function == storageFunction).First();
            var newHostDirectory = hostStorage.GetConfigurableDirectories().Where(d => d.Function == storageFunction).First();

            HostDirectories.Remove(oldHostDirectory);
            HostDirectories.Add(newHostDirectory);
            SaveChanges();
        }

        public DbSet<VirtualMachineConfig> VirtualMachines { get; set; }

        public void AddVirtualMachineConfig(VirtualMachineConfig vmConfig)
        {
            VirtualMachines.Add(vmConfig);
            SaveChanges();
        }

        /// <summary>
        /// Returns null if a virtual machine config with the specified name is not found
        /// </summary>
        public VirtualMachineConfig TryGetVirtualMachineConfig(string vmName)
        {
            return VirtualMachines.Where(vm => vm.Name == vmName).FirstOrDefault();
        }

        /// <summary>
        /// Does nothing a virtual machine config with the specified name is not found
        /// </summary>
        public void RemoveVirtualMachineConfig(string vmName)
        {
            var vmConfig = TryGetVirtualMachineConfig(vmName);
            if (vmConfig != null)
            {
                VirtualMachines.Remove(vmConfig);
                SaveChanges();
            }
        }

        // Configure table name
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HostDirectory>().ToTable("HostDirectory");
            modelBuilder.Entity<VirtualMachineConfig>().ToTable("VirtualMachine");
        }

        // Bootstrap the config database
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
