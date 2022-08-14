$exitcode = 0

echo "Checking if OpenSSH client is installed ..."
$capability = Get-WindowsCapability -Online -Name OpenSSH.Client

if ($capability.State -eq "Installed") 
{
   echo "OK - OpenSSH client is installed"
   $exitcode = 0
}
else
{
   echo "KO - OpenSSH client is not installed"
   echo ""
   echo "Installing OpenSSH client ..."
   try
   {
      $image = Add-WindowsCapability -Online -Name OpenSSH.Client
      if($image.RestartNeeded)
      {
         echo "Please restart your computer to enable OpenSSH client"
         $exitcode = 1
      }
      else
      {
         echo "OpenSSH client was sucessfully installed"
         $exitcode = 0
      }
   }
   catch
   {
      echo "Failed to install OpenSSH client"
      $exitcode = 2
   }
}

exit $exitcode