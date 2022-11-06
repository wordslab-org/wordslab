using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.config;

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

        private DbSet<HostMachineConfig> HostMachineConfigTable { get; set; }

        public HostMachineConfig HostMachineConfig
        {
            get
            {
                return HostMachineConfigTable.FirstOrDefault();
            }
        }

        public void InitializeHostMachineConfig(HostMachineConfig config)
        {
            if(HostMachineConfigTable.Any())
            {
                throw new InvalidOperationException("Host machine configuration already initialized");
            }
            HostMachineConfigTable.Add(config);
            SaveChanges();
        }

        /// <summary>
        /// Moves the specified host directory and all its contents under a new base directory,
        /// then saves the new location in the configuration database.
        /// </summary>
        public void MoveHostStorageLocationTo(StorageLocation storageLocation, string destinationBaseDir)
        {
            var config = HostMachineConfig;
            if(config == null)
            {
                throw new InvalidOperationException("Host machine configuration not yet initialized");
            }

            hostStorage.MoveConfigurableDirectoryTo(storageLocation, destinationBaseDir);

            config.VirtualMachineClusterPath = hostStorage.VirtualMachineClusterDirectory;
            config.VirtualMachineDataPath = hostStorage.VirtualMachineDataDirectory;
            config.BackupPath = hostStorage.BackupDirectory;
            SaveChanges();
        }

        /*
        private DbSet<CloudAccountConfig> CloudAccountConfigTable { get; set; }

        public CloudAccountConfig CloudAccountConfig
        {
            get
            {
                return CloudAccountConfigTable.FirstOrDefault();
            }
        }

        public void InitializeCloudAccountConfig(CloudAccountConfig config)
        {
            if (CloudAccountConfigTable.Any())
            {
                throw new InvalidOperationException("Cloud account configuration already initialized");
            }
            CloudAccountConfigTable.Add(config);
            SaveChanges();
        }
        */

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

        public DbSet<VirtualMachineInstance> VirtualMachineInstances { get; set; }

        public void AddVirtualMachineInstance(VirtualMachineInstance vmInstance)
        {
            VirtualMachineInstances.Add(vmInstance);
            SaveChanges();
        }

        /// <summary>
        /// Returns null if a virtual machine config with the specified name is not found
        /// </summary>
        public VirtualMachineInstance TryGetLastVirtualMachineInstance(string vmName)
        {
            return VirtualMachineInstances.Where(instance => instance.Name == vmName).OrderByDescending(instance => instance.StartTimestamp).FirstOrDefault();
        }

        // Configure table name
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HostMachineConfig>().ToTable("HostMachine");
            modelBuilder.Entity<CloudAccountConfig>().ToTable("CloudAccount");
            modelBuilder.Entity<VirtualMachineConfig>().ToTable("VirtualMachine");
            modelBuilder.Entity<VirtualMachineInstance>().ToTable("VMInstance").HasKey(instance => new { instance.Name, instance.DateTimeCreated });
        }

        // Bootstrap the config database
        private void InitializeHostStorage()
        {
            var config = HostMachineConfig;
            if (config != null)
            {
                hostStorage.InitConfigurableDirectories(
                    config.VirtualMachineClusterPath, config.VirtualMachineDataPath, config.BackupPath);
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
