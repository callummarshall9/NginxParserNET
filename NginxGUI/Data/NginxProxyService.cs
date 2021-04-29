using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SystemdIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NginxGUI.Dtos;
using NginxGUI.Entities;
using NginxParser.Entities;

namespace NginxGUI.Data 
{
    public class NginxProxyManagementService 
    {
        public ApplicationDbContext _dataContext;

        public class NginxProxyManagementError 
        {
            public string Code { get; set; }
            public string Description { get; set; }
        }
        
        public class NginxProxyManagementResult
        {
            public bool Succeeded { get; set; }
            public List<NginxProxyManagementError> Errors { get; set; }
        }
        
        public class NginxProxyManagementResult<T>: NginxProxyManagementResult where T: class
        {
            public T Result { get; set; }
        }
        
        private readonly string _nginxConfigDirectory;
        private readonly string _systemdTargetDirectory;
        private readonly string _systemCtlExecutablePath;
        private ServiceManager _serviceManager;

        public NginxProxyManagementService(ApplicationDbContext dataContext, IConfiguration configuration)
        {
            _nginxConfigDirectory = configuration.GetValue("NginxConfigDirectory", "/etc/nginx/conf.d/");
            _systemdTargetDirectory = configuration.GetValue("SystemdTargetsDirectory", "/etc/systemd/system/");
            _serviceManager = new ServiceManager(configuration.GetValue("SystemdExecutablePath", "/usr/bin/systemctl"));
            _dataContext = dataContext;
        }
        
        public NginxProxyManagementResult<T> Exception<T>(Exception ex) where T : class
        {
            return new()
            {
                Succeeded = false,
                Errors = new List<NginxProxyManagementError>()
                {
                    new()
                    {
                        Code = ex.Message,
                        Description = ex.StackTrace
                    }
                }
            };
        }

        public async Task<NginxProxyManagementResult<IList<NginxProxyService>>> CreateNginxProxyServices(IList<CreateNginxProxyServiceDto> services)
        {
            try
            {
                foreach (var nginxProxyServiceDto in services.Where(r => !File.Exists($"{_systemdTargetDirectory}{r.ProxyService.SystemdServiceName}") && r.CreateTemplate != string.Empty))
                {
                    if (string.IsNullOrEmpty(nginxProxyServiceDto.ProxyService.SystemdServiceName))
                        nginxProxyServiceDto.ProxyService.SystemdServiceName = $"{nginxProxyServiceDto.ProxyService.NginxFileName}.service";
                    await File.WriteAllTextAsync($"{_systemdTargetDirectory}{nginxProxyServiceDto.ProxyService.SystemdServiceName}", nginxProxyServiceDto.CreateTemplate);
                }
                await _dataContext.AddRangeAsync(services.Select(r => r.ProxyService));
                await _dataContext.SaveChangesAsync();
                return new NginxProxyManagementResult<IList<NginxProxyService>>()
                {
                    Result = services.Select(r => r.ProxyService).ToList(),
                    Succeeded = true
                };
            }
            catch (Exception ex)
            {
                return Exception<IList<NginxProxyService>>(ex);
            }
        }

        public async Task<NginxProxyManagementResult<IList<NginxProxyService>>> GetProxyServices(string nginxFileName)
        {
            try
            {
                return new NginxProxyManagementResult<IList<NginxProxyService>>()
                {
                    Result = await _dataContext.NginxProxyServices.Where(r => r.NginxFileName == nginxFileName).ToListAsync(),
                    Succeeded = true
                };
            }
            catch (Exception ex)
            {
                return Exception<IList<NginxProxyService>>(ex);
            }
        }
        

        public void EnableProxyServiceOnStartup(string systemdService) => _serviceManager.EnableServiceStartOnStartup(systemdService);
        
        public void DisableProxyServiceOnStartup(string systemdService) => _serviceManager.DisableServiceStartOnStartup(systemdService);

        public void ReloadDaemon() => _serviceManager.ReloadDaemon();
        public void StartProxyService(string systemdService) => _serviceManager.StartService(systemdService);

        public void StopProxyService(string systemdService) => _serviceManager.StopService(systemdService);

