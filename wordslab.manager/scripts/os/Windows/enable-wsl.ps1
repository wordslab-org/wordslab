$exitcode = 0

echo "Checking if Windows Virtual Machine Platform is enabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform

if ($feature.State -eq "Enabled") 
{
   echo "OK - Windows Virtual Machine Platform is enabled"
   $exitcode = 0
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
         $exitcode = 1
      }
      else
      {
         echo "Windows Virtual Machine Platform is enabled"
         $exitcode = 0
      }
   }
   catch
   {
      echo "Failed to enable Windows Virtual Machine Platform"
      $exitcode = 2
   }
}

if ($exitcode -eq 2)
{
    exit $exitcode
}

echo "Checking if Windows Subsystem for Linux is enabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux

if ($feature.State -eq "Enabled") 
{
   echo "OK - Windows Subsystem for Linux is enabled"
   $exitcode = $exitcode + 0
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
         if ($exitcode -eq 0)
         {
            $exitcode = 1
         }
      }
      else
      {
         echo "Windows Subsystem for Linux is enabled"
         $exitcode = $exitcode + 0
      }
   }
   catch
   {
      echo "Failed to enable Windows Subsystem for Linux"
      $exitcode = 2
   }
}

exit $exitcode