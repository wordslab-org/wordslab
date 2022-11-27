$vmname = $args[0]
$vmaddress = $args[1]
echo "Creating network config for VM $vmname ($vmaddress)"
$firewallports = ""
for ( $i = 0; $i -lt ($args.count-2)/3; $i++ ) 
{
   $vmport = $args[2+3*$i]
   $localport = $args[2+3*$i+1]
   $exposelan = $args[2+3*$i+2]
   echo "- proxy VM port $vmport from localhost port $localport"
   netsh interface portproxy add v4tov4 listenaddress=0.0.0.0 listenport=$localport connectaddress=$vmaddress connectport=$vmport
   if ( $exposelan -eq "y" )
   {
      if ( $firewallports.Length -gt 0 ) {
         $firewallports = $firewallports + ","
      }
      $firewallports = $firewallports + $localport
   }
}
if ( $firewallports.Length -gt 0 )
{
   echo "- configure firewall to allow inbound traffic on ports $firewallports"
   netsh advfirewall firewall add rule name="$vmname" protocol=TCP dir=in localport=$firewallports action=allow
}