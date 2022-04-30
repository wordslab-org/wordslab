using System.Text;

namespace wordslab.manager.os
{
    public static class Storage
    {
        private static Dictionary<string, DriveInfo> drivesInfo;

        public static Dictionary<string,DriveInfo> GetDrivesInfo()
        {
            if (drivesInfo == null)
            {
                drivesInfo = new Dictionary<string, DriveInfo>();
                if (OS.IsWindows)
                {
                    var wmiDisksRequest = "Get-WmiObject Win32_DiskDrive | ForEach-Object { $disk = $_; $partitions = \"ASSOCIATORS OF {Win32_DiskDrive.DeviceID='$($disk.DeviceID)'} WHERE AssocClass = Win32_DiskDriveToDiskPartition\"; Get-WmiObject -Query $partitions | ForEach-Object { $partition = $_; $drives = \"ASSOCIATORS OF  {Win32_DiskPartition.DeviceID='$($partition.DeviceID)'} WHERE AssocClass = Win32_LogicalDiskToPartition\"; Get-WmiObject -Query $drives | ForEach-Object { New-Object -Type PSCustomObject -Property @{ DiskId = $disk.DeviceID; DiskSN = $disk.SerialNumber; DiskSize = $disk.Size; DiskModel = $disk.Model; PartitionId = $partition.Name; PartitionSize = $partition.Size; DrivePath = $_.DeviceID; VolumeName = $_.VolumeName; TotalSize = $_.Size; FreeSpace = $_.FreeSpace; } } } }";

                    var disksProperties = new List<object>();
                    var outputParser = Command.Output.GetList(null, @"(?<name>\w+)\s+:\s+(?<value>.+)\s*$", dict => new KeyValuePair<string, string>(dict["name"], dict["value"]), disksProperties);
                    Command.Run("powershell", $"-EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(wmiDisksRequest))}", outputHandler: outputParser.Run, errorHandler: _ => { });

                    var disksPropEnum = disksProperties.Cast<KeyValuePair<string, string>>().GetEnumerator();
                    while (disksPropEnum.MoveNext())
                    {
                        var driveInfo = new DriveInfo();
                        string serialNumber = null;
                        for (var i = 0; i < 10; i++)
                        {
                            var keyValue = disksPropEnum.Current;
                            switch (keyValue.Key)
                            {
                                case "DiskId":
                                    driveInfo.DiskId = keyValue.Value;
                                    break;
                                case "DiskSN":
                                    serialNumber = keyValue.Value;
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
                            if (i < 9) disksPropEnum.MoveNext();
                        }

                        var wmiMediaTypeRequest = $"Get-WmiObject -Class MSFT_PhysicalDisk -Namespace root\\Microsoft\\Windows\\Storage | Where-Object {{$_.SerialNumber -eq \"{serialNumber}\"}} | Select -ExpandProperty MediaType";
                        Command.Run("powershell", $"-EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(wmiMediaTypeRequest))}",
                           outputHandler: o => driveInfo.IsSSD = o.Trim() == "4", errorHandler: _ => { });

                        drivesInfo.Add(driveInfo.DrivePath, driveInfo);
                    }
                }
                else if (OS.IsLinux)
                {
                    var blockDevices = new List<object>();
                    var outputParser = Command.Output.GetList(null,
                        "{\"type\":\"(?<type>.*)\", \"path\":\"?(?<path>[^\"]*)\"?, \"model\":\"?(?<model>[^\"]*)\"?, \"size\":(?<size>.*), \"rota\":(?<rota>.*), \"mountpoint\":\"?(?<mount>[^\"]*)\"?, \"fstype\":.*, \"label\":\"?(?<label>[^\"]*)\"?, \"partlabel\":.*, \"fssize\":\"?(?<fssize>[^\"]*)\"?, \"fsavail\":\"?(?<fsavail>[^\"]*)\"?}",
                        dict => dict, blockDevices);
                    Command.Run("lsblk", "-I 8 -Jb -o TYPE,PATH,MODEL,SIZE,ROTA,MOUNTPOINT,FSTYPE,LABEL,PARTLABEL,FSSIZE,FSAVAIL", outputHandler: outputParser.Run);

                    string diskId = null;
                    string diskModel = null;
                    bool isSSD = false;
                    uint diskSizeMB = 0;
                    foreach (var dict in blockDevices.Cast<Dictionary<string, string>>())
                    {
                        if (dict["type"] == "disk")
                        {
                            diskId = dict["path"];
                            diskModel = dict["model"];
                            isSSD = dict["rota"] == "true";
                            diskSizeMB = (uint)(long.Parse(dict["size"]) / MEGA);
                        }
                        else if (dict["type"] == "part" && dict["mount"] != "null")
                        {
                            var driveInfo = new DriveInfo();

                            driveInfo.DiskId = diskId;
                            driveInfo.DiskModel = diskModel;
                            driveInfo.IsSSD = isSSD;
                            driveInfo.DiskSizeMB = diskSizeMB;

                            driveInfo.PartitionId = dict["path"];
                            driveInfo.PartitionSizeMB = (uint)(long.Parse(dict["size"]) / MEGA);
                            driveInfo.DrivePath = dict["mount"];
                            driveInfo.VolumeName = dict["label"];
                            if (driveInfo.VolumeName == "null") driveInfo.VolumeName = String.Empty;
                            driveInfo.TotalSizeMB = (uint)(long.Parse(dict["fssize"]) / MEGA);
                            driveInfo.FreeSpaceMB = (uint)(long.Parse(dict["fsavail"]) / MEGA);

                            drivesInfo.Add(driveInfo.DrivePath, driveInfo);
                        }
                    }
                }
                else if (OS.IsMacOS)
                {
                    string[] volumes = null;
                    Command.Run("ls", "-1 /Volumes", outputHandler: o => volumes = o.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

                    string[] systemVolumes = null;
                    Command.Run("ls", "-1 /System/Volumes", outputHandler: o => systemVolumes = o.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

                    var volumesList = new List<string>();
                    foreach (string volume in volumes) { volumesList.Add($"/Volumes/{volume}"); }
                    foreach (string volume in systemVolumes) { volumesList.Add($"/System/Volumes/{volume}"); }

                    var fileSystems = new List<object>();
                    var outputParser = Command.Output.GetList(null, @"(?<filesystem>[\w/]+)\s+(?<blocks>\d+)\s+(?<used>\d+)\s+(?<available>\d+)\s+\d+%\s+(?<mountedon>.+)",
                        dict => dict, fileSystems);
                    Command.Run("df", "-b", outputHandler: outputParser.Run);

                    foreach (var volume in volumesList)
                    {
                        var volumeProps = new List<object>();
                        var outputParser2 = Command.Output.GetList(null, @"\s*(?<key>.+):\s+(?:(?:.*\((?<size>\d+).*\).*)|(?<value>.+))",
                            dict => dict, volumeProps);
                        Command.Run("diskutil", $"info \"{volume}\"", outputHandler: outputParser2.Run);
                        var volumeDict = new Dictionary<string, string>();
                        foreach (var prop in volumeProps.Cast<Dictionary<string, string>>())
                        {
                            volumeDict.Add(prop["key"], prop["value"] != String.Empty ? prop["value"] : prop["size"]);
                        }

                        var fileSystemProps = fileSystems.Cast<Dictionary<string, string>>().Where(dict => dict["mountedon"] == volumeDict["Mount Point"]).First();

                        var diskName = volumeDict["Part of Whole"];

                        var diskProps = new List<object>();
                        var outputParser3 = Command.Output.GetList(null, @"\s*(?<key>.+):\s+(?:(?:.*\((?<size>\d+).*\).*)|(?<value>.+))",
                            dict => dict, diskProps);
                        Command.Run("diskutil", $"info \"{diskName}\"", outputHandler: outputParser3.Run);
                        var diskDict = new Dictionary<string, string>();
                        foreach (var prop in diskProps.Cast<Dictionary<string, string>>())
                        {
                            diskDict.Add(prop["key"], prop["value"] != String.Empty ? prop["value"] : prop["size"]);
                        }

                        var driveInfo = new DriveInfo();

                        driveInfo.DiskId = diskDict["Device Node"];
                        driveInfo.DiskModel = diskDict["Device / Media Name"];
                        driveInfo.IsSSD = diskDict["Solid State"] != "No";
                        driveInfo.DiskSizeMB = (uint)(long.Parse(diskDict["Disk Size"]) / MEGA);

                        driveInfo.PartitionId = volumeDict["Device Node"];
                        driveInfo.PartitionSizeMB = (uint)(long.Parse(volumeDict["Container Total Space"]) / MEGA);

                        driveInfo.DrivePath = volumeDict["Mount Point"];
                        driveInfo.VolumeName = volumeDict["Volume Name"];
                        if (driveInfo.VolumeName == "null") driveInfo.VolumeName = String.Empty;

                        var fileSystem = fileSystems.Cast<Dictionary<string, string>>().Where(fs => fs["mountedon"] == driveInfo.DrivePath).First();

                        driveInfo.TotalSizeMB = (uint)(long.Parse(fileSystem["used"]) * 512 / MEGA);
                        driveInfo.FreeSpaceMB = (uint)(long.Parse(fileSystem["available"]) * 512 / MEGA);
                        driveInfo.TotalSizeMB += driveInfo.FreeSpaceMB;

                        drivesInfo.Add(driveInfo.DrivePath, driveInfo);
                    }
                }
            }
            return drivesInfo;
        }

        public static int GetDirectorySizeMB(DirectoryInfo parentDirectory)
        {
            var directorySize = parentDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            return (int)(directorySize / MEGA);
        }

        public static DriveInfo GetDriveInfoFromPath(string path)
        {
            var parentDirectory = new FileInfo(path).Directory;
            var realDirectory = parentDirectory.ResolveLinkTarget(true);

            var sortedMountPoints = drivesInfo.Keys.OrderByDescending(path => path.Length);
            foreach(var mountPoint in sortedMountPoints)
            {
                if(realDirectory.FullName.StartsWith(mountPoint))
                {
                    var driveInfo = drivesInfo[mountPoint];
                    return driveInfo;
                }
            }
            return null;
        }

        public static bool IsPathOnSSD(string path)
        {
            var driveInfo = Storage.GetDriveInfoFromPath(path);
            if(driveInfo != null)
            {
                return driveInfo.IsSSD;
            }
            else
            {
                return false;
            }
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
        public bool   IsSSD;
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
