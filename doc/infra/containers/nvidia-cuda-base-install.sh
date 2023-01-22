# nvidia/cuda:12.0.0-base-ubuntu22.04
# https://gitlab.com/nvidia/container-images/cuda/-/raw/master/dist/12.0.0/ubuntu2204/base/Dockerfile

# FROM ubuntu:22.04

export NVARCH=x86_64
export NVIDIA_REQUIRE_CUDA="cuda>=12.0 brand=tesla,driver>=450,driver<451 brand=tesla,driver>=470,driver<471 brand=unknown,driver>=470,driver<471 brand=nvidia,driver>=470,driver<471 brand=nvidiartx,driver>=470,driver<471 brand=geforce,driver>=470,driver<471 brand=geforcertx,driver>=470,driver<471 brand=quadro,driver>=470,driver<471 brand=quadrortx,driver>=470,driver<471 brand=titan,driver>=470,driver<471 brand=titanrtx,driver>=470,driver<471 brand=tesla,driver>=510,driver<511 brand=unknown,driver>=510,driver<511 brand=nvidia,driver>=510,driver<511 brand=nvidiartx,driver>=510,driver<511 brand=geforce,driver>=510,driver<511 brand=geforcertx,driver>=510,driver<511 brand=quadro,driver>=510,driver<511 brand=quadrortx,driver>=510,driver<511 brand=titan,driver>=510,driver<511 brand=titanrtx,driver>=510,driver<511 brand=tesla,driver>=515,driver<516 brand=unknown,driver>=515,driver<516 brand=nvidia,driver>=515,driver<516 brand=nvidiartx,driver>=515,driver<516 brand=geforce,driver>=515,driver<516 brand=geforcertx,driver>=515,driver<516 brand=quadro,driver>=515,driver<516 brand=quadrortx,driver>=515,driver<516 brand=titan,driver>=515,driver<516 brand=titanrtx,driver>=515,driver<516 brand=tesla,driver>=520,driver<521 brand=unknown,driver>=520,driver<521 brand=nvidia,driver>=520,driver<521 brand=nvidiartx,driver>=520,driver<521 brand=geforce,driver>=520,driver<521 brand=geforcertx,driver>=520,driver<521 brand=quadro,driver>=520,driver<521 brand=quadrortx,driver>=520,driver<521 brand=titan,driver>=520,driver<521 brand=titanrtx,driver>=520,driver<521"
export NV_CUDA_CUDART_VERSION=12.0.107-1
export NV_CUDA_COMPAT_PACKAGE=cuda-compat-12-0
export TARGETARCH=amd64

# LABEL maintainer "NVIDIA CORPORATION <cudatools@nvidia.com>"

apt-get update
apt-get install -y --no-install-recommends gnupg2 curl ca-certificates 
curl -fsSLO https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2204/${NVARCH}/cuda-keyring_1.0-1_all.deb
dpkg -i cuda-keyring_1.0-1_all.deb
#apt-get purge --autoremove -y curl
rm cuda-keyring_1.0-1_all.deb
rm -rf /var/lib/apt/lists/*

export CUDA_VERSION=12.0.0

# For libraries in the cuda-compat-* package: https://docs.nvidia.com/cuda/eula/index.html#attachment-a
apt-get update
apt-get install -y --no-install-recommends cuda-cudart-12-0=${NV_CUDA_CUDART_VERSION} ${NV_CUDA_COMPAT_PACKAGE}
rm -rf /var/lib/apt/lists/*

# Required for nvidia-docker v1
echo "/usr/local/nvidia/lib" >> /etc/ld.so.conf.d/nvidia.conf
echo "/usr/local/nvidia/lib64" >> /etc/ld.so.conf.d/nvidia.conf

export PATH=/usr/local/nvidia/bin:/usr/local/cuda/bin:${PATH}
export LD_LIBRARY_PATH=/usr/local/nvidia/lib:/usr/local/nvidia/lib64

# NVIDIA DEEP LEARNING CONTAINER LICENSE
curl -fsSLO https://gitlab.com/nvidia/container-images/cuda/-/raw/master/NGC-DL-CONTAINER-LICENSE

# nvidia-container-runtime
export NVIDIA_VISIBLE_DEVICES=all
export NVIDIA_DRIVER_CAPABILITIES=compute,utility