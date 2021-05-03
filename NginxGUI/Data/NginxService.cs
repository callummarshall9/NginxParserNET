using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using NginxGUI.Dtos;
using NginxParser.Collections;
using NginxParser.Entities;

namespace NginxGUI.Data
{
    public class NginxService
    {
        private readonly IConfiguration _configuration;
        private readonly string _nginxConfigDirectory;

        public NginxService(IConfiguration configuration)
        {
            _configuration = configuration;
            _nginxConfigDirectory = _configuration.GetValue("NginxConfigDirectory", "/etc/nginx/conf.d/");
        }
        public class NginxError
        {
            public string Code { get; set; }
            public string Description { get; set; }
        }
        
        public class NginxResult<T> : NginxResult
        {
            public T Result { get; set; }
        }
        
        public class NginxResult
        {
            public bool Succeeded { get; set; }
            public List<NginxError> Errors { get; set; }
        }

        public class NginxEntry
        {
            public string FileName { get; set; }
            public NginxServerList ServerList { get; set; }
        }

        public NginxResult Exception(Exception ex)
        {
            return new()
            {
                Succeeded = false,
                Errors = new List<NginxError>()
                {
                    new()
                    {
                        Code = ex.Message,
                        Description = ex.StackTrace
                    }
                }
            };
        }

        public NginxResult<T> Exception<T>(Exception ex) where T: class
        {
            return new()
            {
                Succeeded = false,
                Result = null,
                Errors = new List<NginxError>()
                {
                    new()
                    {
                        Code = ex.Message,
                        Description = ex.StackTrace
                    }
                }
            };
        }

