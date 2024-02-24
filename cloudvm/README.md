# Running in a cloud Vm

1. Go to https://cloud.jarvislabs.ai/

2. Create an account and launch a VM with the following properties
- Framework: PyTorch-2.1
- GPU Type: RTX6000 Ada-48GB
- Number of GPUs: 1
- Duration: Hourly
- Cost: $1.165/hr
- RAM | Cores: 128GB | 32
- Reserved
- SSD: 200GB - $0.025/hr

3. Click on the Jupyterlab Notebook icon, and when in Jupyterlab launch a terminal
   
4. From the terminal, execute the following commands :
- wget https://raw.githubusercontent.com/wordslab-org/wordslab/main/cloudvm/create_workspace_project.sh
- wget https://raw.githubusercontent.com/wordslab-org/wordslab/main/cloudvm/delete_workspace_project.sh
- chmod u+x *.sh
- ./create_workspace_project.sh https://github.com/wordslab-org/wordslab-llms.git

5. Open the notebooks from your Github projet by selecting the Python kernel corresponding to to your Github project name
- install all the required packages from this kernel
- they will be installed in an isolated virtual environment

6. Use the Jupyterlab Git plugin on the left of the screen to commit then push your changes to the source repo
