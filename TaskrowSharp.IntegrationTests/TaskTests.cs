﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TaskrowSharp.Tests
{
    [TestFixture]
    public class TaskTests
    {
        TaskrowClient taskrowClient;

        [OneTimeSetUp]
        public void Setup()
        {
            taskrowClient = UtilsTest.GetTaskrowClient();
        }

        [Test]
        public void ListTasksOpenByGroup_OK()
        {
            var groupTest = taskrowClient.ListGroups().First();

            var tasks = taskrowClient.ListTasksByGroup(groupTest.GroupID);
            Assert.IsTrue(tasks.Count > 0);
        }
    }
}