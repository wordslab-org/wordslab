call wordslab-stop.bat

wsl --unregister wordslab-os
wsl --unregister wordslab-cluster
wsl --unregister wordslab-data

wsl --list --verbose