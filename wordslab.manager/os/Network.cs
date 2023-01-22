using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace wordslab.manager.os
{
    public static class Network
    {
        public class IPAddressStatus
        {
            public string Address;
            public bool IsLoopback;
            public bool IsWireless;
            public string NetworkInterfaceId;
            public string NetworkInterfaceName;
        }

        public static Dictionary<string, IPAddressStatus> GetIPAddressesAvailable()
        {
            var addressesStatus = new Dictionary<string, IPAddressStatus>();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var addressStatus = new IPAddressStatus();
                            addressStatus.Address = ip.Address.ToString();
                            addressStatus.IsLoopback = IPAddress.IsLoopback(ip.Address);
                            addressStatus.IsWireless = networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
                            addressStatus.NetworkInterfaceId = networkInterface.Name;
                            addressStatus.NetworkInterfaceName = networkInterface.Name;
                            addressesStatus.Add(addressStatus.Address.ToString(), addressStatus);
                        }
                    }
                }
            }
            return addressesStatus;
        }

        public static Dictionary<string, HashSet<int>> GetTcpPortsInUsePerIPAddress()
        {
            var portsInUse = new Dictionary<string, HashSet<int>>();
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();
            foreach (var endpoint in tcpEndPoints)
            {
                if (endpoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ip = endpoint.Address.ToString();
                    HashSet<int> ports;
                    if (portsInUse.ContainsKey(ip))
                    {
                        ports = portsInUse[ip];
                    }
                    else
                    {
                        ports = new HashSet<int>();
                        portsInUse.Add(ip, ports);
                    }
                    ports.Add(endpoint.Port);
                }
            }
            return portsInUse;
        }

        public static HashSet<int> GetAllTcpPortsInUse()
        {
            return new HashSet<int>(GetTcpPortsInUsePerIPAddress().Values.SelectMany(it => it));
        }

        public static int GetNextAvailablePort(int defaultPort, HashSet<int> portsInUse)
        {
            for (int port = defaultPort; port < defaultPort + 1000; port++)
            {
                if (!portsInUse.Contains(port))
                {
                    return port;
                }
            }
            return 0;
        }

        /*
        /// <summary>
        /// Bytes Sent/sec is the rate at which bytes are sent over each network adapter, including framing characters.
        /// </summary>
        public UInt64 BytesSentPersec { get; set; }

        /// <summary>
        /// Bytes Received/sec is the rate at which bytes are received over each network adapter, including framing characters.
        /// </summary>
        public UInt64 BytesReceivedPersec { get; set; }

        // Windows 

         if (includeBytesPersec)
                {
                    string query = UseAsteriskInWMI ? $"SELECT * FROM Win32_PerfFormattedData_Tcpip_NetworkAdapter WHERE Name = '{networkAdapter.Name.Replace("(", "[").Replace(")", "]")}'"
                                                    : $"SELECT BytesSentPersec, BytesReceivedPersec FROM Win32_PerfFormattedData_Tcpip_NetworkAdapter WHERE Name = '{networkAdapter.Name.Replace("(", "[").Replace(")", "]")}'";
                    using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(_managementScope, query, _enumerationOptions);
                    foreach (ManagementObject managementObject in managementObjectSearcher.Get())
                    {
                        networkAdapter.BytesSentPersec = GetPropertyValue<ulong>(managementObject["BytesSentPersec"]);
                        networkAdapter.BytesReceivedPersec = GetPropertyValue<ulong>(managementObject["BytesReceivedPersec"]);
                    }
                }

        // Linux

        if (includeBytesPersec)
        {
            char[] charSeparators = new char[] { ' ' };


            foreach (NetworkAdapter networkAdapter in networkAdapterList)
            {
                List<string>? networkAdapterUsageLast = TryReadFileLines("/proc/net/dev").FirstOrDefault(l => l.Trim().StartsWith(networkAdapter.Name))?.Trim().Split(charSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
                Task.Delay(1000).Wait();
                List<string>? networkAdapterUsageNow = TryReadFileLines("/proc/net/dev").FirstOrDefault(l => l.Trim().StartsWith(networkAdapter.Name))?.Trim().Split(charSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (networkAdapterUsageLast != null && networkAdapterUsageLast.Count > 0 && networkAdapterUsageNow != null && networkAdapterUsageNow.Count > 0)
                {
                    networkAdapter.BytesReceivedPersec = Convert.ToUInt64(networkAdapterUsageNow[1]) - Convert.ToUInt64(networkAdapterUsageLast[1]);
                    networkAdapter.BytesSentPersec = Convert.ToUInt64(networkAdapterUsageNow[9]) - Convert.ToUInt64(networkAdapterUsageLast[9]);
                }
            }
        }
        */

        public class PortConfig
        {
            public int VmPort { get; set; }

            public int HostPort { get; set; }

            public bool AllowAccessFromLAN { get; set; }
        }

        public static void CreateNetworkConfig(string vmName, string vmAddress, List<PortConfig> portsConfig, string scriptsDirectory, string logsDirectory)
        {
            if (OS.IsWindows)
            {
                var scriptArguments = $"{vmName} {vmAddress}";
                foreach(var portConfig in portsConfig)
                {
                    scriptArguments += $" {portConfig.VmPort} {portConfig.HostPort} {(portConfig.AllowAccessFromLAN?'y':'n')}";
                }
                Command.ExecuteShellScript(Path.Combine(scriptsDirectory, "os", "Network"), "create-network-config.ps1", scriptArguments, logsDirectory, runAsAdmin: true);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void DeleteNetworkConfig(string vmName, List<PortConfig> portsConfig, string scriptsDirectory, string logsDirectory)
        {
            if (OS.IsWindows)
            {
                var scriptArguments = $"{vmName}";
                foreach (var portConfig in portsConfig)
                {
                    scriptArguments += $" {portConfig.HostPort} {(portConfig.AllowAccessFromLAN ? 'y':'n')}";
                }
                Command.ExecuteShellScript(Path.Combine(scriptsDirectory, "os", "Network"), "delete-network-config.ps1", scriptArguments, logsDirectory, runAsAdmin: true);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

}
