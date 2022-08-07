using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using wordslab.manager.os;

namespace wordslab.manager.cli.host
{
    public class NetworkInfoCommand : Command<NetworkInfoCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Network info for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            DisplayNetworkInfo();

            return 0;
        }

        internal static void DisplayNetworkInfo()
        {
            AnsiConsole.WriteLine($"Network info: IPv4 addresses");
            var addresses = Network.GetIPAddressesAvailable();
            foreach (var address in addresses.Values)
            {
                AnsiConsole.WriteLine($"- {address.Address}");
                AnsiConsole.WriteLine($"  . network interface name : {address.NetworkInterfaceName}");
                if (address.IsLoopback)
                {
                    AnsiConsole.WriteLine($"  . loopback               : {address.IsLoopback}");
                }
                if (address.IsWireless)
                {
                    AnsiConsole.WriteLine($"  . wireless               : {address.IsWireless}");
                }
            }
            AnsiConsole.WriteLine();
        }

        public class Settings : CommandSettings
        { }
    }

    public class NetworkStatusCommand : Command<NetworkStatusCommand.Settings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.WriteLine($"Network status for the host machine: {OS.GetMachineName()}");
            AnsiConsole.WriteLine();

            DisplayNetworkStatus();

            return 0;
        }

        internal static void DisplayNetworkStatus()
        {
            AnsiConsole.WriteLine($"Network info: TCP ports in use");
            var portsSets = Network.GetTcpPortsInUsePerIPAddress();
            foreach (var ip in portsSets.Keys)
            {
                AnsiConsole.Write($"- {ip} : ");
                foreach (var port in portsSets[ip])
                {
                    AnsiConsole.Write(port);
                    AnsiConsole.Write(' ');
                }
                AnsiConsole.WriteLine();
            }
            AnsiConsole.WriteLine();
        }


        public class Settings : CommandSettings
        { }
    }
}
