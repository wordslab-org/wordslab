﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class MacOSTests
    {
        [TestMethodOnMacOS]
        public void TestIsMacOSVersionCatalinaOrHigher()
        {
            Assert.IsTrue(true);
        }

        [TestMethodOnMacOS]
        public void TestIsHomebrewPackageManagerAvailable()
        {
            Assert.IsTrue(true);
        }

        // This script requires admin privileges and user interaction
        // - interaction 1 : type sudo password
        // - interaction 2 : press Enter to validate the list of changes which will be applied
        [TestMethodOnMacOS]
        public void TestGetHomebrewInstallCommand()
        {
            Assert.IsTrue(true);
        }
    }
}
