using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SystemdIntegration;

namespace NginxGUI.Entities 
{
    public class NginxProxyService 
    {
        [Key]
        public int Id { get; set; }
        public string NginxFileName { get; set; }
        public string SystemdServiceName { get; set; }

        private ServiceManager _manager;
        private Journalctl _journalctl;
        public NginxProxyService()
        {
            _manager = new ServiceManager("/usr/bin/systemctl");
            _journalctl = new Journalctl(sysLogIdentifier: SystemdServiceName);
        }

        public bool EnabledOnStartup => _manager.ServiceOnStartup(SystemdServiceName);

        public bool IsActive => _manager.ServiceActive(SystemdServiceName);

        public List<string> JournalCtlLogs
        {
            get 
            { 
                _journalctl.SysLogIdentifier = SystemdServiceName;
                return _journalctl.GetLogs();
            }
        }
    }
}