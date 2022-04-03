echo "Checking if Windows Virtual Machine Platform is enabled ..."
$feature = Get-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform
if ($feature.State -eq "Enabled") 
{
   echo "OK - Windows Virtual Machine Platform is enabled"
}
else
{
   echo "KO - Windows Virtual Machine Platform is disabled"
   exit 1
}
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
   exit 2
}
