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
   exit 1
}