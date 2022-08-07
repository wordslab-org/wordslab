#!/bin/ash
downloadpath=$(wslpath "$1")

mkdir -p /etc/rancher
mkdir -p /var/lib
mkdir -p /var/log/rancher

mkdir -p /var/lib/rancher/k3s/agent/images
cp $downloadpath/$2 /var/lib/rancher/k3s/agent/images/k3s-airgap-images-amd64.tar

cp $downloadpath/wordslab-cluster-start.sh /root/wordslab-cluster-start.sh
chmod a+x /root/wordslab-cluster-start.sh

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" >> /etc/wsl.conf