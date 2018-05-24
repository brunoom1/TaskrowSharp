﻿using System;
using NUnit.Framework;

namespace TaskrowSharp.IntegrationTests
{
    [TestFixture]
    public class ConnectTests
    {
        ConfigurationFile configurationFile;

        [OneTimeSetUp]
        public void Setup()
        {
            this.configurationFile = UtilsTest.GetConfigurationFile();
        }

        [Test]
        public void Connect_AccessKey_OK()
        {
            var taskrowClient = new TaskrowSharp.TaskrowClient();
            taskrowClient.Connect(new Uri(configurationFile.ServiceUrl), configurationFile.AccessKey);

            taskrowClient.Disconnect();
        }

        [Test]
        public void Connect_AccessKey_ServiceUrl_HttpsRequired()
        {
            var taskrowClient = new TaskrowSharp.TaskrowClient();

            Assert.Throws(typeof(InvalidServiceUrlException),
                () => taskrowClient.Connect(new Uri("http://yourdomain.taskrow.com"), configurationFile.AccessKey)
            );
        }

        [Test]
        public void Connect_AccessKey_ServiceUrl_Invalid()
        {
            var taskrowClient = new TaskrowSharp.TaskrowClient();

            Assert.Throws(typeof(InvalidServiceUrlException),
                () => taskrowClient.Connect(new Uri("http://test.com"), configurationFile.AccessKey)
            );
        }
        
        [Test]
        public void Connect_AccessKey_WrongAccessKey()
        {
            var taskrowClient = new TaskrowSharp.TaskrowClient();

            Assert.Throws(typeof(AuthenticationException),
                () => taskrowClient.Connect(new Uri(configurationFile.ServiceUrl), "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
            );
        }
    }
}
