echo "Checking if Windows Subsystem for Linux is disabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux

if ($feature.State -eq "Disabled") 
{
   echo "OK - Windows Subsystem for Linux is disabled"
   exit 0
}

else
{
   echo "KO - Windows Subsystem for Linux is enabled"
   echo ""
   echo "Disabling Windows Subsystem for Linux ..."
   try
   {
      $image = Disable-WindowsOptionalFeature -Online -NoRestart -FeatureName Microsoft-Windows-Subsystem-Linux
      if($image.RestartNeeded)
      {
         echo "Please restart your computer to disable Windows Subsystem for Linux"
         exit 1
      }
      else
      {
         echo "Windows Subsystem for Linux is disabled"
         exit 0
      }
   }
   catch
   {
      echo "Failed to disale Windows Subsystem for Linux"
      exit 2
   }
}