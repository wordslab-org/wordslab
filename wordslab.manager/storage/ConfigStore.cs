﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
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
        /// Returns null if a virtual machine instance with the specified name is not found
        /// </summary>
        public VirtualMachineInstance TryGetFirstVirtualMachineInstance(string vmName)
        {
            return VirtualMachineInstances.Where(instance => instance.Name == vmName).OrderBy(instance => instance.StartTimestamp).FirstOrDefault();
        }

        /// <summary>
        /// Returns null if a virtual machine instance with the specified name is not found
        /// </summary>
        public VirtualMachineInstance TryGetLastVirtualMachineInstance(string vmName)
        {
            return VirtualMachineInstances.Where(instance => instance.Name == vmName).OrderByDescending(instance => instance.StartTimestamp).FirstOrDefault();
        }

        public TimeSpan GetVirtualMachineInstanceTotalRunningTime(string vmName)
        {
            var totalTicks = VirtualMachineInstances.Where(vmi => vmi.Name == vmName && vmi.StopTimestamp != null).ToList().Select(vmi => vmi.StopTimestamp.Value.Subtract(vmi.StartTimestamp).Ticks).DefaultIfEmpty(0).Sum();
            return new TimeSpan(totalTicks);
        }

        /// <summary>
        /// Returns an empy list if a virtual machine instance with the specified name is not found
        /// </summary>
        public List<VirtualMachineInstance> GetVirtualMachineInstancesHistory(string vmName)
        {
            return VirtualMachineInstances.Where(instance => instance.Name == vmName).OrderBy(instance => instance.StartTimestamp).ToList();
        }

        public DbSet<KubernetesAppInstall> KubernetesApps { get; set; }

        public List<KubernetesAppInstall> ListKubernetesAppsInstalledOn(string vmName)
        {
            return KubernetesApps.Where(app => app.VirtualMachineName == vmName && !app.UninstallDate.HasValue).ToList();
        }

        public void AddKubernetesApp(KubernetesAppInstall app)
        {
            KubernetesApps.Add(app);
            SaveChanges();
        }

        /// <summary>
        /// Returns null if a kubernetes app is not found on the specified virtual machine and with the specified content hash
        /// </summary>
        public KubernetesAppInstall TryGetKubernetesApp(string vmName, string yamlFileHash)
        {
            return KubernetesApps.Where(app => app.VirtualMachineName == vmName && app.YamlFileHash == yamlFileHash).Include(app => app.ContainerImages).ThenInclude(image => image.Layers).FirstOrDefault();
        }

        /// <summary>
        /// Does nothing if a kubernetes app is not found on the specified virtual machine and with the specified content hash
        /// </summary>
        public void RemoveKubernetesApp(string vmName, string yamlFileHash)
        {
            var app = TryGetKubernetesApp(vmName, yamlFileHash);
            if (app != null)
            {
                app.UninstallDate = DateTime.Now;
                SaveChanges();
            }
        }

        public DbSet<KubernetesAppDeployment> KubernetesAppDeployments { get; set; }

        public void AddAppDeployment(KubernetesAppDeployment app)
        {
            KubernetesAppDeployments.Add(app);
            SaveChanges();
        }

        /// <summary>
        /// Returns null if a kubernetes app deployment is not found on the specified virtual machine and in the specified namespace
        /// </summary>
        public KubernetesAppDeployment TryGetKubernetesAppDeployment(string vmName, string deploymentNamespace)
        {
            return KubernetesAppDeployments.Where(app => app.VirtualMachineName == vmName && app.Namespace == deploymentNamespace).FirstOrDefault();
        }

        /// <summary>
        /// Does nothing if a kubernetes app is not found on the specified virtual machine and in the specified namespace
        /// </summary>
        public void RemoveKubernetesAppDeployment(string vmName, string deploymentNamespace)
        {
            var app = TryGetKubernetesAppDeployment(vmName, deploymentNamespace);
            if (app != null)
            {
                app.RemovalDate = DateTime.Now;
                SaveChanges();
            }
        }

        /// <summary>
        /// Installed container images
        /// </summary>
        public DbSet<ContainerImageInfo> ContainerImages { get; set; }
        
        public void AddContainerImage(ContainerImageInfo image)
        {
            ContainerImages.Add(image);
            SaveChanges();
        }
              
        public ContainerImageInfo TryGetContainerImageByName(string normalizedImageName)
        {
            return ContainerImages.Where(image => image.Name == normalizedImageName).Include(image => image.Layers).FirstOrDefault();
        }

        public ContainerImageInfo TryGetContainerImageByDigest(string digest)
        {
            return ContainerImages.Where(image => image.Digest == digest).Include(image => image.Layers).FirstOrDefault();
        }        

        /// <summary>
        /// Installed container image layers
        /// </summary>
        public DbSet<ContainerImageLayerInfo> ContainerImageLayers { get; set; }

        public void AddContainerImageLayer(ContainerImageLayerInfo layer)
        {
            ContainerImageLayers.Add(layer);
        }

        public ContainerImageLayerInfo TryGetContainerImageLayer(string digest)
        {
            return ContainerImageLayers.Where(layer => layer.Digest == digest).FirstOrDefault();
        }

        // Configure table name
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set table names
            modelBuilder.Entity<HostMachineConfig>().ToTable("HostMachine");
            // modelBuilder.Entity<CloudAccountConfig>().ToTable("CloudAccount");
            modelBuilder.Entity<VirtualMachineConfig>().ToTable("VirtualMachine");
            modelBuilder.Entity<VirtualMachineInstance>().ToTable("VMInstance");
            modelBuilder.Entity<KubernetesAppInstall>().ToTable("KubernetesApp");
            modelBuilder.Entity<KubernetesAppDeployment>().ToTable("AppDeployment");
            modelBuilder.Entity<ContainerImageInfo>().ToTable("ContainerImage");
            modelBuilder.Entity<ContainerImageLayerInfo>().ToTable("ImageLayer");

            // Describe entities many to many relationships
            modelBuilder.Entity<KubernetesAppInstall>(app => app.HasMany(app => app.ContainerImages).WithMany(image => image.UsedByKubernetesApps));
            modelBuilder.Entity<ContainerImageInfo>(image => image.HasMany(image => image.Layers).WithMany(layer => layer.UsedByContainerImages));
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
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DateOnly))]
        public static void CreateOrUpdateDbSchemaAndInitializeHostStorage(IServiceProvider hostServiceProvider)
        {
            using (var scope = hostServiceProvider.CreateScope())
            {
                var servicesInScope = scope.ServiceProvider;
                try
                {
                    var dbContextFactory = servicesInScope.GetRequiredService<IDbContextFactory<ConfigStore>>();
                    using var configStore = dbContextFactory.CreateDbContext();
                    configStore.Database.Migrate();
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
    
    /// <summary>
    /// Used only at build time to compute database migrations between versions.
    /// </summary>
    public class ConfigStoretFactory : IDesignTimeDbContextFactory<ConfigStore>
    {
        public ConfigStore CreateDbContext(string[] args)
        {
            var hostStorage = new HostStorage();
            var databasePath = Path.Combine(hostStorage.ConfigDirectory, "wordslab-config.db");
            
            var optionsBuilder = new DbContextOptionsBuilder<ConfigStore>();
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
            return new ConfigStore(optionsBuilder.Options, hostStorage);
        }
    }
}
