using System.Collections.Generic;
using System.Linq;
using NginxParser.Collections;
using NginxParser.Entities;
using NUnit.Framework;

namespace NginxTests
{
    public class NginxParserTests
    {
        
        private static string TestConfig = @"
		    server {
    			listen 443 ssl http2;
    			server_name *.localhost;
    			ssl_certificate /etc/pki/tls/certs/server.crt;
    			ssl_certificate_key /etc/pki/tls/certs/server.key;
    			location / {
    				proxy_pass http://[::1]:5000/;
    				proxy_http_version 1.1;
    				proxy_set_header Host $host;
    				proxy_set_header Upgrade $http_upgrade;
    				proxy_set_header Connection keep-alive;
    				proxy_cache_bypass $http_upgrade;
    				proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    				proxy_set_header X-Forwarded-Proto $scheme;
    			}
    			location /example/ {
		                    proxy_pass http://[::1]:5005/;
		                    proxy_http_version 1.1;
		                    proxy_set_header Host $host;
		                    proxy_set_header Upgrade $http_upgrade;
		                    proxy_set_header Connection keep-alive;
		                    proxy_cache_bypass $http_upgrade;
		                    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
		                    proxy_set_header X-Forwarded-Proto $scheme;
							location /foo {
								root /var/www/html
							}
    			}
		    }
		    
		    server {
    			listen 53263 ssl http2;
    			server_name localhost;
    			ssl_certificate /etc/pki/tls/certs/server.crt;
    			ssl_certificate_key /etc/pki/tls/certs/server.key;
    			location / {
    				proxy_pass http://[::1]:5005/;
					proxy_http_version 1.1;
    				proxy_set_header Host $host;
					proxy_set_header Upgrade $http_upgrade;
					proxy_set_header Connection keep-alive;
					proxy_cache_bypass $http_upgrade;
					proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
					proxy_set_header X-Forwarded-Proto $scheme;
    			}
		    }
		    
		    server {
    			listen 80;
    			server_name *.localhost;
    			rewrite https://$server_name$request_uri? permanent;
		    }
		";

        private NginxServerList nginxConfig;
        
        [SetUp]
        public void Setup()
        {
	        nginxConfig = NginxParser.NginxParser.Parse(TestConfig);
        }

        [Test]
        public void ServerTokensParsed()
        {
	        Assert.AreEqual(nginxConfig.Count,3);
        }

        [Test]
        public void LocationsParsed()
        {
	        Assert.AreEqual(nginxConfig[0].Locations.Count, 2);
	        Assert.AreEqual(nginxConfig[1].Locations.Count, 1);
        }

        [Test]
        public void NestedLocationsParsed()
        {
	        Assert.AreEqual(nginxConfig[0].Locations[1].Locations.Count, 1);
        }

        [Test]
        public void ServerNamesParsed()
        {
	        var serverNames = nginxConfig.SelectMany(k => k.Properties.Where(p => p.Key == "server_name").Select(p => p.Value)).ToList();
	        Assert.AreEqual(serverNames, new List<string>() { "*.localhost", "localhost", "*.localhost"});
        }

        [Test]
        public void ListenParsed()
        {
	        var listens = nginxConfig.SelectMany(k => k.Properties.Where(p => p.Key == "listen").Select(p => p.Value)).ToList();
	        Assert.AreEqual(listens, new List<string>() { "443 ssl http2", "53263 ssl http2", "80" });
        }
        public void PropertiesHasNoLocations(NginxLocation location)
        {
	        var r = location.Properties.Any(c => c.Key.Contains("location"));
	        Assert.AreEqual(r, false);
	        location.Locations.ForEach(PropertiesHasNoLocations);
        }

        [Test]
        public void LocationsNotInProperties()
        {
	        nginxConfig.ForEach(k => k.Locations.ForEach(PropertiesHasNoLocations));
        }
        
    }
}