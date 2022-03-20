using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    public class Linux
    {
        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.
        // => extract from PlatformDetection class
        public static string GetOSDistribution()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var distribution = "Linux";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
                {
                    distribution = "FreeBSD";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ILLUMOS")))
                {
                    var versionDescription = RuntimeInformation.OSDescription.Split(' ')[2];
                    switch (versionDescription)
                    {
                        case string version when version.StartsWith("omnios"):
                            distribution = "OmniOS";
                            break;
                        case string version when version.StartsWith("joyent"):
                            distribution = "SmartOS";
                            break;
                        case string version when version.StartsWith("illumos"):
                            distribution = "OpenIndiana";
                            break;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("SOLARIS")))
                {
                    distribution = "Solaris";
                }
                else if (File.Exists("/etc/os-release"))
                {
                    foreach (string line in File.ReadAllLines("/etc/os-release"))
                    {
                        if (line.StartsWith("ID=", StringComparison.Ordinal))
                        {
                            distribution = line.Substring(3).Trim('"', '\'');
                            break;
                        }
                    }
                }
                return distribution;
            }
            else {  return String.Empty; }
        }

        public static bool IsLinuxVersionUbuntu1804OrHigher()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {                
                var targetVersion = new Version(18,04);
                return GetOSDistribution() == "ubuntu" && OS.GetOSVersion() >= targetVersion;
            }
            else { return false; }
        }
    }
}
