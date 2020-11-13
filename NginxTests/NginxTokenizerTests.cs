using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NginxParser;
using NUnit.Framework;

namespace NginxTests
{
    public class NginxTokenizerTests
    {
	    private static string TestConfigRelationship = @"
		    server {
    			listen 443 ssl http2;
    			server_name *.localhost;
    			ssl_certificate /etc/pki/tls/certs/server.crt;
    			ssl_certificate_key /etc/pki/tls/certs/server.key;
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
		";
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
								root /var/www/html;
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

        private List<TokenEntry> tokens;
        
        [SetUp]
        public void Setup()
        {
	        tokens = NginxParser.NginxTokenizer.Tokenize(TestConfig).ToList();
        }

        public void TokenHasCorrectControlLevel(TokenEntry tokenEntry)
        {
	        var currentControl = tokenEntry.ControlFlowLevel;
	        if (tokenEntry.Tokens.Any())
	        {
		        var maxChildControl = tokenEntry.Tokens.Max(k => k.ControlFlowLevel);
		        var minChildControl = tokenEntry.Tokens.Min(k => k.ControlFlowLevel);
		        tokenEntry.Tokens.ForEach(TokenHasCorrectControlLevel);
		        Assert.AreEqual(maxChildControl, minChildControl);
		        Assert.AreEqual(currentControl + 1, maxChildControl);
	        }
        }

        [Test]
        public void CorrectControlLevel()
        {
	        tokens.ForEach(TokenHasCorrectControlLevel);
        }
        
        [Test]
        public void ChildTokenLevelCorrect()
        {
	        Assert.AreEqual(tokens.SelectMany(k => k.Tokens).Select(k => k.ControlFlowLevel).Min(), 2);
	        Assert.AreEqual(tokens.SelectMany(k => k.Tokens).Select(k => k.ControlFlowLevel).Max(), 2);
        }

        public void TokenHasCorrectPlaceHolders(TokenEntry tokenEntry)
        {
	        if (tokenEntry.Tokens.Any())
	        {
		        Assert.AreEqual(tokenEntry.Content.Contains("[token]"), true);
		        try
		        {
			        Assert.AreEqual(Regex.Matches(tokenEntry.Content, Regex.Escape("[token]")).Count, tokenEntry.Tokens.Count);
			        tokenEntry.Tokens.ForEach(TokenHasCorrectControlLevel);
		        }
		        catch (Exception)
		        {
			        Console.WriteLine(tokenEntry.Name);
			        Console.WriteLine(tokenEntry.Content);
			        throw;
		        }

	        }
        }

        [Test]
        public void TokenPlaceHoldersCorrectSingleChild()
        {
	        tokens = NginxParser.NginxTokenizer.Tokenize(TestConfigRelationship);
	        tokens.ForEach(TokenHasCorrectPlaceHolders);
        }
        
        [Test]
        public void TokenPlaceHoldersCorrectMultipleChildren()
        {
	        tokens.ForEach(TokenHasCorrectPlaceHolders);
        }

        [Test]
        public void TokenLevelCorrect()
        {
	        Assert.AreEqual(tokens.Count, 3);
	        Assert.AreEqual(tokens.Max(k => k.ControlFlowLevel), 1);
        }

        [Test]
        public void FlattenCorrect()
        {
	        Assert.AreEqual(tokens.Any(k => k.FlattenContent().Contains("[token]")), false);
        }
    }
}