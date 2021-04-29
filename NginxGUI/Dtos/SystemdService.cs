using System.Collections.Generic;
using System.Linq;

namespace NginxGUI.Dtos 
{
    
    public class SystemdService 
    {
        public string Description { get; set; }
        public string WorkingDirectory { get; set; }
        public string ExecStart { get; set; }
        public string Restart { get; set; }
        public string RestartSec { get; set; }
        public string KillSignal { get; set; }
        public string SyslogIdentifier { get; set; }
        public string User { get; set; }
        public List<string> Environment { get; set; } = new();
        public string WantedBy { get; set; } = "multi-user.target";

        public string Prefix { get; set; } = "/usr/bin/dotnet";

        public string Build()
        {
            return
$@"[Unit]
Description={Description}
[Service]
WorkingDirectory={WorkingDirectory}
ExecStart={(Prefix != string.Empty ? Prefix + $" \"{ExecStart}\"" : $"\"{ExecStart}\"")}
Restart={Restart}
RestartSec={RestartSec}
KillSignal={KillSignal}
SyslogIdentifier={SyslogIdentifier}
User={User}
{string.Join('\n', Environment.Select(e => "Environment=" + e))}
[Install]
WantedBy={WantedBy}
";
        }
    }
}