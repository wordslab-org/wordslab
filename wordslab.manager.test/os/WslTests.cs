using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using wordslab.manager.os;
using wordslab.manager.storage;

using System.Net.Http.Json;
using System.Collections.Generic;

namespace wordslab.manager.test.os
{
    [TestClass]
    public class WslTests
    {
        [TestMethodOnWindows]
        public void T01_Teststatus()
        {
            var status = Wsl.status();

            Assert.IsNotNull(status);
            Assert.IsTrue(status.IsInstalled);
            Assert.AreEqual(status.DefaultVersion, 2);
            Assert.IsNotNull(status.DefaultDistribution);
            Assert.IsNotNull(status.LinuxKernelVersion);
            Assert.IsNotNull(status.LastWSLUpdate);
        }

        [TestMethodOnWindows]
        public void T02_TestIsWindowsVersionOKForWSL2()
        {
            var windowsOKforWSL2 = Wsl.IsWindowsVersionOKForWSL2();
            Assert.IsTrue(windowsOKforWSL2);
        }

        [TestMethodOnWindows]
        public void T03_TestIsWSL2AlreadyInstalled()
        {
            var wsl2Installed = Wsl.IsWSL2AlreadyInstalled();
            Assert.IsTrue(wsl2Installed);
        }

        [TestMethodOnWindows]
        public void T04_TestGetNvidiaGPUAvailableForWSL2()
        {
            var nvidiaGPUAvailable = Wsl.GetNvidiaGPUAvailableForWSL2();
            Assert.IsTrue(nvidiaGPUAvailable.Contains("NVIDIA"));
        }

        [TestMethodOnWindows]
        public void T05_TestIsWindowsVersionOKForWSL2WithGPU()
        {
            var windowsOKforWSL2withGPU = Wsl.IsWindowsVersionOKForWSL2WithGPU();
            Assert.IsTrue(windowsOKforWSL2withGPU);
        }

        [TestMethodOnWindows]
        public void T06_TestIsNvidiaDriverVersionOKForWSL2WithGPU()
        {
            var nvidiaDriverOKforWSL2withGPU = Wsl.IsNvidiaDriverVersionOKForWSL2WithGPU();
            Assert.IsTrue(nvidiaDriverOKforWSL2withGPU);
        }

        [TestMethodOnWindows]
        public void T07_TestIsLinuxKernelVersionOKForWSL2WithGPU()
        {
            var linuxKernelVersionOK = Wsl.IsLinuxKernelVersionOKForWSL2WithGPU();
            Assert.IsTrue(linuxKernelVersionOK);
        }

        [TestMethodOnWindows]
        public void T08_TestUpdateLinuxKernelVersion()
        {
            var storage = new HostStorage();
            Wsl.update(storage.ScriptsDirectory, storage.LogsDirectory);

            var status = Wsl.status();
            Assert.IsTrue(status.LinuxKernelVersion.Major >= 5);
        }

        [TestMethodOnWindows]
        public void T09_TestsetDefaultVersion()
        {
            Wsl.setDefaultVersion(2);
            Assert.AreEqual(2, Wsl.status().DefaultVersion);
        }

        [TestMethodOnWindows]
        public void T10_Testlist()
        {
            var installedDistribs = Wsl.list();
            Assert.IsTrue(installedDistribs.Count > 0);
            foreach (var distrib in installedDistribs)
            {
                Assert.IsTrue(!String.IsNullOrEmpty(distrib.Distribution));
            }

            var onlineDistribs = Wsl.list(online: true);
            Assert.IsTrue(onlineDistribs.Count > 0);
            foreach (var distrib in onlineDistribs)
            {
                Assert.IsFalse(distrib.IsDefault);
                Assert.IsFalse(distrib.IsRunning);
                Assert.IsTrue(!String.IsNullOrEmpty(distrib.Distribution));
                Assert.IsTrue(!String.IsNullOrEmpty(distrib.OnlineFriendlyName));
            }
        }

        [TestMethodOnWindows]
        public void T11_Testinstall()
        {
            var availableDistribs = Wsl.list(online: true);
            var distribution = availableDistribs.Where(d => d.Distribution == "Ubuntu-20.04").First().Distribution;

            var installedDistribs = Wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));

            Wsl.installDistribution(distribution);

