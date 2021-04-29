using System;
using System.Diagnostics;
using System.IO;
using LinuxPrivilegeEscalation;

namespace SystemdIntegration
{
    public class ServiceManager
    {
        private readonly string _serviceManagerPath;
        public ServiceManager(string serviceManagerPath)
        {
            _serviceManagerPath = serviceManagerPath;
        }

        public void ExecuteCommand(string command)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _serviceManagerPath,
                Arguments = command,
                RedirectStandardOutput = true
            })?.WaitForExit();
        }

        public void EnableServiceStartOnStartup(string serviceName)
        {
            ExecuteCommand($"enable {serviceName}");
        }

        public void DisableServiceStartOnStartup(string serviceName)
        {
            ExecuteCommand($"disable {serviceName}");
        }
        public void ReloadDaemon()
        {
            ExecuteCommand($"daemon-reload");
        }
        

        public bool ServiceActive(string serviceName)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = _serviceManagerPath,
                Arguments = $"is-active {serviceName}",
                RedirectStandardOutput = true
            });
            TextReader reader = new StreamReader(process?.StandardOutput?.BaseStream);
            var contents = reader.ReadToEnd();
            return contents.Trim() == "active";
        }

        public bool ServiceOnStartup(string serviceName)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = _serviceManagerPath,
                Arguments = $"is-enabled {serviceName}",
                RedirectStandardOutput = true
            });
            TextReader reader = new StreamReader(process?.StandardOutput?.BaseStream);
            var contents = reader.ReadToEnd();
            return contents.Trim() == "enabled";
        }

        public void StartService(string serviceName)
        {
            ExecuteCommand($"start {serviceName}");
        }
        
        public void StopService(string serviceName)
        {
            ExecuteCommand($"stop {serviceName}");
        }
    }
}