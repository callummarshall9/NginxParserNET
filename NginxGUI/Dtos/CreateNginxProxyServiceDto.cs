using SystemdIntegration;
using NginxGUI.Entities;

namespace NginxGUI.Dtos 
{
    public class CreateNginxProxyServiceDto 
    {
        public string CreateTemplate { get; set; }
        public NginxProxyService ProxyService { get; set; }
        public CreateNginxProxyServiceDto()
        {
            ProxyService = new();
        }
    }
}