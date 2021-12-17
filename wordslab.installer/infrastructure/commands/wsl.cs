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

        public static bool status()
        {
            string output = null;
            Command.Run("wsl", "--status", outputHandler: o => output=o);
            if(output != null && output.Contains(": 2"))
            {
                return true;
            }
            else
            {
                return false;
            }
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
