using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;

namespace wordslab.manager.os
{
    public class Compute
    {
        // https://github.com/Jinjinov/Hardware.Info

        public class CPUInfo
        {            
            /// <summary>
            /// Name of the processor manufacturer.
            /// </summary>
            public string Manufacturer { get; set; } = string.Empty;

            /// <summary>
            /// Commercial name and main properties of the CPU
            /// </summary>
            public string ModelName { get; set; } = string.Empty;

            /// <summary>
            /// Number of cores for the current instance of the processor. A core is a physical processor on the integrated circuit. For example, in a dual-core processor this property has a value of 2.
            /// </summary>
            public UInt32 NumberOfCores { get; set; }

            /// <summary>
            /// Number of logical processors for the current instance of the processor. For processors capable of hyperthreading, this value includes only the processors which have hyperthreading enabled.
            /// </summary>
            public UInt32 NumberOfLogicalProcessors { get; set; }

            /// <summary>
            /// Maximum speed of the processor, in MHz.
            /// </summary>
            public UInt32 MaxClockSpeedMhz { get; set; }

            /// <summary>
            /// Size of the Level 2 processor cache. A Level 2 cache is an external memory area that has a faster access time than the main RAM memory.
            /// </summary>
            public UInt32 L2CacheSizeKB { get; set; }

            /// <summary>
            /// Size of the Level 3 processor cache. A Level 3 cache is an external memory area that has a faster access time than the main RAM memory.
            /// </summary>
            public UInt32 L3CacheSizeKB { get; set; }

            /// <summary>
            /// Processor information that describes the processor features. 
            /// For an x86 class CPU, the field format depends on the processor support of the CPUID instruction. 
            /// If the instruction is supported, the property contains 2 (two) DWORD formatted values. 
            /// The first is an offset of 08h-0Bh, which is the EAX value that a CPUID instruction returns with input EAX set to 1. 
            /// The second is an offset of 0Ch-0Fh, which is the EDX value that the instruction returns. 
            /// Only the first two bytes of the property are significant and contain the contents of the DX register at CPU reset—all others are set to 0 (zero), and the contents are in DWORD format.
            /// </summary>
            public string FeatureFlags { get; set; } = string.Empty;
        }

        // Utility methods for Windows Management Infrastructure

        private static string _managementScope = "root\\cimv2";
        private static System.Management.EnumerationOptions _enumerationOptions = new System.Management.EnumerationOptions() { ReturnImmediately = true, Rewindable = false, Timeout = System.Management.EnumerationOptions.InfiniteTimeout };

        private static T GetPropertyValue<T>(object obj) where T : struct
        {
            return (obj == null) ? default(T) : (T)obj;
        }
        private static string GetPropertyString(object obj)
        {
            return (obj is string str) ? str : string.Empty;
        }

        // https://en.wikipedia.org/wiki/CPUID
        private static string GetFeatureFlagsFromCpuId()
        {
            StringBuilder sb = new StringBuilder();

            if (RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                (int eax1, int ebx1, int ecx1, int edx1) = X86Base.CpuId(0x00000001, 0x00000000);
                (int eax7, int ebx7, int ecx7, int edx7) = X86Base.CpuId(0x00000007, 0x00000000);
                (int eax8, int ebx8, int ecx8, int edx8) = X86Base.CpuId(unchecked((int)0x80000001), 0x00000000);
                
                for (byte pos = 0; pos < 32; pos++)
                {
                    if ((edx1 & (1 << pos)) != 0)
                    {
                        sb.Append(CpuIdFlags.edx1Feature[pos]);
                        sb.Append(' ');
                    }
                }
                for (byte pos = 11; pos < 32; pos++)
                {
                    if ((edx8 & (1 << pos)) != 0)
                    {
                        sb.Append(CpuIdFlags.edx8Feature[pos]);
                        sb.Append(' ');
                    }
                }
                for (byte pos = 0; pos < 32; pos++)
                {
                    if ((ecx1 & (1 << pos)) != 0)
                    {
                        sb.Append(CpuIdFlags.ecx1Feature[pos]);
                        sb.Append(' ');
                    }
                }
                for (byte pos = 0; pos < 32; pos++)
                {
                    if ((ecx8 & (1 << pos)) != 0)
                    {
                        sb.Append(CpuIdFlags.ecx8Feature[pos]);
                        sb.Append(' ');
                    }
                }
                for (byte pos = 0; pos < 32; pos++)
                {
                    if ((ebx7 & (1 << pos)) != 0)
                    {
                        sb.Append(CpuIdFlags.ebx7Feature[pos]);
                        sb.Append(' ');
                    }
                }
            }

            return sb.ToString();
        }