            installedDistribs = Wsl.list();
            Assert.IsTrue(installedDistribs.Any(d => d.Distribution == distribution));
        }

        [TestMethodOnWindows]
        public void T12_Testunregister()
        {
            var distribution = "Ubuntu-20.04";
            Wsl.unregister(distribution);

            var installedDistribs = Wsl.list();
            Assert.IsFalse(installedDistribs.Any(d => d.Distribution == distribution));
        }

        [TestMethodOnWindows]
        public void T13_TestsetDefaultDistribution()
        {
            Assert.IsFalse(Wsl.list().Any(d => d.Distribution == "Ubuntu-20.04" && d.IsDefault));
            Wsl.setDefaultDistribution("Ubuntu-20.04");
            Assert.IsTrue(Wsl.list().Any(d => d.Distribution == "Ubuntu-20.04" && d.IsDefault));
            Wsl.setDefaultDistribution("Ubuntu");
        }

        [TestMethodOnWindows]
        public void T14_Testexec()
        {
            string output = null;
            Wsl.exec("cat /etc/os-release", "Ubuntu-20.04", outputHandler: o => output = o);
            Assert.IsTrue(output.Contains("NAME=\"Ubuntu\""));
        }

        [TestMethodOnWindows]
        public void T15_TestexecShell()
        {
            string output = null;
            Wsl.execShell("echo $HOME", "Ubuntu-20.04", outputHandler: o => output = o);
            Assert.IsTrue(output.StartsWith("/home"));
        }

        [TestMethodOnWindows]
        public void T16_TestCheckRunningDistribution()
        {
            string distribution;
            string version; ;
            Wsl.CheckRunningDistribution("Ubuntu-20.04", out distribution, out version);
            Assert.AreEqual(distribution, "Ubuntu");
            Assert.AreEqual(version, "20.04");
        }

        [TestMethodOnWindows]
        public void T17_Testterminate()
        {
            Wsl.exec("echo 0", "Ubuntu-20.04");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 1);

            Wsl.terminate("Ubuntu-20.04");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 0);
        }

        [TestMethodOnWindows]
        public void T18_Testshutdown()
        {
            Wsl.exec("echo 0", "Ubuntu");
            Wsl.exec("echo 0", "Ubuntu-20.04");
            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 2);

            Wsl.shutdown();

            Assert.IsTrue(Wsl.list().Where(d => d.IsRunning).Count() == 0);
        }
        
        [TestMethodOnWindows]
        public void T19_TestRead_wslconfig()
        {
            var config = Wsl.Read_wslconfig();
            Assert.IsTrue(config != null);
            Assert.IsTrue(config.LoadedFromFile);
        }
                
        [TestMethodOnWindows]
        public void T20_TestWrite_wslconfig()
        {
            var config = Wsl.Read_wslconfig();
            config.nestedVirtualization = true;
            Wsl.Write_wslconfig(config);

            var config2 = Wsl.Read_wslconfig();
            Assert.IsTrue(config2.LoadedFromFile);
            Assert.IsTrue(config2.nestedVirtualization);
        }

        [TestMethodOnWindows]
        public void T21_TestsetVersion()
        {
            Wsl.setVersion("Ubuntu-20.04", 2);
            Assert.IsTrue(Wsl.list().Any(d => d.Distribution == "Ubuntu-20.04" && d.WslVersion == 2));
        }

        [TestMethodOnWindows]
        public void T22_Testexport()
        {
            var exportedFile = @"c:\tmp\distrotest.tar";
            if(File.Exists(exportedFile))
            {
                File.Delete(exportedFile);
            }

            Wsl.export("Ubuntu-20.04", exportedFile);
            Assert.IsTrue(File.Exists(exportedFile));
        }

        [TestMethodOnWindows]
        public void T23_Testimport()
        {
            var exportedFile = @"c:\tmp\distrotest.tar";
            if (File.Exists(exportedFile))
            {
                Wsl.import("distrotest", @"c:\tmp", exportedFile);
                File.Delete(exportedFile);

                Wsl.exec("echo 0", "distrotest");
            }

            Wsl.unregister("distrotest");
        }

        [TestMethodOnWindows]
        public void T24_TestGetVirtualMachineWorkingSetMB()
        {
            Wsl.exec("echo 0", "Ubuntu-20.04");

            var vmMemoryMB = Wsl.GetVirtualMachineWorkingSetMB();
            Assert.IsTrue(vmMemoryMB > 100 && vmMemoryMB < 2000);

            Wsl.shutdown();

            vmMemoryMB = Wsl.GetVirtualMachineWorkingSetMB();
            Assert.IsTrue(vmMemoryMB == 0);
        }

        [TestMethodOnWindows]
        public async Task T25_TestWslCommandWithStreamingResult()
        {
            try
            {
                var manifest = await PullImageAsync("wordslab-org/lambda-stack-cpu", "0.1.13-22.04.2");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return;
            var vmName = "wordslab-test-cluster";
            string result;
            //Wsl.execShell("k3s crictl rmi --prune", vmName, outputHandler: output => result = output);
            Wsl.execShell("k3s ctr images pull ghcr.io/wordslab-org/lambda-stack-server:22.04.2", vmName, timeoutSec:60, outputHandler: output => result = output);
        }

        private static readonly HttpClient _httpClient = new HttpClient();
        
        public static async Task<ManifestResponse> PullImageAsync(string imageName, string tag)
        {
            // Step 1: Authentication request
            var authResponse = await _httpClient.GetFromJsonAsync<Dictionary<string,string>>($"https://ghcr.io/token?scope=repository:{imageName}:pull&service=ghcr.io");
            var authToken = authResponse["token"];

            // Step 2: Manifest request
            var manifestRequest = new HttpRequestMessage(HttpMethod.Get, $"https://ghcr.io/v2/{imageName}/manifests/{tag}");
            manifestRequest.Headers.Add("Authorization", $"Bearer {authToken}");
            manifestRequest.Headers.Add("Accept", "application/vnd.docker.distribution.manifest.v2+json");
            var manifestResponse = await _httpClient.SendAsync(manifestRequest);
            manifestResponse.EnsureSuccessStatusCode();
            var manifest = await manifestResponse.Content.ReadFromJsonAsync<ManifestResponse>();

            // Step 3: Checksum requests
            foreach (var layer in manifest.layers)
            {                
                var checksumRequest = new HttpRequestMessage(HttpMethod.Head, $"https://ghcr.io/v2/{imageName}/blobs/{layer.digest}");
                checksumRequest.Headers.Add("Authorization", $"Bearer {authToken}");
                var checksumResponse = await _httpClient.SendAsync(checksumRequest);
                checksumResponse.EnsureSuccessStatusCode();
                var result = await checksumResponse.Content.ReadAsStringAsync();
            }

            // https://github.com/containerd/containerd/blob/main/docs/content-flow.md

            // Download :
            // wget --header="Authorization: Bearer djE6d29yZHNsYWItb3JnL2xhbWJkYS1zdGFjay1zZXJ2ZXI6MTY4MTE0NDQ2OTcyMzQ3MDc2Mw==" https://ghcr.io/v2/wordslab-org/lambda-stack-server/blobs/sha256:1bdbbbfaf9ee012367f8103c3425fcaa74845576d919bf5f978480dc459c322f

            // k3s crictl rmi --prune
            // k3s ctr image pull ghcr.io/wordslab-org/lambda-stack-server:22.04.2

            // Download in porgress with resume capability :
            // k3s ctr content active
            // REF SIZE    AGE
            // layer - sha256:c781ed05e2ff0cdd3c235c9d71270b2ea2adbe86dfb2e728cfc5b12dec98ab65   1.464GB 3 hours   

            // Find on disk while downloading :
            // cat /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/ingest/aa86d66378175e273c04b7adbb726b50824a64dc0783c30508ab4361f21c381a/ref
            // k8s.io / 1 / layer - sha256:c781ed05e2ff0cdd3c235c9d71270b2ea2adbe86dfb2e728cfc5b12dec98ab65
            // ls -al /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/ingest/aa86d66378175e273c04b7adbb726b50824a64dc0783c30508ab4361f21c381a/
            // drwxr - xr - x 3 root root       4096 Apr 10 17:39..-rw - r--r-- 1 root root 1463812096 Apr 10 17:49 data
            // - rw - r--r-- 1 root root         86 Apr 10 17:39 ref

            // Find on disk after download :
            // ls -al /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/blobs/sha256/fc4a5e1c03378ec8b1824c54bd25943af415adc040cc2071c8324c530a4962fb
            //    -r--r--r-- 1 root root 284407131 <== size in manifest

            return manifest;
        }

        public class ManifestResponse
        {
            public int schemaVersion { get; set; }
            public string mediaType { get; set; }
            public Config config { get; set; }
            public Layer[] layers { get; set; }

            public class Config
            {
                public string mediaType { get; set; }
                public long size { get; set; }
                public string digest { get; set; }
            }

            public class Layer
            {
                public string mediaType { get; set; }
                public long size { get; set; }
                public string digest { get; set; }
            }
        }
    }
}
