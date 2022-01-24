echo "Checking if Windows Virtual Machine Platform is disabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform

if ($feature.State -eq "Disabled") 
{
   echo "OK - Windows Virtual Machine Platform is disabled"
   exit 0
}

else
{
   echo "KO - Windows Virtual Machine Platform is enabled"
   echo ""
   echo "Disabling Windows Virtual Machine Platform ..."
   try
   {
      $image = Disable-WindowsOptionalFeature -Online -NoRestart -FeatureName VirtualMachinePlatform
      if($image.RestartNeeded)
      {
         echo "Please restart your computer to disable Windows Virtual Machine Platform"
         exit 1
      }
      else
      {
         echo "Windows Virtual Machine Platform is disabled"
         exit 0
      }
   }
   catch
   {
      echo "Failed to disale Windows Virtual Machine Platform"
      exit 2
   }
}