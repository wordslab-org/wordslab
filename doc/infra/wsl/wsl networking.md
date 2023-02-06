# On Windows

- LAN: 192.168.0.155
- WSL: 192.168.240.1

Carte r�seau sans fil Wi-Fi�:
   Adresse IPv4. . . . . . . . . . . . . .: 192.168.0.155
   Masque de sous-r�seau. . . .�. . . . . : 255.255.255.0

Carte Ethernet vEthernet (WSL) :
   Adresse IPv4. . . . . . . . . . . . . .: 192.168.240.1
   Masque de sous-r�seau. . . .�. . . . . : 255.255.240.0

# On Linux

- WSL: 192.168.253.9

eth0: <BROADCAST,MULTICAST,UP,LOWER_UP> mtu 1500 qdisc mq state UP group default qlen 1000
    inet 192.168.253.9/20 brd 192.168.255.255 scope global eth0

- cat /etc/resolv.conf

 # This file was automatically generated by WSL. To stop automatic generation of this file, add the following entry to /etc/wsl.conf:
 # [network]
 # generateResolvConf = false
 nameserver 192.168.240.1

- cat /etc/hosts

 # This file was automatically generated by WSL. To stop automatic generation of this file, add the following entry to /etc/wsl.conf:
 # [network]
 # generateHosts = false
 127.0.0.1       localhost
 127.0.1.1       YOGA720.localdomain     YOGA720
 127.0.0.1       kubernetes.docker.internal

# localhostForwarding = true

C:\Users\laure\.wslconfig

[wsl2]
memory = 4GB
processors = 2
localhostForwarding = true
pageReporting = true
nestedVirtualization = true

## Open port 80 first on Windows then on Linux

Windows ports

0.0.0.0 [11]
- 80
- 135
- 445
...

127.0.0.1 [4]
- 27060
- 49818
- 59509
...

URL: http://192.168.0.155/

> Browser: answer from 192.168.0.155
> Windows: request from 192.168.0.155

URL: http://192.168.253.9/

> Browser: answer from 192.168.253.9
> Linux: request from 192.168.240.1

URL: http://127.0.0.1/

> Browser: answer from 192.168.0.155
> Windows: request from 127.0.0.1

## Open port 80 first on Linux then on Windows

> WINDOWS START ERROR : port already in use

Windows ports

0.0.0.0 [10]
- 135
- 445
- 5040
...

127.0.0.1 [5]
- 80
- 27060
- 49818
...

URL: http://192.168.0.155/

ERROR

URL: http://192.168.253.9/

> Browser: answer from 192.168.253.9
> Linux: request from 192.168.240.1

URL: http://127.0.0.1/

> Browser: answer from 192.168.253.9
> Linux: request from 127.0.0.1

# localhostForwarding = false
C:\Users\laure\.wslconfig

[wsl2]
memory = 4GB
processors = 2
localhostForwarding = false
pageReporting = true
nestedVirtualization = true

WARNING: after RESTART, the IP address of the Linux VM changes (=> 192.168.246.125)

## Open port 80 first on Windows then on Linux

same behavior as before

## Open port 80 first on Linux then on Windows

> NO error, Windows starts OK

Windows ports

0.0.0.0 [11]
- 80
- 135
- 445
...

127.0.0.1 [4]
- 27060
- 49818
- 59509
...

URL: http://192.168.0.155/

> Browser: answer from 192.168.0.155
> Windows: answer from 192.168.246.125

URL: http://192.168.253.9/

> Browser: answer from 192.168.246.125
> Linux: request from 192.168.240.1

URL: http://127.0.0.1/

> Browser: answer from 192.168.0.155
> Windows: request from 127.0.0.1

# Port fowarding

Needs to be admin :

#All the ports you want to forward separated by coma
$ports=@(80,443,10000,3000,5000);


#[Static ip]
#You can change the addr to your ip config to listen to a specific address
$addr='0.0.0.0';
$ports_a = $ports -join ",";

#Remove Firewall Exception Rules
Remove-NetFireWallRule -DisplayName 'WSL 2 Firewall Unlock' ";

#adding Exception Rules for inbound and outbound Rules
New-NetFireWallRule -DisplayName 'WSL 2 Firewall Unlock' -Direction Outbound -LocalPort $ports_a -Action Allow -Protocol TCP
New-NetFireWallRule -DisplayName 'WSL 2 Firewall Unlock' -Direction Inbound -LocalPort $ports_a -Action Allow -Protocol TCP

for( $i = 0; $i -lt $ports.length; $i++ ){
  $port = $ports[$i];
    netsh interface portproxy delete v4tov4 listenport=$port listenaddress=$addr
    netsh interface portproxy add v4tov4 listenport=$port listenaddress=$addr connectport=$port connectaddress=$remoteport
}