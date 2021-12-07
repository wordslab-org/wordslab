# paths.ts

interface Paths {
  /** Directory which holds configuration. */
  config: string;
  /** Directory which holds logs. */
  logs: string;
  /** Directory which holds caches that may be removed. */
  cache: string;
  /** Directory holding the WSL distribution (Windows-specific). */
  wslDistro: string;
  /** Directory holding the WSL data distribution (Windows-specific). */
  wslDistroData: string;
  /** Directory holding Lima state (macOS-specific). */
  lima: string;
  /** Directory holding provided binary resources */
  integration: string;
}

/**
 * Win32Paths implements paths for Windows.
 */
export class Win32Paths implements Paths {
  protected readonly appData = process.env['APPDATA'] || path.join(os.homedir(), 'AppData', 'Roaming');
  protected readonly localAppData = process.env['LOCALAPPDATA'] || path.join(os.homedir(), 'AppData', 'Local');
  get config() {
    return path.join(this.appData, APP_NAME);
  }

  get logs() {
    return path.join(this.localAppData, APP_NAME, 'logs');
  }

  get cache() {
    return path.join(this.localAppData, APP_NAME, 'cache');
  }

  get wslDistro() {
    return path.join(this.localAppData, APP_NAME, 'distro');
  }

  get wslDistroData() {
    return path.join(this.localAppData, APP_NAME, 'distro-data');
  }

  get lima(): string {
    throw new Error('lima not available for Windows');
  }

  /**
 * DarwinPaths implements paths for Darwin / macOS.
 */
export class DarwinPaths implements Paths {
  config = path.join(os.homedir(), 'Library', 'Preferences', APP_NAME);
  logs = path.join(os.homedir(), 'Library', 'Logs', APP_NAME);
  cache = path.join(os.homedir(), 'Library', 'Caches', APP_NAME);
  lima = path.join(os.homedir(), 'Library', 'Application Support', APP_NAME, 'lima');
  hyperkit = path.join(os.homedir(), 'Library', 'State', APP_NAME, 'driver');
  integration = '/usr/local/bin';
  get wslDistro(): string {
    throw new Error('wslDistro not available for darwin');
  }

  get wslDistroData(): string {
    throw new Error('wslDistro not available for darwin');
  }
}

/**
 * LinuxPaths implements paths for Linux.
 */
export class LinuxPaths implements Paths {
  protected readonly dataHome = process.env['XDG_DATA_HOME'] || path.join(os.homedir(), '.local', 'share');
  protected readonly configHome = process.env['XDG_CONFIG_HOME'] || path.join(os.homedir(), '.config');
  protected readonly cacheHome = process.env['XDG_CACHE_HOME'] || path.join(os.homedir(), '.cache');
  get config() {
    return path.join(this.configHome, APP_NAME);
  }

  get logs() {
    return path.join(this.dataHome, APP_NAME, 'logs');
  }

  get cache() {
    return path.join(this.cacheHome, APP_NAME);
  }

  get wslDistro(): string {
    throw new Error('wslDistro not available for Linux');
  }

  get wslDistroData(): string {
    throw new Error('wslDistro not available for Linux');
  }

  get lima(): string {
    return path.join(this.dataHome, APP_NAME, 'lima');
  }

