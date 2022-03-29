using System.Text;

namespace wordslab.manager.os
{
    public static class Storage
    {
        public static Dictionary<string,DriveInfo> GetDrivesInfo()
        {
            var drivesInfo = new Dictionary<string,DriveInfo>();
            if (OS.IsWindows)
            {
                var wmiDisksRequest = "Get-WmiObject Win32_DiskDrive | ForEach-Object { $disk = $_; $partitions = \"ASSOCIATORS OF {Win32_DiskDrive.DeviceID='$($disk.DeviceID)'} WHERE AssocClass = Win32_DiskDriveToDiskPartition\"; Get-WmiObject -Query $partitions | ForEach-Object { $partition = $_; $drives = \"ASSOCIATORS OF  {Win32_DiskPartition.DeviceID='$($partition.DeviceID)'} WHERE AssocClass = Win32_LogicalDiskToPartition\"; Get-WmiObject -Query $drives | ForEach-Object { New-Object -Type PSCustomObject -Property @{ DiskId = $disk.DeviceID; DiskSize = $disk.Size; DiskModel = $disk.Model; PartitionId = $partition.Name; PartitionSize = $partition.Size; DrivePath = $_.DeviceID; VolumeName = $_.VolumeName; TotalSize = $_.Size; FreeSpace = $_.FreeSpace; } } } }";

                var disksProperties = new List<object>(); 
                var outputParser = Command.Output.GetList(null, @"(?<name>\w+)\s+:\s+(?<value>.+)\s*$", dict => new KeyValuePair<string,string>(dict["name"],dict["value"]) , disksProperties);
                Command.Run("powershell", $"-EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(wmiDisksRequest))}", outputHandler: outputParser.Run, errorHandler: _=>{});

                var disksPropEnum = disksProperties.Cast<KeyValuePair<string, string>>().GetEnumerator();
                while(disksPropEnum.MoveNext())
                {
                    var driveInfo = new DriveInfo();
                    for (var i = 0; i < 9; i++)
                    {
                        var keyValue = disksPropEnum.Current;
                        switch (keyValue.Key)
                        {
                            case "DiskId":
                                driveInfo.DiskId = keyValue.Value;
                                break;
                            case "DiskSize":
                                driveInfo.DiskSizeMB = (uint)(ulong.Parse(keyValue.Value) / MEGA);
                                break;
                            case "DiskModel":
                                driveInfo.DiskModel = keyValue.Value;
                                break;
                            case "PartitionId":
                                driveInfo.PartitionId = keyValue.Value;
                                break;
                            case "PartitionSize":
                                driveInfo.PartitionSizeMB = (uint)(ulong.Parse(keyValue.Value) / MEGA);
                                break;
                            case "DrivePath":
                                driveInfo.DrivePath = keyValue.Value;
                                break;
                            case "VolumeName":
                                driveInfo.VolumeName = keyValue.Value;
                                break;
                            case "TotalSize":
                                driveInfo.TotalSizeMB = (uint)(ulong.Parse(keyValue.Value) / MEGA);
                                break;
                            case "FreeSpace":
                                driveInfo.FreeSpaceMB = (uint)(ulong.Parse(keyValue.Value) / MEGA);
                                break;
                        }
                        if(i<8) disksPropEnum.MoveNext();
                    }
                    drivesInfo.Add(driveInfo.DrivePath, driveInfo);        
                }
            }
            return drivesInfo;
        }

        public static int GetDirectorySizeMB(DirectoryInfo parentDirectory)
        {
            var directorySize = parentDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            return (int)(directorySize / MEGA);
        }

        private const uint MEGA = 1024 * 1024;

        public static DirectoryInfo GetApplicationDirectory()
        {
            return new DirectoryInfo(AppContext.BaseDirectory);
        }
    }

    public class DriveInfo
    {
        public string DiskId;
        public string DiskModel;
        public uint   DiskSizeMB;

        public string PartitionId;
        public uint   PartitionSizeMB;

        public string DrivePath;
        public string VolumeName;

        public uint   TotalSizeMB;
        public uint   FreeSpaceMB;
        public byte   PercentUsedSpace => (byte)((TotalSizeMB - FreeSpaceMB) * 100 / TotalSizeMB);
    }
}
