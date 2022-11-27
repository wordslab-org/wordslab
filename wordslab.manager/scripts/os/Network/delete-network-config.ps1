$vmname = $args[0]
echo "Deleting network config for VM $vmname"
$firewallports = ""
for ( $i = 0; $i -lt ($args.count-1)/2; $i++ ) 
{
   $localport = $args[1+2*$i]
   $exposelan = $args[1+2*$i+1]
   echo "- stop proxy from localhost port $localport"
   netsh interface portproxy delete v4tov4 listenaddress=0.0.0.0 listenport=$localport
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
   echo "- delete firewall config which allowed inbound traffic on ports $firewallports"
   netsh advfirewall firewall delete rule name="$vmname"
}