#!/bin/bash
downloadpath=$(/usr/bin/wslpath "$1")

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" >> /etc/wsl.conf

mkdir -p /var/volume/rancher/k3s

cp $downloadpath/wordslab-data-start.sh /root/wordslab-data-start.sh
chmod a+x /root/wordslab-data-start.sh
