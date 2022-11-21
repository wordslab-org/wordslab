#!/bin/ash

mkdir -p /var/volume/rancher

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" >> /etc/wsl.conf