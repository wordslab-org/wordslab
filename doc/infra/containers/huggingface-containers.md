# Containers repositories for Dockerhub

https://hub.docker.com/u/huggingface

https://github.com/huggingface/transformers/tree/main/docker

https://github.com/philschmid/huggingface-container

# Available images

4.24.0-pt1.13-cuda11.6	transformers	training	4.24.0-pt-cuda;latest-pt-cuda;4.24.0-pt1.13-cuda11.6	dockerfile	huggingface/transformers-training:4.24.0-pt1.13-cuda11.6	False
4.24.0-pt1.13-cpu	transformers	training	4.24.0-pt-cpu;latest-pt-cpu;4.24.0-pt1.13-cpu	dockerfile	huggingface/transformers-training:4.24.0-pt1.13-cpu	False
4.24.0-tf2.9-cuda11.2	transformers	training	4.24.0-tf-cuda;latest-tf-cuda;4.24.0-tf2.9-cuda11.2	dockerfile	huggingface/transformers-training:4.24.0-tf2.9-cuda11.2	False
4.24.0-tf2.9-cpu	transformers	training	4.24.0-cpu;latest-cpu;4.24.0-tf2.9-cpu	dockerfile	huggingface/transformers-training:4.24.0-tf2.9-cpu	False
4.24.0-pt1.13-cuda11.6	transformers	inference	4.24.0-cuda;latest-cuda;4.24.0-pt1.13-cuda11.6	dockerfile	huggingface/transformers-inference:4.24.0-pt1.13-cuda11.6	False
4.24.0-pt1.13-cpu	transformers	inference	4.24.0-cpu;latest-cpu;4.24.0-pt1.13-cpu	dockerfile	huggingface/transformers-inference:4.24.0-pt1.13-cpu	False
4.24.0-tf2.9-cuda11.2	transformers	inference	4.24.0-tf-cuda;latest-tf-cuda;4.24.0-tf2.9-cuda11.2	dockerfile	huggingface/transformers-inference:4.24.0-tf2.9-cuda11.2	False
4.24.0-tf2.9-cpu	transformers	inference	4.24.0-cpu;latest-cpu;4.24.0-tf2.9-cpu	dockerfile	huggingface/transformers-inference:4.24.0-tf2.9-cpu	False

# CPU

## Inference

https://github.com/philschmid/huggingface-container/blob/main/containers/transformers/inference/4.24.0/pt1.13/cpu/Dockerfile

FROM ubuntu:20.04

LABEL maintainer="Hugging Face"

RUN apt-get update \
    && apt-get -y upgrade --only-upgrade systemd openssl cryptsetup \
    && apt-get install -y \
    bzip2 \
    curl \
    git \
    git-lfs \
    tar \
    gcc \
    g++ \
    # audio
    libsndfile1-dev \
    ffmpeg \
    && apt-get clean autoremove --yes \
    && rm -rf /var/lib/{apt,dpkg,cache,log}

# install micromamba
ENV MAMBA_ROOT_PREFIX=/opt/conda
ENV PATH=/opt/conda/bin:$PATH
RUN curl -L https://micromamba.snakepit.net/api/micromamba/linux-64/latest | tar -xj "bin/micromamba" \
    && touch /root/.bashrc \
    && ./bin/micromamba shell init -s bash -p /opt/conda  \
    && grep -v '[ -z "\$PS1" ] && return' /root/.bashrc  > /opt/conda/bashrc

WORKDIR /app
# install python dependencies
COPY environment.yaml /app/environment.yaml
RUN micromamba install -y -n base -f environment.yaml \
    && rm environment.yaml \
    && micromamba clean --all --yes

[environment.yaml]

name: base
channels:
- conda-forge
dependencies:
- python=3.9.13
- pytorch::pytorch=1.13.0=py3.9_cpu_0
- pip:
  - transformers[sklearn,sentencepiece,audio,vision]==4.24.0

## Training

https://github.com/philschmid/huggingface-container/blob/main/containers/transformers/training/4.24.0/pt1.13/cpu/Dockerfile

[environment.yaml]

name: base
channels:
- conda-forge
dependencies:
- python=3.9.13
- pytorch::pytorch=1.13.0=py3.9_cpu_0
- tensorboard
- jupyter
- pip:
  - transformers[sklearn,sentencepiece,audio,vision]==4.24.0
  - datasets==2.3.2

# GPU

## Inference

https://github.com/philschmid/huggingface-container/blob/main/containers/transformers/inference/4.24.0/pt1.13/cuda11.6/Dockerfile

FROM nvidia/cuda:11.6.1-base-ubuntu20.04

...

ENV LD_LIBRARY_PATH="/opt/conda/lib:${LD_LIBRARY_PATH}"

...

[environment.yaml]

name: base
channels:
- conda-forge
dependencies:
- python=3.9.13
- nvidia::cudatoolkit=11.6
- pytorch::pytorch=1.13.0=py3.9_cuda11.6*
- pip:
  - transformers[sklearn,sentencepiece,audio,vision]==4.24.0

## Training

https://github.com/philschmid/huggingface-container/blob/main/containers/transformers/training/4.24.0/pt1.13/cuda11.6/Dockerfile

[environment.yaml]

name: base
channels:
- conda-forge
dependencies:
- python=3.9.13
- nvidia::cudatoolkit=11.6
- pytorch::pytorch=1.13.0=py3.9_cuda11.6*
- tensorboard
- mpi4py=3.0
- jupyter
- pip:
  - transformers[sklearn,sentencepiece,audio,vision]==4.24.0
  - datasets==2.3.2

# "All" container

https://github.com/huggingface/transformers/blob/main/docker/transformers-all-latest-gpu/Dockerfile


