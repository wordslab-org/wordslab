namespace wordslab.manager.os
{
    public static class Storage
    {
        public static Dictionary<string,DriveStatus> GetDrivesStatus()
        {
            var drivesStatus = new Dictionary<string,DriveStatus>();
            foreach (var drive in DriveInfo.GetDrives())
            {                
                if (drive.IsReady == true && (drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Network))
                {
                    var status = new DriveStatus();
                    status.Name = drive.Name;
                    status.Label = drive.VolumeLabel;
                    status.RootDirectory = drive.RootDirectory.FullName;
                    status.TotalSizeMB = (int)(drive.TotalSize / 1048576);
                    status.FreeSpaceMB = (int)(drive.AvailableFreeSpace / 1048576);
                    status.PercentUsedSpace = 100-(int)(((float)drive.AvailableFreeSpace / drive.TotalSize) * 100);
                    status.IsNetworkDrive = drive.DriveType == DriveType.Network;

                    drivesStatus[drive.Name] = status;
                }
            }
            return drivesStatus;
        }

        public static int GetDirectorySizeMB(DirectoryInfo parentDirectory)
        {
            var directorySize = parentDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            return (int)(directorySize / 1048576);
        }

        public static DirectoryInfo GetApplicationDirectory()
        {
            return new DirectoryInfo(AppContext.BaseDirectory);
        }
    }

    public class DriveStatus
    {
        public string Name;
        public string Label;
        public string RootDirectory;
        public bool IsNetworkDrive;

        public int TotalSizeMB;
        public int FreeSpaceMB;
        public int PercentUsedSpace;
    }
}
