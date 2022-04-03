#!/bin/bash
ssh -p 3022 ubuntu@0.0.0.0 sudo ./wordslab-k3s-start.sh
ssh -p 3022 ubuntu@0.0.0.0 k3s kubectl cluster-info
mkdir -p ~/.kube
scp -P 3022 ubuntu@0.0.0.0:/etc/rancher/k3s/k3s.yaml ~/.kube/wordslab-config