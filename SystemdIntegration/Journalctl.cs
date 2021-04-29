using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SystemdIntegration
{
    public class Journalctl
    {
        public string SysLogIdentifier { get; set; }
        private string _journalctlPath;
        
        public Journalctl(string sysLogIdentifier = null, string journalctlPath=null)
        {
            SysLogIdentifier = sysLogIdentifier;
            _journalctlPath ??= "/usr/bin/journalctl";
        }

        public List<string> GetLogs()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = _journalctlPath,
                Arguments = $"-u {SysLogIdentifier} -b",
                RedirectStandardOutput = true
            });
            TextReader reader = new StreamReader(process?.StandardOutput?.BaseStream);
            var contents = reader.ReadToEnd();
            return contents.Split('\n').ToList();
        }
    }
}