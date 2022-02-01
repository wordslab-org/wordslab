wsl --import --version 2 wordslab-data %10 %1\%2
wsl -d wordslab-data -- cp $(wslpath '%1')/wordslab-data-init.sh /root/wordslab-data-init.sh 
wsl -d wordslab-data -- chmod a+x /root/wordslab-data-init.sh
wsl -d wordslab-data -- /root/wordslab-data-init.sh '%1'
wsl --terminate wordslab-data 
wsl -d wordslab-data -- /root/wordslab-data-start.sh

wsl --import --version 2 wordslab-cluster %9 %1\%2
wsl -d wordslab-cluster -- cp $(wslpath '%1')/wordslab-cluster-init.sh /root/wordslab-cluster-init.sh
wsl -d wordslab-cluster -- chmod a+x /root/wordslab-cluster-init.sh
wsl -d wordslab-cluster -- /root/wordslab-cluster-init.sh '%1' '%5'
wsl --terminate wordslab-cluster 
wsl -d wordslab-cluster -- /root/wordslab-cluster-start.sh

wsl --import --version 2 wordslab-os %8 %1\%3
wsl -d wordslab-os -- cp $(wslpath '%1')/wordslab-os-init.sh /root/wordslab-os-init.sh
wsl -d wordslab-os -- chmod a+x /root/wordslab-os-init.sh
wsl -d wordslab-os -- /root/wordslab-os-init.sh '%1' '%4' '%6'
wsl -d wordslab-os -- nvidia-smi -L
if ERRORLEVEL 0 wsl -d wordslab-os -- /root/wordslab-gpu-init.sh '%1' '%7'
wsl --terminate wordslab-os
wsl -d wordslab-os -- /root/wordslab-os-start.sh

