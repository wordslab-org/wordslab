$exitcode = 0

echo "Checking if Windows Subsystem for Linux is disabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux

if ($feature.State -eq "Disabled") 
{
   echo "OK - Windows Subsystem for Linux is disabled"
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
         $exitcode = 1
      }
      else
      {
         echo "Windows Subsystem for Linux is disabled"
         $exitcode = 0
      }
   }
   catch
   {
      echo "Failed to disale Windows Subsystem for Linux"
      $exitcode = 2
   }
}

echo "Checking if Windows Virtual Machine Platform is disabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform

if ($feature.State -eq "Disabled") 
{
   echo "OK - Windows Virtual Machine Platform is disabled"
   $exitcode = $exitcode + 0
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
         $exitcode = $exitcode + 4
      }
      else
      {
         echo "Windows Virtual Machine Platform is disabled"
         $exitcode = $exitcode + 0
      }
   }
   catch
   {
      echo "Failed to disale Windows Virtual Machine Platform"
      $exitcode = $exitcode + 8
   }
}

exit $exitcode