namespace wordslab.installer.infrastructure.commands
{
    // https://docs.microsoft.com/en-us/windows/wsl/basic-commands
    public static class wsl
    {
        public const string COMMAND = "wsl";

        // Execute Linux binary files

        public static bool exec(string commandLine, string distribution = null, string workingDirectory = null, string userName = null)
        {
            return true;
        }

        public static bool execShell(string commandLine, string workingDirectory = null)
        {
            return true;
        }

        // Manage Windows Subsystem for Linux

        public static bool install(string distributionName = "Ubuntu")
        {
            return true;
        }

        public static bool setDefaultVersion(int version)
        {
            return true;
        }

        public static bool shutdown()
        {
            return true;
        }

        public class StatusResult
        {
            public bool IsInstalled { get { return DefaultVersion > 0;  } }
            
            public int     DefaultVersion;
            public string  DefaultDistribution;
            public Version LinuxKernelVersion;
            public string  LastWSLUpdate;
        }

        public static StatusResult status()
        {
            var result = new StatusResult();
            try
            {
                string? distrib = null;
                string? wslver = null;
                string? wsldate = null;
                string? linuxver = null;
                var outputParser = Command.Output.GetValue(@":\s+(?<distrib>[a-zA-Z]+[^\s]*)\s*$", s => distrib = s).
                                                  GetValue(@":\s+(?<wslver>[\d])\s*$", s => wslver = s).
                                                  GetValue(@"\s+(?<wsldate>\d+(?:/\d+)+)\s*$", s => wsldate = s).
                                                  GetValue(@":\s+(?<linuxver>(?:\d+\.)+\d+)\s*$", s => linuxver = s);

                Command.Run("wsl", "--status", outputHandler: outputParser.Run);

                if (!String.IsNullOrEmpty(wslver)) result.DefaultVersion = Int32.Parse(wslver);
                if (!String.IsNullOrEmpty(distrib)) result.DefaultDistribution = distrib;
                if (!String.IsNullOrEmpty(linuxver)) result.LinuxKernelVersion = new Version(linuxver);
                if (!String.IsNullOrEmpty(wsldate)) result.LastWSLUpdate = wsldate;
            }
            catch (Exception ex)
            { 
                // This method is used to check if wsl is installed => do nothing in case of exception
            }
            return result;
        }

        public static bool update(bool rollback = false)
        {
            return true;
        }

        // Manage distributions in Windows Subsystem for Linux

        public static bool export(string distribution, string filename)
        {
            return true;
        }

        public static bool import(string distribution, string installPath, string filename, int version = 2)
        {
            return true;
        }

        public static bool list(bool all = false, bool quiet = false, bool verbose = false, bool online = false)
        {
            return true;
        }

        public static bool setDefaultDistribution(string distributionName)
        {
            return true;
        }

        public static bool setVersion(string distribution, int version)
        {
            return true;
        }

        public static bool terminate(string distribution)
        {
            return true;
        }

        public static bool unregister(string distribution)
        {
            return true;
        }
    }
}
