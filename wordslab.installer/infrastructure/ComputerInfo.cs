using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace wordslab.installer.infrastructure
{
    // https://github.com/NickStrupat/ComputerInfo
    public class ComputerInfo
    {
        static ComputerInfo()
        {
            if (IsWindows)
            {
                GetTotalPhysicalMemory = Windows.GetTotalPhysicalMemory;
                GetAvailablePhysicalMemory = Windows.GetAvailablePhysicalMemory;
                GetTotalVirtualMemory = Windows.GetTotalVirtualMemory;
                GetAvailableVirtualMemory = Windows.GetAvailableVirtualMemory;
            }
            else if (IsMacOS)
            {
                GetTotalPhysicalMemory = MacOS.GetTotalPhysicalMemory;
                GetAvailablePhysicalMemory = MacOS.GetAvailablePhysicalMemory;
                GetTotalVirtualMemory = MacOS.GetTotalVirtualMemory;
                GetAvailableVirtualMemory = MacOS.GetAvailableVirtualMemory;
            }
            else if (IsLinux)
            {
                GetTotalPhysicalMemory = Linux.GetTotalPhysicalMemory;
                GetAvailablePhysicalMemory = Linux.GetAvailablePhysicalMemory;
                GetTotalVirtualMemory = Linux.GetTotalVirtualMemory;
                GetAvailableVirtualMemory = Linux.GetAvailableVirtualMemory;
            }
            else
                throw new PlatformNotSupportedException();
        }

        private static readonly Boolean IsWindows = RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        private static readonly Boolean IsMacOS = RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
        private static readonly Boolean IsLinux = RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

        private static readonly Func<UInt64> GetTotalPhysicalMemory;
        private static readonly Func<UInt64> GetAvailablePhysicalMemory;
        private static readonly Func<UInt64> GetTotalVirtualMemory;
        private static readonly Func<UInt64> GetAvailableVirtualMemory;

        public UInt64 TotalPhysicalMemory => GetTotalPhysicalMemory.Invoke();
        public UInt64 AvailablePhysicalMemory => GetAvailablePhysicalMemory.Invoke();
        public UInt64 TotalVirtualMemory => GetTotalVirtualMemory.Invoke();
        public UInt64 AvailableVirtualMemory => GetAvailableVirtualMemory.Invoke();

        public CultureInfo InstalledUICulture => CultureInfo.InstalledUICulture;
        public String OSPlatform => Environment.OSVersion.Platform.ToString();
        public String OSVersion => Environment.OSVersion.Version.ToString();

        internal static class Windows
        {
            public static UInt64 GetTotalPhysicalMemory() => MemoryStatus.TotalPhysicalMemory;
            public static UInt64 GetAvailablePhysicalMemory() => MemoryStatus.AvailablePhysicalMemory;
            public static UInt64 GetTotalVirtualMemory() => MemoryStatus.TotalVirtualMemory;
            public static UInt64 GetAvailableVirtualMemory() => MemoryStatus.AvailableVirtualMemory;

            private static InternalMemoryStatus internalMemoryStatus;
            private static InternalMemoryStatus MemoryStatus => internalMemoryStatus ?? (internalMemoryStatus = new InternalMemoryStatus());

            private class InternalMemoryStatus
            {
                private readonly Boolean isOldOS;
                private MEMORYSTATUS memoryStatus;
                private MEMORYSTATUSEX memoryStatusEx;

                internal InternalMemoryStatus()
                {
                    isOldOS = Environment.OSVersion.Version.Major < 5;
                }

                internal UInt64 TotalPhysicalMemory
                {
                    get
                    {
                        Refresh();
                        return !isOldOS ? memoryStatusEx.ullTotalPhys : memoryStatus.dwTotalPhys;
                    }
                }

                internal UInt64 AvailablePhysicalMemory
                {
                    get
                    {
                        Refresh();
                        return !isOldOS ? memoryStatusEx.ullAvailPhys : memoryStatus.dwAvailPhys;
                    }
                }

                internal UInt64 TotalVirtualMemory
                {
                    get
                    {
                        Refresh();
                        return !isOldOS ? memoryStatusEx.ullTotalVirtual : memoryStatus.dwTotalVirtual;
                    }
                }

                internal UInt64 AvailableVirtualMemory
                {
                    get
                    {
                        Refresh();
                        return !isOldOS ? memoryStatusEx.ullAvailVirtual : memoryStatus.dwAvailVirtual;
                    }
                }

                private void Refresh()
                {
                    if (isOldOS)
                    {
                        memoryStatus = new MEMORYSTATUS();
                        GlobalMemoryStatus(ref memoryStatus);
                    }
                    else
                    {
                        memoryStatusEx = new MEMORYSTATUSEX();
                        memoryStatusEx.Init();
                        if (!GlobalMemoryStatusEx(ref memoryStatusEx))
                            throw new Win32Exception("Could not obtain memory information due to internal error.");
                    }
                }
            }

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern void GlobalMemoryStatus(ref MEMORYSTATUS lpBuffer);

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern Boolean GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

            internal struct MEMORYSTATUS
            {
                internal UInt32 dwLength;
                internal UInt32 dwMemoryLoad;
                internal UInt32 dwTotalPhys;
                internal UInt32 dwAvailPhys;
                internal UInt32 dwTotalPageFile;
                internal UInt32 dwAvailPageFile;
                internal UInt32 dwTotalVirtual;
                internal UInt32 dwAvailVirtual;
            }

            internal struct MEMORYSTATUSEX
            {
                internal UInt32 dwLength;
                internal UInt32 dwMemoryLoad;
                internal UInt64 ullTotalPhys;
                internal UInt64 ullAvailPhys;
                internal UInt64 ullTotalPageFile;
                internal UInt64 ullAvailPageFile;
                internal UInt64 ullTotalVirtual;
                internal UInt64 ullAvailVirtual;
                internal UInt64 ullAvailExtendedVirtual;

                internal void Init()
                {
                    dwLength = checked((UInt32)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
                }
            }
        }

        internal static class MacOS
        {
            public static UInt64 GetTotalPhysicalMemory() => GetSysCtlIntegerByName("hw.memsize"); // HW_NCPU
            public static UInt64 GetAvailablePhysicalMemory() => throw new NotImplementedException();
            public static UInt64 GetTotalVirtualMemory() => throw new NotImplementedException();
            public static UInt64 GetAvailableVirtualMemory() => throw new NotImplementedException();

            private static IntPtr SizeOfLineSize = (IntPtr)IntPtr.Size;

            public static UInt64 GetSysCtlIntegerByName(String name)
            {
                sysctlbyname(name, out var lineSize, ref SizeOfLineSize, IntPtr.Zero, IntPtr.Zero);
                return (UInt64)lineSize.ToInt64();
            }

            [DllImport("libc")]
            private static extern Int32 sysctlbyname(String name, out IntPtr oldp, ref IntPtr oldlenp, IntPtr newp, IntPtr newlen);
        }

        internal static class Linux
        {
            public static UInt64 GetTotalPhysicalMemory() => GetBytesFromLine("MemTotal:");
            public static UInt64 GetAvailablePhysicalMemory() => GetBytesFromLine("MemFree:");
            public static UInt64 GetTotalVirtualMemory() => GetBytesFromLine("SwapTotal:");
            public static UInt64 GetAvailableVirtualMemory() => GetBytesFromLine("SwapFree:");

            private static String[] GetProcMemInfoLines() => File.ReadAllLines("/proc/meminfo");

            private static UInt64 GetBytesFromLine(String token)
            {
                const String KbToken = "kB";
                var memTotalLine = GetProcMemInfoLines().FirstOrDefault(x => x.StartsWith(token))?.Substring(token.Length);
                if (memTotalLine != null && memTotalLine.EndsWith(KbToken) && UInt64.TryParse(memTotalLine.Substring(0, memTotalLine.Length - KbToken.Length), out var memKb))
                    return memKb * 1024;
                throw new Exception();
            }
        }
    }
}
