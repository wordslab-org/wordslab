#!/bin/bash
downloadpath=$(wslpath "$1")

echo -e "\n#Locales\nexport LANGUAGE=C\nexport LANG=C\nexport LC_ALL=C" >> ~/.bashrc
source ~/.bashrc

apt-get update
apt-get -y install ca-certificates
apt-get -y install vim-tiny
apt-get -y install gnupg2 curl

mkdir -p /etc/rancher
mkdir -p /var/lib
mkdir -p /var/log/rancher
mkdir -p /var/volume/rancher

cp $downloadpath/$2 /usr/local/bin/k3s
chmod a+x /usr/local/bin/k3s

echo -e "alias kubectl='k3s kubectl'" >> ~/.bash_aliases
source ~/.bashrc

cp $downloadpath/$3 /usr/local/bin/helm
chmod a+x /usr/local/bin/helm

echo -e "[automount]\nenabled=false\n[interop]\nenabled=false\nappendWindowsPath=false" > /etc/wsl.conf