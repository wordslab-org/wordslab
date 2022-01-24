echo "Checking if Windows Subsystem for Linux is enabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux

if ($feature.State -eq "Enabled") 
{
   echo "OK - Windows Subsystem for Linux is enabled"
   exit 0
}

else
{
   echo "KO - Windows Subsystem for Linux is disabled"
   echo ""
   echo "Enabling Windows Subsystem for Linux ..."
   try
   {
      $image = Enable-WindowsOptionalFeature -Online -NoRestart -FeatureName Microsoft-Windows-Subsystem-Linux
      if($image.RestartNeeded)
      {
         echo "Please restart your computer to enable Windows Subsystem for Linux"
         exit 1
      }
      else
      {
         echo "Windows Subsystem for Linux is enabled"
         exit 0
      }
   }
   catch
   {
      echo "Failed to enable Windows Subsystem for Linux"
      exit 2
   }
}