        public NginxResult CreateNginxServerList(string name)
        {
            try
            {
                var content = new StringBuilder();
                NginxServerList serverList = new NginxServerList();
                serverList.Add(new NginxServer());
                serverList.Build(content);
                if (!File.Exists($"{_nginxConfigDirectory}{name}.conf"))
                {
                    File.WriteAllText($"{_nginxConfigDirectory}{name}.conf", content.ToString());
                    return new NginxResult
                    {
                        Succeeded = true
                    };
                }

                return new NginxResult
                {
                    Succeeded = false,
                    Errors = new List<NginxError>()
                    {
                        new ()
                        {
                            Code = "001",
                            Description = "File exists"
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return Exception(ex);
            }
        }
        
        public NginxResult<NginxServerList> ParseServerConfig(string serverConfigPath)
        {
            try
            {
                return new NginxResult<NginxServerList>
                {
                    Succeeded = true,
                    Result = NginxParser.NginxParser.Parse(File.ReadAllText($"{_nginxConfigDirectory}{serverConfigPath}"))
                };
            }
            catch (Exception ex)
            {
                return Exception<NginxServerList>(ex);
            }
        }

        public NginxResult<List<NginxEntry>> GetConfigs()
        {
            try
            {
                var fileList = Directory.GetFiles(_nginxConfigDirectory).Where(f => f.EndsWith(".conf"));
                return new NginxResult<List<NginxEntry>>
                {
                    Succeeded = true,
                    Result = fileList.Select(nginxConfig => new NginxEntry
                    {
                        ServerList = NginxParser.NginxParser.Parse(System.IO.File.ReadAllText($"{nginxConfig}")),
                        FileName = Path.GetFileName(nginxConfig)
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                return Exception<List<NginxEntry>>(ex);
            }
        }

        public NginxResult<NginxEntry> UpdateNginxConfig(string fileName, NginxServerList serverList)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                FileInfo fileInfo = new FileInfo($"{_nginxConfigDirectory}/{fileName}");
                File.Copy($"{_nginxConfigDirectory}{fileName}", $"{fileInfo.Directory.FullName}/{fileInfo.Name}_{DateTimeOffset.Now:yyyyMMMMddhhmmss}.bak");
                serverList.Build(stringBuilder);
                File.WriteAllText($"{_nginxConfigDirectory}{fileName}", stringBuilder.ToString());
                var result = CheckNginxConfig();
                if (!result.Succeeded)
                {
                    return new ()
                    {
                        Succeeded = result.Succeeded,
                        Errors = result.Errors.Concat(new List<NginxError> { new() { Code = "NGINX_ERR", Description = result.Result }}).ToList()
                    };
                }
                return new()
                {
                    Succeeded = true,
                    Result = new()
                    {
                        FileName = fileName,
                        ServerList = serverList
                    }
                };
            }
            catch (Exception ex)
            {
                return Exception<NginxEntry>(ex);
            }
        }

        public NginxResult<string> CheckNginxConfig()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/pkexec",
                Arguments = $"/usr/sbin/nginx -t",
                RedirectStandardError = true
            });
            var contents = process?.StandardError?.ReadToEnd();
            return new NginxResult<string>
            {
                Succeeded = !contents.Contains("failed"),
                Result = contents,
                Errors = contents.Split('\n').Where(f => f.Contains("[emerg]")).Select(e => new NginxError
                {
                    Code = "NGINX_ERR",
                    Description = e
                }).ToList()
            };
        }
        
        public void ReloadNginx()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/pkexec",
                Arguments = $"/usr/sbin/nginx -s reload",
                RedirectStandardError = true
            });
            process?.WaitForExit();
        }

        public NginxResult CreateCertificate(NginxServer server, CertificateRequestForm requestForm)
        {
            try
            {
                var templateScript = File.ReadAllText("certificateGen.sh");
                var crtPath = $"/etc/pki/tls/certs/{requestForm.CommonName}.crt";
                var keyPath = $"/etc/pki/tls/certs/{requestForm.CommonName}.key";
                var newScript = templateScript
                    .Replace("%csrPath%", $"/etc/pki/tls/certs/{requestForm.CommonName}.csr")
                    .Replace("%crtPath%", crtPath)
                    .Replace("%subject%", requestForm.Build())
                    .Replace("%keyPath%", keyPath)
                    .Replace("%pemPath%", $"/etc/pki/tls/certs/{requestForm.CommonName}.pem");
                var tmpFileName = "/tmp/certificateGen-tmp-" + new Random().Next() + ".sh";
                File.WriteAllText(tmpFileName, newScript);
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "/usr/bin/pkexec",
                    Arguments = $"/usr/bin/sh {tmpFileName}",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                });
                process?.WaitForExit();
                if (server.Properties.Exists(p => p.Key == "ssl_certificate"))
                    server.Properties.First(p => p.Key == "ssl_certificate").Value = crtPath;
                else
                    server.Properties.Add(new PropertyEntry {Key = "ssl_certificate", Value = crtPath});
                if (server.Properties.Exists(p => p.Key == "ssl_certificate_key"))
                    server.Properties.First(p => p.Key == "ssl_certificate_key").Value = keyPath;
                else
                    server.Properties.Add(new PropertyEntry {Key = "ssl_certificate_key", Value = keyPath});
                return new NginxResult()
                {
                    Succeeded = true
                };
            }
            catch (Exception ex)
            {
                return Exception(ex);
            }

        }

        public NginxResult<NginxEntry> DeleteNginxServer(string fileName, NginxEntry collection, NginxServer server)
        {
            try
            {
                collection.ServerList.Remove(server);
                File.Copy($"{_nginxConfigDirectory}{fileName}", $"{_nginxConfigDirectory}{fileName}_{DateTimeOffset.Now:yyyyMMMMddhhmmss}.bak");
                StringBuilder stringBuilder = new();
                collection.ServerList.Build(stringBuilder);
                if (collection.ServerList.Count > 0)
                    File.WriteAllText($"{_nginxConfigDirectory}{fileName}", stringBuilder.ToString());
                else
                    File.Delete($"{_nginxConfigDirectory}{fileName}");
                return new NginxResult<NginxEntry>
                {
                    Succeeded = true,
                    Result = collection
                };
            }
            catch (Exception ex)
            {
                return Exception<NginxEntry>(ex);
            }
        }
    }
}