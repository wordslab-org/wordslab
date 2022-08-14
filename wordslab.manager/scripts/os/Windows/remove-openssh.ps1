$exitcode = 0

echo "Checking if OpenSSH client is installed ..."
$capability = Get-WindowsCapability -Online -Name OpenSSH.Client

if ($capability.State -eq "Uninstalled") 
{
   echo "OK - OpenSSH client is not installed"
}
else
{
   echo "KO - OpenSSH client is installed"
   echo ""
   echo "Uninstalling OpenSSH client ..."
   try
   {
      $image = Remove-WindowsCapability -Online -Name OpenSSH.Client
      if($image.RestartNeeded)
      {
         echo "Please restart your computer to uninstall OpenSSH client"
         $exitcode = 1
      }
      else
      {
         echo "OpenSSH client was uninstalled"
         $exitcode = 0
      }
   }
   catch
   {
      echo "Failed to uninstall OpenSSH client"
      $exitcode = 2
   }
}

exit $exitcode