        public static CPUInfo GetCPUInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-processor

                string query = "SELECT Manufacturer, Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize FROM Win32_Processor";
                using ManagementObjectSearcher mos = new ManagementObjectSearcher(_managementScope, query, _enumerationOptions);
                using var mociter = mos.Get().GetEnumerator();
                mociter.MoveNext();
                ManagementObject mo = (ManagementObject)mociter.Current;
                CPUInfo cpu = new CPUInfo
                {
                    Manufacturer = GetPropertyString(mo["Manufacturer"]),
                    ModelName = GetPropertyString(mo["Name"]),
                    NumberOfCores = GetPropertyValue<uint>(mo["NumberOfCores"]),
                    NumberOfLogicalProcessors = GetPropertyValue<uint>(mo["NumberOfLogicalProcessors"]),
                    MaxClockSpeedMhz = GetPropertyValue<uint>(mo["MaxClockSpeed"]),
                    L2CacheSizeKB = GetPropertyValue<uint>(mo["L2CacheSize"]),
                    L3CacheSizeKB = GetPropertyValue<uint>(mo["L3CacheSize"]),                                        
                    FeatureFlags = GetFeatureFlagsFromCpuId()
                };
                return cpu;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string[] lines = Command.TryReadFileLines("/proc/cpuinfo");

                Regex vendorIdRegex = new Regex(@"^vendor_id\s+:\s+(.+)");
                Regex modelNameRegex = new Regex(@"^model name\s+:\s+(.+)");
                Regex cpuSpeedRegex = new Regex(@"^cpu MHz\s+:\s+(.+)");
                Regex cacheSizeRegex = new Regex(@"^cache size\s+:\s+(.+)\s+KB");
                Regex physicalCoresRegex = new Regex(@"^cpu cores\s+:\s+(.+)");
                Regex logicalCoresRegex = new Regex(@"^siblings\s+:\s+(.+)");

                CPUInfo cpu = new CPUInfo();
                foreach (string line in lines)
                {
                    Match match = vendorIdRegex.Match(line);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        cpu.Manufacturer = match.Groups[1].Value.Trim();
                        continue;
                    }

                    match = modelNameRegex.Match(line);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        cpu.ModelName = match.Groups[1].Value.Trim();
                        continue;
                    }

