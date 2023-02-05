#!/bin/bash
mv ~/k3s /usr/local/bin/k3s
chmod a+x /usr/local/bin/k3s

mkdir -p /var/lib/rancher/k3s/agent/images
mv ~/k3s-airgap-images-amd64.tar /var/lib/rancher/k3s/agent/images/k3s-airgap-images-amd64.tar

echo -e "alias kubectl='k3s kubectl'" >> ~/.bash_aliases
source ~/.bashrc

mv ~/helm /usr/local/bin/helm
chmod a+x /usr/local/bin/helm

mv ~/buildctl /usr/local/bin/buildctl
chmod a+x /usr/local/bin/buildctl
mv ~/buildkitd /usr/local/bin/buildkitd
chmod a+x /usr/local/bin/buildkitd
mv ~/nerdctl /usr/local/bin/nerdctl
chmod a+x /usr/local/bin/nerdctl

echo -e "alias nerdctl='nerdctl --address /run/k3s/containerd/containerd.sock --namespace k8s.io'" >> ~/.bash_aliases
source ~/.bashrc