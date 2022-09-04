#!/bin/bash
kubernetesPort=$1

mount -o bind /mnt/wsl/wordslab-cluster/etc/rancher /etc/rancher
mount -o bind /mnt/wsl/wordslab-cluster/var/lib /var/lib
mount -o bind /mnt/wsl/wordslab-cluster/var/log/rancher /var/log/rancher
mount -o bind /mnt/wsl/wordslab-data/var/volume/rancher /var/volume/rancher

mkdir -p /etc/rancher/k3s
rm -f /etc/rancher/k3s/k3s.yaml
mkdir -p /var/lib/rancher/k3s
k3s --version | grep -o "v[0-9].*\s" > /var/lib/rancher/k3s/version
mkdir -p /var/log/rancher/k3s
mkdir -p /var/volume/rancher/k3s

export IPTABLES_MODE=legacy
nohup /usr/local/bin/k3s server --https-listen-port $kubernetesPort --log /var/log/rancher/k3s/k3s-$(date +%Y%m%d-%H%M%S).log --default-local-storage-path /var/volume/rancher/k3s </dev/null >/dev/null 2>&1 &

until [ -f /etc/rancher/k3s/k3s.yaml ]
do
     sleep 1
done

ps x | grep -o "[0-9].*\sk3s server" | grep -Eo "^[0-9]+" > /var/lib/rancher/k3s/pid

# K3s version    : cat /var/lib/rancher/k3s/version
# K3s process id : cat /var/lib/rancher/k3s/pid
# VM IP address  : hostname -I | grep -Eo "^[0-9\.]+"
# Kubeconfig file: cat /etc/rancher/k3s/k3s.yaml
