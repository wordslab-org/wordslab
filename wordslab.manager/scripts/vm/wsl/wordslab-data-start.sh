#!/bin/ash
dataServiceName=$1

mkdir -p /mnt/wsl/$dataServiceName
mount --bind / /mnt/wsl/$dataServiceName