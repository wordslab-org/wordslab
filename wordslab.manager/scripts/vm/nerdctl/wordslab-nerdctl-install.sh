#!/bin/bash

NERDCTL_VERSION=1.1.0

curl -L https://github.com/containerd/nerdctl/releases/download/v${NERDCTL_VERSION}/nerdctl-full-${NERDCTL_VERSION}-linux-amd64.tar.gz --output nerdctl.tar.gz
tar -xvf nerdctl.tar.gz -C /usr/local/ bin/buildctl bin/buildkitd bin/nerdctl
rm nerdctl.tar.gz

echo -e "alias nerdctl='nerdctl --address /run/k3s/containerd/containerd.sock --namespace k8s.io'" >> ~/.bash_aliases
source ~/.bashrc

# Launch the buildkit dameon as a prerequisite to enable the nerdctl build command
# > wordslab-buildkit-start.sh

# You can then use the nerdctl build command with a Dockerfile to create a container image

# > nerdctl build --file xxx --tag yyy
# Included in the alias so that the image is accessible from Kubernetes: --namespace k8s.io 
# Not necessary : --buildkit-host unix:///run/buildkit/buildkitd.sock
 
# Test pod

# kubectl apply -f - <<EOF
# apiVersion: v1
# kind: Pod
# metadata:
#   name: vim
# spec:
#   containers:
#     - name: vim
#       image: wordslab-org/vim:1.0.0
#       imagePullPolicy: Never
# EOF

# kubectl get pods
# kubectl exec -it vim -- /bin/bash