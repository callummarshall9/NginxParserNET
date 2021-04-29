using NUnit.Framework;

namespace SystemdIntegration.Tests
{
    public class ServiceManagerTests
    {
        private ServiceManager _serviceManager;
        
        [SetUp]
        public void Setup()
        {
            _serviceManager = new ServiceManager("/usr/bin/systemctl");
        }

        [Test]
        public void AkmodsEnabled()
        {
            //Only valid on NVIDIA systems with RPMFusion driver.
            Assert.AreEqual(true, _serviceManager.ServiceOnStartup("akmods.service"));
        }

        [Test]
        public void NginxEnabled()
        {
            _serviceManager.EnableServiceStartOnStartup("nginx.service");
            //Assert.AreEqual(true, _serviceManager.ServiceOnStartup("nginx.service"));
            //_serviceManager.DisableServiceStartOnStartup("nginx.service");
        }
        
        
    }
}