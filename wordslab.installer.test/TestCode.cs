using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using wordslab.installer.infrastructure;

namespace wordslab.installer.test
{
    [TestClass]
    public class TestCode
    {
        [TestMethod]
        public void DebugCode()
        {
            var procs = Process.GetProcessesByName("Vmmem");
            if(procs.Length > 0)
            {
                var vmProc = procs[0];
                int vmMemoryMB = (int)(vmProc.WorkingSet64 / (1024*1024));

                var computerInfo = new ComputerInfo();
                int totalMemoryGB = (int)Math.Round(computerInfo.TotalPhysicalMemory / (1024 * 1024 * 1024f), 0);
                int availableMemoryMB = (int)(computerInfo.AvailablePhysicalMemory / (1024 * 1024));

                int cpuCount = Environment.ProcessorCount;
            }
        }
    }
}
