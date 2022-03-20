using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    public class Memory
    {
        // https://github.com/Jinjinov/Hardware.Info

        public class MemoryInfo
        {
            /// <summary>
            /// The amount of actual physical memory, in bytes.
            /// </summary>
            public ulong TotalPhysical { get; set; }

            /// <summary>
            /// The amount of physical memory currently available, in bytes. 
            /// This is the amount of physical memory that can be immediately reused without having to write its contents to disk first. 
            /// It is the sum of the size of the standby, free, and zero lists.
            /// </summary>
            public ulong AvailablePhysical { get; set; }
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

        // Native call for macOS

        [DllImport("libc")]
        static extern int sysctlbyname(string name, out IntPtr oldp, ref IntPtr oldlenp, IntPtr newp, IntPtr newlen);

        public static MemoryInfo GetMemoryInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MemoryInfo mem = new MemoryInfo();

                MEMORYSTATUSEX memoryStatusEx = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memoryStatusEx))
                {
                    mem.TotalPhysical = memoryStatusEx.ullTotalPhys;
                    mem.AvailablePhysical = memoryStatusEx.ullAvailPhys;
                }

                return mem;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                MemoryInfo mem = new MemoryInfo();

                string[] meminfo = Command.TryReadFileLines("/proc/meminfo");
                mem.TotalPhysical = GetBytesFromLine(meminfo, "MemTotal:");
                mem.AvailablePhysical = GetBytesFromLine(meminfo, "MemAvailable:");

                return mem;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MemoryInfo mem = new MemoryInfo();

                // TO DO : top -l 1 | grep PhysMem: | awk '{print $10}'

                IntPtr SizeOfLineSize = (IntPtr)IntPtr.Size;
                if (sysctlbyname("hw.memsize", out IntPtr lineSize, ref SizeOfLineSize, IntPtr.Zero, IntPtr.Zero) == 0)
                {
                    mem.TotalPhysical = (ulong)lineSize.ToInt64();
                }

                return mem;
            }
            else
            {
                throw new InvalidOperationException($"Operating system {RuntimeInformation.OSDescription} not supported");
            }
        }        
    }
}
