using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WebBrowserTests
    {
        [TestMethod]
        public void TestOpen()
        {
            WebBrowser.Open("www.cognitivefactory.fr");
        }
    }
}
