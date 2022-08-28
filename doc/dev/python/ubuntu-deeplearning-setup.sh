#!/bin/bash

# -- EXECUTE AS NB_USER ---

# https://docs.fast.ai/

source .bashrc

mamba install -y jupyter_contrib_nbextensions ipywidgets

mamba install -y -c fastchan fastai

pip install timm

mamba install -y -c huggingface datasets transformers

#mamba install -c conda-forge spacy <- already installed by fastai
mamba install -y cupy

mamba install -y diffusers ftfy

pip install gradio