# WSL image with optional GPU support

## Source URLS

https://docs.microsoft.com/en-us/windows/wsl/use-custom-distro

https://github.com/rancher-sandbox/rancher-desktop-wsl-distro

https://k3d.io/v5.2.1/usage/advanced/cuda/
 
## rancher-desktop-wsl-distro

### build.yaml / release.yaml

actions/checkout@v2

VERSION_ID={git tag}
BUILD_ID={github sha}

make distro.tar

actions/upload-artifact@v2 distro distro.tar
actions/download-artifact@v2 distro

mv distro.tar distro-${tag#v}.tar

gh release create "${github.ref}" "distro-${tag#v}.tar" --title "${tag}"

### Makefile

NERDCTL_VERSION = 0.15.0
AGENT_VERSION = 0.1.1

wget -O "nerdctl-$(NERDCTL_VERSION).tgz" "https://github.com/containerd/nerdctl/releases/download/v${NERDCTL_VERSION}/nerdctl-full-${NERDCTL_VERSION}-linux-amd64.tar.gz"
wget -O "rancher-desktop-guestagent-$(AGENT_VERSION)" "https://github.com/rancher-sandbox/rancher-desktop-agent/releases/download/v${AGENT_VERSION}/rancher-desktop-guestagent"

# --iidfile		Write the image ID to the file
docker build --iidfile "image-id" --file "Dockerfile" .

# --cidfile		Write the container ID to the file
# /bin/true     On Unix-like operating systems, the true command's sole purpose is to return a successful exit status
docker create --cidfile "container-id" "$(shell cat "image-id")" -- /bin/true

docker export --output "distro.tar" "$(shell cat "container-id")"

docker rm -f "$(shell cat "container-id")"

### Dockerfile

FROM alpine as builder

ADD files/ /
- inittab       # (busybox) inittab : add support for running OpenRC within the container.
                # The /etc/inittab file is the configuration file used by the System V (SysV) initialization system in Linux
- rc.conf       # This file is appended to /etc/rc.conf to override defaults.

- crond.initd                       # This is a replacement for the default /etc/init.d/crond to run crond in a container.
- rancher-desktop-guestagent.initd  # This script is used on WSL to manage the guest agent.

- wsl-init      # This script is used to launch (busybox) init on WSL2.  This is necessary as we need to do some mount namespace shenanigans
- wsl-service   # This script is used to manage an (OpenRC) service on WSL.  This is required as we need to put services in separate PID namespaces there

- wsl.conf      # WSL configuration options : prevent processing /etc/fstab, since it doesn't exist.
                # The /etc/fstab file is a system configuration file that contains all available disks, disk partitions and their options
                # [Automount] 

- os-release    # /etc/os-release for Rancher Desktop's Linux distribution for WSL2
- build.sh      # See below


COPY nerdctl-${NERDCTL_VERSION}.tgz /nerdctl.tgz
COPY rancher-desktop-guestagent-${AGENT_VERSION} /rancher-desktop-guestagent

RUN /bin/sh /build.sh
- # Bootstrap an alpine chroot in /distro & Remove unnecessary packages
- # Add openrc
  - cat rc.conf >> /distro/etc/rc.conf
  - install -m 644 inittab /distro/etc/inittab
- # Disable mounting devfs & cgroups, WSL does that for us.
  - echo 'skip_mount_dev="YES"' >> /distro/etc/conf.d/devfs
  - echo 'rc_cgroup_mode="none"' >> /distro/etc/conf.d/cgroups
- # Add init script
  - install -D wsl-init /distro/usr/local/bin/wsl-init
  - install -D wsl-service /distro/usr/local/bin/wsl-service
- # Create default runlevels
  - chroot /distro /sbin/rc-update add machine-id sysinit
  - echo 'rc_need="!dev"' >> /distro/etc/conf.d/machine-id
- # Add logrotate
  - apk --root /distro add logrotate
  - install crond.initd /distro/etc/init.d/crond
  - chroot /distro /sbin/rc-update add crond default
- # Create the root user (and delete all other users)
  - echo 'root:x:0:0:root:/root:/bin/sh' > /distro/etc/passwd
  - echo 'docker:x:101:root' > /distro/etc/group
  - # The UUCP group is needed internally by OpenRC for /run/lock.
  - # https://github.com/OpenRC/openrc/blob/openrc-0.13.11/sh/init.sh.Linux.in#L71
  - echo 'uucp:x:14:root' >> /distro/etc/group
- # Add default CA certificates (and update-ca-certificates).
  - apk --root /distro add ca-certificates
  - # We don't need the cert symlinks; they'll get regenerated on start.
  - find /distro/etc/ssl/certs -type l -delete
- # Install nerdctl
  - tar -xvf /nerdctl.tgz -C /distro/usr/local/ \ bin/buildctl \  bin/buildkitd \ ...
  - # Add packages required for nerdctl
  - apk --root /distro add iptables ip6tables
- # Add guest agent
  - install rancher-desktop-guestagent /distro/usr/local/bin
  - install rancher-desktop-guestagent.initd /distro/etc/init.d/rancher-desktop-guestagent
  - chroot /distro /sbin/rc-update add rancher-desktop-guestagent default
- # Add Moby components
  - apk --root /distro add docker-engine
  - apk --root /distro add curl # for healthcheck
- # Create required directories
  - install -d /distro/var/log
  - ln -s /run /distro/var/run
- # Clean up apk metadata and other unneeded files
  - rm -rf /distro/var/cache/apk
  - rm -rf /distro/etc/network
- # Generate /etc/os-release; we do it this way to evaluate variables.
  - . /os-release
  - rm -f /distro/etc/os-release # Remove the existing Alpine one
  - for field in $(awk -F= '/=/{ print $1 }' /os-release); do
    - ... echo "${field}=\"${value}\"" >> /distro/etc/os-release
- # Configuration for WSL compatibility
  - install -m 644 wsl.conf /distro/etc/wsl.conf 

FROM scratch
COPY --from=builder /distro/ /

## k3d.io guide

### Dockerfile

ARG K3S_TAG="v1.21.2-k3s1"
FROM rancher/k3s:$K3S_TAG as k3s

FROM nvidia/cuda:11.2.0-base-ubuntu18.04

ARG NVIDIA_CONTAINER_RUNTIME_VERSION
ENV NVIDIA_CONTAINER_RUNTIME_VERSION=$NVIDIA_CONTAINER_RUNTIME_VERSION

RUN echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections

RUN apt-get update && \
    apt-get -y install gnupg2 curl

# Install NVIDIA Container Runtime
RUN curl -s -L https://nvidia.github.io/nvidia-container-runtime/gpgkey | apt-key add -

RUN curl -s -L https://nvidia.github.io/nvidia-container-runtime/ubuntu18.04/nvidia-container-runtime.list | tee /etc/apt/sources.list.d/nvidia-container-runtime.list

RUN apt-get update && \
    apt-get -y install nvidia-container-runtime=${NVIDIA_CONTAINER_RUNTIME_VERSION}

COPY --from=k3s / /

RUN mkdir -p /etc && \
    echo 'hosts: files dns' > /etc/nsswitch.conf

RUN chmod 1777 /tmp

# Provide custom containerd configuration to configure the nvidia-container-runtime
RUN mkdir -p /var/lib/rancher/k3s/agent/etc/containerd/

COPY config.toml.tmpl /var/lib/rancher/k3s/agent/etc/containerd/config.toml.tmpl

# Deploy the nvidia driver plugin on startup
RUN mkdir -p /var/lib/rancher/k3s/server/manifests

COPY device-plugin-daemonset.yaml /var/lib/rancher/k3s/server/manifests/nvidia-device-plugin-daemonset.yaml

VOLUME /var/lib/kubelet
VOLUME /var/lib/rancher/k3s
VOLUME /var/lib/cni
VOLUME /var/log

ENV PATH="$PATH:/bin/aux"

ENTRYPOINT ["/bin/k3s"]
CMD ["agent"]

## config.toml.tmpl