        public async Task<NginxProxyManagementResult<IList<NginxProxyService>>> DeleteProxyServices(string nginxFileName, string systemdService = null)
        {
            try
            {
                var proxyServices = await _dataContext.NginxProxyServices.Where(r => r.NginxFileName == nginxFileName && (systemdService != null && r.SystemdServiceName == systemdService)).ToListAsync();
                foreach (var nginxProxyService in proxyServices)
                {
                    if (File.Exists($"{_systemdTargetDirectory}/{nginxProxyService.SystemdServiceName}"))
                        File.Delete($"{_systemdTargetDirectory}/{nginxProxyService.SystemdServiceName}");
                }
                _dataContext.NginxProxyServices.RemoveRange(proxyServices);
                await _dataContext.SaveChangesAsync();
                return new()
                {
                    Succeeded = true,
                    Result = proxyServices
                };
            }
            catch (Exception ex)
            {
                return Exception<IList<NginxProxyService>>(ex);
            }
        }

        public void GenerateMissingProxyUrls(NginxProxyService service)
        {
            var existingProxyUrls = GetProxyUrls(service);
            if (existingProxyUrls.Succeeded && existingProxyUrls.Result.Length == 0)
                SetProxyUrls(service, new[] {"http://127.0.0.1:" + new Random().Next(0, 65536) + "/"});
        }

        public NginxProxyManagementResult<string[]> GetProxyUrls(NginxProxyService service)
        {
            try
            {
                var contents = File.ReadAllLines($"{_systemdTargetDirectory}/{service.SystemdServiceName}");
                var searchString = "Environment=ASPNETCORE_URLS=";
                return new NginxProxyManagementResult<string[]>
                {
                    Succeeded = true, 
                    Result = contents.Where(f => f.StartsWith(searchString))
                        .Select(e => e.Substring(searchString.Length, e.Length-searchString.Length)).FirstOrDefault()
                        ?.Split(',')
                        .ToArray() ?? Array.Empty<string>()
                };
            }
            catch (Exception ex)
            {
                return Exception<string[]>(ex);
            }
        }

        public NginxProxyManagementResult SetProxyUrls(NginxProxyService service, string[] urls)
        {
            var contents = File.ReadAllLines($"{_systemdTargetDirectory}/{service.SystemdServiceName}").ToList();
            var environmentItem = contents.FirstOrDefault(f => f.StartsWith("Environment=ASPNETCORE_URLS="));
            if (environmentItem == null)
                contents.Insert(contents.IndexOf("[Service]")+1, $"Environment=ASPNETCORE_URLS={string.Join(',', urls)}");
            else
                contents[contents.IndexOf(environmentItem)] = $"Environment=ASPNETCORE_URLS={string.Join(',', urls)}";
            File.WriteAllLines($"{_systemdTargetDirectory}/{service.SystemdServiceName}", contents);
            return new NginxProxyManagementResult
            {
                Succeeded = true
            };
        }

        public NginxProxyManagementResult LinkLocationToProxy(string proxyUrl, string systemdServiceName,  NginxLocation location)
        {
            location.Properties.Add(new PropertyEntry {Key = "# SystemdService Proxy", Value = systemdServiceName});
            location.Properties.Add(new PropertyEntry {Key = "proxy_pass", Value = proxyUrl});
            location.Properties.Add(new PropertyEntry {Key = "proxy_http_version", Value = "1.1"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "Upgrade $http_upgrade"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "Connection keep-alive"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "Host $host"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_cache_bypass", Value = "$http_upgrade"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "X-Forwarded-For $proxy_add_x_forwarded_for"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "X-Forwarded-Proto $scheme"});
            location.Properties.Add(new PropertyEntry {Key = "# SystemdService", Value = "end"});
            return new NginxProxyManagementResult
            {
                Succeeded = true
            };
        }

        public NginxProxyManagementResult LinkLocationToWebSocket(string proxyUrl, string systemdServiceName, NginxLocation location)
        {
            location.Properties.Add(new PropertyEntry {Key = "# SystemdService WebSocket", Value = systemdServiceName});
            location.Properties.Add(new PropertyEntry {Key = "proxy_pass", Value = proxyUrl});
            location.Properties.Add(new PropertyEntry {Key = "proxy_http_version", Value = "1.1"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "Upgrade $http_upgrade"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "Connection $http_upgrade"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "Host $host"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_cache_bypass", Value = "$http_upgrade"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "X-Forwarded-For $proxy_add_x_forwarded_for"});
            location.Properties.Add(new PropertyEntry {Key = "proxy_set_header", Value = "X-Forwarded-Proto $scheme"});
            location.Properties.Add(new PropertyEntry {Key = "# SystemdService", Value = "end"});
            return new NginxProxyManagementResult
            {
                Succeeded = true
            };
        }
        
    }
}