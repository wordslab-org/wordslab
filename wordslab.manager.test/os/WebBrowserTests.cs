using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WebBrowserTests
    {
        [TestMethod]
        public void T01_TestOpen()
        {
            WebBrowser.Open("https://www.cognitivefactory.fr");
        }
    }
}
