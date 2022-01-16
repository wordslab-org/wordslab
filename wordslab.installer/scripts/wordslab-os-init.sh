#!/bin/bash
downloadpath=$(wslpath "$1")

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" >> /etc/wsl.conf

apt-get update
apt-get -y install ca-certificates
apt-get -y install vim-tiny
apt-get -y install gnupg2 curl

mkdir -p /etc/rancher
mkdir -p /var/lib
mkdir -p /var/log/rancher
mkdir -p /var/volume/rancher

cp $downloadpath/k3s /usr/local/bin/k3s
chmod a+x /usr/local/bin/k3s
echo -e "alias kubectl='k3s kubectl'" >> ~/.bash_aliases

cp $downloadpath/helm /usr/local/bin/helm
chmod a+x /usr/local/bin/helm

cp $downloadpath/wordslab-gpu-init.sh /root/wordslab-gpu-init.sh
chmod a+x /root/wordslab-gpu-init.sh

cp $downloadpath/wordslab-os-start.sh /root/wordslab-os-start.sh
chmod a+x /root/wordslab-os-start.sh
