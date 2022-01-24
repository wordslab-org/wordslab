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
   exit 1
}