  get integration(): string {
    return path.join(os.homedir(), '.local', 'bin');
  }

# background.ts / settings.ts / update/index.ts / main/tray.ts

## 1. Settings load or init

enum ContainerEngine {
  NONE = '',
  CONTAINERD = 'containerd',
  MOBY = 'moby',
}

defaultSettings = {
  version:    3,
  kubernetes: {
    version:         '',
    memoryInGB:      2,
    numberCPUs:      2,
    port:            6443,
    containerEngine: ContainerEngine.CONTAINERD,
  },
  portForwarding:  { includeKubernetesServices: false },
  images:          {
    showAll:   true,
    namespace: 'k8s.io',
  },
  telemetry:       true,
  /** Whether we should check for updates and apply them. */
  updater:        true,
}

settings.init()
- try settings = load()
  - readFile(join(paths.config, 'settings.json'))
  - updateSettings(settings)
    - if (loadedVersion < CURRENT_SETTINGS_VERSION):  updateTable[loadedVersion](settings)
- if error: _isFirstRun = true
  - settings = defaultSettings
  - if (os.platform() === 'darwin' || os.platform() === 'linux'): settings.kubernetes.memoryInGB = Math.min(6, Math.round(totalMemoryInGB / 4.0))
  - if (os.platform() === 'linux' && !process.env['APPIMAGE']): settings.updater = false
  - save(settings)
    - mkdir(paths.config, { recursive: true })
    - writeFile(join(paths.config, 'settings.json'), cfg)
- if success: _isFirstRun = false

## 2. Auto update

// Set up the updater; we may need to quit the app if an update is already queued.
 if (await setupUpdate(cfg, true)) {
    console.log('Will apply update; skipping startup.');
    return;
}
- enabled = settings.updater
  - https://www.electronjs.org/docs/latest/tutorial/updates
  - "update/LonghornProvider.ts"
  - LonghornProvider is a Provider that interacts with Longhorn's
  - [Upgrade Responder](https://github.com/longhorn/upgrade-responder) server to
  - locate upgrade versions.  It assumes that the versions are actually published
  - as GitHub releases.  It also assumes that all versions have assets for all
  - platforms (that is, it doesn't filter by platform on checking).
  - Note that we do internal caching to avoid issues with being double-counted in
   - the stats.
   - upgradeServer: https://desktop.version.rancher.io/v1/checkupgrade
- // Get the latest release from the upgrade responder.
- // Get release information from GitHub releases.
- if (autoUpdater.hasUpdateConfiguration):
- autoUpdater.checkForUpdates()
- setHasQueuedUpdate(true)
- autoUpdater.quitAndInstall(true, true)

## 3. First run

doFirstRun()
- if settings._isFirstRun:
- window.openFirstRun() => see FirstRun.vue
- if (os.platform() === 'darwin' || os.platform() === 'linux'):
  - linkResource('docker', true),
    - linkPath = path.join(paths.integration, name)
    - mkdir(paths.integration, { recursive: true })
    - symlink(resources.executable(name), linkPath, 'file')
  - linkResource('helm', true),
  - linkResource('kim', true), // TODO: Remove when we stop shipping kim
  - linkResource('kubectl', true),
  - linkResource('nerdctl', true)

### 3.1 FirstRun.vue

mounted()
- settings-read	=> settings
- k8s-versions => versions
  - await k8smanager.availableVersions
  - settings.kubernetes.version = versions[0]
- send('firstrun/ready')
  => window.show();

Please select a Kubernetes version:
onChange()
- invoke('settings-write', { kubernetes: { version: this.settings.kubernetes.version } } )

close()
- onChange()
- window.close

## 4. Setup Tray menu and status

setupTray()
- contextMenuItems: [
    {
      id:      'state',
      label:   'Kubernetes is starting',
    {
      id:    'preferences',
      label: 'Preferences',
      click: openPreferences,
    {
      id:      'contexts',
      label:   'Kubernetes Contexts',
      type:    'submenu',
      submenu: [],
    {
      label: 'Quit Rancher Desktop',
      role:  'quit',
  ];

- updateContexts()
  - kc = new KubeConfig()
  - verifyKubeConfig()
    - originalFiles = process.env.KUBECONFIG.split(pth.delimiter)
    - filteredFiles = originalFiles.filter(kubeconfig.hasAccess)
    - process.env.KUBECONFIG = filteredFiles.join(pth.delimiter)
  - loadFromDefault()
    - @kubernetes/client-node : Kubernetes Client Libraries
  - curr = kc.getCurrentContext()
  - cxts = kc.getContexts()
  - contextClick(menuItem) { kubectl.setCurrentContext(menuItem.label) 
    - // The K8s JS library will get the current context but does not have the ability
    - // to save the context. The current version of the package targets k8s 1.18 and
    - // there are new config file features (e.g., proxy) that may be lost by outputting
    - // the config with the library. So, we drop down to kubectl for this.
    - spawn(resources.executable('kubectl'), ['config', 'use-context', cxt], opts)

- kubeconfigPath = kubeconfig.path()
- buildFromConfig(kubeconfigPath);
  - readFileSync(configPath)
  - parsedConfig = yaml.parse(contents)
  - if ((parsedConfig?.clusters || []).length === 0) console.log('Config file has no clusters, will retry later')
  - updateContexts()
  - trayMenu.setContextMenu(contextMenu)

- watcher = watch(kubeconfigPath);
- watcher.on('change', => buildFromConfig(kubeconfigPath))

- mainEvents.on('k8s-check-state', => k8sStateChanged(mgr.state))
  - kubernetesState = state
  - updateMenu()
    - [State.STOPPED]:    'Kubernetes is stopped',
    - [State.STARTING]:   'Kubernetes is starting',
    - [State.STARTED]:    'Kubernetes is running',
    - [State.STOPPING]:   'Kubernetes is shutting down',
    - [State.ERROR]:      'Kubernetes has encountered an error',
- mainEvents.on('settings-update', => settingsChanged())
  - updateMenu()

EVENT: k8s-check-state
- mgr = K8s.factory(arch);
- mgr.on('state-changed', (state: K8s.State) => { mainEvents.emit('k8s-check-state', mgr) }

EVENT: settings-update
- function writeSettings()
- settings.save(cfg)
- mainEvents.emit('settings-update', cfg)

https://github.com/rancher-sandbox/rancher-desktop/blob/f3d631a161ac684983b81329781ce5c44ddc9acf/src/main/tray.ts#L22

## 5. Start UI

window.openPreferences()

### 5.1 General.vue

mounted()
- on('settings-update', this.onSettingsUpdate)
- on('update-state', this.onUpdateState)

## 5.2 Images.vue

mounted()
- on('k8s-check-state', (event, state) => { this.$data.k8sState = state
- on('images-check-state', (event, state) => { this.imageManagerState = state
- on('settings-update', (event, settings) => { this.checkSelectedNamespace();
- $data.images = invoke('images-mounted', true)
- $data.imageManagerState = invoke('images-check-state')
- on('images-namespaces', (event, namespaces) => { this.$data.imageNamespaces = namespaces; this.checkSelectedNamespace();
- send('images-namespaces-read')

EVENT: images-check-state
- images/imageProcessor.ts: ImageProcessor.constructor(k8sManager: K8s.KubernetesBackend)

EVENT: images-mounted
- imageEvents.ts : return imageProcessor.listImages();
  - timers.setInterval(refreshImages, REFRESH_INTERVAL)
  - result = getImages()
    - moby: spawn(resources.executable('docker'), dockerArgs), ['images', '--format', '{{json .}}']
    - nerdctl: spawn(resources.executable('nerdctl'), namespacedArgs), ['images', '--format', '{{json .}}']
  - images = parse(result.stdout)
  - emit('images-changed', this.images)

EVENT: images-namespaces
- images/imageProcessor.ts: relayNamespaces()
  - namespaces = await this.getNamespaces();
    - nerdctl: spawnFile(resources.executable('nerdctl'), ['namespace', 'list', '--quiet'])
    - moby: throw new Error("docker doesn't support namespaces");
  - if (!namespaces.includes('default')) { namespaces.push('default'); }

EVENT: images-namespaces-read
  - background.js: on('images-namespaces-read', (event) => {
    - if (k8smanager.state === K8s.State.STARTED) {
    -  currentImageProcessor?.relayNamespaces();

state() 
- if (this.k8sState !== K8s.State.STARTED) {
  - return 'IMAGE_MANAGER_UNREADY';
- else
  - return this.imageManagerState ? 'READY' : 'IMAGE_MANAGER_UNREADY'

checkSelectedNamespace()
- if (!this.imageNamespaces.includes(this.settings.images.namespace)) {
- K8S_NAMESPACE = 'k8s.io';
- defaultNamespace = this.imageNamespaces.includes(K8S_NAMESPACE) ? K8S_NAMESPACE : this.imageNamespaces[0];
- invoke('settings-write', { images: { namespace: defaultNamespace } } )

onShowAllImagesChanged(value)
- invoke('settings-write', { images: { showAll: value } } )

onChangeNamespace(value) 
- invoke('settings-write', { images: { namespace: value } } 

### 5.3 Integration.vue

mounted()
- on('k8s-integrations', (event, integrations) => { this.$data.integrations = integrations;
- on('k8s-integration-warnings', (event, name, warnings) => { this.$set(this.integrationWarnings, name, warnings)

EVENT: k8s-integrations
- background.js: k8smanager?.listIntegrations()

EVENT: k8s-integration-warnings
- background.js: k8smanager.listIntegrationWarnings()

handleSetIntegration(distro: any, value: any) 
- send('k8s-integration-set', distro, value)

EVENT: 'k8s-integration-set
- background.js: k8smanager.setIntegration(name, newState)
  - unixlikeIntegrations.ts: 
    - INTEGRATIONS = ['docker', 'helm', 'kim', 'kubectl', 'nerdctl']
    - PUBLIC_LINK_DIR = paths.integration (return '/usr/local/bin';)
    - linkPath = path.join(PUBLIC_LINK_DIR, name)
    - desiredPath = resources.executable(name)
    - symlink(desiredPath, linkPath)
  - wsl.ts:
    - // We need to get the Linux path to our helper executable; it is easier to just get WSL to do the transformation for us.
    - WSLHelperPath = resources.get('linux', 'bin', 'wsl-helper')
    - execWSL('--distribution', distro, '--exec', WSLHelperPath, 'kubeconfig', `--enable=${ state }`)
    - go/wsl-helper:
      - // the kubeconfig command is used to set up a symlink on the Linux side 
      - // to point at the Windows-side kubeconfig.  
      - // Note that we must pass the kubeconfig path in as an environment variable 
      - // to take advantage of the path translation capabilities of WSL2 interop

### 5.4 K8s.vue

computed:
- hasSystemPreferences() { return !os.platform().startsWith('win');
- hasContainerEnginePreferences() { return !os.platform().startsWith('win');
- availMemoryInGB() { return Math.ceil(os.totalmem() / 2 ** 30);
- availNumCPUs() { return os.cpus().length;
- cannotReset() { return ![K8s.State.STARTED, K8s.State.ERROR].includes(this.state);

created()
- if (this.hasSystemPreferences)
  - if (this.settings.kubernetes.memoryInGB > this.availMemoryInGB) {
      - this.settings.kubernetes.memoryInGB = this.availMemoryInGB
  - if (this.settings.kubernetes.numberCPUs > this.availNumCPUs) {
      - this.settings.kubernetes.numberCPUs = this.availNumCPUs;

mounted()
- send('k8s-current-port');
- send('k8s-current-engine');
- send('k8s-restart-required');
- send('k8s-versions');

- on('k8s-check-state', (event, stt) => { that.$data.state = stt;
  ->  k8sManager.on('state-changed', (state: K8s.State) => { window.send('k8s-check-state', state);

- on('k8s-current-port', (event, port) => { this.currentPort = port;
  -> ipcMain.on('k8s-current-port', () => { window.send('k8s-current-port', k8smanager.desiredPort);
  -> k8sManager.on('current-port-changed', (port: number) => { window.send('k8s-current-port', port);

- on('k8s-current-engine', (event, engine) => { this.currentEngine = engine;
  -> setupImageProcessor() { window.send('k8s-current-engine', cfg.kubernetes.containerEngine);
  -> ipcMain.on('k8s-current-engine', () => { window.send('k8s-current-engine', currentContainerEngine);

- on('k8s-restart-required', (event, required) => {
  -> writeSettings() { ipcMain.emit('k8s-restart-required');
  -> doK8sRestartRequired() {
    - restartRequired = (await k8smanager?.requiresRestartReasons()) ?? {};
    - window.send('k8s-restart-required', restartRequired);
  - containerEngineChangePending = false;
  - for (const key in required) {
     - message = `The cluster must be reset for ${ key } change from ${ required[key][0] } to ${ required[key][1] }.`;
     - handleNotification('info', `restart-${ key }`, message);
     - if (key === 'containerEngine') { this.containerEngineChangePending = true;

- on('k8s-versions', (event, versions) => { this.$data.versions = versions;
  -> ipcMain.on('k8s-versions', => { window.send('k8s-versions', k8smanager.availableVersions);
  -> k8sManager.on('versions-updated', => { window.send('k8s-versions', mgr.availableVersions);
  - if (!versions.includes(this.settings.kubernetes.version)) {
    - oldVersion = this.settings.kubernetes.version;
    - message = `Saved Kubernetes version ${ oldVersion } not available, using ${ versions[0] }.`;
    - handleNotification('info', 'invalid-version', message);
  - this.settings.kubernetes.version = versions[0];

- on('settings-update', (event, settings) => { this.$data.settings = settings;

// Reset a Kubernetes cluster to default at the same version
// @param { 'auto' | 'wipe' } mode How to do the reset
reset(mode) {
- wipe = this.containerEngineChangePending || mode === 'wipe' || this.state !== K8s.State.STARTED;
- consequence = {
        true:  'Wiping Kubernetes will delete all workloads, configuration, and images.',
        false: 'Resetting Kubernetes will delete all workloads and configuration.',
      }[wipe];
- if (confirm(`${ consequence } Do you want to proceed?`)) {
- this.state = K8s.State.STOPPING;
- send('k8s-reset', wipe ? 'wipe' : 'fast');

EVENT 'k8s-reset': doK8sReset(arg: 'fast' | 'wipe' | 'changeEngines')
- if (![K8s.State.STARTED, K8s.State.STOPPED, K8s.State.ERROR].includes(k8smanager.state)) {
  - console.log(`Skipping reset, invalid state ${ k8smanager.state }`);
- switch (arg) {
- case 'fast':
  - k8smanager.reset(cfg.kubernetes);
- case 'changeEngines':
  - k8smanager.stop();
  - startK8sManager();
- case 'wipe':
  - k8smanager.stop();
  - k8smanager.del();
  - startK8sManager();

restart() {
- this.state = K8s.State.STOPPING;
- send('k8s-restart');

EVENT 'k8s-restart': ipcMain.on('k8s-restart', () => {
- if (cfg.kubernetes.port !== k8smanager.desiredPort) {
  - // On port change, we need to wipe the VM.
  - doK8sReset('wipe');
- if (cfg.kubernetes.containerEngine !== currentContainerEngine) {
  - doK8sReset('changeEngines');
- switch (k8smanager.state) { case K8s.State.STOPPED: case K8s.State.STARTED:
  - // Calling start() will restart the backend, possible switching versions as a side-effect.
  - startK8sManager();
  
onChange(event) {
- if (event.target.value !== this.settings.kubernetes.version) {
  - if (this.settings.kubernetes.port !== this.currentPort) {
    - confirmationMessage = `Changing versions will require a full reset of Kubernetes (loss of workloads) because the desired port has also changed (from ${ this.currentPort } to ${ this.settings.kubernetes.port })`;
  - if (semver.lt(event.target.value, this.settings.kubernetes.version)) {
    - confirmationMessage = `Changing from version ${ this.settings.kubernetes.version } to ${ event.target.value } will reset Kubernetes.`;
  - else {
    - confirmationMessage = `Changing from version ${ this.settings.kubernetes.version } to ${ event.target.value } will upgrade Kubernetes`;
  - confirmationMessage += ' Do you want to proceed?';
  - if (confirm(confirmationMessage)) {
    - invoke('settings-write', { kubernetes: { version: event.target.value } })

onChangeEngine(desiredEngine) {
- if (desiredEngine !== this.settings.kubernetes.containerEngine) {
- confirmationMessage = [`Changing container engines from ${ this.containerEngineNames[this.currentEngine] } to ${ this.containerEngineNames[desiredEngine] } will require a restart of Kubernetes)`, ' Do you want to proceed?'].join('');
- if (confirm(confirmationMessage)) {
- invoke('settings-write', { kubernetes: { containerEngine: desiredEngine } });
- restart();

handleUpdateMemory(value) 
- settings.kubernetes.memoryInGB = value;
- invoke('settings-write',{ kubernetes: { memoryInGB: value } });

handleUpdateCPU(value) {
- settings.kubernetes.numberCPUs = value;
- invoke('settings-write', { kubernetes: { numberCPUs: value } });
  
handleUpdatePort(value) {
- settings.kubernetes.port = value;
- invoke('settings-write', { kubernetes: { port: value } });

### 5.5 PortForwarding.vue

mounted() 

- on('k8s-check-state', (event, state) => { this.$data.state = state;
- on('service-changed', (event, services) => { this.$data.services = services;
- invoke('service-fetch').then((services) => { this.$data.services = services;
- on('settings-update', (event, settings) => { this.$data.settings = settings;

EVENT 'service-changed':
- k8sengine/client.ts: class KubeClient
  -  this.services = new k8s.ListWatch(
      '/api/v1/services',
      new WrappedWatch(this.kubeconfig, () => {
        this.emit('service-changed', this.listServices());
      }),
      () => this.coreV1API.listServiceForAllNamespaces());
- k8sManager.on('service-changed', (services: K8s.ServiceEntry[]) => window.send('service-changed', services);

EVENT 'service-fetch':
- ipcMain.handle('service-fetch', (event, namespace) => { return k8smanager.listServices(namespace);

onIncludeK8sServicesChanged(value) {
- if (value !== this.settings.portForwarding.includeKubernetesServices) {
  - invoke('settings-write', { portForwarding: { includeKubernetesServices: value } } );

### 5.6 Troubleshooting.vue

factoryReset() {
- message = `Doing a factory reset will remove your cluster and all rancher-desktop settings; 
       you will need to re-do the initial set up again.  Are you sure you want to factory reset?`.replace(/\s+/g, ' ');
- if (confirm(message)) { ipcRenderer.send('factory-reset');

// Do a factory reset of the application.  
// This will stop the currently running cluster (if any), and delete all of its data.  
// This will also remove any rancher-desktop data, and restart the application.
EVENT 'factory-reset':
- // Clean up the Kubernetes cluster
- k8smanager.factoryReset();
- if (os.platform() === 'darwin') { // Unlink binaries
  - for (const name of ['docker', 'helm', 'kim', 'kubectl', 'nerdctl']) {
  - ipcMain.emit('install-set', { reply: () => { } }, name, false);
-  // Remove app settings
- settings.clear();
- // Restart
- Electron.app.relaunch();

showLogs() {
- ipcRenderer.send('troubleshooting/show-logs');

EVENT 'troubleshooting/show-logs':
- Electron.shell.openPath(paths.logs);

## 6. Start KubernetesBackend

k8smanager = newK8sManager();
- arch = (Electron.app.runningUnderRosettaTranslation || os.arch() === 'arm64') ? 'aarch64' : 'x86_64';
- mgr = K8s.factory(arch
  - switch (os.platform()) {
  - case 'linux':
    - return new LimaBackend(arch);
  - case 'darwin':
    - return new LimaBackend(arch);
  - case 'win32':
    - return new WSLBackend();
  - default:
    - return new OSNotImplemented();
- mgr.on('state-changed', => { window.send('k8s-check-state', state);
  - if (state === K8s.State.STARTED) {
    - if (!cfg.kubernetes.version) {
      - writeSettings({ kubernetes: { version: mgr.version } });
    - currentImageProcessor?.relayNamespaces();
- mgr.on('current-port-changed', (port: number) => { window.send('k8s-current-port', port);
- mgr.on('service-changed', (services: K8s.ServiceEntry[]) => { window.send('service-changed', services);
- mgr.on('progress', () => { window.send('k8s-progress', mgr.progress);
- mgr.on('versions-updated', async() => { window.send('k8s-versions', await mgr.availableVersions);

currentContainerEngine = settings.ContainerEngine.NONE;
currentImageProcessor = null;
imageEventHandler = null;

startBackend(cfg)
- checkBackendValid(); [see below for WSL, return null for Lima]
  -  invalidReason = k8smanager.getBackendInvalidReason();
    - try { this.isDistroRegistered();
      - this.execWSL(['--list', '--quiet']);
    - catch (ex):
      - message = `
          Windows Subsystem for Linux does not appear to be installed.
          Please install it manually:
          https://docs.microsoft.com/en-us/windows/wsl/install-win10
        `.replace(/[ \t]{2,}/g, '');
      - return new K8s.KubernetesError('WSL Not Installed', message);
  - if (invalidReason) { Electron.app.quit();
- startK8sManager();
  - changedContainerEngine = currentContainerEngine !== cfg.kubernetes.containerEngine
  - currentContainerEngine = cfg.kubernetes.containerEngine;
  -  if (changedContainerEngine) { setupImageProcessor();
  - k8smanager.start(cfg.kubernetes)

setupImageProcessor()
- imageProcessor = getImageProcessor(cfg.kubernetes.containerEngine, k8smanager);
  - switch (engineName) {
  - case ContainerEngine.MOBY:
    - cachedImageProcessors[engineName] = new MobyImageProcessor(k8sManager);
  - case ContainerEngine.CONTAINERD:
    - cachedImageProcessors[engineName] = new NerdctlImageProcessor(k8sManager);
- return <ImageProcessor>cachedImageProcessors[engineName];

- currentImageProcessor?.deactivate();
- if (!imageEventHandler) { imageEventHandler = new ImageEventHandler(imageProcessor);
- imageEventHandler.imageProcessor = imageProcessor;
- currentImageProcessor = imageProcessor;
- currentImageProcessor.activate();
- currentImageProcessor.namespace = cfg.images.namespace;
- window.send('k8s-current-engine', cfg.kubernetes.containerEngine);

ipcMain.handle('service-forward', async(event, service, state) => {
- forwarder = k8smanager?.portForwarder;
- if (forwarder) {
  - if (state) { forwarder.forwardPort(service.namespace, service.name, service.port);
  - else { forwarder.cancelForward(service.namespace, service.name, service.port);

Electron.app.on('before-quit', async(event) => {
- k8smanager?.stop();

## WSLBackend

https://adamtheautomator.com/windows-subsystem-for-linux/

### WSL Backend - Constructor

INSTANCE_NAME = 'rancher-desktop';
DATA_INSTANCE_NAME = 'rancher-desktop-data';

// A list of distributions in which we should never attempt to integrate with.
DISTRO_BLACKLIST = [
  'rancher-desktop', // That's ourselves
  'rancher-desktop-data', // Another internal distro
  'docker-desktop', // Not meant for interactive use
  'docker-desktop-data', // Not meant for interactive use
];

// The version of the WSL distro we expect.
DISTRO_VERSION = '0.8';

// The list of directories that are in the data distribution (persisted across version upgrades).
DISTRO_DATA_DIRS = [
  '/etc/rancher',
  '/var/lib',
];

get distroFile() { return resources.get(os.platform(), `distro-${ DISTRO_VERSION }.tar`); }

get downloadURL() { return 'https://github.com/k3s-io/k3s/releases/download'; }

// k3sHelper : k3sHelper.ts -> class K3sHelper
// resources : resources.ts -> get resources embedded in the application itself

- cfg: Settings['kubernetes'] | undefined;

- // Reference to the _init_ process in WSL.  All other processes should be children of this one.  
- // Note that this is busybox init, running in a custom mount & pid namespace.
- process: childProcess.ChildProcess | null = null;
- client: K8s.Client | null = null;

- // Interval handle to update the progress. 
- // The return type is odd because TypeScript is pulling in some of the DOM definitions here, which has an incompatible setInterval/clearInterval.
- progressInterval: ReturnType<typeof timers.setInterval> | undefined;

- // The version of Kubernetes currently running.
- activeVersion: ShortVersion = '';
- // The port the Kubernetes server is listening on (default 6443)
- currentPort = 0;
- // The port Kubernetes should listen on; this may not match reality if Kubernetes isn't up.
- #desiredPort = 6443;
- // Helper object to manage available K3s versions.
- k3sHelper = new K3sHelper();

get version(): ShortVersion { return this.activeVersion; }

get port(): number { return this.currentPort; }

get availableVersions(): ShortVersion[] { return this.k3sHelper.availableVersions; }

get desiredVersion(): ShortVersion {
- availableVersions = await this.k3sHelper.availableVersions;
- version = this.cfg?.version || availableVersions[0];
- if (!version) { throw new Error('No version available'); }
- if (!availableVersions.includes(version))
  - console.error(`Could not use saved version ${ version }, not in ${ availableVersions }`);
  - version = availableVersions[0];
- return version;

- // The current operation underway; used to avoid responding to state changes when we're in the process of doing a different one.
- currentAction: Action = Action.NONE;
- // The current user-visible state of the backend.
- internalState: K8s.State = K8s.State.STOPPED;

get state() { return this.internalState; }

setState(state: K8s.State) {
- this.internalState = state;
- this.emit('state-changed', this.state);
- switch (this.state) {
  - case K8s.State.STOPPING:
  - case K8s.State.STOPPED:
  - case K8s.State.ERROR:
    - this.client?.destroy();
}

- progressTracker: ProgressTracker;
- progress: K8s.KubernetesProgress = { current: 0, max: 0 };

get backend(): 'wsl' { return 'wsl'; }

get cpus(): number {
- // This doesn't make sense for WSL2, since that's a global configuration.
- return 0;

get memory(): number {
- // This doesn't make sense for WSL2, since that's a global configuration.
- return Promise.resolve(0);

get desiredPort() {
- return this.#desiredPort;

constructor()
- this.k3sHelper.on('versions-updated', () => this.emit('versions-updated'));
- this.k3sHelper.initialize()
- this.progressTracker = new ProgressTracker((progress) => { this.progress = progress; this.emit('progress');

### WSL Backend - START

SUMMARY OF START SEQUENCE
- new K3sHelper()
- k3sHelper.initialize()
- // Starting Kubernetes
- // Downloading Kubernetes components
- EXECUTE 2 IN PARALLEL
  -  EXECUTE 3 SEQUENTIALLY
    - upgradeDistroAsNeeded()
    - ensureDistroRegistered()
    - initDataDistribution()
  - // Checking k3s images
  - k3sHelper.ensureK3sImages(desiredVersion))
- // Stopping existing instance'
- this.process?.kill('SIGTERM'); 
- await this.killStaleProcesses();
- // Mounting WSL data
- this.mountData()
- EXECUTE 5 IN PARALLEL
  - EXECUTE 5 SEQUENTIALLY
    - // Starting WSL environment'
    - this.writeFile('/etc/init.d/k3s', SERVICE_SCRIPT_K3S, 0o755);
    - logPath = await this.wslify(paths.logs);
    - rotateConf = LOGROTATE_K3S_SCRIPT.replace(/\r/g, '').replace('/var/log', logPath);
    - this.writeFile('/etc/logrotate.d/k3s', rotateConf, 0o644);
    - this.runInit();
  - // Installing image scanner
  - this.installTrivy()
  - // Installing CA certificates
  - this.installCACerts()
  - // Installing helpers
  - this.installWSLHelpers()
  - EXECUTE 3 SEQUENTIALLY
    - // Installing k3s
    - this.deleteIncompatibleData(desiredVersion);
    - this.installK3s(desiredVersion);
    - this.persistVersion(desiredVersion);
- await this.startService('k3s', {  PORT: this.#desiredPort.toString(), LOG_DIR: await this.wslify(paths.logs), IPTABLES_MODE: 'legacy' });
- // Waiting for Kubernetes API'
- this.k3sHelper.waitForServerReady(() => this.ipAddress, this.#desiredPort));
- // Updating kubeconfig
- this.k3sHelper.updateKubeconfig(async() => await this.captureCommand(await this.getWSLHelperPath(), 'k3s', 'kubeconfig')));
- // Waiting for services
- this.client = new K8s.Client();
- await this.client.waitForServiceWatcher();
- // Trigger kuberlr to ensure there's a compatible version of kubectl in place
- await childProcess.spawnFile(resources.executable('kubectl'), ['config', 'current-context'], { stdio: Logging.k8s });
- // Waiting for nodes
- await this.client?.waitForReadyNodes()

start(cfg.kubernetes)
- this.#desiredPort = config.port;
- this.cfg = config;

- this.currentAction = Action.STARTING;
- progressTracker.action('Starting Kubernetes',
  - this.setState(K8s.State.STARTING);
  - if (this.progressInterval) { timers.clearInterval(this.progressInterval); }
 
- this.progressInterval = timers.setInterval(() => {
          const statuses = [
            this.k3sHelper.progress.checksum,
            this.k3sHelper.progress.exe,
            this.k3sHelper.progress.images,
          ];
  - progressTracker.numeric('Downloading Kubernetes components',sum('current'), sum('max'),);

- const desiredVersion = await this.desiredVersion;
- await Promise.all([
            await this.upgradeDistroAsNeeded();
            await this.ensureDistroRegistered();
            await this.initDataDistribution();
          })(),
           this.progressTracker.action('Checking k3s images',100,
            this.k3sHelper.ensureK3sImages(desiredVersion)),
        ]);
- if (this.currentAction !== Action.STARTING)  
  - // User aborted before we finished
  - return;
- timers.clearInterval(this.progressInterval);
- this.progressInterval = undefined;

- // If we were previously running, stop it now.
- await this.progressTracker.action('Stopping existing instance', 100, async() => { this.process?.kill('SIGTERM'); await this.killStaleProcesses(); });

- progressTracker.action('Mounting WSL data', 100, this.mountData());

- await Promise.all([
- progressTracker.action('Starting WSL environment', 100, async() => {
  - this.writeFile('/etc/init.d/k3s', SERVICE_SCRIPT_K3S, 0o755);
  - logPath = await this.wslify(paths.logs);
  - rotateConf = LOGROTATE_K3S_SCRIPT.replace(/\r/g, '').replace('/var/log', logPath);
  - this.writeFile('/etc/logrotate.d/k3s', rotateConf, 0o644);
  - this.runInit();
 }),
- progressTracker.action('Installing image scanner', 100, this.installTrivy()),
- progressTracker.action('Installing CA certificates', 100, this.installCACerts()),
- progressTracker.action('Installing helpers', 50, this.installWSLHelpers()),
- progressTracker.action('Installing k3s', 100, async() => {
  - await this.deleteIncompatibleData(desiredVersion);
  - await this.installK3s(desiredVersion);
  - await this.persistVersion(desiredVersion);
  }),
]);

- await this.startService('k3s', {
    PORT:          this.#desiredPort.toString(),
    LOG_DIR:       await this.wslify(paths.logs),
    IPTABLES_MODE: 'legacy',
});

- if (this.currentAction !== Action.STARTING) {
  - // User aborted
  - return;

- progressTracker.action('Waiting for Kubernetes API', 100,
  - this.k3sHelper.waitForServerReady(() => this.ipAddress, this.#desiredPort));

- progressTracker.action('Updating kubeconfig', 100,
  - this.k3sHelper.updateKubeconfig(async() => await this.captureCommand(await this.getWSLHelperPath(), 'k3s', 'kubeconfig')));

- progressTracker.action('Waiting for services', 50,
  - this.client = new K8s.Client();
  - await this.client.waitForServiceWatcher();
  - this.client.on('service-changed', (services) => { this.emit('service-changed', services); });
  - this.activeVersion = desiredVersion;
  - this.currentPort = this.#desiredPort;
  - this.emit('current-port-changed', this.currentPort);
  - // Trigger kuberlr to ensure there's a compatible version of kubectl in place
  - await childProcess.spawnFile(resources.executable('kubectl'), ['config', 'current-context'], { stdio: Logging.k8s });

- progressTracker.action('Waiting for nodes', 100,
  - if (!await this.client?.waitForReadyNodes()) { throw new Error('No client'); } });
    - this.setState(K8s.State.STARTED);
  - catch (ex) {
    - this.setState(K8s.State.ERROR);
  - finally {
    - if (this.progressInterval) {  timers.clearInterval(this.progressInterval); this.progressInterval = undefined; }
    - this.currentAction = Action.NONE;

### WSL Backend START functions

package.json
- "scripts": {
  - "dev": "node ./scripts/dev.mjs",
  - "generate:nerdctl-stub": "powershell scripts/windows/generate-nerdctl-stub.ps1",
  - "build": "node ./scripts/build.mjs",
  - "sign": "node scripts/ts-wrapper.js scripts/sign.ts",
  - "generateReleases": "node ./scripts/generateReleaseList.js",
  - "postinstall": "node ./scripts/postinstall.mjs",
  - "postuninstall": "electron-builder install-app-deps"

- scripts/download/wsl.mjs
  - v = '0.8'
  - https://github.com/rancher-sandbox/rancher-desktop-wsl-distro/releases/download/v${ v }/distro-${ v }.tar
- scripts/download/tools.mjs
  - kuberlrVersion = '0.4.1'
  - https://github.com/flavio/kuberlr/releases/download/v${ kuberlrVersion }
  - await bindKubectlToKuberlr(kuberlrPath, path.join(binDir, exeName('kubectl')))
  - // Download Kubectl into kuberlr's directory of versioned kubectl's
  - kubeVersion = (await getResource('https://dl.k8s.io/release/stable.txt')).trim();
  - kubectlURL = `https://dl.k8s.io/${ kubeVersion }/bin/${ kubePlatform }/${ cpu }/${ exeName('kubectl') }`;
  - // Download go-swagger (build tool, for host only)
  - goSwaggerVersion = 'v0.28.0';
  - goSwaggerURLBase = `https://github.com/go-swagger/go-swagger/releases/download/${ goSwaggerVersion }`;
  - goSwaggerExecutable = exeName(`swagger_${ kubePlatform }_amd64`);
  - goSwaggerURL = `${ goSwaggerURLBase }/${ goSwaggerExecutable }`;
  - // Download Helm. It is a tar.gz file that needs to be expanded and file moved.
  - helmVersion = '3.6.3';
  - helmURL = `https://get.helm.sh/helm-v${ helmVersion }-${ kubePlatform }-${ cpu }.tar.gz`;
  - // Download Docker
  - dockerVersion = 'v20.10.9';
  - dockerURLBase = `https://github.com/rancher-sandbox/rancher-desktop-docker-cli/releases/download/${ dockerVersion }`;
  - dockerExecutable = exeName(`docker-${ kubePlatform }-${ cpu }`);
  - dockerURL = `${ dockerURLBase }/${ dockerExecutable }`;
  - // Download Kim
  - kimVersion = '0.1.0-beta.7';
  - kimURLBase = `https://github.com/rancher/kim/releases/download/v${ kimVersion }`;
  - kimExecutable = exeName(`kim-${ kubePlatform }-amd64`);
  - kimURL = `${ kimURLBase }/${ kimExecutable }`;
  - // Download Trivy
  - // Always run this in the VM, so download the *LINUX* version into binDir
  - // and move it over to the wsl/lima partition at runtime.
  - // This will be needed when RD is ported to linux as well, because there might not be
  - // an image client running on the host.
  - // Sample URLs:
  - // https://github.com/aquasecurity/trivy/releases/download/v0.18.3/trivy_0.18.3_checksums.txt
  - // https://github.com/aquasecurity/trivy/releases/download/v0.18.3/trivy_0.18.3_macOS-64bit.tar.gz
  - trivyURLBase = 'https://github.com/aquasecurity/trivy/releases';
  - trivyBasename = `trivy_${ trivyVersion }_${ trivyOS }`;
  - trivyURL = `${ trivyURLBase }/download/${ trivyVersionWithV }/${ trivyBasename }.tar.gz`;

PATHS
- get config() { return path.join(this.appData, APP_NAME);get logs() { return path.join(this.localAppData, APP_NAME, 'logs');
- get cache() { return path.join(this.localAppData, APP_NAME, 'cache');
- get wslDistro() { return path.join(this.localAppData, APP_NAME, 'distro');
- get wslDistroData() { return path.join(this.localAppData, APP_NAME, 'distro-data');


registeredDistros({ runningOnly = false }): string[] {
- args = ['--list', '--quiet'];
- if (runningOnly) {
  - args.push('--running');
- stdout = await this.execWSL({ capture: true }, ...args);
- return stdout.split(/[\r\n]+/).map(x => x.trim()).filter(x => x);


isDistroRegistered({ distribution = INSTANCE_NAME, runningOnly = false }): boolean {
- distros = await this.registeredDistros({ runningOnly });
- distros.includes(distribution || INSTANCE_NAME);


protected async getDistroVersion(): Promise<string> {
- const script = '[ -e /etc/os-release ] && . /etc/os-release ; echo ${VERSION_ID:-0.1}';
- return (await this.captureCommand('/bin/sh', '-c', script)).trim();


upgradeDistroAsNeeded()
- if (!await this.isDistroRegistered()) {
  - // If the distribution is not registered, there is nothing to upgrade.
  - return;
- existingVersion = await this.getDistroVersion();
- desiredVersion = DISTRO_VERSION;
- if (semver.lt(existingVersion, desiredVersion, true)) {
  - // Make sure we copy the data over before we delete the old distro
  - await this.progressTracker.action('Upgrading WSL distribution', 100, async() => {
    - await this.initDataDistribution();
    - await this.execWSL('--unregister', INSTANCE_NAME);


ensureDistroRegistered()
- if (await this.isDistroRegistered()) {
  - // k3s is already registered.
  - return;
- await this.progressTracker.action('Registering WSL distribution', 100, async() => {
  - await mkdir(paths.wslDistro, { recursive: true });
  - await this.execWSL('--import', INSTANCE_NAME, paths.wslDistro, this.distroFile, '--version', '2');
- if (!await this.isDistroRegistered()) {
  - throw new Error(`Error registering WSL2 distribution`);


// execWSL runs wsl.exe with the given arguments, redirecting all output to the log files.
execWSL(options: execOptions & { capture: true }, ...args: string[]): string;
- options: execOptions & { capture?: boolean } = {};
- stream = await Logging.wsl.fdStream;
- // We need two separate calls so TypeScript can resolve the return values.
- if (options.capture) {
  - { stdout } = await childProcess.spawnFile('wsl.exe', args, {
    - ...options,
    - encoding:    options.encoding ?? 'utf16le',
    - stdio:       ['ignore', 'pipe', stream],
    - windowsHide: true,
  - return stdout;
- } catch (ex) {
  - if (!options.expectFailure) {
    - console.log(`WSL failed to execute wsl.exe ${ args.join(' ') }: ${ ex }`);
  - throw ex;


// execCommand runs the given command in the K3s WSL environment.
execCommand(options: execOptions & { capture: true }, ...command: string[]): string;
- // Print a slightly different message if execution fails.
- return await this.execWSL({ encoding: 'utf-8', ...options, expectFailure: true}, 
  - '--distribution', INSTANCE_NAME, '--exec', ...command);


// captureCommand runs the given command in the K3s WSL environment and returns the standard output.
captureCommand(options: execOptions, ...command: string[]): string;
- return await this.execCommand({ capture: true }, optionsOrArg, ...command);


// Convert a Windows path to a path in the WSL subsystem:
// - Changes \s to /s
// - Figures out what the /mnt/DRIVE-LETTER path should be
wslify(windowsPath: string): string {
- return (await this.captureCommand('wslpath', '-a', '-u', windowsPath)).trimEnd();


// Copy a file from Windows to the WSL distribution.
 wslInstall(windowsPath: string, targetDirectory: string) {
- wslSourcePath = await this.wslify(windowsPath);
- basename = path.basename(windowsPath);
- // Don't use `path.join` or the backslashes will come back.
- targetFile = `${ targetDirectory }/${ basename }`;
- stdout = await this.captureCommand('cp', wslSourcePath, targetFile);


// Run the given installation script.
runInstallScript(scriptContents: string, scriptName: string, ...args: string[]) {
- workdir = await mkdtemp(path.join(os.tmpdir(), `rd-${ scriptName }-`));
- scriptPath = path.join(workdir, scriptName);
- wslScriptPath = await this.wslify(scriptPath);
- writeFile(scriptPath, scriptContents.replace(/\r/g, ''), 'utf-8');
- await this.execCommand('chmod', 'a+x', wslScriptPath);
- await this.execCommand(wslScriptPath, ...args);
- rm(workdir, { recursive: true });


// If the WSL distribution we use to hold the data doesn't exist, create it
// and copy the skeleton over from the active one.
initDataDistribution()
- workdir = await mkdtemp(path.join(os.tmpdir(), 'rd-distro-'));
- if (!await this.isDistroRegistered({ distribution: DATA_INSTANCE_NAME })) {
  - await this.progressTracker.action('Initializing WSL data', 100, async() => {
  - // Create a distro archive from the main distro.
  - // WSL seems to require a working /bin/sh for initialization.
  - const REQUIRED_FILES = [
              '/bin/busybox', // Base tools
              '/bin/mount', // Required for WSL startup
              '/bin/sh', // WSL requires a working shell to initialize
              '/lib', // Dependencies for busybox
              '/etc/wsl.conf', // WSL configuration for minimal startup
              '/etc/passwd', // So WSL can spawn programs as a user
            ];
  - archivePath = path.join(workdir, 'distro.tar');
  - console.log('Creating initial data distribution...');
  - // Make sure all the extra data directories exist
  - await Promise.all(DISTRO_DATA_DIRS.map((dir) => {
    - return this.execCommand('/bin/busybox', 'mkdir', '-p', dir);
  - // Figure out what required files actually exist in the distro; they
  - // may not exist on various versions.
  - extraFiles = (await Promise.all(REQUIRED_FILES.map(async(path) => {
    - await this.execCommand({ expectFailure: true }, 'busybox', '[', '-e', path, ']');
    - return path;
    - } catch (ex) {
      - // Exception expected - the path doesn't exist
      - return undefined;
  - }))).filter(defined);
  - await this.execCommand('tar', '-cf', await this.wslify(archivePath),
    - '-C', '/', ...extraFiles, ...DISTRO_DATA_DIRS);
  - await this.execWSL('--import', DATA_INSTANCE_NAME, paths.wslDistroData, archivePath, '--version', '2');
- } else {
  - console.log('data distro already registered');  
- await this.progressTracker.action('Updating WSL data', 100, async() => {
  - // We may have extra directories (due to upgrades); copy any new ones over.
  - const missingDirs: string[] = [];
  - await Promise.all(DISTRO_DATA_DIRS.map(async(dir) => {
    - await this.execWSL({ expectFailure: true, encoding: 'utf-8' },
      - '--distribution', DATA_INSTANCE_NAME, '--exec', '/bin/busybox', '[', '!', '-d', dir, ']');
    - missingDirs.push(dir);
  - if (missingDirs.length > 0) {
    - // Copy the new directories into the data distribution.
    - // Note that we're not using compression, since we (kind of) don't have gzip...
    - const archivePath = await this.wslify(path.join(workdir, 'data.tar'));
    - await this.execCommand('tar', '-cf', archivePath, '-C', '/', ...missingDirs);
    - await this.execWSL('--distribution', DATA_INSTANCE_NAME, '--exec', '/bin/busybox', 'tar', '-xf', archivePath, '-C', '/');
- } finally {
- rmdir(workdir, { recursive: true });


killStaleProcesses()
- // Attempting to terminate a distribution is a no-op.
- await this.execWSL('--terminate', INSTANCE_NAME);


// Mount the data distribution over.
mountData()
- mountRoot = '/mnt/wsl/rancher-desktop/run/data';
- await this.execCommand('mkdir', '-p', mountRoot);
- // Only bind mount the root if it doesn't exist; because this is in the
- // shared mount (/mnt/wsl/), it can persist even if all of our distribution
- // instances terminate, as long as the WSL VM is still running.  Once that
- // happens, it is no longer possible to unmount the bind mount...
- // However, there's an exception: the underlying device could have gone
- // missing (!); if that happens, we _can_ unmount it.
- mountInfo = await this.execWSL( { capture: true, encoding: 'utf-8' },
  - '--distribution', DATA_INSTANCE_NAME, '--exec', 'busybox', 'cat', '/proc/self/mountinfo');
  - // https://www.kernel.org/doc/html/latest/filesystems/proc.html#proc-pid-mountinfo-information-about-mounts
  - // We want fields 5 "mount point" and 10 "mount source".
- matchRegex = new RegExp(String.raw`...`.trim().replace(/\s+/g, String.raw`\s+`));
- mountFields = mountInfo.split(/\r?\n/).map(line => matchRegex.exec(line)).filter(defined);
- hasValidMount = false;
- for (mountLine of mountFields) {
  - { mountPoint, mountSource: device } = mountLine.groups ?? {};
  - if (mountPoint !== mountRoot || !device) { continue; }
  - // Some times we can have the mount but the disk is missing.
  - // In that case we need to umount it, and the re-mount.
  - await this.execWSL( { expectFailure: true },
    - '--distribution', DATA_INSTANCE_NAME, '--exec', 'busybox', 'test', '-e', device);
    - console.log(`Found a valid mount with ${ device }: ${ mountLine.input }`);
    - hasValidMount = true;
  - } catch (ex) {
    - // Busybox returned error, the devices doesn't exist.  Unmount.
    - console.log(`Unmounting missing device ${ device }: ${ mountLine.input }`);
    - await this.execWSL('--distribution', DATA_INSTANCE_NAME, '--exec', 'busybox', 'umount', mountRoot);
- if (!hasValidMount) {
  - console.log(`Did not find a valid mount, mounting ${ mountRoot }`);
  - await this.execWSL('--distribution', DATA_INSTANCE_NAME, 'mount', '--bind', '/', mountRoot);
 - await Promise.all(DISTRO_DATA_DIRS.map(async(dir) => {
      await this.execCommand('mkdir', '-p', dir);
      await this.execCommand('mount', '-o', 'bind', `${ mountRoot }/${ dir.replace(/^\/+/, '') }`, dir);


// src/assets/scripts/service-k3s
// Alpine => OpenRC
// Ubuntu => https://www.rancher.co.jp/docs/k3s/latest/en/running/#systemd
// WSL => https://dev.to/bowmanjd/you-probably-don-t-need-systemd-on-wsl-windows-subsystem-for-linux-49gn
SERVICE_SCRIPT_K3S
- #!/sbin/openrc-run
- # This script is used to manage k3s via OpenRC.
- depend() {
  - after network-online
  - want cgroups
- start_pre() {
  - rm -f /tmp/k3s.*
- supervisor=supervise-daemon
- name=k3s
- command=/usr/local/bin/k3s
- command_args="server ${ENGINE} --https-listen-port ${PORT} ${K3S_EXEC:-}"
- K3S_LOGFILE="${K3S_LOGFILE:-${LOG_DIR:-/var/log}/${RC_SVCNAME}.log}"
- output_log="${K3S_OUTFILE:-${K3S_LOGFILE}}"
- error_log="${K3S_ERRFILE:-${K3S_LOGFILE}}"
- pidfile="/var/run/k3s.pid"
- respawn_delay=5
- respawn_max=0
- set -o allexport
- if [ -f /etc/environment ]; then source /etc/environment; fi


LOGROTATE_K3S_SCRIPT
- /var/log/k3s {
-  missingok
-  notifempty
-  copytruncate
- }


// Runs /sbin/init in the Rancher Desktop WSL2 distribution.
// This manages {this.process}.
runInit()
- // The process should already be gone by this point, but make sure.
- this.process?.kill('SIGTERM');
- this.process = childProcess.spawn('wsl.exe',
  - ['--distribution', INSTANCE_NAME, '--exec', '/usr/local/bin/wsl-init'],
    - { env: {
      - ..process.env,
      - WSLENV:           `${ process.env.WSLENV }:DISTRO_DATA_DIRS`,
      - DISTRO_DATA_DIRS: DISTRO_DATA_DIRS.join(':'),
    - },
    - stdio:       ['ignore', await Logging.wsl.fdStream, await Logging.wsl.fdStream],
    - windowsHide: true,
  - });
- this.process.on('exit', async(status, signal) => {
  - if ([0, null].includes(status) && ['SIGTERM', null].includes(signal)) {
    - console.log('/sbin/init exited gracefully.');
    - await this.stop();
  - } else {
    - console.log(`/sbin/init exited with status ${ status } signal ${ signal }`);
    - await this.stop();
    - this.setState(K8s.State.ERROR);


// On Windows Trivy is run via WSL as there's no native port.
// Ensure that all relevant files are in the wsl mount, not the windows one.
installTrivy()
- // download-resources.sh installed trivy into the resources area
- // This function moves it into /usr/local/bin/ so when trivy is
- // invoked to run through wsl, it runs faster.
- trivyExecPath = resources.get('linux', 'bin', 'trivy');
- await this.execCommand('mkdir', '-p', '/var/local/bin');
- await this.wslInstall(trivyExecPath, '/usr/local/bin');

// https://github.com/bayaro/windows-certs-2-wsl
// https://docs.microsoft.com/fr-fr/dotnet/api/system.security.cryptography.x509certificates.x509store?view=net-6.0
installCACerts()
- certs: (string | Buffer)[] = await mainEvents.emit('cert-get-ca-certificates');
  - src/main/networking/index.ts
  - certs = https.globalAgent.options.ca;
  - if (os.platform() === 'win32') {
    - // On Windows, win-ca doesn't add CAs into the agent; rather, it patches
    - // `tls.createSecureContext()` instead, so we don't have a list of CAs here.
    - // We need to fetch it manually.
    - rawCerts = Array.from(WinCA({ generator: true, format: WinCA.der2.pem }));
    - certs.push(...rawCerts.map(cert => cert.replace(/\r/g, '')));
  - } else if (os.platform() === 'linux') {
    - // On Linux, linux-ca doesn't add CAs into the agent; so we add them manually.
    - // Not sure if this is a bug or a feature, but linux-cA returns a nested
    - // array with certs
    - certs.push(...(await LinuxCA.getAllCerts(true)).flat());
- workdir = await mkdtemp(path.join(os.tmpdir(), 'rd-ca-'));
- this.execCommand('/bin/sh', '-c', 'rm -f /usr/local/share/ca-certificates/rd-*.crt');
- // Unlike the Lima backends, we can freely copy files in parallel into the
- // WSL distro, so we don't require the use of tar here.
- await Promise.all(certs.map(async(cert, index) => {
  - filename = `rd-${ index }.crt`;
  - stream.Readable.from(cert),
  - createWriteStream(path.join(workdir, filename), { mode: 0o600 }),
  - await this.execCommand('cp',
    - await this.wslify(path.join(workdir, filename)),
    - '/usr/local/share/ca-certificates/');
      }));
- await rmdir(workdir, { recursive: true });
- await this.execCommand('/usr/sbin/update-ca-certificates');


// Install helper tools for WSL (nerdctl integration).
installWSLHelpers() {
- await this.runInstallScript(INSTALL_WSL_HELPERS_SCRIPT,
  -'install-wsl-helpers', await this.wslify(resources.get('linux', 'bin', 'nerdctl-stub')));

// /assets/scripts/install-wsl-helper
INSTALL_WSL_HELPERS_SCRIPT
- # This script installs WSL helpers into the shared WSL mount at `/mnt/wsl`.
- # Usage: $0 <path to nerdctl-stub>
- # The nerdctl shim must be setuid root to be able to create bind mounts within
- # /mnt/wsl so that nerdctl can see it.
- mkdir -p "/mnt/wsl/rancher-desktop/bin/"
- cp "${1}" "/mnt/wsl/rancher-desktop/bin/nerdctl"
- chmod u+s "/mnt/wsl/rancher-desktop/bin/nerdctl"


deleteIncompatibleData(desiredVersion: string) {
- existingVersion = await this.getPersistedVersion();
- if (!existingVersion) { return; }
- if (semver.gt(existingVersion, desiredVersion)) {
  - console.log(`Deleting incompatible Kubernetes state due to downgrade from ${ existingVersion } to ${ desiredVersion }...`);
  -await this.progressTracker.action( 'Deleting incompatible Kubernetes state', 100,
   - this.k3sHelper.deleteKubeState((...args) => this.execCommand(...args)));


Install K3s into the VM for execution.
installK3s(version: ShortVersion) {
- fullVersion = this.k3sHelper.fullVersion(version);
- await this.runInstallScript(INSTALL_K3S_SCRIPT,
  - 'install-k3s', fullVersion, await this.wslify(path.join(paths.cache, 'k3s')));

// /assets/scripts/install-k3s
INSTALL_K3S_SCRIPT
- VERSION="${1}"
- CACHE_DIR="${CACHE_DIR:-${2}}"
- # Update symlinks for k3s and images to new version
- K3S_DIR="${CACHE_DIR}/${VERSION}"
- # Make sure any outdated kubeconfig file is gone
- mkdir -p /etc/rancher/k3s
- rm -f /etc/rancher/k3s/k3s.yaml
- # Add images
- K3S=k3s
- ARCH=amd64
- if [ "$(uname -m)" = "aarch64" ]; then
  - K3S=k3s-arm64
  - ARCH=arm64
- IMAGES="/var/lib/rancher/k3s/agent/images"
- mkdir -p "${IMAGES}"
- ln -s -f "${K3S_DIR}/k3s-airgap-images-${ARCH}.tar" "${IMAGES}"
- # Add k3s binary
- ln -s -f "${K3S_DIR}/${K3S}" /usr/local/bin/k3s
- # The file system may be readonly (on macOS)
- chmod a+x "${K3S_DIR}/${K3S}" || true


// Persist the given version into the WSL disk, so we can look it up later.
persistVersion(version: ShortVersion) {
- filepath = '/var/lib/rancher/k3s/version';
- await this.execCommand('/bin/sh', '-c', `echo '${ version }' > ${ filepath }`);


startService('k3s', {  PORT: this.#desiredPort.toString(), LOG_DIR: await this.wslify(paths.logs), IPTABLES_MODE: 'legacy' });

// Start the given OpenRC service.
startService(service: string, conf: Record<string, string> | undefined) {
- if (conf) { await this.writeConf(service, conf); }
- await this.execCommand('/usr/local/bin/wsl-service', service, 'start');
// https://github.com/rancher-sandbox/rancher-desktop-wsl-distro/blob/main/files/wsl-service

// Write a configuration file for an OpenRC service.
writeConf(service: string, settings: Record<string, string>) {
- contents = Object.entries(settings).map(([key, value]) => `${ key }="${ value }"\n`).join('');
- await this.writeFile(`/etc/conf.d/${ service }`, contents);


this.getWSLHelperPath(), 'k3s', 'kubeconfig'

// https://github.com/rancher-sandbox/rancher-desktop/blob/main/src/k8s-engine/client.ts
K8s.Client().waitForServiceWatcher();
- while (true) {
  - currentTime = Date.now();
  - if ((currentTime - startTime) > maxWaitTime) {
    - console.log(`Waited more than ${ maxWaitTime / 1000 } secs for kubernetes to fully start up. Giving up.`);
    - break;
  - if (await this.getServiceListWatch()) {
    - break;
  - await util.promisify(setTimeout)(waitTime);

// Get the service watcher, ensuring that it's actually ready to react to
K8s.Client().getServiceListWatch() {
- if (this.services) { return this.services; }
- // If this API call reports that there are zero services currently running,
- // return null (and it's up to the caller to retry later).
- // This doesn't make complete sense, because if we've reached this point,
- // the k3s server must be running. But with wsl we've observed that the service
- // watcher needs more time to start up. When this call returns at least one
- // service, it's ready.
- if ((await this.coreV1API.listServiceForAllNamespaces()).body.items.length === 0) {
  - return null;
- this.services = new k8s.ListWatch('/api/v1/services', 
  - new WrappedWatch(this.kubeconfig, () => { this.emit('service-changed', this.listServices()); }),
    - () => this.coreV1API.listServiceForAllNamespaces());
- return this.services;


childProcess.spawnFile(resources.executable('kubectl'), ['config', 'current-context'], { stdio: Logging.k8s });


// https://github.com/rancher-sandbox/rancher-desktop/blob/main/src/k8s-engine/client.ts
// Wait for at least one node in the cluster to become ready.  This is taken
K8s.Client().waitForReadyNodes() {
- while (true) {
  - { body: nodes } = await this.coreV1API.listNode();
  - conditions = nodes?.items?.flatMap(node => node.status?.conditions ?? []);
  - ready = conditions.some(condition => condition.type === 'Ready' && condition.status === 'True');
  - if (ready) { return nodes; }
  - await util.promisify(setTimeout)(1_000);


### WSL Backend - STOP DEL RESET

stop() {
- // When we manually call stop, the subprocess will terminate, which will
- // cause stop to get called again.  Prevent the re-entrancy.
- // If we're in the middle of starting, also ignore the call to stop (from
- // the process terminating), as we do not want to shut down the VM in that
- // case.
- if (this.currentAction !== Action.NONE) { return; }
- this.currentAction = Action.STOPPING;
- this.setState(K8s.State.STOPPING);
- await this.progressTracker.action('Stopping Kubernetes', 10, async() => {
  - this.process?.kill('SIGTERM');
  - await this.execWSL('--terminate', INSTANCE_NAME);
  - catch (ex) {
    - // Terminating a non-running distribution is a no-op; so we might have
    - // tried to terminate it when it hasn't been registered yet.
    - if (await this.isDistroRegistered({ runningOnly: true })) { throw ex; }
  - this.setState(K8s.State.STOPPED);
- catch (ex) {
  - this.setState(K8s.State.ERROR);
- finally {
  - this.currentAction = Action.NONE;

del() {
- await this.progressTracker.action('Deleting Kubernetes', 20, async() => {
  - await this.stop();
  - if (await this.isDistroRegistered()) {
    - await this.execWSL('--unregister', INSTANCE_NAME);
  - if (await this.isDistroRegistered({ distribution: DATA_INSTANCE_NAME })) {
    - await this.execWSL('--unregister', DATA_INSTANCE_NAME);
  - this.cfg = undefined;

reset(config: Settings['kubernetes']) {
- this.progressTracker.action('Resetting Kubernetes state...', 5, async() => {
  - await this.stop();
  - // Mount the data first so they can be deleted correctly.
  - await this.mountData();
  - await this.k3sHelper.deleteKubeState((...args) => this.execCommand(...args));
  - await this.start(config);

factoryReset() {
- await this.del();
- await Promise.all([paths.cache, paths.config].map(
  - dir => rm(dir, { recursive: true })));
- await rmdir(paths.logs, { recursive: true });
- catch (error) {
  - // On Windows, we will probably fail to delete the directory as the log
  - // files are held open; we should ignore that error.
  - if (error.code !== 'ENOTEMPTY') { throw error; }


### WSL Backend - Services and Port forwarding

listServices(namespace?: string)
- return this.client?.listServices(namespace) || [];

isServiceReady(namespace: string, service: string)
- return (await this.client?.isServiceReady(namespace, service)) || false;

get portForwarder()
- return this;

forwardPort(namespace: string, service: string, port: number | string)
- await this.client?.forwardPort(namespace, service, port);
 
cancelForward(namespace: string, service: string, port: number | string): Promise<void> {
- await this.client?.cancelForwardPort(namespace, service, port);


https://docs.microsoft.com/en-us/windows/wsl/networking
- if you are building a networking app in your Linux distribution
  - you can access it from a Windows app using localhost (just like you normally would)
- if you want to access a networking app running on Windows from your Linux distribution
  - you need to use the IP address of your host machine
  - obtain the IP address of your host machine by running this command from your Linux distribution: 
    - cat /etc/resolv.conf
  - copy the IP address following the term: nameserver
- when using remote IP addresses to connect to your applications
  - they will be treated as connections from the Local Area Network (LAN)
  - this means that you will need to make sure your application can accept LAN connections
  - for example, you may need to bind your application to 0.0.0.0 instead of 127.0.0.1
  - please keep security in mind when making these changes as this will allow connections from your LAN
- accessing a WSL 2 distribution from your local area network (LAN)
  - WSL 2 has a virtualized ethernet adapter with its own unique IP address
  - currently, to enable this workflow you will need to go through the same steps as you would for a regular virtual machine
  - here's an example PowerShell command to add a port proxy that listens on port 4000 on the host and connects it to port 4000 to the WSL 2 VM with IP address 192.168.101.100
    - netsh interface portproxy add v4tov4 listenport=4000 listenaddress=0.0.0.0 connectport=4000 connectaddress=192.168.101.100


### WSL Backend - Integrations

 listIntegrations()
- result: Record<string, boolean | string> = {};
- executable = await this.getWSLHelperPath();
- for (const distro of await this.registeredDistros()) {
  - if (DISTRO_BLACKLIST.includes(distro)) { continue; }
  - kubeconfigPath = await this.k3sHelper.findKubeConfigToUpdate('rancher-desktop');
  - stdout = await this.execWSL( 
      - { env: KUBECONFIG: kubeconfigPath,  WSLENV: `${ process.env.WSLENV }:KUBECONFIG/up`, },
      - '--distribution', distro, '--exec',
      - executable, 'kubeconfig', '--show'
      - if (['true', 'false'].includes(stdout.trim())) {
         - result[distro] = stdout.trim() === 'true';
      -else {
        - result[distro] = stdout.trim();
- return result;
 
listIntegrationWarnings()
- // No implementation warnings available.
  
 setIntegration(distro: string, state: boolean)
- if (!(await this.registeredDistros()).includes(distro)) {
  - console.error(`Cannot integrate with unregistred distro ${ distro }`);
  - return 'Unknown distribution';
- executable = await this.getWSLHelperPath();
- kubeconfigPath = await this.k3sHelper.findKubeConfigToUpdate('rancher-desktop');
- await this.execWSL(
 - { env: KUBECONFIG: kubeconfigPath,  WSLENV: `${ process.env.WSLENV }:KUBECONFIG/up`, },
      - '--distribution', distro, '--exec',
      - executable, 'kubeconfig', `--enable=${ state }`,
- console.log(`kubeconfig integration for ${ distro } set to ${ state }`);

### WSL Backend - Wsl Helper

 getWSLHelperPath()
 - // We need to get the Linux path to our helper executable; it is easier to
- // just get WSL to do the transformation for us.
- await this.execCommand(
      { env: { EXE_PATH: resources.get('linux', 'bin', 'wsl-helper'), WSLENV: `${ process.env.WSLENV }:EXE_PATH/up`,},
      'printenv', 'EXE_PATH');
- return stdout.trim();

src/go/wsl-helper/cmd/root.go
- // Rancher Desktop WSL2 integration helper

src/go/wsl-helper/cmd/kubeconfig.go
- // Set up ~/.kube/config in the WSL2 environment
- // This command configures the Kubernetes configuration inside a WSL2 distribution.
- // wslhelper kubeconfig {configPath} --show|--enable
- kubeconfigCmd.PersistentFlags().String("kubeconfig", "", "Path to Windows kubeconfig, in /mnt/... form.")
- kubeconfigCmd.PersistentFlags().Bool("enable", true, "Set up config file")
- kubeconfigCmd.PersistentFlags().Bool("show", false, "Get the current state rather than set it")
- if !os.Stat(configPath) => Errorf("Could not open Windows kubeconfig: %w", err)
- configDir := path.Join(os.Getenv("HOME"), ".kube")
- linkPath := path.Join(configDir, "config")
- if show 
  - // The output is "true", "false", or an error message for UI.
  - // We will only return nil in this path.
  - err := os.Readlink(linkPath)
  - if err != nil {
    - if errors.Is(err, os.ErrNotExist) => fmt.Println("false")
	- if errors.Is(err, syscall.EINVAL) => fmt.Printf("File %s exists and is not a symlink\n", linkPath)
	- else => fmt.Printf("%s\n", err)
  - else if target == configPath  => fmt.Println("true")
  - else
    - // For a symlink pointing elsewhere, we assume we can overwrite.
	- fmt.Println("false")
  - return nil
- if enable
  - err = os.Mkdir(configDir, 0o750)
  - if err != nil && !errors.Is(err, os.ErrExist) => return err
  - err = os.Symlink(configPath, linkPath)
  - if err != nil  
    - // If it already exists, do nothing; even if it's not a symlink.
    - if errors.Is(err, os.ErrExist) => return nil
	- else => return err
  - else 
    - // No need to create if we want to remove it
	- err := os.Readlink(linkPath)
	- if err != nil
      - if errors.Is(err, os.ErrNotExist) => return nil
	  - else => return err
    - if target == configPath 
      - err = os.Remove(linkPath)
	 - if err != nil && !errors.Is(err, os.ErrNotExist) => return err
  - return nil

src/go/wsl-helper/cmd/k3s.go 

src/go/wsl-helper/cmd/k3s_kubeconfig.go

src/go/wsl-helper/cmd/dockerproxy_start.go 

src/go/wsl-helper/cmd/dockerproxy_serve_linux.go

src/go/wsl-helper/cmd/dockerproxy_serve_windows.go


## Lima Backend

### Lima Backend - START

SUMMARY OF START SEQUENCE
- // Starting kubernetes
- ensureArchitectureMatch()
- // Downloading Kubernetes components
- EXECUTE 3 IN PARALLEL
  - // Checking k3s images
  - k3sHelper.ensureK3sImages(desiredVersion)
  - // Ensuring virtualization is supported
  - ensureVirtualizationSupported()
  - // Updating cluster configuration
  - updateConfig(desiredVersion)

- if ((await this.status)?.status === 'Running')
  - // Stopping existing instance
  - ssh('sudo', '/sbin/rc-service', 'k3s', 'stop')
  - if (isDowngrade)
    - // If we're downgrading, stop the VM (and start it again immediately),
    - // to ensure there are no containers running (so we can delete files).
    - lima('stop', MACHINE_NAME)

-  // Start the VM; if it's already running, this does nothing.
- startVM()
- deleteIncompatibleData(isDowngrade);

- EXECUTE 3 IN PARALLEL
  - EXECUTE 2 IN SEQUENCE
    - // Installing k3s
    - installK3s(desiredVersion)
    - writeServiceScript()
  - // Installing image scanner
  - installTrivy()
  - // Installing CA certificates
  - installCACerts()
- if (os.platform() === 'darwin')
  - // Installing tools
  - installToolsWithSudo()

-  if (this.#currentContainerEngine === ContainerEngine.MOBY)
  - // Starting docker server
  - ssh('sudo', '/sbin/rc-service', 'docker', 'start')
  - ssh('sudo', 'sh', '-c', 'while [ ! -S /var/run/docker.sock ] ; do sleep 1 ; done; chmod a+rw /var/run/docker.sock')

- // Starting k3s
- ssh('sudo', '/sbin/rc-service', '--ifnotstarted', 'k3s', 'start')
- followLogs()

- // Waiting for Kubernetes API
- k3sHelper.waitForServerReady()
- childProcess.spawnFile(this.limactl,
  - ['shell', '--workdir=.', MACHINE_NAME, 'ls', '/etc/rancher/k3s/k3s.yaml'],
  - { env: this.limaEnv, stdio: 'ignore' });
- // Updating kubeconfig
- this.k3sHelper.updateKubeconfig(
  - () => this.limaWithCapture('shell', '--workdir=.', MACHINE_NAME, 'sudo', 'cat', '/etc/rancher/k3s/k3s.yaml')))
- // Waiting for services
- K8s.Client().waitForServiceWatcher()


### Lima Backend - VM Commands

scripts/download/lima.mjs 
- getLima(platform)
  - limaRepo = 'https://github.com/rancher-sandbox/lima-and-qemu';
  - limaTag = 'v1.12';
  - url = `${ limaRepo }/releases/download/${ limaTag }/lima-and-qemu.${ platform }.tar.gz`
  - tarPath = path.join(resourcesDir, `lima-${ limaTag }.tgz`)
  - download(url, tarPath)
  - childProcess.spawn('/usr/bin/tar', ['-xf', tarPath], { cwd: limaDir, stdio: 'inherit' });
- getAlpineLima(arch)
  - alpineLimaRepo = 'https://github.com/lima-vm/alpine-lima';
  - alpineLimaTag = 'v0.2.2';
  - alpineLimaEdition = 'rd';
  - alpineLimaVersion = '3.14.3';
  - url = `${ alpineLimaRepo }/releases/download/${ alpineLimaTag }/alpine-lima-${ alpineLimaEdition }-${ alpineLimaVersion }-${ arch }.iso`
  - destPath = path.join(process.cwd(), 'resources', os.platform(), `alpine-lima-${ alpineLimaTag }-${ alpineLimaEdition }-${ alpineLimaVersion }.iso`);
  - download(url, destPath)

src/assets/lima-config.yaml
ssh:
  loadDotSSHPubKeys: false
firmware:
  legacyBIOS: true
containerd:
  system: false
  user: false
# Provisioning scripts run on every boot, not just initial VM provisioning.
provision:
- # When the ISO image is updated, only preserve selected data from /etc but otherwise use the new files.
  mode: system
  script: |
    #!/bin/sh
    set -o errexit -o nounset -o xtrace
    mkdir -p /bootfs
    mount --bind / /bootfs
    # /bootfs/etc is empty on first boot because it has been moved to /mnt/data/etc by lima
    if [ -f /bootfs/etc/os-release ] && ! diff -q /etc/os-release /bootfs/etc/os-release; then
      cp /etc/machine-id /bootfs/machine-id
      cp /etc/ssh/ssh_host* /bootfs/etc/ssh/
      mkdir -p /etc/docker /etc/rancher
      cp -pr /etc/docker /bootfs/etc
      cp -pr /etc/rancher /bootfs/etc
      rm -rf /mnt/data/etc.prev
      mkdir /mnt/data/etc.prev
      mv /etc/* /mnt/data/etc.prev
      mv /bootfs/etc/* /etc
      # lima has applied changes while the "old" /etc was in place; restart to apply them to the updated one.
      reboot
    fi
    umount /bootfs
    rmdir /bootfs
- # Make sure hostname doesn't change during upgrade from earlier versions
  mode: system
  script: |
    #!/bin/sh
    hostname lima-rancher-desktop
- # Create host.rancher-desktop.internal and host.docker.internal aliases for host.lima.internal
  mode: system
  script: |
    #!/bin/sh
    sed -i 's/host.lima.internal.*/host.lima.internal host.rancher-desktop.internal host.docker.internal/' /etc/hosts
- # Make sure interface rd0 takes priority over eth0
  mode: system
  script: |
    #!/bin/sh
    set -o errexit -o nounset -o xtrace
    RD0="$(ip route | grep "^default.* dev rd0 ")"
    if [ -z "${RD0}" ]; then
      exit 0
    fi
    ETH0="$(ip route | grep "^default.* dev eth0 ")"
    IP="$(echo "$RD0" | awk '{print $3}')"
    METRIC="$(echo "$ETH0" | awk '{print $NF}')"
    ip route del default via "$IP" dev rd0
    ip route add default via "$IP" dev rd0 metric $((METRIC - 1))
- # Clean up filesystems
  mode: system
  script: |
    #!/bin/sh
    set -o errexit -o nounset -o xtrace
    # During boot is the only safe time to delete old k3s versions.
    rm -rf /var/lib/rancher/k3s/data
    # Delete all tmp files older than 3 days.
    find /tmp -depth -mtime +3 -delete
- # Make mount-points shared.
  mode: system
  script: |
    #!/bin/sh
    set -o errexit -o nounset -o xtrace
    for dir in / /etc /tmp /var/lib; do
      mount --make-shared "${dir}"
    done
- # This sets up cron (used for logrotate)
  mode: system
  script: |
    #!/bin/sh
    # Move logrotate to hourly, because busybox crond only handles time jumps up
    # to one hour; this ensures that if the machine is suspended over long
    # periods, things will still happen often enough.  This is idempotent.
    mv -n /etc/periodic/daily/logrotate /etc/periodic/hourly/
    rc-update add crond default
    rc-service crond start
portForwards:
- guestPortRange: [1, 65535]
  hostIP: "0.0.0.0"


MACHINE_NAME = '0'
INTERFACE_NAME = 'rd0'

// The following files, and their parents up to /, must only be writable by root,
// and none of them are allowed to be symlinks (lima-vm requirements).
VDE_DIR = '/opt/rancher-desktop';
RUN_LIMA_LOCATION = '/private/var/run/rancher-desktop-lima';
LIMA_SUDOERS_LOCATION = '/private/etc/sudoers.d/rancher-desktop-lima';

CONFIG_PATH = path.join(paths.lima, '_config', `${ MACHINE_NAME }.yaml`);

get limactl() { return resources.executable('lima/bin/limactl'); }

ensureArchitectureMatch()
- if (os.platform().startsWith('darwin'))
- expectedArch = this.arch === 'aarch64' ? 'arm64' : this.arch;
- childProcess.spawnFile('file', [this.limactl])
- if (!stdout.trim().match(`executable ${ expectedArch }$`))

ensureVirtualizationSupported()
- if (os.platform().startsWith('darwin'))
- stdout = await childProcess.spawnFile('sysctl', ['kern.hv_support'])
- if (!/:\s*1$/.test(stdout.trim()))

get currentConfig(): <LimaConfiguration | undefined> {
- configPath = path.join(paths.lima, MACHINE_NAME, 'lima.yaml');
- configRaw = readFile(configPath, 'utf-8');

// Update the Lima configuration.  
// This may stop the VM if the base disk image needs to be changed.
updateConfig(desiredVersion)
currentConfig = await this.currentConfig;
- baseConfig = currentConfig || {};
- config = merge({}, baseConfig, DEFAULT_CONFIG as LimaConfiguration, {
      images: [{
        location: this.baseDiskImage,
        arch:     this.arch,
      }],
      cpus:   this.cfg?.numberCPUs || 4,
      memory: (this.cfg?.memoryInGB || 4) * 1024 * 1024 * 1024,
      mounts: [
        { location: path.join(paths.cache, 'k3s'), writable: false },
        { location: '~', writable: true },
        { location: '/tmp/rancher-desktop', writable: true },
      ],
      ssh: { localPort: await this.sshPort },
      k3s: { version: desiredVersion.version },
- updateConfigPortForwards(config);
- if (currentConfig) {
  - // update existing configuration
  - configPath = path.join(paths.lima, MACHINE_NAME, 'lima.yaml');
  - // Updating outdated virtual machine',
  - updateBaseDisk(currentConfig)
  - writeFile(configPath, yaml.stringify(config), 'utf-8');
- } else {
  - // new configuration
  - mkdir(path.dirname(this.CONFIG_PATH), { recursive: true });
  - writeFile(this.CONFIG_PATH, yaml.stringify(config));
  - if (os.platform().startsWith('darwin')) {
    - childProcess.spawnFile('tmutil', ['addexclusion', paths.lima]);
- this.#externalInterfaceName = config.networks?.find(entry => (('lima' in entry) && ('interface' in entry)) )?.interface ?? INTERFACE_NAME;

// Check if the base (alpine) disk image is out of date; if yes, update it
// without removing existing data.  This is only ever called from updateConfig
// to ensure that the passed-in lima configuration is the one before we overwrote it.
// This will stop the VM if necessary.
updateBaseDisk(currentConfig: LimaConfiguration) 
- // Lima does not have natively have any support for this; we'll need to
- // reach into the configuration and:
- // 1) Figure out what the old base disk version is.
- // 2) Confirm that it's out of date.
- // 3) Change out the base disk as necessary.
- // Unfortunately, we don't have a version string anywhere _in_ the image, so
- // we will have to rely on the path in lima.yml instead.
- ......
- case 0: // The image is the same version as what we have
  - return;
- case 1: // Need to update the image.
  - break;
- log(`Attempting to update base image from ${ existingVersion } to ${ IMAGE_VERSION }...`);
- if ((await this.status)?.status === 'Running') {
  - await this.lima('stop', MACHINE_NAME);
- diskPath = path.join(paths.lima, MACHINE_NAME, 'basedisk');
- copyFile(this.baseDiskImage, diskPath);

updateConfigPortForwards(config)
- allPortForwards: Array<Record<string, any>> | undefined = config.portForwards;
- if (!allPortForwards) {
  - // This shouldn't happen, but fix it anyway
  - config.portForwards = allPortForwards = DEFAULT_CONFIG.portForwards ?? [];
- dockerPortForwards = allPortForwards?.find(entry => Object.keys(entry).length === 2 &&
  - entry.guestSocket === '/var/run/docker.sock' &&
  - ('hostSocket' in entry));
- if (!dockerPortForwards) {
  - config.portForwards?.push({guestSocket: '/var/run/docker.sock', hostSocket:  'docker')

startVM()
- // Installing networking requirements'
- installCustomLimaNetworkConfig();
- installToolsWithSudo();
- //'Starting virtual machine'
- lima('start', '--tty=false', await this.isRegistered ? MACHINE_NAME : this.CONFIG_PATH);
- // Symlink the logs (especially if start failed) so the users can find them
- machineDir = path.join(paths.lima, MACHINE_NAME);
- readdir(machineDir).then(filenames => filenames.filter(x => x.endsWith('.log'))
  - .forEach(filename => fs.promises.symlink(
    - path.join(path.relative(paths.logs, machineDir), filename),
    - path.join(paths.logs, `lima.${ filename }`))

lima('stop', MACHINE_NAME)
- childProcess.spawnFile(this.limactl, args, { env: this.limaEnv, stdio: console })

installToolsWithSudo()
// https://github.com/lima-vm/vde_vmnet
// https://github.com/lima-vm/lima/blob/master/docs/network.md
- installVDETools(commands, explanations);
- ensureRunLimaLocation(commands, explanations);
- createLimaSudoersFile(commands, explanations, randomTag);
- configureDockerSocket(commands, explanations);

## K3sHelper

class K3sHelper {

releaseApiUrl = 'https://api.github.com/repos/k3s-io/k3s/releases?per_page=100';
releaseApiAccept = 'application/vnd.github.v3+json';

cachePath = path.join(paths.cache, 'k3s-versions.json');
minimumVersion = new semver.SemVer('1.15.0');

constructor(arch: K8s.Architecture) { this.arch = arch; }

/** Read the cached data and fill out this.versions. */
readCache() {
- cachePath = path.join(paths.cache, 'k3s-versions.json');
- cacheData: FullVersion[] = JSON.parse(fs.readFile(this.cachePath, 'utf-8'));
- for (versionString of cacheData) {
  - version = semver.parse(versionString);
  - this.versions[`v${ version.version }`] = version;
}

/** Write this.versions into the cache file. */
writeCache() {
- cacheData = JSON.stringify(Object.values(this.versions).map(v => v.raw));
- mkdir(paths.cache, { recursive: true });
- writeFile(this.cachePath, cacheData, 'utf-8');
}

/** The files we need to download for the current architecture. */
protected get filenames() {
- switch (this.arch) {
  - case 'x86_64':
  - return {
        exe:      'k3s',
        images:   'k3s-airgap-images-amd64.tar',
        checksum: 'sha256sum-amd64.txt',
  - case 'aarch64':
  - return {
        exe:      'k3s-arm64',
        images:   'k3s-airgap-images-arm64.tar',
        checksum: 'sha256sum-arm64.txt',
}

updateCache() {
- readCache()
- response = fetch(releaseApiUrl, { headers: { Accept: this.releaseApiAccept } });
- for (entry of response.json() as ReleaseAPIEntry[]) {
  - processVersion(entry)
- writeCache();
- emit('versions-updated');
}

processVersion(entry: ReleaseAPIEntry): boolean {
- // Process one version entry retrieved from GitHub, inserting it into the cache.
- // @param entry The GitHub API response entry to process.
- // @returns Whether more entries should be fetched.  Note that we will err on the side of getting more versions if we are unsure.
}

/** Initialize the version fetcher. */
initialize() {
- readCache();
- if(this.versions).length > 0) {
  - // Start a cache update asynchronously without waiting for it
  - updateCache();
- else
  - await updateCache();
} }

/** The versions that are available to install.  These do not contain the k3s build suffix, in the form `v1.2.3`. */
get availableVersions(): ShortVersion[] {
- initialize().then(() => {
- versions = Object.keys(this.versions);
- // XXX Temporary hack for Rancher Desktop 0.6.0: Skip 1.22+
- versions = versions.filter(v => semver.lt(v, '1.22.0'));
- return versions.sort(semver.compare).reverse();
}

fullVersion(shortVersion: ShortVersion): FullVersion {
- parsedVersion = semver.parse(shortVersion);
- versionKey = `v${ parsedVersion.version }`;
-  return this.versions[versionKey].raw;
}

/** The download URL prefix for K3s releases. */
protected get downloadUrl() {
- return 'https://github.com/k3s-io/k3s/releases/download';
}

/** Variable to keep track of download progress */
progress = {
exe:      { current: 0, max: 0 },
images:   { current: 0, max: 0 },
checksum: { current: 0, max: 0 },
}

/** Ensure that the K3s assets have been downloaded into the cache, which is at (paths.cache())/k3s.
 * @param shortVersion The version of K3s to download, without the k3s suffix. */
ensureK3sImages(shortVersion: ShortVersion) {
- fullVersion = this.fullVersion(shortVersion);
- cacheDir = path.join(paths.cache, 'k3s');
- verifyChecksums = async(dir: string) => {
  - sumFile = readFile(path.join(dir, this.filenames.checksum), 'utf-8');
   - sums: Record<string, string> = {};
  - for (const line of sumFile.split(/[\r\n]+/)) {
    - match = /^\s*([0-9a-f]+)\s+(.*)/i.exec(line.trim());
    - if (!match) { continue; }
    - [, sum, filename] = match;
    - sums[filename] = sum;
  - promises = [this.filenames.exe, this.filenames.images].map(async(filename) => {
    - hash = crypto.createHash('sha256');
    - createReadStream(path.join(dir, filename)).pipe(hash);
    - digest = hash.digest('hex');
    - if (digest.localeCompare(sums[filename], undefined, { sensitivity: 'base' }) !== 0) 
      - return new Error(`${ filename } has invalid digest ${ digest }, expected ${ sums[filename] }`);
- mkdir(cacheDir, { recursive: true });
- if (!await verifyChecksums(path.join(cacheDir, fullVersion))) {
  - workDir = .mkdtemp(path.join(cacheDir, `tmp-${ fullVersion }-`));
  - Object.entries(this.filenames).map(async([filekey, filename]) => {
    - fileURL = `${ this.downloadUrl }/${ fullVersion }/${ filename }`;
    - outPath = path.join(workDir, filename);
    - response = fetch(fileURL);
    - if (!response.ok) { throw new Error(`Error downloading ${ filename } ${ fullVersion }: ${ response.statusText }`); }
    - progresskey = filekey as keyof typeof K3sHelper.prototype.filenames;
    - status = this.progress[progresskey];
    - status.current = 0;
    - progress = new DownloadProgressListener(status);
    - writeStream = fs.createWriteStream(outPath);
    - status.max = parseInt(response.headers.get('Content-Length') || '0');
    - stream.pipeline(response.body, progress, writeStream);
    - error = await verifyChecksums(workDir);
    - if (error) { console.log('Error verifying checksums after download', error); }
    - safeRename(workDir, path.join(cacheDir, fullVersion));
    - finally { rmdir(workDir, { recursive: true, maxRetries: 3 }); }
}

  /** Wait the K3s server to be ready after starting up.
   * This will check that the proper TLS certificate is generated by K3s; this
   * is required as if the VM IP address changes, K3s will use a certificate
   * that is only valid for the old address for a short while.  If we attempt to
   * communicate with the cluster at this point, things will fail.
   * @param getHost A function to return the IP address that K3s will listen on
   *                internally.  This may be called multiple times, as the
   *                address may not be ready yet.
   * @param port The port that K3s will listen on */
waitForServerReady(getHost: () => Promise<string|undefined>, port: number): Promise<void> {
- host: string | undefined;
- console.log(`Waiting for K3s server to be ready on port ${ port }...`);
- while (true) {
- host = getHost();
- if (typeof host === 'undefined') {
  - await setTimeout(500);
  - continue;
- await new Promise<void>((resolve, reject) => {
  - socket = tls.connect( { host, port, rejectUnauthorized: false },
                () => {
                    - cert = socket.getPeerCertificate();
                    - // Check that the certificate contains a SubjectAltName that
                    - // includes the host we're looking for; when the server starts, it
                    - // may be using an obsolete certificate from a previous run that
                    - // doesn't include the current IP address.
                    - names = cert.subjectaltname.split(',').map(s => s.trim());
                    - acceptable = [`IP Address:${ host }`, `DNS:${ host }`];
                    - if (!names.some(name => acceptable.includes(name))) {
                        - return reject({ code: 'EAGAIN' });
                    - // Check that the certificate is valid; if the IP address _didn't_
                    - // change, but the cert is old, we need to wait for it to be
                    - // regenerated.
                    - if (Date.parse(cert.valid_from).valueOf() >= Date.now()) {
                    - return reject({ code: 'EAGAIN' });
                }
   - socket.on('error', reject);
- break;
- } catch (error) {
  - switch (error.code) {
    - case 'EAGAIN':
    - case 'ECONNREFUSED':
    - case 'ECONNRESET':
      - break;
  - default:
    - console.error(error);
- await setTimeout(1_000)
    

/** Find the home directory, in a way that is compatible with the @kubernetes/client-node package. */
protected async findHome() {
- tryAccess = async(path: string) => {
      - try { access(path); return true; } 
      - catch { return false; }
- if (process.env.HOME && await tryAccess(process.env.HOME)) {
  - return process.env.HOME;   
- if (process.env.HOMEDRIVE && process.env.HOMEPATH) {
  - homePath = path.join(process.env.HOMEDRIVE, process.env.HOMEPATH);
  - if (await tryAccess(homePath)) { return homePath; }
- if (process.env.USERPROFILE && tryAccess(process.env.USERPROFILE)) {
   - return process.env.USERPROFILE;
- return null;
 
/** Find the kubeconfig file containing the given context; if none is found, return the default kubeconfig path.
  * @param contextName The name of the context to look for */
findKubeConfigToUpdate(contextName: string) {
- candidatePaths = process.env.KUBECONFIG?.split(path.delimiter) || [];
- for (kubeConfigPath of candidatePaths) {
  - config = new KubeConfig();
  - config.loadFromFile(kubeConfigPath, { onInvalidEntry: ActionOnInvalid.FILTER });
  - if (config.contexts.find(ctx => ctx.name === contextName)) {
    - return kubeConfigPath;
  - home = await this.findHome();
  - if (home) {
    - kubeDir = path.join(home, '.kube');
    - mkdir(kubeDir, { recursive: true });
    - return path.join(kubeDir, 'config');

/** Update the user's kubeconfig such that the K3s context is available and set as the current context.  
  * This assumes that K3s is already running.
  * @param configReader A function that returns the kubeconfig from the K3s VM. */
updateKubeconfig(configReader: () => Promise<string>) {
- contextName = 'rancher-desktop';
- workDir = amkdtemp(path.join(os.tmpdir(), 'rancher-desktop-kubeconfig-'));
- workPath = path.join(workDir, 'kubeconfig');
- // For some reason, using KubeConfig.loadFromFile presents permissions
- // errors; doing the same ourselves seems to work better.  Since the file
- // comes from the WSL container, it must not contain any paths, so there
- // is no need to fix it up.  This also lets us use an external function to
- // read the kubeconfig.
- workConfig = new KubeConfig();
- workContents = await configReader();
- workConfig.loadFromString(workContents);
- // @kubernetes/client-node deosn't have an API to modify the configs...
- contextIndex = workConfig.contexts.findIndex(context => context.name === workConfig.currentContext);
- if (contextIndex >= 0) {
  - context = workConfig.contexts[contextIndex];
  - userIndex = workConfig.users.findIndex(user => user.name === context.user);
  - clusterIndex = workConfig.clusters.findIndex(cluster => cluster.name === context.cluster);
  - if (userIndex >= 0) {
    - workConfig.users[userIndex] = { ...workConfig.users[userIndex], name: contextName };
  - if (clusterIndex >= 0) {
    - workConfig.clusters[clusterIndex] = { ...workConfig.clusters[clusterIndex], name: contextName };
- workConfig.contexts[contextIndex] = { ...context, name: contextName, user: contextName, cluster: contextName
- workConfig.currentContext = contextName;
- userPath = await this.findKubeConfigToUpdate(contextName);
- userConfig = new KubeConfig();
- // @kubernetes/client-node throws when merging things that already exist
- merge = <T extends { name: string }>(list: T[], additions: T[]) => {
  - for (const addition of additions) {
    - index = list.findIndex(item => item.name === addition.name);
- ........ more lines to update the kubeconfig ........
}

/**  We normally parse all the config files, yaml and json, with yaml.parse, so yaml.parse should work with json here. */
ensureContentsAreYAML(contents: string): string {
- try {
  - return yaml.stringify(yaml.parse(contents));
- } catch (err) {
  - console.log(`Error in k3sHelper.ensureContentsAreYAML: ${ err }`);
- }
- return contents;
}

/** Delete state related to Kubernetes.  This will ensure that images are not deleted.
  * @param execAsRoot A function to run commands on the VM as root.*/
deleteKubeState(execAsRoot: (...args: string[]) => Promise<void>) {
- directories = [
      '/var/lib/kubelet', // https://github.com/kubernetes/kubernetes/pull/86689
      // We need to keep /var/lib/rancher/k3s/agent/containerd for the images.
      '/var/lib/rancher/k3s/data',
      '/var/lib/rancher/k3s/server',
      '/var/lib/rancher/k3s/storage',
      '/etc/rancher/k3s',
      '/run/k3s',
    ];
- console.log(`Attempting to remove K3s state: ${ directories.sort().join(' ') }`);
- await Promise.all(directories.map(d => execAsRoot('rm', '-rf', d)));
}

## MobyImageProcessor

## NerdctlImageProcessor

