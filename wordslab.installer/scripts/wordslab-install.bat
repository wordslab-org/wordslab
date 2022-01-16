wsl --import --version 2 wordslab-data C:\Users\laure\AppData\Local\wordslab\vm-data C:\tmp\alpine.tar
wsl -d wordslab-data -- downloadpath=$(/usr/bin/wslpath "C:\tmp") && cp $downloadpath/wordslab-data-init.sh /root/wordslab-data-init.sh && chmod a+x /root/wordslab-data-init.sh 
wsl -d wordslab-data -- /root/wordslab-data-init.sh C:\tmp

wsl --import --version 2 wordslab-cluster C:\Users\laure\AppData\Local\wordslab\vm-cluster C:\tmp\alpine.tar
wsl -d wordslab-cluster -- downloadpath=$(/usr/bin/wslpath "C:\tmp") && cp $downloadpath/wordslab-cluster-init.sh /root/wordslab-cluster-init.sh && chmod a+x /root/wordslab-cluster-init.sh 
wsl -d wordslab-cluster -- /root/wordslab-cluster-init.sh C:\tmp

wsl --import --version 2 wordslab-os C:\Users\laure\AppData\Local\wordslab\vm-os C:\tmp\ubuntu.tar
wsl -d wordslab-os -- downloadpath=$(/usr/bin/wslpath "C:\tmp") && cp $downloadpath/wordslab-os-init.sh /root/wordslab-os-init.sh && chmod a+x /root/wordslab-os-init.sh 
wsl -d wordslab-os -- /root/wordslab-os-init.sh C:\tmp
wsl -d wordslab-os -- nvidia-smi -L
if ERRORLEVEL 0 wsl -d wordslab-os -- /root/wordslab-gpu-init.sh C:\tmp

wordslab-stop.bat

