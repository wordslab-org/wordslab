#!/bin/bash
downloadpath=$(wslpath "$1")
k3sexecutablefile=$2
k3simagesfile=$3
helmfile=$4

apt update
apt -y install ca-certificates
apt -y install vim-tiny
apt -y install gnupg2 curl

cp $downloadpath/$k3sexecutablefile ~/k3s
cp $downloadpath/$k3simagesfile ~/k3s-airgap-images-amd64.tar

mkdir -p /etc/rancher
mkdir -p /var/lib
mkdir -p /var/log/rancher
mkdir -p /var/volume/rancher

cp $downloadpath/$helmfile ~/helm

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" >> /etc/wsl.conf