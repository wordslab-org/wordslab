using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using wordslab.manager.config;

namespace wordslab.manager.os
{
    public interface IVirtualMachineShell
    {
        int ExecuteCommand(string command, string commandArguments = "", int timeoutSec = 10, Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null);
    }

    public class Kubernetes
    {
        public static int DownloadImageInContentStore(ContainerImageInfo imageInfo, IVirtualMachineShell vmShell, int timeoutSec=300)
        {
            // Containerd content flow docs: 
            // https://github.com/containerd/containerd/blob/main/docs/content-flow.md

            // Note: if you want to remove all unused images for testing
            // k3s crictl rmi --prune

            // Image pull command:
            // ctr image pull ghcr.io/wordslab-org/lambda-stack-cuda:0.1.13-22.04.2
            string k3sArguments = $"ctr image pull {imageInfo.Registry}/{imageInfo.Repository}:{imageInfo.Tag}";
            string output = null;
            string error = null;
            var exitCode = vmShell.ExecuteCommand("k3s", k3sArguments, timeoutSec: timeoutSec,
                outputHandler: o => output = o, errorHandler: e => error = e);
            if (output != null && output.Contains("done:"))
            {
                return exitCode;
            }
            else
            {
                if (error == null) { error = "could not find 'done:' in the output"; }
                throw new InvalidOperationException($"Error while executing command {k3sArguments} : \"{error}\"");
            }
        }

        public static bool DeleteImageFromContentStore(ContainerImageInfo imageInfo, IVirtualMachineShell vmShell, int timeoutSec = 300)
        {
            // Image and images layers delete commands:
            // ctr image remove ghcr.io/wordslab-org/lambda-stack-server:22.04.2
            string k3sArguments = $"ctr image remove {imageInfo.Registry}/{imageInfo.Repository}:{imageInfo.Tag}";
            string error = null;
            var imageFoundAndDeleted = true;
            vmShell.ExecuteCommand("k3s", k3sArguments, timeoutSec: timeoutSec, errorHandler: e => error = e);
            if(error != null)
            {
                if (error.Contains("image not found"))
                {
                    imageFoundAndDeleted = false;
                }
                else
                {
                    throw new InvalidOperationException($"Error while executing command {k3sArguments} : \"{error}\"");
                }
            }
            // ctr content prune refrences ghcr.io/wordslab-org/lambda-stack-server:22.04.2
            vmShell.ExecuteCommand("k3s", $"ctr content prune references {imageInfo.Registry}/{imageInfo.Repository}:{imageInfo.Tag}", timeoutSec: timeoutSec);
            
            return imageFoundAndDeleted;
        }

        public static long CheckImageBytesToDownload(ContainerImageInfo imageInfo, IVirtualMachineShell vmShell)
        {
            // Already downloaded layers:
            // k3s ctr content list -q
            // sha256: 0027c958fa3f3976058f1bebc8fa49fc6272528659331074aba57ad24aecc23e
            // sha256:0233247f4c0fad20be467785f7e93fc0213a076a9f0dff630bdec21ec3b6441
            // ...

            // Find on disk after download :
            // ls -al /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/blobs/sha256/fc4a5e1c03378ec8b1824c54bd25943af415adc040cc2071c8324c530a4962fb
            //    -r--r--r-- 1 root root 284407131 ... <== size in manifest

            string downloadedLayers = "";
            vmShell.ExecuteCommand("k3s", "ctr content list -q", outputHandler: output => downloadedLayers = output);

            var downloadedLayersSet = new HashSet<string>();
            foreach (var line in downloadedLayers.Split('\n'))
            {
                downloadedLayersSet.Add(line.Trim());
            }

            // Download in progress with resume capability:
            // k3s ctr content active
            // REF                                                                             SIZE    AGE
            // layer-sha256:c781ed05e2ff0cdd3c235c9d71270b2ea2adbe86dfb2e728cfc5b12dec98ab65   31.46MB 26 seconds

            // Find on disk while downloading :
            // ls -al /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/ingest/aa86d66378175e273c04b7adbb726b50824a64dc0783c30508ab4361f21c381a/
            // -rw-r--r-- 1 root root 31457280 Apr 15 15:08 data
            // -rw-r--r-- 1 root root       86 Apr 15 15:07 ref
            // -rw -r--r-- 1 root root       29 Apr 15 15:07 startedat
            // -rw -r--r-- 1 root root       10 Apr 15 15:07 total
            // -rw -r--r-- 1 root root       30 Apr 15 15:08 updatedat
            //
            // cat /var/lib/rancher/k3s/agent/containerd/io.containerd.content.v1.content/ingest/aa86d66378175e273c04b7adbb726b50824a64dc0783c30508ab4361f21c381a/ref
            // k8s.io/1/layer-sha256:c781ed05e2ff0cdd3c235c9d71270b2ea2adbe86dfb2e728cfc5b12dec98ab65

            string inprogressLayers = "";
            vmShell.ExecuteCommand("k3s", "ctr content active", outputHandler: output => inprogressLayers = output);

            var inprogressLayersDict = new Dictionary<string, long>();
            foreach (var line in inprogressLayers.Split('\n'))
            {
                if (line.StartsWith("layer-"))
                {
                    var parts = line.Split(new char[] { '-', '\t' });
                    var digest = parts[1];
                    if(downloadedLayers.Contains(digest))
                    {
                        continue;
                    }
                    var sizeAndUnit = parts[2];
                    var multiply = 1L;
                    var unitChars = 2;
                    if (sizeAndUnit.EndsWith("KB"))
                    {
                        multiply = 1024L;
                    }
                    else if (sizeAndUnit.EndsWith("MB"))
                    {
                        multiply = 1024 * 1024L;
                    }
                    else if (sizeAndUnit.EndsWith("GB"))
                    {
                        multiply = 1024 * 1024 * 1024L;
                    }
                    else if (sizeAndUnit.EndsWith("B"))
                    {
                        unitChars = 1;
                    }
                    sizeAndUnit = sizeAndUnit.Substring(0, sizeAndUnit.Length - unitChars);
                    var size = (long)(float.Parse(sizeAndUnit,CultureInfo.InvariantCulture) * multiply);
                    inprogressLayersDict.Add(digest, size);
                }
            }            

            long remainingBytes = 0;
            foreach (var layer in imageInfo.Layers)
            {
                if (downloadedLayersSet.Contains(layer.Digest))
                {
                    continue;
                }
                else if (inprogressLayersDict.ContainsKey(layer.Digest))
                {
                    remainingBytes += layer.Size - inprogressLayersDict[layer.Digest];
                }
                else
                {
                    remainingBytes += layer.Size;
                }
            }

            return remainingBytes;
        }

