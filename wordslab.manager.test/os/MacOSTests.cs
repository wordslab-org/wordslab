using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class MacOSTests
    {
        [TestMethodOnMacOS]
        public void T01_TestIsMacOSVersionCatalinaOrHigher()
        {
            var versionok = MacOS.IsMacOSVersionCatalinaOrHigher();
            Assert.IsTrue(versionok);
        }

        [TestMethodOnMacOS]
        public void T02_TestIsHomebrewPackageManagerAvailable()
        {
            var homebrewok = MacOS.IsHomebrewPackageManagerAvailable();
            Assert.IsTrue(homebrewok);
        }

        // This script requires admin privileges and user interaction
        // - interaction 1 : type sudo password
        // - interaction 2 : press Enter to validate the list of changes which will be applied
        [TestMethodOnMacOS]
        public void T03_TestGetHomebrewInstallCommand()
        {
            var installcommand = MacOS.GetHomebrewInstallCommand();
            Assert.IsTrue(installcommand.Length > 10);
        }
    }
}
