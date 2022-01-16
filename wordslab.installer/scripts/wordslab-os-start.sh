#!/bin/bash

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
nohup /usr/local/bin/k3s server --https-listen-port 6443 --log /var/log/rancher/k3s/k3s-$(date +%Y%m%d-%H%M%S).log --default-local-storage-path /var/volume/rancher/k3s </dev/null >/dev/null 2>&1 &
sleep 1

# Output 1: k3s process id
ps | grep -o \"[0-9].*k3s-server\" | grep -Eo \"^[0-9]+\" | tee /var/lib/rancher/k3s/pid
# Output 2: VM ip address
hostname -I | grep -Eo \"^[0-9\.]+\"
# Output 3: Command to get kubeconfig file
echo "wsl -d wordslab-os -- cat /etc/rancher/k3s/k3s.yaml"
