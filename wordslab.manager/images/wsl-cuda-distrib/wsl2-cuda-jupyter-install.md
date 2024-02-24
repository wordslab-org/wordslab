cd c:\wordslab\vm\distrib

curl -L -o ubuntu-jammy.tar https://partner-images.canonical.com/oci/jammy/current/ubuntu-jammy-oci-amd64-root.tar.gz 
wsl --import pytorch c:\wordslab\vm\distrib ubuntu-jammy.tar
del ubuntu-jammy.tar

wsl -d pytorch
cd ~

apt update
apt install --yes sudo curl wget vim emacs tmux screen git git-lfs htop nvtop traceroute

wget https://raw.githubusercontent.com/fastai/fastsetup/master/setup-conda.sh
chmod u+x setup-conda.sh
./setup-conda.sh
rm Miniconda3-latest-Linux-x86_64.sh
rm setup-conda.sh
exit

wsl -d pytorch
cd ~

# https://anaconda.org/nvidia/libnvjitlink
# https://anaconda.org/pytorch/pytorch-cuda
conda install -y cuda -c nvidia/label/cuda-12.1.0
conda install --force-reinstall -y libnvjitlink=12.1.105 libnvjitlink-dev=12.1.105 -c nvidia

conda install -y pytorch torchvision torchaudio pytorch-cuda=12.1 -c pytorch -c nvidia/label/cuda-12.1.0

# https://docs.nvidia.com/cuda/cuda-toolkit-release-notes/index.html
# CUDA 12.1 GA - Windows driver version : >=531.14
# nvidia-smi: Driver Version: 551.52         CUDA Version: 12.4
# https://www.nvidia.fr/download/driverResults.aspx/200016/fr
# Driver Version: 531.18       CUDA Version: 12.1

import os
os.environ["KINETO_LOG_LEVEL"] = "0"

import torch
from torch.profiler import profile, record_function, ProfilerActivity

input_ids = torch.randint(low=0, high=32000, size=(50,1024), dtype=torch.int64).to(0)
attention_mask = torch.ones(50,1024).to(0)

with profile(activities=[ProfilerActivity.CPU,ProfilerActivity.CUDA]) as prof:
    with record_function("model_inference"):
        with torch.no_grad():
            outputs = input_ids*attention_mask

print(prof.key_averages().table())

=> does not work

---

Go to nvidia control panel, activate developer parameters, then allow perf counters access to all users

install: https://aka.ms/vs/17/release/vs_BuildTools.exe

curl https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86_64.exe -o miniconda.exe
start /wait "" miniconda.exe /S
del miniconda.exe

cd C:\Users\laure\miniconda3
condabin\activate.bat

conda install -y cuda -c nvidia/label/cuda-12.1.0
conda install --force-reinstall -y libnvjitlink=12.1.105 libnvjitlink-dev=12.1.105 -c nvidia

conda install -y pytorch torchvision torchaudio pytorch-cuda=12.1 -c pytorch -c nvidia/label/cuda-12.1.0
conda install -c conda-forge ninja

conda install -c conda-forge jupyterlab
conda install -c conda-forge jupyterlab_execute_time
conda install -c rapidsai-nightly -c conda-forge jupyterlab-nvdashboard
conda install -c conda-forge jupyterlab-git
conda install -c conda-forge ipympl

notepad create-workspace-project.ps1
---
# Check for at least one argument
if ($args.Count -eq 0) {
    Write-Host "Please provide at least one argument to create a workspace project directory and its associated virtual environment."
    Write-Host "New project in workspace\myprojectdir : create-workspace-project myprojectdir"
    Write-Host "Github repo in workspace\fastbook     : create-workspace-project https://github.com/fastai/fastbook.git"
    Write-Host "Github repo in workspace\myprojectdir : create-workspace-project https://github.com/fastai/fastbook.git myprojectdir"
    exit 1
}

$gitUrl = $null
$dirName = $null

# Determine if the argument is a git URL
if ($args[0] -match '\.git$') {
    $gitUrl = $args[0]
    if ($args.Length -gt 1) {
        $dirName = $args[1]
    }
    else {
        $dirName = [System.IO.Path]::GetFileNameWithoutExtension($args[0])
    }
}
else {
    $dirName = $args[0]
}

# Check if the directory already exists
if (Test-Path -Path "workspace\$dirName") {
    Write-Host "Directory workspace\$dirName already exists: please choose another project name"
    exit 1
}

"Creating project directory: workspace\$dirName"
New-Item -Path "workspace\$dirName" -ItemType Directory -Force

if ($null -ne $gitUrl) {
    Write-Host "Cloning git repository: $gitUrl"
    git clone $gitUrl "workspace\$dirName"
    Set-Location -Path "workspace\$dirName"
}
else {
    Set-Location -Path "workspace\$dirName"
    git init 2>$null
}

".venv" | Out-File -FilePath .gitignore -Append
git add .gitignore
Write-Host "Creating a virtual environment and Jupyter kernel for project: $dirName"
python -m venv --system-site-packages --prompt $dirName .venv
.venv\Scripts\Activate.ps1
python -m ipykernel install --user --name=$dirName

if (Test-Path -Path "requirements.txt") {
    Write-Host "Installing the dependencies listed in requirements.txt"
    pip install -r requirements.txt
}
else {
    New-Item -Path "requirements.txt" -ItemType File
    git add requirements.txt
}

Write-Host "Virtual environment is ready for project $dirName"
---

notepad create-workspace-project.bat
---
@echo off
PowerShell -ExecutionPolicy Bypass -File .\create-workspace-project.ps1 %*
---

notepad delete-workspace-project.ps1
---
# Check if the first argument is provided
if (-not $args[0]) {
    Write-Host "Please provide the name of the workspace project directory you want to delete."
    Write-Host "Delete project in workspace\myprojectdir : delete-workspace-project myprojectdir"
    exit 1
}
else {
    $dirName = $args[0]
}

# Check if the directory exists
if (-not (Test-Path -Path "workspace\$dirName")) {
    Write-Host "Directory workspace\$dirName not found: please choose another project name"
    exit 1
}

Write-Host "Deleting the Jupyter kernel for project: $dirName"
jupyter kernelspec uninstall -y $dirName

Write-Host "Deleting the workspace project directory: workspace\$dirName"
Remove-Item -Path "workspace\$dirName" -Recurse -Force
---

notepad delete-workspace-project.bat
---
@echo off
PowerShell -ExecutionPolicy Bypass -File .\delete-workspace-project.ps1 %*
---

mkdir workspace
mkdir models

conda env config vars set JUPYTERLAB_SETTINGS_DIR=workspace\.jupyter\lab\user-settings
conda env config vars set JUPYTERLAB_WORKSPACES_DIR=workspace\.jupyter\lab\workspaces
conda env config vars set HF_HOME=models\huggingface
conda env config vars set FASTAI_HOME=models\fastai
conda env config vars set TORCH_HOME=models\torch
conda env config vars set KERAS_HOME=models\keras
conda env config vars set TFHUB_CACHE_DIR=models\tfhub_modules

condabin\activate.bat

create-workspace-project.bat https://github.com/wordslab-org/wordslab-llms.git

notepad start-jupyterlab.bat
---
jupyter lab --notebook-dir=workspace
---

=> works !!
