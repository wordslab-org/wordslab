#!/bin/ash
clusterServiceName=$1

mkdir -p /mnt/wsl/$clusterServiceName
mount --bind / /mnt/wsl/$clusterServiceName