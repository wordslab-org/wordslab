#!/bin/bash
downloadpath=$(wslpath "$1")

mkdir ~/wordslab-downloads
cd ~/wordslab-downloads

# https://partner-images.canonical.com/oci/ => Ubuntu minimum images
wget https://partner-images.canonical.com/oci/focal/20220105/ubuntu-focal-oci-amd64-root.tar.gz
gunzip ubuntu-focal-oci-amd64-root.tar.gz
mv ubuntu-focal-oci-amd64-root.tar $downloadpath/ubuntu.tar

# https://alpinelinux.org/downloads/ => Alpine mini root filesystem
wget https://dl-cdn.alpinelinux.org/alpine/v3.15/releases/x86_64/alpine-minirootfs-3.15.0-x86_64.tar.gz
gunzip alpine-minirootfs-3.15.0-x86_64.tar.gz
mv alpine-minirootfs-3.15.0-x86_64.tar $downloadpath/alpine.tar

# https://github.com/k3s-io/k3s/releases/ => Rancher k3s releases
wget https://github.com/k3s-io/k3s/releases/download/v1.22.5+k3s1/k3s -O $downloadpath/k3s
wget https://github.com/k3s-io/k3s/releases/download/v1.22.5+k3s1/k3s-airgap-images-amd64.tar -O $downloadpath/k3s-airgap-images-amd64.tar

# https://github.com/helm/helm/releases => Helm releases
wget https://get.helm.sh/helm-v3.7.2-linux-amd64.tar.gz 
tar -zxvf helm-v3.7.2-linux-amd64.tar.gz
mv linux-amd64/helm $downloadpath/helm

cd ~
rm -rf ~/wordslab-downloads
