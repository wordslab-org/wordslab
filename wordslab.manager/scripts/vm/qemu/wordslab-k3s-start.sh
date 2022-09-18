#!/bin/bash
kubernetesPort=6443
httpIngressPort=80
httpsIngressPort=443

mount -o bind /mnt/wordslab-data/var/volume/rancher /var/volume/rancher

rm -f /etc/rancher/k3s/k3s.yaml
k3s --version | grep -o "v[0-9].*\s" > /var/lib/rancher/k3s/version

echo <<EOF > /var/lib/rancher/k3s/server/manifests/traefik-config.yaml
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

nohup /usr/local/bin/k3s server --write-kubeconfig-mode 644 --https-listen-port $kubernetesPort --log /var/log/rancher/k3s/k3s-$(date +%Y%m%d-%H%M%S).log --default-local-storage-path /var/volume/rancher/k3s </dev/null >/dev/null 2>&1 &

until [ -f /etc/rancher/k3s/k3s.yaml ]
do
     sleep 1
done

ps x | grep -o "[0-9].*\sk3s server" | grep -Eo "^[0-9]+" > /var/lib/rancher/k3s/pid

# K3s version    : cat /var/lib/rancher/k3s/version
# K3s process id : cat /var/lib/rancher/k3s/pid
# Kubeconfig file: cat /etc/rancher/k3s/k3s.yaml