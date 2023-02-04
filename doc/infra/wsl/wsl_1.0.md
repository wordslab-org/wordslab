# The Windows Subsystem for Linux in the Microsoft Store is now generally available on Windows 10 and 11

https://devblogs.microsoft.com/commandline/the-windows-subsystem-for-linux-in-the-microsoft-store-is-now-generally-available-on-windows-10-and-11/

## Store version of WSL

- there are two types of WSL distros: “WSL 1”, and “WSL 2” type distros
- these matter for how your distro runs and behaves, as they have different architectures
- WSL 2 distros have faster file system performance and use a real Linux kernel, but require virtualization

- there is also the “in-Windows” version of WSL as a Windows Optional component
- and WSL in the Microsoft Store as the “Store version of WSL”
- These matter for how WSL is serviced on your machine
- and what latest updates and features you’ll get

- WSL 2 is the default distro type,
- the Store version of WSL is the default install location for new users who run wsl --install 
- and easily upgradeable by running wsl --update for existing users

- WSL in the Store will now also be available on Windows 10
- Windows 10 users will also be able to enjoy all of the latest features for WSL including systemd and Linux GUI app support

- you will need to be running Windows 10 version 21H1, 21H2, or 22H2
- check that KB5020030 is installed on Windows 10
  > wmic qfe get hotfixid | findstr "KB5020030"
- or on Windows 11 21H2 with all of the November updates applied
- check that KB5019157 on Windows 11
  > wmic qfe get hotfixid | findstr "KB5019157"

- if you’re a new user you can just run wsl --install
- if you’re an existing user run wsl --update to update to the latest Store version

- you can check if you’re on the Store version by running wsl --version
- if that command fails then you are running the in-Windows version of WSL and need to upgrade to the Store version
  > wsl --version
  > echo %ERRORLEVEL% -> -1

Test Windows 10 :

> wsl --status
Distribution par défaut : Ubuntu-22.04
Version par défaut : 2
La dernière mise à jour effectuée du Sous-système Windows pour Linux date du 15/08/2022
--- BEFORE ---
Le sous-système Windows pour le noyau Linux peut être manuellement mis à jour avec « WSL--Update », mais les mises à jour automatiques ne peuvent pas être effectuées en raison des paramètres de votre système.
Pour recevoir les mises à jour automatiques du noyau, activez le paramètre Windows Update : « Recevoir les mises à jour d’autres produits Microsoft lors de la mise à jour de Windows ».
Pour plus d’informations, rendez-vous sur https://aka.ms/wsl2kernel.
--- AFTER chnaging Windows update settings ---
Les mises à jour WSL automatiques sont activées.
---
Version du noyau : 5.10.102.1

> wsl --update
=> prompt Admin
Installation en cours : Sous-système Windows pour Linux
[================          28,0%                           ]
Sous-système Windows pour Linux a été installé.

> wsl --status
Distribution par défaut : Ubuntu-22.04
Version par défaut : 2

> wsl --version
Version WSL : 1.0.3.0
Version du noyau : 5.15.79.1
Version WSLg : 1.0.47
Version MSRDC : 1.2.3575
Version direct3D : 1.606.4
Version de DXCore : 10.0.25131.1002-220531-1700.rs-onecore-base2-hyp
version Windows : 10.0.19045.2546

## Improvements in store version

1. wsl --install

  - installation from the Microsoft Store by default
  - the Virtual machine platform optional component will still be enabled
  - all of WSLg and the WSL kernel are packaged into the same WSL package
  - will no longer enable the “Windows Subsystem for Linux” optional component
  - will no longer install the WSL kernel or WSLg MSI packages as they are no longer needed
  - by default Ubuntu will still be installed
  - --no-distribution to not install a distribution when installing WSL
  - --no-launch to not launch the distro after installing
  - --web-download to download the most recent version of WSL through GitHub releases

2. wsl --update

  - will now check for and apply updates for the WSL MSIX package from the Microsoft Store
  - opening the Microsoft Store page by default
  - --web-download to allow updates from GitHub release
  - When running WSL using the Windows optional component version, once a week we will show a message on start up indicating that you can upgrade to the Store version by running wsl --update

