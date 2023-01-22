# Test GPU access

!nvidia-smi

# Install Pytorch

mamba install --yes pytorch torchvision torchaudio pytorch-cuda=11.7 -c pytorch -c nvidia
mamba clean --all -f -y 
fix-permissions "${CONDA_DIR}"
fix-permissions "/home/${NB_USER}"

# Test Pytorch

import torch

device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
print('Using device:', device)
print()

if device.type == 'cuda':
    print(torch.cuda.get_device_name(0)+" "+str(round(torch.cuda.get_device_properties(0).total_memory/1024**3,1))+" GB")
    print('Memory Usage:')
    print('Allocated:', round(torch.cuda.memory_allocated(0)/1024**3,1), 'GB')
    print('Cached:   ', round(torch.cuda.memory_reserved(0)/1024**3,1), 'GB')

# Install Huggingface

mamba install -c huggingface transformers
mkdir -p /home/wordslab/notebooks/huggingface-cache
export HUGGINGFACE_HUB_CACHE=/home/wordslab/notebooks/huggingface-cache

# Test Huggingface

from transformers import pipeline
print(pipeline('sentiment-analysis')('I love Huggingface'))
