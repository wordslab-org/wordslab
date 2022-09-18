#!/bin/bash
downloadpath=$(wslpath "$1")
k3sexecutablefile=$2
k3simagesfile=$3
helmfile=$4

apt update
apt -y install ca-certificates
apt -y install vim-tiny
apt -y install gnupg2 curl

cp $downloadpath/$k3sexecutablefile /usr/local/bin/k3s
chmod a+x /usr/local/bin/k3s
echo -e "alias kubectl='k3s kubectl'" >> ~/.bash_aliases
source ~/.bashrc

mkdir -p /etc/rancher
mkdir -p /var/lib
mkdir -p /var/log/rancher
mkdir -p /var/volume/rancher

mkdir -p /var/lib/rancher/k3s/agent/images
cp $downloadpath/$k3simagesfile /var/lib/rancher/k3s/agent/images/k3s-airgap-images-amd64.tar

cp $downloadpath/$helmfile /usr/local/bin/helm
chmod a+x /usr/local/bin/helm

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" >> /etc/wsl.conf