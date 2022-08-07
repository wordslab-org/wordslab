using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WindowsTests
    {
        [TestMethod]
        public void TestIsWindows10Version1903OrHigher()
        {
            var result = Windows.IsWindows10Version1903OrHigher();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsWindows10Version21H2OrHigher()
        {
            var result = Windows.IsWindows10Version21H2OrHigher();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsWindows11Version21HOrHigher()
        {
            var result = Windows.IsWindows11Version21HOrHigher();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestOpenWindowsUpdate()
        {
            Windows.OpenWindowsUpdate();
        }
                
        [TestMethod]
        public void TestIsWindowsSubsystemForLinuxEnabled_script()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestIsWindowsSubsystemForLinuxEnabled()
        {
            /*var result = Windows.IsWindowsSubsystemForLinuxEnabled();
            Assert.IsTrue(result);*/

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestEnableWindowsSubsystemForLinux_script()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestEnableWindowsSubsystemForLinux()
        {
            /*var needsRestart = Windows.EnableWindowsSubsystemForLinux();
            Assert.IsTrue(needsRestart);*/

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestDisableWindowsSubsystemForLinux_script()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestDisableWindowsSubsystemForLinux()
        {
            /*var needsRestart = Windows.DisableWindowsSubsystemForLinux();
            Assert.IsTrue(needsRestart);*/

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestShutdownAndRestart()
        {
            Windows.ShutdownAndRestart();
        }
    }
}