        public delegate void DownloadProgressHandler(long totalDownloadSize, long totalBytesDownloaded, int progressPercentage);

        public static async Task DownloadImageInContentStoreWithProgress(ContainerImageInfo imageInfo, IVirtualMachineShell vmShell, DownloadProgressHandler progressHandler, int timeoutSec = 300)
        {
            var totalBytesToDownload = CheckImageBytesToDownload(imageInfo, vmShell);
            var previousBytesRemaining = totalBytesToDownload;
            progressHandler(totalBytesToDownload, 0, 0);

            var downloadTask = Task.Run(() => DownloadImageInContentStore(imageInfo, vmShell, timeoutSec));
            while (!downloadTask.IsCompleted)
            {
                var bytesRemaining = CheckImageBytesToDownload(imageInfo, vmShell);
                var bytesDownloaded = totalBytesToDownload - bytesRemaining;
                var percentProgress = 100;
                if(totalBytesToDownload > 0)
                {
                    percentProgress = (int)(bytesDownloaded / (float)totalBytesToDownload * 100);
                }
                progressHandler(totalBytesToDownload, bytesDownloaded, percentProgress);
                previousBytesRemaining = bytesRemaining;

                // test at a regular interval if the task is finished
                if (!downloadTask.IsCompleted)
                {
                    await Task.Delay(1000); // wait for 1 second before checking again
                }
            }

            if(downloadTask.IsFaulted)
            {
                throw downloadTask.Exception;
            }
        }

        public static int ApplyYamlFileAndWaitForResources(string yamlFileContent, string deploymentNamespace, IVirtualMachineShell vmShell, int timeoutSec=30)
        {
            // Save the yaml file content on  the VM disk
            vmShell.ExecuteCommand("mkdir", "-p KubernetesApps");
            vmShell.ExecuteCommand("echo", $"-e {ToLiteral(yamlFileContent)} > KubernetesApps/{deploymentNamespace}.yaml");

            // Create the namespace and apply the yaml file
            vmShell.ExecuteCommand("k3s", $"kubectl create namespace {deploymentNamespace}");
            return vmShell.ExecuteCommand("k3s", $"kubectl apply -f KubernetesApps/{deploymentNamespace}.yaml -n {deploymentNamespace} --wait", timeoutSec);
        }

        public static int DeleteResourcesFromYamlFile(string deploymentNamespace, IVirtualMachineShell vmShell)
        {
            // Delete the resources from the yaml file and delete the namespace            
            vmShell.ExecuteCommand("k3s", $"kubectl delete -f KubernetesApps/{deploymentNamespace}.yaml -n {deploymentNamespace} --wait");
            return vmShell.ExecuteCommand("k3s", $"kubectl delete namespace {deploymentNamespace}");
        }

        private static string ToLiteral(string input)
        {
            StringBuilder literal = new StringBuilder(input.Length + 2);
            literal.Append("\"");
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\"': literal.Append("\\\""); break;
                    case '`': literal.Append("\\`"); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        literal.Append(c);
                        break;
                }
            }
            literal.Append("\"");
            return literal.ToString();
        }
    }    
}
