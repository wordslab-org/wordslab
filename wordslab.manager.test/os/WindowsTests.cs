using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;
using wordslab.manager.storage;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WindowsTests
    {
        [TestMethodOnWindows]
        public void T01_TestIsWindows10Version1903OrHigher()
        {
            var result = Windows.IsWindows10Version1903OrHigher();
            Assert.IsTrue(result);
        }

        [TestMethodOnWindows]
        public void T02_TestIsWindows10Version21H2OrHigher()
        {
            var result = Windows.IsWindows10Version21H2OrHigher();
            Assert.IsTrue(result);
        }

        [TestMethodOnWindows]
        public void T03_TestIsWindows11Version21HOrHigher()
        {
            var result = Windows.IsWindows11Version21HOrHigher();
            Assert.IsFalse(result);
        }

        [TestMethodOnWindows]
        public void T04_TestOpenWindowsUpdate()
        {
            Windows.OpenWindowsUpdate();
        }
                
        [TestMethodOnWindows]
        public void T05_TestIsWindowsSubsystemForLinuxEnabled_script()
        {
            var storage = new HostStorage();
            var script = Windows.IsWindowsSubsystemForLinuxEnabled_script(storage.ScriptsDirectory);
            Assert.IsTrue(script.Length > 10);
        }

        [TestMethodOnWindows]
        public void T06_TestIsWindowsSubsystemForLinuxEnabled()
        {
            var storage = new HostStorage();
            var enabled = Windows.IsWindowsSubsystemForLinuxEnabled(storage.ScriptsDirectory, storage.LogsDirectory);
            Assert.IsTrue(enabled);
        }

        [TestMethodOnWindows]
        public void T07_TestDisableWindowsSubsystemForLinux_script()
        {
            var storage = new HostStorage();
            var script = Windows.DisableWindowsSubsystemForLinux_script(storage.ScriptsDirectory);
            Assert.IsTrue(script.Length > 10);
        }

        [TestMethodOnWindows]
        public void T08_TestDisableWindowsSubsystemForLinux()
        {
            var storage = new HostStorage();
            var needsRestart = Windows.DisableWindowsSubsystemForLinux(storage.ScriptsDirectory, storage.LogsDirectory);
            Assert.IsTrue(needsRestart);

            var enabled = Windows.IsWindowsSubsystemForLinuxEnabled(storage.ScriptsDirectory, storage.LogsDirectory);
            Assert.IsFalse(enabled);
        }

        [TestMethodOnWindows]
        public void T09_TestEnableWindowsSubsystemForLinux_script()
        {
            var storage = new HostStorage();
            var script = Windows.EnableWindowsSubsystemForLinux_script(storage.ScriptsDirectory);
            Assert.IsTrue(script.Length > 10);
        }

        [TestMethodOnWindows]
        public void T10_TestEnableWindowsSubsystemForLinux()
        {
            var storage = new HostStorage();
            var needsRestart = Windows.EnableWindowsSubsystemForLinux(storage.ScriptsDirectory, storage.LogsDirectory);
            Assert.IsTrue(needsRestart);

            var enabled = Windows.IsWindowsSubsystemForLinuxEnabled(storage.ScriptsDirectory, storage.LogsDirectory);
            Assert.IsTrue(enabled);
        }

        [TestMethodOnWindows]
        [Ignore]
        public void T11_TestShutdownAndRestart()
        {
            Windows.ShutdownAndRestart();
        }
    }
}
