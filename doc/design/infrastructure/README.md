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