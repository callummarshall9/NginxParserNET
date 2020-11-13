using System.Collections.Generic;
using System.Linq;
using System.Text;
using NginxParser.Collections;
using NginxParser.Entities;
using NUnit.Framework;

namespace NginxTests
{
    public class NginxBuilderTests
    {
        private StringBuilder output;
        private NginxServerList testServers;
        [SetUp]
        public void Setup()
        {
            output = new StringBuilder();
            testServers = new NginxServerList();
            var testServer = new NginxServer();
            testServer.Properties.Add(new PropertyEntry
            {
                Key = "server_name",
                Value = "localhost"
            });
            testServer.Properties.Add(new PropertyEntry
            {
                Key = "listen",
                Value = "443 ssl http2"
            });
            testServer.Locations.Add(new NginxLocation
            {
                Name = "location /",
                Properties = new List<PropertyEntry>
                {
                    new PropertyEntry
                    {
                        Key = "root",
                        Value = "/var/www/html"
                    }
                }
            });
            testServers.Add(testServer);
        }
        [Test]
        public void Builds()
        {
            testServers.Build(output);
        }

        [Test]
        public void Parses()
        {
            Builds();
            NginxParser.NginxParser.Parse(output.ToString());
        }

        [Test]
        public void ParsedCorrectly()
        {
            Builds();
            var serverConfig = NginxParser.NginxParser.Parse(output.ToString());
            Assert.AreEqual(serverConfig.Count, 1);
            Assert.AreEqual(serverConfig[0].Properties.First(k => k.Key == "server_name").Value, "localhost");
            Assert.AreEqual(serverConfig[0].Locations.Count, 1);
            Assert.AreEqual(serverConfig[0].Locations[0].Properties.First(k => k.Key == "root").Value, "/var/www/html");
        }
    }
}