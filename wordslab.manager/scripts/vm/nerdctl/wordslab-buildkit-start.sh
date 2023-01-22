#!/bin/bash

# Launch the buildkit dameon as a prerequisite to enable the nerdctl build command

# https://raw.githubusercontent.com/rancher-sandbox/rancher-desktop/c634346735f01c0f70d65bfee1624ae946c8184f/pkg/rancher-desktop/assets/scripts/buildkit.confd
/usr/local/bin/buildkitd --addr=unix:///run/buildkit/buildkitd.sock --containerd-worker=true --containerd-worker-addr=/run/k3s/containerd/containerd.sock --containerd-worker-gc --oci-worker=false 2>&1 |tee /var/log/buildkit.log

# You can then use the nerdctl build command with a Dockerfile to create a container image

# > nerdctl build --file xxx --tag yyy
# If you want the image to be accessible from Kubernetes: --namespace k8s.io 
# Not necessary : --buildkit-host unix:///run/buildkit/buildkitd.sock
 