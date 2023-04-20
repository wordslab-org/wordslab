using System.Text;
using wordslab.manager.config;

namespace wordslab.manager.os
{
    public interface IVirtualMachineShell
    {
        int ExecuteCommand(string command, string commandArguments = "", int timeoutSec = 10, Action<string> outputHandler = null, Action<string> errorHandler = null, Action<int> exitCodeHandler = null);
    }

    public class Kubernetes
    {
        public static int DownloadImageInContentStore(ContainerImageInfo imageInfo, IVirtualMachineShell vmShell)
        {
            // Containerd content flow docs: 
            // https://github.com/containerd/containerd/blob/main/docs/content-flow.md

            // Note: if you want to remove unused images for testing
            // k3s crictl rmi --prune

            // Image pull command:
            // ctr image pull ghcr.io/wordslab-org/lambda-stack-cuda:0.1.13-22.04.2
            return vmShell.ExecuteCommand("k3s", $"ctr image pull {imageInfo.Registry}/{imageInfo.Repository}:{imageInfo.Tag}");
        }

        public static long CheckImageBytesToDownload(ContainerImageInfo imageInfo, IVirtualMachineShell vmShell)
        {
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
                    var parts = line.Split(new char[] { '-', ' ' });
                    var digest = parts[1];
                    var sizeAndUnit = parts[2];
                    var multiply = 1L;
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
                    if (multiply > 1)
                    {
                        sizeAndUnit = sizeAndUnit.Substring(0, sizeAndUnit.Length - 2);
                    }
                    var size = (long)(float.Parse(sizeAndUnit) * multiply);
                    inprogressLayersDict.Add(digest, size);
                }
            }

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
                downloadedLayersSet.Add(line);
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

        public static int ApplyYamlFileAndWaitForResources(string yamlFileContent, string yamlFileName, IVirtualMachineShell vmShell, int timeoutSec=30)
        {
            vmShell.ExecuteCommand("mkdir", "-p KubernetesApps");
            vmShell.ExecuteCommand("echo", $"-e \"{ToLiteral(yamlFileContent)}\" > KubernetesApps/{yamlFileName}");
            return vmShell.ExecuteCommand("kubectl", $"apply -f KubernetesApps/{yamlFileName} --wait", timeoutSec);
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
