#!/bin/bash
mount -o bind /mnt/wordslab-cluster/etc/rancher /etc/rancher
mount -o bind /mnt/wordslab-cluster/var/lib /var/lib
mount -o bind /mnt/wordslab-cluster/var/log/rancher /var/log/rancher
mount -o bind /mnt/wordslab-data/var/volume/rancher /var/volume/rancher

rm -f /etc/rancher/k3s/k3s.yaml
k3s --version | grep -o "v[0-9].*\s" > /var/lib/rancher/k3s/version

nohup /usr/local/bin/k3s server --write-kubeconfig-mode 644 --https-listen-port 6443 --log /var/log/rancher/k3s/k3s-$(date +%Y%m%d-%H%M%S).log --default-local-storage-path /var/volume/rancher/k3s </dev/null >/dev/null 2>&1 &

until [ -f /etc/rancher/k3s/k3s.yaml ]
do
     sleep 1
done

ps x | grep -o "[0-9].*\sk3s server" | grep -Eo "^[0-9]+" > /var/lib/rancher/k3s/pid