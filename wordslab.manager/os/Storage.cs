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
                // TO DO : detect SSD
                /* 
Get-WmiObject Win32_DiskDrive | Select DeviceID,SerialNumber

DeviceID           SerialNumber
--------           ------------
\\.\PHYSICALDRIVE0 0025_38B5_71B9_168C.

Get-WmiObject -Class MSFT_PhysicalDisk -Namespace root\Microsoft\Windows\Storage | Select SerialNumber,MediaType

SerialNumber         MediaType
------------         ---------
0025_38B5_71B9_168C.         4

3 = HDD
4 = SSD
                 */

                var wmiDisksRequest = "Get-WmiObject Win32_DiskDrive | ForEach-Object { $disk = $_; $partitions = \"ASSOCIATORS OF {Win32_DiskDrive.DeviceID='$($disk.DeviceID)'} WHERE AssocClass = Win32_DiskDriveToDiskPartition\"; Get-WmiObject -Query $partitions | ForEach-Object { $partition = $_; $drives = \"ASSOCIATORS OF  {Win32_DiskPartition.DeviceID='$($partition.DeviceID)'} WHERE AssocClass = Win32_LogicalDiskToPartition\"; Get-WmiObject -Query $drives | ForEach-Object { New-Object -Type PSCustomObject -Property @{ DiskId = $disk.DeviceID; DiskSize = $disk.Size; DiskModel = $disk.Model; PartitionId = $partition.Name; PartitionSize = $partition.Size; DrivePath = $_.DeviceID; VolumeName = $_.VolumeName; TotalSize = $_.Size; FreeSpace = $_.FreeSpace; } } } }";

                var disksProperties = new List<object>();
                var outputParser = Command.Output.GetList(null, @"(?<name>\w+)\s+:\s+(?<value>.+)\s*$", dict => new KeyValuePair<string, string>(dict["name"], dict["value"]), disksProperties);
                Command.Run("powershell", $"-EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(wmiDisksRequest))}", outputHandler: outputParser.Run, errorHandler: _ => { });

                var disksPropEnum = disksProperties.Cast<KeyValuePair<string, string>>().GetEnumerator();
                while (disksPropEnum.MoveNext())
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
                        if (i < 8) disksPropEnum.MoveNext();
                    }
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
                foreach (var dict in blockDevices.Cast<Dictionary<string,string>>())
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
                Command.Run("ls", "-1 /Volumes", outputHandler: o => volumes = o.Split(new char[] {'\n','\r'}, StringSplitOptions.RemoveEmptyEntries));

                string[] systemVolumes = null;
                Command.Run("ls", "-1 /System/Volumes", outputHandler: o => volumes = o.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

                var volumesList = new List<string>();
                foreach (string volume in volumes) { volumesList.Add($"/Volumes/{volume}"); }
                foreach (string volume in systemVolumes) { volumesList.Add($"/System/Volumes/{volume}"); }

                var fileSystems = new List<object>();
                var outputParser = Command.Output.GetList(null, @"(?<filesystem>[\w/]+)\s+(?<blocks>\d+)\s+(?<used>\d+)\s+(?<available>\d+)\s+\d+%\s+(?<mountedon>.+)",
                    dict => dict, fileSystems);
                Command.Run("df", "-b", outputHandler: outputParser.Run);

                foreach(var volume in volumesList)
                {
                    var fileSystemProps = fileSystems.Cast<Dictionary<string, string>>().Where(dict => dict["mountedon"] == volume).First();

                    var volumeProps = new List<object>();
                    var outputParser2 = Command.Output.GetList(null, @"\s*(?<key>.+):\s+(?:(?:.*\((?<size>\d+).*\).*)|(?<value>.+))",
                        dict => dict, volumeProps);
                    Command.Run("diskutil", $"info \"{volume}\"", outputHandler: outputParser2.Run);

                    /*
                    var diskName = volumeProps["Part of Whole"];

                    var diskProps = new List<object>();
                    var outputParser3 = Command.Output.GetList(null, @"\s*(?<key>.+):\s+(?:(?:.*\((?<size>\d+).*\).*)|(?<value>.+))",
                        dict => dict, diskProps);
                    Command.Run("diskutil", $"info \"{volume}\"", outputHandler: outputParser2.Run);

                    var driveInfo = new DriveInfo();

                    driveInfo.DiskId = diskId;
                    driveInfo.DiskModel = diskModel;
                    driveInfo.IsSSD = isSSD;
                    driveInfo.DiskSizeMB = diskSizeMB;

                    driveInfo.PartitionId = dict["path"];
                    driveInfo.PartitionSizeMB = (uint)(long.Parse(dict["size"]) / MEGA);
                    driveInfo.DrivePath = dict["mountpoint"];
                    driveInfo.VolumeName = dict["label"];
                    if (driveInfo.VolumeName == "null") driveInfo.VolumeName = String.Empty;
                    driveInfo.TotalSizeMB = (uint)(long.Parse(dict["fssize"]) / MEGA);
                    driveInfo.FreeSpaceMB = (uint)(long.Parse(dict["fsavail"]) / MEGA);

                    drivesInfo.Add(driveInfo.DrivePath, driveInfo);
                    */
                }

                /*
                
                diskutil info /Volumes/macOS

   Device Identifier:         disk2s5
   Device Node:               /dev/disk2s5
   Part of Whole:             disk2

   Volume Name:               macOS
   Mounted:                   Yes
   Mount Point:               /

   File System Personality:   APFS
   Type (Bundle):             apfs
   Name (User Visible):       APFS

   Media Type:                Generic

   Disk Size:                 53.3 GB (53343117312 Bytes) (exactly 104185776 512-Byte-Units)

   Container Total Space:     53.3 GB (53343117312 Bytes) (exactly 104185776 512-Byte-Units)
   Container Free Space:      18.4 GB (18405576704 Bytes) (exactly 35948392 512-Byte-Units)
 
   Read-Only Media:           No

   Device Location:           Internal
   Removable Media:           Fixed

   Solid State:               No

   APFS Container:            disk2

                    diskutil info disk2

   Device Identifier:         disk2
   Device Node:               /dev/disk2
   Device / Media Name:       QEMU HARDDISK
                 
   Disk Size:                 53.3 GB (53343117312 Bytes) (exactly 104185776 512-Byte-Units)
   Device Block Size:         4096 Bytes

   Read-Only Media:           No

   Device Location:           Internal
   Removable Media:           Fixed

   Solid State:               No

                df -b  ( -b      Use (the default) 512-byte blocks.)

Filesystem    512-blocks     Used Available Capacity  Mounted on
/dev/disk2s5   104185776 21657432  35949288    38%    /
devfs                374      374         0   100%    /dev
/dev/disk2s1   104185776 45265056  35949288    56%    /System/Volumes/Data
/dev/disk2s4   104185776     2088  35949288     1%    /private/var/vm
map auto_home          0        0         0   100%    /System/Volumes/Data/home



                DiskId = Device Node:               /dev/disk2
                DiskModel = Device / Media Name:       QEMU HARDDISK
                IsSSD = Solid State:               No
                DiskSizeMB = Disk Size:                 53.3 GB (53343117312 Bytes) (exactly 104185776 512-Byte-Units)

                PartitionId = Device Node:               /dev/disk2s5
                PartitionSizeMB = Container Total Space:     53.3 GB (53343117312 Bytes) (exactly 104185776 512-Byte-Units)

                DrivePath = "mountpoint":"/"
                VolumeName = "label":"linuxdata" / "label":null
                TotalSizeMB = df: Used 21657432 + Avaimable 35949288
                FreeSpaceMB = 18.4 GB (18405576704 Bytes) (exactly 35948392 512-Byte-Units)           
                 */
            }
            return drivesInfo;
        }

        public static int GetDirectorySizeMB(DirectoryInfo parentDirectory)
        {
            var directorySize = parentDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            return (int)(directorySize / MEGA);
        }

        private const uint MEGA = 1000 * 1000;

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
