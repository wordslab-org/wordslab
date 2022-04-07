#!/bin/bash
ssh-keyscan -H -p 3022 0.0.0.0 >> ~/.ssh/known_hosts
scp -P 3022 k3s ubuntu@0.0.0.0:~/k3s
scp -P 3022 k3s-airgap-images-amd64.tar ubuntu@0.0.0.0:~/k3s-airgap-images-amd64.tar
scp -P 3022 helm ubuntu@0.0.0.0:~/helm
scp -P 3022 wordslab-k3s-install.sh ubuntu@0.0.0.0:~/wordslab-k3s-install.sh
scp -P 3022 wordslab-k3s-start.sh ubuntu@0.0.0.0:~/wordslab-k3s-start.sh
ssh -p 3022 ubuntu@0.0.0.0 chmod a+x wordslab-k3s*.sh
ssh -p 3022 ubuntu@0.0.0.0 sudo ./wordslab-k3s-install.sh