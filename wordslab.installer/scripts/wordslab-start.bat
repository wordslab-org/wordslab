wsl -d wordslab-data -- /root/wordslab-data-start.sh
wsl -d wordslab-cluster -- /root/wordslab-cluster-start.sh
wsl -d wordslab-os -- /root/wordslab-os-start.sh

wsl -d wordslab-os -- kubectl cluster-info