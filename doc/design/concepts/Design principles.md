# wordslab infrastructure

## Design goals

### Vision

Git and Github solved the collaboration problem for developers:
- you can use them to work alone, in a closed team, or very widely with open source communities
- each developer works in its dedicated dev environment on a copy of the project files
- this dedicated dev environment can be manually installed on their personal computer or hosted in the cloud
- developers share their work through pull requests reviewed and merged in a shared repository
- a shared CI/CD pipeline tests the contribution and builds a new version of the deliverables
- the deliverables are uploaded to a central artifacts repository in a standard format

Docker and Kubernetes solved the deployment problem for developers and operations:
- developers can bundle and describe all dependencies and deployment requirements of their app in a standard way
- Kubernetes clusters provide a standardized hosting environment everywhere you go: your machine, your entreprise datacenter, your cloud provider
- operations teams can just pull a package from the central artifacts repository and run it anywhere
- the underlying Kubernetes infrastructure helps with resiliency, scalability, security

We want to extend this ecosystem to the **Natural Language Processing workflows and tools** with a very simple platform that encapsulates all the infrastructure concerns.

### Scope

Natural Language Processing workflows add these specific concerns:

Role: data engineer
- data ingestion jobs
- data transformation steps
- datasets management

Role: subject matter expert
- dataset business analysis
- concepts management
- data annotation with concepts

Role conversation designer:
- collect missing information
- route to people, apps or docs
- help with message composition & additional info

Role: data scientist
- model development
- model training and evaluation
- combination of models in pipelines

Role: app developer
- optimize and wrap the pipeline in efficient services
- compose the services to form an app
- describe the app deployment

Role: app integrator
- plug the app into existing communication channels
- connect the app to existing document sources
- connect the app to existing IT systems

Role: environments operator
- intensive compute resources sharing
- GPU support in dev & deployment environments
- deployment of services with high storage, memory & compute requirements

Role: continous improvement analyst
- collect logs, feedback, perf stats from production in training env
- analyze these signals from production
- produce new training datasets and improvement requests

### Value proposition

Many companies are trying to provide such a platform, but wordslab is different :

1. Built for enthousiasts at home AND big companies

- Can be installed on your Personal Computer OR in multiple datacenters
- Free and one-click setup for an individual AND easy integration in an entreprise ecosystem

2. Built for data privacy and security

- On premise or dedicated cloud deployments: your data is not shared with your provider
- Personal and sensitive information is protected by design

3. Built for business AND technical experts

- NLP workflows are a collaboration between many business and technical roles
- Don't try to oversimplify for business users only

4. Focused on business outcomes NOT on algorithms research

- Provides all the extra features needed to build an effective solution for your business
- NLP Workflows optimized for business gains NOT only technical performance

5. Built around standards for open collaboration

- Modeling language and the world knowledge is incredibly labor intensive
- Companies need to collaborate broadly to share the load and to make real progress 

### Design principles

Well-behaved install on shared infrastructure
- Always make the assumption that the machine you run on is shared with other users and applications
- Install and operate the platform with restricted OS access rights and predefined resources limits
- Minimize, document and monitor clearly all impacts you have on the host infrastructure

Minimal prerequisites for the hosting infrastructure
- Try to bundle as many technical dependencies as possible inside the platform itself
- The platform should be self-sufficient on a single freshly installed machine by default
- Progressively enable reuse of existing services in the hosting infrastructure when they are available

Don't reinvent the wheel
- Reuse the most widely used tools and standards in the open source ecosystem whenever possible
- Use only open source components with no specific restictions in their licence
- Use only production ready, well supported and proven components

Allow broad access
- Enable a fully featured 100% free experience on a Personal Computer on any OS
- Minimize the cost to extend the platform with a GPU in the cloud if needed
- Select lightweight components so that the platform can run on mainstream machines

Focus on simplicity and intuitive user experience
- Invest heavily to reduce as much as possible the cognitive load for the users
- Provide workflow oriented and guided user interfaces instead of feature oriented screens
- Be prescriptive and avoid presenting choices to the user unless it's strictly necessary

Make no compromise on data privacy and security
- Even on a personal computer, make sure that the data stays private and secure
- Acces control, data protection and regular backups are mandatory
- Personal data removal is mandatory (GDPR compilance)

Focus on entreprise Natural Langage Processing
- Don't try to build a generic AI platform, stay focused on NLP solutions
- Large web companies work on use cases very specific to their own business: target more traditional entreprises needs  
- Build all the text processing pipelines around the **spacy** library, used as a backbone

Extend the existing ecosystem for open collaboration
- Build the platform around the well-known **Git and Github** concepts and workflows for collaboration
- Design features and standards to enable wide sharing and reuse of all parts of NLP solution

Provide an end-to-end process from dev to deployment
- Span all process steps from data collection and model training to production hosting and continuous improvement
- Enable separate deployment of dev, training, test and production features
- Design carefully the data and code transfers from one environment to the other with security constraints in mind

Build on technical standards to ensure entreprise integration
- Installation on standard Kubernetes clusters with minimal prerequisites and privileges
- Enable several installations of the platform side by side on the same machine or cluster to test upgrades
- Support for integration with entreprise Authentication, Secrets management, Artifacts repos, Backup, Monitoring and Alerting

Comply with entreprise security and resiliency requirements
- Full security documentation of the platform and all components used
- Option to install and operate behind a firewall with no access to the internet
- Segration of execution environments (dev, train, test, prod) and admin roles
- Provide high availability options for all features of the platform 

## Hosting architecture

### Hardware concepts

wordslab tries to enable you to take advantage of all the compute resources you already own to be able to build and deploy cognitive solutions at the lowest price point possible.

