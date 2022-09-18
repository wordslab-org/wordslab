# Optimize disk

C:\Users\laure\AppData\Local\Packages\CanonicalGroupLimited.Ubuntu20.04onWindows_79rhkp1fndgsc\LocalState\ext4.vhdx

- Before : 2 088 960 Ko

> wsl --shutdown & wait 60 sec

- Then : 2 088 960 Ko

> diskpart (=> Windows message - elevation as admin, launch new console program)

select vdisk file=C:\Users\laure\AppData\Local\Packages\CanonicalGroupLimited.Ubuntu20.04onWindows_79rhkp1fndgsc\LocalState\ext4.vhdx
attach vdisk readonly
compact vdisk
detach vdisk
exit

- After : 1 138 688 Ko

# Diskpart script

To create a diskpart script, create a text file that contains the Diskpart commands that you want to run, 
with one command per line, and no empty lines. You can start a line with rem to make the line a comment. 

To run a diskpart script, at the command prompt, type the following command,
where scriptname is the name of the text file that contains your script:

> diskpart /s scriptname.txt > logfile.txt

When using the diskpart command as a part of a script, we recommend that you complete all of the diskpart operations together as part of a single diskpart script. 
You can run consecutive diskpart scripts, but you must allow at least 15 seconds between each script for a complete shutdown of the previous execution before running the diskpart command again in successive scripts.
therwise, the successive scripts might fail. 
You can add a pause between consecutive diskpart scripts by adding the timeout /t 15 command to your batch file along with your diskpart scripts.

When diskpart starts, the diskpart version and computer name display at the command prompt. 
By default, if diskpart encounters an error while attempting to perform a scripted task, diskpart stops processing the script and displays an error code (unless you specified the noerr parameter). 
However, diskpart always returns errors when it encounters syntax errors, regardless of whether you used the noerr parameter. 
The noerr parameter enables you to perform useful tasks such as using a single script to delete all partitions on all disks regardless of the total number of disks.

# Resize disk

https://docs.microsoft.com/en-us/windows/wsl/vhd-size

The WSL 2 VHD uses the ext4 file system. 
This VHD automatically resizes to meet your storage needs and has an initial maximum size of 256GB.

> wsl --shutdown

> diskpart

select vdisk file=C:\Users\laure\AppData\Local\Packages\CanonicalGroupLimited.Ubuntu20.04onWindows_79rhkp1fndgsc\LocalState\ext4.vhdx
detail vdisk

" ID du type de périphérique : 0 (Inconnu)
" ID du fournisseur : {00000000-0000-0000-0000-000000000000} (Inconnu)
" État : Ajouté 
" Taille virtuelle :  256 G octets
" Taille physique : 1112 M octets
" ...

expand vdisk maximum=<sizeInMegaBytes> (expand vdisk maximum=512000)
exit

Make WSL aware that it can expand its file system's size by running these commands from your WSL distribution command line.

> wsl -d Ubuntu

> mount | grep ext4

/dev/sdb on / type ext4 (rw,relatime,discard,errors=remount-ro,data=ordered)

> sudo resize2fs /dev/sdb <sizeInMegabytes>M (sudo resize2fs /dev/sdb 512000M)

NB : Impossible to SHRINK on Windows 10 Home
