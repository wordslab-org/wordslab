# Test program

compute [memoryGB] [processors]

# Machine

8 logical processors
8 GB RAM

# Wsl config

[wsl2]
memory = 2GB
processors = 4
localhostForwarding = false
pageReporting = true
nestedVirtualization = true
swap = 2GB
swapFile = c:\temp\swap.vhdx

Before startup :

c:\swap\swap.vhdx does not exist

# Wsl startup

Ubuntu 20.04

vmmem
- processor 0%
- memory 162 Mo

c:\swap\swap.vhdx : 64 Mo

# compute 1 1

vmmem
- processor 12%
- memory 380 Mo

c:\swap\swap.vhdx : 64 Mo

laurent@YOGA720:/mnt/c/Users/laure$ free -m
              total        used        free      shared  buff/cache   available
Mem:           1975         256        1673           0          45        1622
Swap:          2048           0        2048

# compute 2 2

vmmem
- processor 24%
- memory 583 Mo

c:\swap\swap.vhdx : 64 Mo

laurent@YOGA720:/mnt/c/Users/laure$ free -m
              total        used        free      shared  buff/cache   available
Mem:           1975         443        1485           0          45        1435
Swap:          2048           0        2048

# compute 10 3

vmmem
- processor 37%
- memory 2062 Mo

c:\swap\swap.vhdx : 97 Mo

laurent@YOGA720:/mnt/c/Users/laure$ free -m
              total        used        free      shared  buff/cache   available
Mem:           1975        1853          83           0          38          29
Swap:          2048         109        1938

# quiet time

vmmem
- processor 0%
- memory 234 Mo

c:\swap\swap.vhdx : 97 Mo

# compute 15 4

vmmem
- processor 49%
- memory 2096 Mo

c:\swap\swap.vhdx : 130 Mo

laurent@YOGA720:/mnt/c/Users/laure$ free -m
              total        used        free      shared  buff/cache   available
Mem:           1975        1861          84           0          29          25
Swap:          2048          82        196

# compute 30 6

vmmem
- processor 49%
- memory 2095 Mo

c:\swap\swap.vhdx : 162 Mo

laurent@YOGA720:/mnt/c/Users/laure$ free -m
              total        used        free      shared  buff/cache   available
Mem:           1975        1861          84           0          29          25
Swap:          2048          82        196

# terminate (wsl -t)

60 sec after
- vmmem killed
- c:\swap\swap.vhdx deleted

# windows : compute 7 8 // linux : idle

windows
- processor 97%
- memory 7259 Mo

# windows : compute 7 8 // linux : compute 1 3

windows
- processor 90%

linux
- processor 7%

# linux : compute 1 3 // windows : compute 7 8 

windows
- processor 86%

linux
- processor 8%

=> Windows always takes priority anyway