                    match = physicalCoresRegex.Match(line);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        if (uint.TryParse(match.Groups[1].Value, out uint numberOfCores))
                            cpu.NumberOfCores = numberOfCores;
                        continue;
                    }

                    match = logicalCoresRegex.Match(line);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        if (uint.TryParse(match.Groups[1].Value, out uint numberOfLogicalProcessors))
                            cpu.NumberOfLogicalProcessors = numberOfLogicalProcessors;
                        continue;
                    }

                    match = cpuSpeedRegex.Match(line);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        if (double.TryParse(match.Groups[1].Value, out double currentClockSpeed))
                            cpu.MaxClockSpeedMhz = (uint)currentClockSpeed;
                        continue;
                    }

                    // TO DO : missing L2 cache size 

                    match = cacheSizeRegex.Match(line);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        if (uint.TryParse(match.Groups[1].Value, out uint cacheSize))
                            cpu.L3CacheSizeKB = cacheSize;
                        continue;
                    }

                    // TO DO : missing ProcessorId 
                }
                return cpu;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                CPUInfo cpu = new CPUInfo();

                string processOutput = null;
                Command.Run("sysctl", "-n machdep.cpu.brand_string", outputHandler: output => processOutput=output);
                
                // TO DO : missing Manufacturer
                
                cpu.ModelName = processOutput;

                Command.Run("sysctl", "-n hw.physicalcpu", outputHandler: output => processOutput = output);
                if (uint.TryParse(processOutput, out uint numberOfCores))
                    cpu.NumberOfCores = numberOfCores;

                Command.Run("sysctl", "-n hw.logicalcpu", outputHandler: output => processOutput = output);
                if (uint.TryParse(processOutput, out uint numberOfLogicalProcessors))
                    cpu.NumberOfLogicalProcessors = numberOfLogicalProcessors;

                string[] info = processOutput.Split('@');
                if (info.Length > 1)
                {
                    string speedString = info[1].Trim();
                    uint speed = 0;

                    if (speedString.EndsWith("GHz"))
                    {
                        string number = speedString.Replace("GHz", string.Empty).Trim();
                        if (uint.TryParse(number, out speed))
                            speed *= 1000;
                    }
                    else if (speedString.EndsWith("KHz"))
                    {
                        string number = speedString.Replace("KHz", string.Empty).Trim();
                        if (uint.TryParse(number, out speed))
                            speed /= 1000;
                    }
                    else if (speedString.EndsWith("MHz"))
                    {
                        string number = speedString.Replace("MHz", string.Empty).Trim();
                        uint.TryParse(number, out speed);
                    }

                    cpu.ModelName = info[0];
                    cpu.MaxClockSpeedMhz = speed;
                }

                Command.Run("sysctl", "-n hw.l2cachesize", outputHandler: output => processOutput = output);
                if (uint.TryParse(processOutput, out uint L2CacheSize))
                    cpu.L2CacheSizeKB = L2CacheSize;

                Command.Run("sysctl", "-n hw.l3cachesize", outputHandler: output => processOutput = output);
                if (uint.TryParse(processOutput, out uint L3CacheSize))
                    cpu.L3CacheSizeKB = L3CacheSize;

                // TO DO : missing ProcessorId

                return cpu;
            }
            else
            {
                throw new InvalidOperationException($"Operating system {RuntimeInformation.OSDescription} not supported");
            }
        }

        /// <summary>
        /// % Processor Time is the percentage of elapsed time that the processor spends to execute a non-Idle thread. 
        /// It is calculated by measuring the percentage of time that the processor spends executing the idle thread and then subtracting that value from 100%. 
        /// (Each processor has an idle thread that consumes cycles when no other threads are ready to run). 
        /// This counter is the primary indicator of processor activity, and displays the average percentage of busy time observed during the sample interval. 
        /// It should be noted that the accounting calculation of whether the processor is idle is performed at an internal sampling interval of the system clock (10ms). 
        /// On todays fast processors, % Processor Time can therefore underestimate the processor utilization as the processor may be spending a lot of time servicing threads between the system clock sampling interval. 
        /// Workload based timer applications are one example of applications which are more likely to be measured inaccurately as timers are signaled just after the sample is taken.
        /// </summary>
        public static int GetPercentCPUTime() 
        {
            int percentProcessorTime = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string QueryString = "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name = '_Total'";
                using ManagementObjectSearcher mos = new ManagementObjectSearcher(_managementScope, QueryString, _enumerationOptions);
                using var mociter = mos.Get().GetEnumerator();
                mociter.MoveNext();
                ManagementObject mo = (ManagementObject)mociter.Current;
                percentProcessorTime = (int)GetPropertyValue<ulong>(mo["PercentProcessorTime"]);
                
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string[] cpuUsageLineLast = Command.TryReadFileLines("/proc/stat");
                Task.Delay(500).Wait();
                string[] cpuUsageLineNow = Command.TryReadFileLines("/proc/stat");
                if (cpuUsageLineLast.Length > 0 && cpuUsageLineNow.Length > 0)
                {
                    var cpuStatLast = cpuUsageLineLast[0];
                    var cpuStatNow = cpuUsageLineNow[0];

                    char[] charSeparators = new char[] { ' ' };

                    // Get all columns but skip the first (which is the "cpu" string) 
                    List<string> cpuSumLine = cpuStatNow.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                    cpuSumLine.RemoveAt(0);

                    // Get all columns but skip the first (which is the "cpu" string) 
                    List<string> cpuLastSumLine = cpuStatLast.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                    cpuLastSumLine.RemoveAt(0);

                    ulong cpuSum = 0;
                    cpuSumLine.ForEach(s => cpuSum += Convert.ToUInt64(s));

                    ulong cpuLastSum = 0;
                    cpuLastSumLine.ForEach(s => cpuLastSum += Convert.ToUInt64(s));

                    // Get the delta between two reads 
                    ulong cpuDelta = cpuSum - cpuLastSum;
                    // Get the idle time Delta 
                    ulong cpuIdle = Convert.ToUInt64(cpuSumLine[3]) - Convert.ToUInt64(cpuLastSumLine[3]);
                    // Calc percentage 
                    ulong cpuUsed = cpuDelta - cpuIdle;

                    percentProcessorTime = (int)(100 * cpuUsed / cpuDelta);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                throw new InvalidOperationException($"Operating system {RuntimeInformation.OSDescription} not supported");
            }
            else
            {
                throw new InvalidOperationException($"Operating system {RuntimeInformation.OSDescription} not supported");
            }
            return percentProcessorTime;
        }

        public class GPUInfo
        {
            public GPUInfo(int index, string name, int memory)
            {
                Index = index;
                Name = name;
                MemoryMB = memory;
            }

            public int Index { get; set; }
            public string Name { get; set; }
            public int MemoryMB { get; set; }
            public GPUArchitectureInfo Architecture
            {
                get
                {
                    if (Name.Contains("GeForce GTX 10") || Name.Contains("TITAN X"))
                    {
                        return GPUArchitectureInfo.Pascal;
                    }
                    else if (Name.Contains("TITAN V"))
                    {
                        return GPUArchitectureInfo.Volta;
                    }
                    else if (Name.Contains("GeForce GTX 16") || Name.Contains("GeForce RTX 20") || Name.Contains("TITAN RTX"))
                    {
                        return GPUArchitectureInfo.Turing;
                    }
                    else if (Name.Contains("GeForce GTX 30"))
                    {
                        return GPUArchitectureInfo.Ampere;
                    }
                    else
                    {
                        return GPUArchitectureInfo.Unknown;
                    }
                }
            }
        }

        // Pascal: GeForce GTX 10xx | TITAN X
        // Volta: TITAN V
        // Turing: GeForce GTX 16xx | GeForce RTX 20xx | TITAN RTX
        // Ampere: GeForce RTX 30xx
        public enum GPUArchitectureInfo
        {
            Unknown,
            Pascal,
            Volta,
            Turing,
            Ampere
        }

        // nvidia-smi --query-gpu=index,gpu_name,memory.total --format=csv,noheader
        // > 0, NVIDIA GeForce GTX 1050, 4096 MiB
        public static List<GPUInfo> GetNvidiaGPUsInfo()
        {
            var gpus = new List<object>();
            var outputParser = Command.Output.GetList(null,
                @"(?<index>\d+),\s*(?<name>[^,]+),\s*(?<memory>\d+)",
                dict => new GPUInfo(Int32.Parse(dict["index"]), dict["name"], Int32.Parse(dict["memory"])),
                gpus);

            try
            {
                Command.Run("nvidia-smi", "--query-gpu=index,gpu_name,memory.total --format=csv,noheader", outputHandler: outputParser.Run);
            }
            catch (Exception)
            { /* no Nvidia GPU available */ }

            return gpus.Cast<GPUInfo>().ToList();
        }
    }

    static class CpuIdFlags
    {
        public static string[] edx1Feature;
        public static string[] ecx1Feature;

        public static string[] edx8Feature;
        public static string[] ecx8Feature;

        public static string[] ebx7Feature;

        static CpuIdFlags()
        {
            edx1Feature = new string[32];
            edx1Feature[0] = "fpu";
            edx1Feature[1] = "vme";
            edx1Feature[2] = "de";
            edx1Feature[3] = "pse";
            edx1Feature[4] = "tsc";
            edx1Feature[5] = "msr";
            edx1Feature[6] = "pae";
            edx1Feature[7] = "mce";
            edx1Feature[8] = "cx8";
            edx1Feature[9] = "apic";
            edx1Feature[10] = "(reserved)";
            edx1Feature[11] = "sep";
            edx1Feature[12] = "mtrr";
            edx1Feature[13] = "pge";
            edx1Feature[14] = "mca";
            edx1Feature[15] = "cmov";
            edx1Feature[16] = "pat";
            edx1Feature[17] = "pse36";
            edx1Feature[18] = "psn";
            edx1Feature[19] = "clflush";
            edx1Feature[20] = "(reserved)";
            edx1Feature[21] = "dts";
            edx1Feature[22] = "acpi";
            edx1Feature[23] = "mmx";
            edx1Feature[24] = "fxsr";
            edx1Feature[25] = "sse";
            edx1Feature[26] = "sse2";
            edx1Feature[27] = "ss";
            edx1Feature[28] = "ht";
            edx1Feature[29] = "tm";
            edx1Feature[30] = "ia64";
            edx1Feature[31] = "pbe";

            ecx1Feature = new string[32];
            ecx1Feature[0] = "sse3";
            ecx1Feature[1] = "pclmulqdq";
            ecx1Feature[2] = "dtes64";
            ecx1Feature[3] = "monitor";
            ecx1Feature[4] = "ds-cpl";
            ecx1Feature[5] = "vmx";
            ecx1Feature[6] = "smx";
            ecx1Feature[7] = "est";
            ecx1Feature[8] = "tm2";
            ecx1Feature[9] = "ssse3";
            ecx1Feature[10] = "cnxt-id";
            ecx1Feature[11] = "sdbg";
            ecx1Feature[12] = "fma";
            ecx1Feature[13] = "cx16";
            ecx1Feature[14] = "xtpr";
            ecx1Feature[15] = "pdcm";
            ecx1Feature[16] = "(reserved)";
            ecx1Feature[17] = "pcid";
            ecx1Feature[18] = "dca";
            ecx1Feature[19] = "sse4_1";
            ecx1Feature[20] = "sse4_2";
            ecx1Feature[21] = "x2apic";
            ecx1Feature[22] = "movbe";
            ecx1Feature[23] = "popcnt";
            ecx1Feature[24] = "tsc-deadline";
            ecx1Feature[25] = "aes";
            ecx1Feature[26] = "xsave";
            ecx1Feature[27] = "osxsave";
            ecx1Feature[28] = "avx";
            ecx1Feature[29] = "f16c";
            ecx1Feature[30] = "rdrand";
            ecx1Feature[31] = "hypervisor";

            edx8Feature = new string[32];
            edx8Feature[0] = "fpu";
            edx8Feature[1] = "vme";
            edx8Feature[2] = "de";
            edx8Feature[3] = "pse";
            edx8Feature[4] = "tsc";
            edx8Feature[5] = "msr";
            edx8Feature[6] = "pae";
            edx8Feature[7] = "mce";
            edx8Feature[8] = "cx8";
            edx8Feature[9] = "apic";
            edx8Feature[10] = "(reserved)";
            edx8Feature[11] = "syscall";
            edx8Feature[12] = "mtrr";
            edx8Feature[13] = "pge";
            edx8Feature[14] = "mca";
            edx8Feature[15] = "cmov";
            edx8Feature[16] = "pat";
            edx8Feature[17] = "pse36";
            edx8Feature[18] = "(reserved)";
            edx8Feature[19] = "mp";
            edx8Feature[20] = "nx";
            edx8Feature[21] = "(reserved)";
            edx8Feature[22] = "mmxext";
            edx8Feature[23] = "mmx";
            edx8Feature[24] = "fxsr";
            edx8Feature[25] = "fxsr_opt";
            edx8Feature[26] = "pdpe1gb";
            edx8Feature[27] = "rdtscp";
            edx8Feature[28] = "(reserved)";
            edx8Feature[29] = "lm";
            edx8Feature[30] = "3dnowext";
            edx8Feature[31] = "3dnow";

            ecx8Feature = new string[32];
            ecx8Feature[0] = "lahf_lm";
            ecx8Feature[1] = "cmp_legacy";
            ecx8Feature[2] = "svm";
            ecx8Feature[3] = "extapic";
            ecx8Feature[4] = "cr8_legacy";
            ecx8Feature[5] = "abm";
            ecx8Feature[6] = "sse4a";
            ecx8Feature[7] = "misalignsse";
            ecx8Feature[8] = "3dnowprefetch";
            ecx8Feature[9] = "osvw";
            ecx8Feature[10] = "ibs";
            ecx8Feature[11] = "xop";
            ecx8Feature[12] = "skinit";
            ecx8Feature[13] = "wdt";
            ecx8Feature[14] = "(reserved)";
            ecx8Feature[15] = "lwp";
            ecx8Feature[16] = "fma4";
            ecx8Feature[17] = "tce";
            ecx8Feature[18] = "(reserved)";
            ecx8Feature[19] = "nodeid_msr";
            ecx8Feature[20] = "(reserved)";
            ecx8Feature[21] = "tbm";
            ecx8Feature[22] = "topoext";
            ecx8Feature[23] = "perfctr_core";
            ecx8Feature[24] = "perfctr_nb";
            ecx8Feature[25] = "(reserved)";
            ecx8Feature[26] = "dbx";
            ecx8Feature[27] = "perftsc";
            ecx8Feature[28] = "pcx_l2i";
            ecx8Feature[29] = "(reserved)";
            ecx8Feature[30] = "(reserved)";
            ecx8Feature[31] = "(reserved)";

            ebx7Feature = new string[32];
            ebx7Feature[0] = "fsgsbase";
            ebx7Feature[1] = "";
            ebx7Feature[2] = "sgx";
            ebx7Feature[3] = "bmi1";
            ebx7Feature[4] = "hle";
            ebx7Feature[5] = "avx2";
            ebx7Feature[6] = "";
            ebx7Feature[7] = "smep";
            ebx7Feature[8] = "bmi2";
            ebx7Feature[9] = "erms";
            ebx7Feature[10] = "invpcid";
            ebx7Feature[11] = "rtm";
            ebx7Feature[12] = "pqm";
            ebx7Feature[13] = "";
            ebx7Feature[14] = "mpx";
            ebx7Feature[15] = "pqe";
            ebx7Feature[16] = "avx512_f";
            ebx7Feature[17] = "avx512_dq";
            ebx7Feature[18] = "rdseed";
            ebx7Feature[19] = "adx";
            ebx7Feature[20] = "smap";
            ebx7Feature[21] = "avx512_ifma";
            ebx7Feature[22] = "pcommit";
            ebx7Feature[23] = "clflushopt";
            ebx7Feature[24] = "clwb";
            ebx7Feature[25] = "intel_pt";
            ebx7Feature[26] = "avx512_pf";
            ebx7Feature[27] = "avx512_er";
            ebx7Feature[28] = "avx512_cd";
            ebx7Feature[29] = "sha";
            ebx7Feature[30] = "avx512_bw";
            ebx7Feature[31] = "avx512_vl";
        }
    }
}
