# Concepts

## A cloud environment at home

Modern **applications** are built as a combination of multiple independent **services**.

Public cloud platforms from Amazon, Microsoft or Google provide efficient environments to build these modern applications:
- they host deployment **clusters**, a powerful abstraction over hardware and operating systems, which enable you to package, distribute, run and orchestrate hundreds of services
- they allow you to create isolated logical **environments**, where you can develop, test and deploy different versions of the same application side by side
- they manage ready to use **platform services** for you, like databases or web servers, which your applications can simply reuse in one click
- they implement state of the art **security** practices, to protect your data and services from all kinds of cyber attacks

People who want to build or use these modern applications often turn to public cloud platforms for two reasons:

1.they don't want to buy their own hardware

and / or

2. they don't have the skills and time required to install, configure, secure and manage this complex stack of software on their own hardware

Public clouds are great, but they have two important drawbacks:
- they are very expensive if you use them daily
- you need to trust a multinational company to host your sensitive personal data

This limits the groups of people who can experiment with the latest technologies, like AI applications:
- if you are a student or a technology enthousiast at home, 
- if you are a school or a small research lab, 
- if you are a small company with no dedicated IT team,

you certainly can't afford the huge bills which come with a prolonged use of these technologies in a public cloud.

At the same time, the hardware we own at home gets more and more powerful over time. 
In 2022, you can get a gaming PC with 16 logical processors, 32 GB of memory, 2 TB of fast SSD disk, and a powerful Nvidia GPU for 2000$.
This is not cheap, but it is much cheaper than public cloud bills for sustained use over a few months.

The goal of wordslab is to enable anyone to create cloud-like deployment **clusters** on their own personal computer, to experiment with modern applications - like state of the art AI systems - at home and at no additional cost.

This has the potential to unlock a lot of creativity and innovation, help with training, enable collaborative open source projects outside of big companies.

## Controlled impact on the host machine

wordslab is designed to be hosted on a personal computer used every day by a family.

wordslab clusters can be installed on any machine with at least :
- Windows 10, Ubuntu Linux 18.04, or MacOS Catalina
- 4 logical processors, 8 GB of RAM, 100 GB free disk space
- a Nvidia GPU is recommended to train AI applications

**The computer owner stays at all times in control of the impact of the deployment clusters on the host machine**.

1. The deployment clusters have no access to the host operating system and data
- each deployment cluster is isolated in a lightweight **virtual machine**
- the native OS hypervisor guarantees that these virtual machines can't access the host apps and data

2. The deployment clusters are confined in a **sandbox** wich limits their compute and disk usage
- the native OS hypervisor sets upper bounds to the number of logical processors and the amount memory accessible to the virtual machines
- while executing a workload, the virtual machines will only use the resources they need inside these bounds
- this guarantees minimum free resources on the host machine, to ensure that the family computer stays usable for other daily activities

3. The deployment clusters have almost no impact on the software configuration and file system of the host machine
- the only prerequisite is to activate the native operating system hypervisor (WSL2 on Windows, KVM & Qemu on Linux, Hypervisor & Qemu on MacOS)
- the wordslab manager application is simply made of a few files, unzipped in a single directory of your choice
- each virtual machine will only create three data files (for three virtual disks) in directories of your choice

All deployments are executed inside the lightweight virtual machines: the only impact visible on the host machine is growing virtual disks files.

# User experience

## Create a deployment clusters

1. Download and unzip wordslab manager

- open a web browser on the install section of the wordslab website "https://wordslab.org/install"
- select your operating system and follow the instructions

- open a terminal window
- go to the parent directory of your choise
- copy&paste the download command from the wordslab website
- execute the download command to extract wordslab manager files in a ./wordslab subdirectory
- (optional) copy&paste the path command from the wordslab website
- (optional) execute the path command to add wordslab manager the PATH environment variable
- execute "wordslab version" to check if wordslab manager is working

2. Configure the host machine or/and the cloud account

- decide if you want to use a command line or a web interface
- the web interface is more user friendly for non technical users
- the command line is useful if you only have a remote access to the host machine (for example through SSH)
- the command line is also useful if you want to automate / script operations

- launch wordslab without arguments to start the web interface
- (if needed) copy&paste the URL printed in the console to manually open the web interface in your browser
- 2 dashboards are displayed: host machine & cloud account config

- execute wordslab help to learn the command line syntax
- execute wordslab host configure & wordslab cloud configure
- follow the questions

- wordslab tracks if your host machine / your cloud account was previously prepared
- if it is not yet the case, the configuration step is mandatory

- host configuration steps:
  - check software prerequisites
  - check access to download sites or files in the download cache (airgap)
  - announce the operations that will be executed
  - download and install software prerequisites
  - if the user isn't admin of the host machine, some commands may be manually delegated to the admin
  - configure disk paths and maximum directories size
  - configure compute sandbox: maximum processors and memory
  - configure network policy: localhost ports range, authorize to open ports on LAN
  
- cloud account configuration steps
  - follow tutorial to create a cloud account
  - set up billing limits
  - set up secure connexion with the host machine

- after the configuration step, the dashboards display the local and cloud resources status

3. Create one virtual machine

- choose host machine or cloud account
- select processors / memory / GPU / disks size (from minimum / recommended / maximum size)
- select host ports and LAN exposition (host machine)
- create the virtual machine => the virtual machine appears in a list in the right dashboard

# Config object model

1. Configure sandbox
2. Configure virtual machine (inside sandbox)
3. Run virtual machine (starting from config)

HostMachineConfig
- HostName
- Processors
- MemoryGB
- WithGPU
- HostStorage (with MaxSizeGB)
- HostPorts

CloudAccountConfig
- Name
- Credentials
- MonthlyFixedBill
- MonthlyUsageBill

VirtualMachineSpec
- Processors
- MemoryGB
- GPUs
- ClusterDiskMaxSizeGB
- DataDiskMaxSizeGB

VirtualMachineConfig : VirtualMachineSpec
- Name
- VmProvider
- VmModel
- VmSSHPort / HostSSHPort / SSHAccessFromLocalhost
- VmKubernetesPort / HostKubernetesPort / KubernetesAccessFromLocalhost
- VmHttpPort / HostHttpPort / HttpAccessFromLocalhost / HttpAccessFromLAN
- VmHttpsPort / HostHttpsPort / HttpsAccessFromLocalhost / HttpsAccessFromLAN

VirtualMachineStartArguments
- Processors
- MemoryGB
- GPUs
- HttpAccessFromLAN
- HttpsAccessFromLAN

VirtualMachineInstance : VirtualMachineStartArguments
- Name
- HostProcessId
- CloudServiceId
- VmIPAddress

VirtualMachine
- Name -> Config / Instance
- RequiredConfig
- RunningInstance



