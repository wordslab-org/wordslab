#!/bin/bash
mv ~/k3s /usr/local/bin/k3s
chmod a+x /usr/local/bin/k3s

mkdir -p /var/lib/rancher/k3s/agent/images
mv ~/k3s-airgap-images-amd64.tar /var/lib/rancher/k3s/agent/images/k3s-airgap-images-amd64.tar

echo -e "alias kubectl='k3s kubectl'" >> ~/.bash_aliases
source ~/.bashrc

mv ~/helm /usr/local/bin/helm
chmod a+x /usr/local/bin/helm