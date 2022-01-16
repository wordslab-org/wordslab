#!/bin/bash
downloadpath=$(/usr/bin/wslpath "$1")
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)

NVIDIA_CONTAINER_RUNTIME_VERSION=3.7.0-1
apt-get update && apt-get -y install gnupg2 curl
curl -s -L https://nvidia.github.io/nvidia-container-runtime/gpgkey | apt-key add -
curl -s -L https://nvidia.github.io/nvidia-container-runtime/$distribution/nvidia-container-runtime.list | tee /etc/apt/sources.list.d/nvidia-container-runtime.list
apt-get update && apt-get -y install nvidia-container-runtime=${NVIDIA_CONTAINER_RUNTIME_VERSION}

mount -o bind /mnt/wsl/wordslab-cluster/var/lib /var/lib

mkdir -p /var/lib/rancher/k3s/agent/etc/containerd/
cp $downloadpath/config.toml.tmpl /var/lib/rancher/k3s/agent/etc/containerd/config.toml.tmpl

mkdir -p /var/lib/rancher/k3s/server/manifests
cp $downloadpath/device-plugin-daemonset.yaml /var/lib/rancher/k3s/server/manifests/nvidia-device-plugin-daemonset.yaml
