# 1. Make sure k3s uses the nvidia conatiner runtime

https://itnext.io/enabling-nvidia-gpus-on-k3s-for-cuda-workloads-a11b96f967b0

## Make sure a driver is installed

https://docs.nvidia.com/cuda/wsl-user-guide/index.html

The CUDA driver installed on Windows host will be stubbed inside the WSL 2 as libcuda.so, therefore users must not install any NVIDIA GPU Linux driver within WSL 2.

## Install the nvidia-container-runtime

https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/overview.html
https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html#step-2-install-nvidia-container-toolkit

> distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
> curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add —
> curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | sudo tee /etc/apt/sources.list.d/nvidia-docker.list

## Configure k3s to use this runtime

https://docs.k3s.io/advanced#configuring-containerd
https://k3d.io/v5.4.6/usage/advanced/cuda/#configure-containerd

> wget https://k3d.io/v4.4.8/usage/guides/cuda/config.toml.tmpl -O /var/lib/rancher/k3s/agent/etc/containerd/config.toml.tmpl

# 2. Enable GPU access and GPU capabilities in the container spec

https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/user-guide.html#environment-variables-oci-spec

## Mandatory config in the Dockerfile

ENV NVIDIA_VISIBLE_DEVICES all
ENV NVIDIA_DRIVER_CAPABILITIES compute,utility
ENV NVIDIA_REQUIRE_CUDA "cuda>=11.7"

## Testing that this works

cuda-base-test.yaml

apiVersion: v1
kind: Pod
metadata:
  name: cuda-base-test
  labels:
    app: cuda
spec:
  containers:
  - image: docker.io/nvidia/cuda:12.0.0-base-ubuntu22.04
    command:
      - "sleep"
      - "604800"
    imagePullPolicy: IfNotPresent
    name: ubuntu
  restartPolicy: Always

> kubectl apply -f cuda-base-test.yaml
> kubectl exec -it cuda-base-test -- /bin/bash
> nvidia-smi


