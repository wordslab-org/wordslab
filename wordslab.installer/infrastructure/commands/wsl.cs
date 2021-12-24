namespace wordslab.installer.infrastructure.commands
{
    // https://docs.microsoft.com/en-us/windows/wsl/basic-commands
    // https://github.com/agowa338/WSL-DistroLauncher-Alpine
    // C# calls to wslapi.dll : https://programmerall.com/article/2051677932/
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

        /*
        // Executes : wsl -l -v
        // Returns  : 
        // -1 if WSL2 is not installed
        //  0 if WSL2 is ready but no distribution was installed
        //  1 if WSL2 is ready but the default distribution is set to run in WSL version 1
        //  2 if WSL2 is ready and the default distribution is set to run in WSL version 2
        public static int CheckWSLVersion()
        {
            try
            {
                string output;
                string error;
                int exitcode = Process.Run("wsl.exe", "-l -v", 5, out output, out error, true);
                if (exitcode == 0 && String.IsNullOrEmpty(error))
                {
                    if (String.IsNullOrEmpty(output))
                    {
                        return 0;
                    }
                    var lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length < 2)
                    {
                        return 0;
                    }
                    for (var i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        var cols = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (cols.Length == 4)
                        {
                            return Int32.Parse(cols[3]);
                        }
                    }
                    return 0;
                }
            }
            catch (Exception)
            { }
            return -1;
        }

        // Executes : wsl -- uname -r
        // Returns  :  
        // Version object if kernel version was correctly parsed
        // null otherwise
        public static Version CheckKernelVersion()
        {
            try
            {
                string output;
                string error;
                int exitcode = Process.Run("wsl.exe", "-- uname -r", 5, out output, out error);
                if (exitcode == 0 && String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(output))
                {
                    int firstDot = output.IndexOf('.');
                    int secondDot = output.IndexOf('.', firstDot + 1);
                    if(firstDot > 0 && secondDot > firstDot)
                    {
                        var major = Int32.Parse(output.Substring(0, firstDot));
                        var minor = Int32.Parse(output.Substring(firstDot+1, secondDot-firstDot-1));
                        return new Version(major, minor);
                    }
                }
            }
            catch (Exception)
            { }
            return null;
        }

        // Executes : wsl -- cat /etc/*-release
        // Returns  :  
        // true if the default distribution launched by the wsl command is Ubuntu
        // false otherwise
        public static bool CheckUbuntuDistribution(out string distrib, out string version)
        {
            distrib = "unknown";
            version = "?";
            try
            {
                string output;
                string error;
                int exitcode = Process.Run("wsl.exe", "-- cat /etc/*-release", 5, out output, out error);
                if (exitcode == 0 && String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(output))
                {
                    var lines = output.Split('\n');
                    foreach(var line in lines)
                    {
                        if (line.StartsWith("DISTRIB_ID="))
                        {
                            distrib = line.Substring(11);
                        }
                        else if (line.StartsWith("DISTRIB_RELEASE="))
                        {
                            version = line.Substring(16);
                        }
                    }
                    var major = Int32.Parse(version.Substring(0, 2));
                    if( String.Compare(distrib, "Ubuntu", StringComparison.InvariantCultureIgnoreCase) == 0 &&
                        major >= 18)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            { }
            return false;
        }
    }*/
    }
}