3. You can opt in for systemd support

  - currently you need to opt-in to enable systemd for a specific WSL distro
  - we will monitor feedback and investigate making this behavior by default in the future

   – ensure you are running the right version of WSL: version 0.67.6 and above
   - add these lines to the /etc/wsl.conf
     [boot]
     systemd=true
   - run wsl.exe --shutdown from PowerShell to restart your WSL instances
   - check you have systemd running with the command: systemctl list-unit-files --type=service

4. Windows 10 users can now use Linux GUI apps

5. wsl --mount

  - --vhd to make mounting VHD files easier
    > wsl --mount --vhd <pathToVHD>

  - --name to make naming the mountpoint easier
    By default the mountpoint name is generated based on the physical disk or VHD name. 
    This can be overriden with --name
    > wsl --mount <DiskPath> --name myDisk

  - the VHDs for WSL 2 distros are stored in this path: C:\Users\[user]\AppData\Local\Packages\[distro]\LocalState\
  - each WSL 2 distro is stored via a virtual hard disk file called: ext4.vhdx

5. wsl --import and wsl --export

  - --vhd to import or export to a VHD directly

6. wsl --import-in-place

  - to take an existing .vhdx file and register it as a distro

7. wsl --version 

  - to print your version information more easily

# How to manage WSL disk space

https://learn.microsoft.com/en-us/windows/wsl/disk-space

## How to locate the .vhdx file

Powershell:

> (Get-ChildItem -Path HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss | Where-Object { $_.GetValue("DistributionName") -eq '<distribution-name>' }).GetValue("BasePath") + "\ext4.vhdx"

## Check available disk space

> wsl.exe --system -d <distribution-name> df -h /mnt/wslg/distro
Filesystem      Size  Used Avail Use% Mounted on
/dev/sdd        251G   73G  166G  31% /mnt/wslg/distro

The output will include:
- Filesystem: Identifier for the VHD file system
- Size: Total size of the disk (the maximum amount of space allocated to the VHD)
- Used: Amount of space currently being used in the VHD
- Avail: Amount of space left in the VHD (Allocated size minus amount used)
- Use%: Percentage of disk space remaining (Used / Allocated size)
- Mounted on: Directory path where the disk is mounted

The amount of disk space allocated to your VHD by WSL will always show the default maximum amount (1TB in the most recent version of WSL).
Even if the amount of disk space on your actual Windows device is less than that

## Expand the size of your WSL 2 Virtual Hard Disk

> diskpart
> Select vdisk file="<pathToVHD>"
> detail vdisk
> expand vdisk maximum=<sizeInMegaBytes>
> exit

> sudo mount -t devtmpfs none /dev
> mount | grep ext4
> sudo resize2fs /dev/sdb <sizeInMegabytes>M

# Architecture

https://devblogs.microsoft.com/commandline/wslg-architecture/



https://askubuntu.com/questions/1444350/is-there-an-alternative-of-wsl-for-ubuntu/1444374#1444374
https://unix.stackexchange.com/questions/732431/what-is-the-system-distribution-in-the-windows-subsystem-for-linux-wsl

# Changelog

https://github.com/microsoft/WSL/releases

0.47.1 (oct 2021)
- WSLg is now bundled as part of the WSL app
- Add mount --vhd to make mounting VHD files easier
- Add --name feature to wsl.exe --mount
- Switch wsl.exe --install to not require the --distribution argument
- Add wsl.exe --version command
- Update wsl.exe --update to launch the store page.

0.50.2
- Add --no-launch option to wsl.exe --install

0.58.0
- Add wsl.exe --import-in-place to take an existing .vhdx file and register it as a distro
- Introduce --vhd flag for wsl.exe --import and wsl.exe --export operations
- Increase the default max size of the dynamic VHD to 1TB

0.67.6
- Add official support for systemd
- Implement wsl.exe --update --web-download to allow updates directly from GitHub

1.0.3 
- Add a --pre-release option to wsl.exe --update

1.1.0 (preview)
- Attempt to always reuse the same IP address in the WSL NAT network
- Make the localhost relay ignore conflicting binds.