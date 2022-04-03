#!/bin/bash
mv k3s /usr/local/bin/k3s
chmod a+x /usr/local/bin/k3s

mkdir -p /mnt/wordslab-cluster/var/lib/rancher/k3s/agent/images
mv k3s-airgap-images-amd64.tar /mnt/wordslab-cluster/var/lib/rancher/k3s/agent/images/k3s-airgap-images-amd64.tar

echo -e "alias kubectl='k3s kubectl'" >> /home/ubuntu/.bash_aliases
source /home/ubuntu/.bashrc

mv helm /usr/local/bin/helm
chmod a+x /usr/local/bin/helm