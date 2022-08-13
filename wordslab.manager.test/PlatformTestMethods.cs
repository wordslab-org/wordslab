using Microsoft.VisualStudio.TestTools.UnitTesting;
using wordslab.manager.os;

namespace wordslab.manager.test
{
    /// <summary>
    /// An extension to the [TestMethod] attribute : the test is executed on a specific platform only, ignored on all other platforms.
    /// </summary>
    public abstract class TestMethodOnPlatform : TestMethodAttribute
    {
        protected abstract bool CheckPlatform();

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            var ignore = !CheckPlatform();
            if (ignore)
            {
                var message = $"Test not executed on this platform: {OS.GetOSName()}.";
                return new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive,
                        TestFailureException = new AssertInconclusiveException(message)
                    }
                };
            }
            else
            {
                return base.Execute(testMethod);
            }
        }
    }

    public class TestMethodOnWindows : TestMethodOnPlatform
    {
        protected override bool CheckPlatform()
        {
            return OS.IsWindows;
        }
    }

    public class TestMethodOnLinux : TestMethodOnPlatform
    {
        protected override bool CheckPlatform()
        {
            return OS.IsLinux;
        }
    }

    public class TestMethodOnMacOS : TestMethodOnPlatform
    {
        protected override bool CheckPlatform()
        {
            return OS.IsMacOS;
        }
    }
    public class TestMethodOnWindowsOrLinux : TestMethodOnPlatform
    {
        protected override bool CheckPlatform()
        {
            return OS.IsWindows|| OS.IsLinux;
        }
    }

    public class TestMethodOnLinuxOrMacOS : TestMethodOnPlatform
    {
        protected override bool CheckPlatform()
        {
            return OS.IsLinux || OS.IsMacOS;
        }
    }
}
