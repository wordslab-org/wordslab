using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    public class Memory
    {
        // https://github.com/Jinjinov/Hardware.Info

        public class MemoryInfo
        {
            /// <summary>
            /// The amount of actual physical memory.
            /// </summary>
            public ulong TotalPhysicalMB { get; set; }

            /// <summary>
            /// The amount of physical memory currently available. 
            /// This is the amount of physical memory that can be immediately reused without having to write its contents to disk first. 
            /// It is the sum of the size of the standby, free, and zero lists.
            /// </summary>
            public ulong FreePhysicalMB { get; set; }

            /// <summary>
            /// The amount of physical memory currently used by running processes. 
            /// </summary>
            public ulong UsedPhysicalMB { get { return TotalPhysicalMB - FreePhysicalMB; } }
        }

        // Native call for Windows 

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        // Utility method for Linux

        private static ulong GetBytesFromLine(string[] meminfo, string token)
        {
            const string KbToken = "kB";

            string? memLine = meminfo.FirstOrDefault(line => line.StartsWith(token) && line.EndsWith(KbToken));

            if (memLine != null)
            {
                string mem = memLine.Replace(token, string.Empty).Replace(KbToken, string.Empty).Trim();

                if (ulong.TryParse(mem, out ulong memKb))
                    return memKb * 1024;
            }

            return 0;
        }

        private const uint MEGA = 1024 * 1024;

        public static MemoryInfo GetMemoryInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MemoryInfo mem = new MemoryInfo();

                MEMORYSTATUSEX memoryStatusEx = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memoryStatusEx))
                {
                    mem.TotalPhysicalMB = memoryStatusEx.ullTotalPhys / MEGA;
                    mem.FreePhysicalMB = memoryStatusEx.ullAvailPhys / MEGA;
                }

                return mem;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                MemoryInfo mem = new MemoryInfo();

                string[] meminfo = Command.TryReadFileLines("/proc/meminfo");
                mem.TotalPhysicalMB = GetBytesFromLine(meminfo, "MemTotal:") / MEGA;
                mem.FreePhysicalMB = GetBytesFromLine(meminfo, "MemAvailable:") / MEGA;

                return mem;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                uint usedMem = 0;
                uint freeMem = 0;

                var outputParser = Command.Output.GetValue(@"PhysMem: (\d+)M used", value => usedMem = uint.Parse(value))
                                                 .GetValue(@"PhysMem: .* (\d+)M unused", value => freeMem = uint.Parse(value));

                Command.Run("top", "-l 1", outputHandler: outputParser.Run);
                
                MemoryInfo mem = new MemoryInfo();
                mem.TotalPhysicalMB = usedMem + freeMem;
                mem.FreePhysicalMB = freeMem;
                return mem;
            }
            else
            {
                throw new InvalidOperationException($"Operating system {RuntimeInformation.OSDescription} not supported");
            }
        }        
    }
}
