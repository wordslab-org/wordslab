# Windows Subsystem for Linux : wordslab manual install procedure

## Prerequisites

We assume that WSL2 is already installed on your Windows machine and that a default distribution is ready to use (Ubuntu family and Windows interop enabled).

## Download prerequisites in temp directory

1. Copy the contents of the /scripts directory in C:\tmp

2. Open a Windows command line in C:\tmp

> cmd.exe
> cd C:\tmp

3. Use the default WSL distribution to download dependencies in C:\tmp

> wsl -- $(wslpath "C:\tmp")/wordslab-download.sh "C:\tmp"

Check that the following files are now available in C:\tmp :
 - alpine.tar (5 Mo)
 - helm (44 Mo)
 - k3s (52 Mo)
 - k3s-airgap-images-amd64.tar (481 Mo)
 - ubuntu.tar (76 Mo)

## [Optional] Clean previous wordslab install 

1. Uninstall all wordslab distrbutions

**!! WARNING - DANGEROUS !!** : all data will be lost forever - use this script only for wordslab development and testing.

> wordslab-uninstall.bat

Check in the output if all wordslab distribution were deleted.

## Initialize new wordslab install

1. Install all wordslab distributions

> wordslab-install.bat