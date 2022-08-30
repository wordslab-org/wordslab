using System.Diagnostics;
using System.Runtime.InteropServices;

namespace wordslab.manager.os
{
    public static class WebBrowser
    {
        public static void Open(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            // Tested OK on Windows

            // Error message on Linux while launching web browser : 
            // [GFX1-]: glxtest: VA-API test failed: missing or old libva-drm library.
            // ExceptionHandler::GenerateDump cloned child 27580
            // ExceptionHandler::SendContinueSignalToChild sent continue signal to child
            // ExceptionHandler::WaitForContinueSignal waiting for continue signal...
        }
    }
}
