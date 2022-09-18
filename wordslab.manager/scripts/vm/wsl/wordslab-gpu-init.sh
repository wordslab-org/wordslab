#!/bin/bash
scriptspath=$(wslpath "$1")
NVIDIA_CONTAINER_RUNTIME_VERSION=$2

distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-container-runtime/gpgkey | apt-key add -
curl -s -L https://nvidia.github.io/nvidia-container-runtime/$distribution/nvidia-container-runtime.list | tee /etc/apt/sources.list.d/nvidia-container-runtime.list
apt update && apt -y install nvidia-container-runtime=${NVIDIA_CONTAINER_RUNTIME_VERSION}

mkdir -p /var/lib/rancher/k3s/agent/etc/containerd/
cp $scriptspath/config.toml.tmpl /var/lib/rancher/k3s/agent/etc/containerd/config.toml.tmpl

# Device plugin not yet supported in WSL2 :
# > 2022/01/16 13:58:52 Loading NVML
# >panic: runtime error: invalid memory address or nil pointer dereference
# > [signal SIGSEGV: segmentation violation code=0x1 addr=0x11 pc=0x446b40]
# https://github.com/NVIDIA/k8s-device-plugin/issues/207
# The WSL support for libnvidia-container doesn't include an NVML library (i.e. libnvidia-ml.so).
# Support was added directly into libnvidia-container to support WSL2 without this library.
# Unfortunately, the k8s-device-plugin relies on NVML to enumerate GPUs. 
# You would need a WSL2 specific build of the plugin (which doesn't exist) in order to make this work. 
# There is nothing that can be done until support for that is added.

# mkdir -p /var/lib/rancher/k3s/server/manifests
# cp $downloadpath/device-plugin-daemonset.yaml /var/lib/rancher/k3s/server/manifests/nvidia-device-plugin-daemonset.yaml

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" > /etc/wsl.conf