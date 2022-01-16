wsl --import --version 2 wordslab-data C:\Users\laure\AppData\Local\wordslab\vm-data C:\tmp\alpine.tar
wsl -d wordslab-data -- cp $(wslpath 'C:\tmp')/wordslab-data-init.sh /root/wordslab-data-init.sh 
wsl -d wordslab-data -- chmod a+x /root/wordslab-data-init.sh
wsl -d wordslab-data -- /root/wordslab-data-init.sh 'C:\tmp'
wsl --terminate wordslab-data 
wsl -d wordslab-data -- /root/wordslab-data-start.sh

wsl --import --version 2 wordslab-cluster C:\Users\laure\AppData\Local\wordslab\vm-cluster C:\tmp\alpine.tar
wsl -d wordslab-cluster -- cp $(wslpath 'C:\tmp')/wordslab-cluster-init.sh /root/wordslab-cluster-init.sh
wsl -d wordslab-cluster -- chmod a+x /root/wordslab-cluster-init.sh
wsl -d wordslab-cluster -- /root/wordslab-cluster-init.sh 'C:\tmp'
wsl --terminate wordslab-cluster 
wsl -d wordslab-cluster -- /root/wordslab-cluster-start.sh

wsl --import --version 2 wordslab-os C:\Users\laure\AppData\Local\wordslab\vm-os C:\tmp\ubuntu.tar
wsl -d wordslab-os -- cp $(wslpath 'C:\tmp')/wordslab-os-init.sh /root/wordslab-os-init.sh
wsl -d wordslab-os -- chmod a+x /root/wordslab-os-init.sh
wsl -d wordslab-os -- /root/wordslab-os-init.sh 'C:\tmp'
wsl -d wordslab-os -- nvidia-smi -L
if ERRORLEVEL 0 wsl -d wordslab-os -- /root/wordslab-gpu-init.sh 'C:\tmp'
wsl --terminate wordslab-os
wsl -d wordslab-os -- /root/wordslab-os-start.sh