A wordslab install can span several Kubernetes clusters
- multi-node cluster management is not in the scope of the wordslab project
  - wordslab creates and manages directly only single node clusters (not recommended for production environments)
  - wordlabs can use multi-node clusters managed by entreprise IT departments or cloud providers
- hardware resources can be 
  - hosted on existing shared machines or Kubernetes clusters (wordslab admin has no admin rights on the hosting machines or clusters)
  - dedicated and allocated on demand from a public or private cloud subscription (wordslab admin owns these resources)
- shared resources can be
  - Windows, MacOS or Linux machines : the machine owner creates a lightweight VM on the machine, this VM runs a single node K3s cluster, the machine owner gives admin access to this K3s cluster 
  - Kubernetes clusters : the cluster owner creates a namespace with restricted quotas and a specific service account, the cluster owner gives admin acces to this namespace with the restricted service account
- a public or private cloud subscription can be used for
  - the wordslab install admin can create VMs hosting single node clusters
  - it is also possible to create K8s clusters managed by the cloud provider

Each cluster can be always on or started only when and while it is needed

A wordslab install is made of execution environments
- a wordslab install has a single adminstration & operations team
- data sharing is possible inside a wordslab install, impossible between installs (except through export/import)
- an execution environment is a Kubernetes namespace dedicated to a wordlab install
- this Kubernetes namespace can be used by one and only one wordslab install
- each execution environment is tagged  with one or more zone labels : core / dev / train / test / prod
- all zones need to be able to access all the core zones (network routes and security)
- there should be no no direct exchanges between the dev / train / test / prod zones, and the core zone can't initiate calls to the other zones

### Software concepts

wordslab is composed of the following modules
- platform services
- collaboration services
- build services (repositories, training, inference)
- run services
- datasets build
- concepts annotate
- pipelines develop
- document search
- text analyzer
- convesational assistant
- user journeys restitution

Each module is made of instances of components wich will run in zones :  hub / dev / train / test / prod
- one component can have several instances in different zones
- component instances can have several replicas deployed in several execution environments with their zone tag

Summary :
- documented - no direct access : shared Win/Mac/Linux machines, shared Kubernetes clusters
- delegated admin : create lightweight VM on shared machine, create namespace with quotas and service account on Kubernetes cluster
- admin : VMs (install K3s), Kubernetes clusters (create namespace)
- managed : Kubernetes namespaces = execution environments
- specific case : wordslab admin is the owner of the shared machine

### Installer

Installer
- single executable file
- web UI in a browser
- all config and cache files in a single directory

2 phases :
- Boostrap phase : config is stored locally on the intial installer machine
- Init phase : config is stored centrally in the hub zone
- Admin phase : config is replicated on several installer machines

Infrastructure Install activities
0. Create a lightweight VM on the local machine
1.a Request a VM on a shared machine : send instructions with a link to a dedicated VM manager
1.b Create a lightweight VM on a shared machine and give remote access to the VM
2. Create a VM in a cloud subscription
4. Create a K3s cluster in a VM
5. Create a K8s cluster in a cloud subscription
6.a Request a namespace on a shared K8s cluster : send instructions with a link to a dedicated namespace manager
6.b Create a namespace with quotas and account service on a shared K8s cluster and give remote access to the namespace
7. Create namespaces inside managed clusters
8. List all namespaces, their properties and access credentials, check connections and prerequisites
9. Apply zone tags to execution environments

wordslab Install activities

### User experience

1. Download wordslab installer

Go to wordslab.org website
- select your operating system
- download a zip file of the wordslab installer
- extract it in the repository of your choice

Minimal impact on the host machine :
All installer files will be stored in this single directory

Connexions / secrets management :
Installer interacts with local executables
- ssh to VMs
- SDKs to cloud subscriptions
- Kubeclient + kubeconfig to clusters

Config storage :
Single file SQLLite database

2. Execute the wordslab installer

Check for a local Sqlite database in the current directory
- if it doesn't exist : initial setup exprience
- if it exists : start/stop experience

Launch web UI in browser

Initial setup experience
- explain the execution environments : you can take advantage of existing or dedicated hardware resources
- 3 steps : design your architecture, review the impacts, confirm and execute the installation operations
- choose an predefined install type or a custom install
CREATE NEW INSTALL
  INDIVIDUAL (admin of everything / High availability optional)
  1. Single machine
    - Lightweight VM on this computer
    - Dedicated Google Cloud VM
  2. Combination of local and/or cloud machines
    - lightweight VM on this computer
    - + option 1 : Training overflow = start cloud / local VM when heavy training (with GPU)
    - + option 2 : Deployment overflow = always on cloud / local VM to deploy services
  3. High availability (multi-machine clusters)
    - lightweight VM on this computer
    - one or several cloud clusters
  ENTREPRISE (clusters managed by IT or cloud provider / High availability mandatory)
  4. Single Kubernetes cluster
    - shared Kubernetes cluster
    - dedicated Google Cloud cluster
  5. Multiple Kubernetes cluster
    - split zones
JOIN local MACHINE TO EXISTING INSTALL
  6. Follow instructions

First initialize the hub
All other execution environments join the hub
The hub is used to transfer information between exec envs

All exec envs know how to talk to the hub, the hub doesn't know how to access the other exec envs
On exec env doesn't know how to reach the other exec envs

The hub issues client / server certificates to invite and then identify exec envs
Exec envs send heartbeat every minute to the hub (used to determine which environments are online / offline)
Exec envs send pull requests in the hub for other exec envs : the exec envs can then choose to pull these requests from the hub or not 

The hub can centralize URLs meaningful for the users to access different parts of the platform
But the users can also bookmark these URLs and acces directly the services when the hub is down


The exec envs are autonomous and can work without the hub