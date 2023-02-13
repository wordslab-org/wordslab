#!/bin/bash
dataDiskMountPath=$1
kubernetesPort=6443
httpIngressPort=80
httpsIngressPort=443

mount -o bind /mnt/$dataDiskMountPath/var/volume/rancher /var/volume/rancher

mkdir -p /etc/rancher/k3s
mkdir -p /var/lib/rancher/k3s
mkdir -p /var/log/rancher/k3s
mkdir -p /var/volume/rancher/k3s

rm -f /etc/rancher/k3s/k3s.yaml
k3s --version | grep -o "v[0-9].*\s" > /var/lib/rancher/k3s/version

mkdir -p /var/lib/rancher/k3s/server/manifests/
cat << EOF > /var/lib/rancher/k3s/server/manifests/traefik-config.yaml
apiVersion: helm.cattle.io/v1
kind: HelmChartConfig
metadata:
  name: traefik
  namespace: kube-system
spec:
  valuesContent: |-
    ports:
      web:
        exposedPort: $httpIngressPort
      websecure:
        exposedPort: $httpsIngressPort
EOF

# Lanch k3s
nohup /usr/local/bin/k3s server --https-listen-port $kubernetesPort --log /var/log/rancher/k3s/k3s-$(date +%Y%m%d-%H%M%S).log --default-local-storage-path /var/volume/rancher/k3s </dev/null >/dev/null 2>&1 &

until [ -f /etc/rancher/k3s/k3s.yaml ]
do
     sleep 1
done

until [ -n "$(k3s kubectl get crd | grep ingressroutes)" ]
do
     sleep 1
done

ps x | grep -o "[0-9].*\sk3s server" | grep -Eo "^[0-9]+" > /var/lib/rancher/k3s/pid

# Launch the buildkit dameon as a prerequisite to enable the nerdctl build command
# https://raw.githubusercontent.com/rancher-sandbox/rancher-desktop/c634346735f01c0f70d65bfee1624ae946c8184f/pkg/rancher-desktop/assets/scripts/buildkit.confd
/usr/local/bin/buildkitd --addr=unix:///run/buildkit/buildkitd.sock --containerd-worker=true --containerd-worker-addr=/run/k3s/containerd/containerd.sock --containerd-worker-gc --oci-worker=false 2>&1 |tee /var/log/buildkit.log &

# K3s version    : cat /var/lib/rancher/k3s/version
# K3s process id : cat /var/lib/rancher/k3s/pid
# VM IP address  : hostname -I | grep -Eo "^[0-9\.]+"
# Kubeconfig file: cat /etc/rancher/k3s/k3s.yaml
