echo "Checking if Windows Virtual Machine Platform is enabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform

if ($feature.State -eq "Enabled") 
{
   echo "OK - Windows Virtual Machine Platform is enabled"
   exit 0
}

else
{
   echo "KO - Windows Virtual Machine Platform is disabled"
   echo ""
   echo "Enabling Windows Virtual Machine Platform ..."
   try
   {
      $image = Enable-WindowsOptionalFeature -Online -NoRestart -FeatureName VirtualMachinePlatform
      if($image.RestartNeeded)
      {
         echo "Please restart your computer to enable Windows Virtual Machine Platform"
         exit 1
      }
      else
      {
         echo "Windows Virtual Machine Platform is enabled"
         exit 0
      }
   }
   catch
   {
      echo "Failed to enable Windows Virtual Machine Platform"
      exit 2
   }
}