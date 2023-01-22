# nvidia/cuda:12.0.0-runtime-ubuntu22.04
# https://gitlab.com/nvidia/container-images/cuda/-/raw/master/dist/12.0.0/ubuntu2204/runtime/Dockerfile

# FROM nvidia/cuda:12.0.0-base-ubuntu22.04

export NV_CUDA_LIB_VERSION=12.0.0-1

export NV_NVTX_VERSION=12.0.76-1
export NV_LIBNPP_VERSION=12.0.0.30-1
export NV_LIBNPP_PACKAGE=libnpp-12-0=${NV_LIBNPP_VERSION}
export NV_LIBCUSPARSE_VERSION=12.0.0.76-1

export NV_LIBCUBLAS_PACKAGE_NAME=libcublas-12-0
export NV_LIBCUBLAS_VERSION=12.0.1.189-1
export NV_LIBCUBLAS_PACKAGE=${NV_LIBCUBLAS_PACKAGE_NAME}=${NV_LIBCUBLAS_VERSION}

export NV_LIBNCCL_PACKAGE_NAME=libnccl2
export NV_LIBNCCL_PACKAGE_VERSION=2.16.2-1
export NCCL_VERSION=2.16.2-1
export NV_LIBNCCL_PACKAGE=${NV_LIBNCCL_PACKAGE_NAME}=${NV_LIBNCCL_PACKAGE_VERSION}+cuda12.0

# LABEL maintainer "NVIDIA CORPORATION <cudatools@nvidia.com>"

apt-get update
apt-get install -y --no-install-recommends cuda-libraries-12-0=${NV_CUDA_LIB_VERSION} ${NV_LIBNPP_PACKAGE} cuda-nvtx-12-0=${NV_NVTX_VERSION} libcusparse-12-0=${NV_LIBCUSPARSE_VERSION} ${NV_LIBCUBLAS_PACKAGE} ${NV_LIBNCCL_PACKAGE}
rm -rf /var/lib/apt/lists/*

# Keep apt from auto upgrading the cublas and nccl packages. See https://gitlab.com/nvidia/container-images/cuda/-/issues/88
apt-mark hold ${NV_LIBCUBLAS_PACKAGE_NAME} ${NV_LIBNCCL_PACKAGE_NAME}

# Add entrypoint items
# COPY entrypoint.d/ /opt/nvidia/entrypoint.d/
mkdir -p /opt/nvidia/entrypoint.d/
curl -fsSL -o /opt/nvidia/entrypoint.d/10-banner.sh https://gitlab.com/nvidia/container-images/cuda/-/raw/master/entrypoint.d/10-banner.sh
curl -fsSL -o /opt/nvidia/entrypoint.d/12-banner.sh https://gitlab.com/nvidia/container-images/cuda/-/raw/master/entrypoint.d/12-banner.sh
curl -fsSL -o /opt/nvidia/entrypoint.d/15-container-copyright.txt https://gitlab.com/nvidia/container-images/cuda/-/raw/master/entrypoint.d/15-container-copyright.txt
curl -fsSL -o /opt/nvidia/entrypoint.d/30-container-license.txt https://gitlab.com/nvidia/container-images/cuda/-/raw/master/entrypoint.d/30-container-license.txt
curl -fsSL -o /opt/nvidia/entrypoint.d/50-gpu-driver-check.sh https://gitlab.com/nvidia/container-images/cuda/-/raw/master/entrypoint.d/50-gpu-driver-check.sh
curl -fsSL -o /opt/nvidia/entrypoint.d/80-internal-image.sh https://gitlab.com/nvidia/container-images/cuda/-/raw/master/entrypoint.d/80-internal-image.sh
curl -fsSL -o /opt/nvidia/entrypoint.d/90-deprecated-image.sh https://gitlab.com/nvidia/container-images/cuda/-/raw/master/entrypoint.d/90-deprecated-image.sh
# COPY nvidia_entrypoint.sh /opt/nvidia/
mkdir -p /opt/nvidia/
curl -fsSL -o /opt/nvidia/nvidia_entrypoint.sh https://gitlab.com/nvidia/container-images/cuda/-/raw/master/nvidia_entrypoint.sh
export NVIDIA_PRODUCT_NAME="CUDA"
# ENTRYPOINT ["/opt/nvidia/nvidia_entrypoint.sh"]
. /opt/nvidia/nvidia_entrypoint.sh date