﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Schema;
using System.IO;
using System.Diagnostics;
using SenseNet.Communication.Messaging;
using SenseNet.ContentRepository.Storage.Data;
using System.Threading;
using SenseNet.Search.Indexing.Activities;

namespace SenseNet.ContentRepository.Tests.CrossDomain
{
	[TestClass()]
    public class CrossDomainTests : TestBase
	{
		#region Infrastructure
		private TestContext testContextInstance;

		public override TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}
		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before rucming the first test in the class
		//
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before rucming each test
		//
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion

		private Node __testRoot;
		private static int _remotedEngineCount = 3;
		private static AppDomain[] __domains;
        private static IRemotedTests __thisEngine = new RemotedTests();
        private static IRemotedTests[] __remotedEngines;

		private static string _testRootName = "_CrossDomainTests";
		private static string _testRootPath = String.Concat("/Root/", _testRootName);
		public Node TestRoot
		{
			get
			{
				if (__testRoot == null)
				{
					__testRoot = Node.LoadNode(_testRootPath);
					if (__testRoot == null)
					{
						Node node = NodeType.CreateInstance("SystemFolder", Node.LoadNode("/Root"));
						node.Name = _testRootName;
						node.Save();
						__testRoot = Node.LoadNode(_testRootPath);
					}
				}
				return __testRoot;
			}
		}
		private static AppDomain[] Domains
		{
			get
			{
				if (__domains == null)
				{
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var configPath = Path.Combine(baseDir, "sensenet.tests.dll.config");
					var info = new AppDomainSetup { ApplicationBase = baseDir, ConfigurationFile = configPath };
                    
					__domains = new AppDomain[_remotedEngineCount];
                    for (int i = 0; i < _remotedEngineCount; i++)
                    {
                        __domains[i] = AppDomain.CreateDomain("CrossTestsDomain" + i, AppDomain.CurrentDomain.Evidence, info);
                    }
				}
				return __domains;
			}
		}
        private static IRemotedTests ThisEngine { get { return __thisEngine; } }
		private static IRemotedTests[] RemotedEngines
		{
			get
			{
                if (__remotedEngines == null)
                {
                    lock (_remotedEnginesLock)
                    {
                        if (__remotedEngines == null)
                        {
                            var remotedEngines = new IRemotedTests[_remotedEngineCount];
                            for (int i = 0; i < _remotedEngineCount; i++)
                            {
                                remotedEngines[i] = (IRemotedTests)Domains[i].CreateInstanceAndUnwrap("SenseNet.Tests", "SenseNet.ContentRepository.Tests.CrossDomain.RemotedTests");
                                remotedEngines[i].Initialize(AppDomain.CurrentDomain.BaseDirectory);
                            }
                            __remotedEngines = remotedEngines;
                        }
                    }
                }
				return __remotedEngines;
			}
		}

        [ClassCleanup]
        public static void DestroyPlayground()
        {
            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);
        }

        static object _remotedEnginesLock = new object();
        [TestCleanup]
        public void RemoveRemotedEngines()
        {
            lock (_remotedEnginesLock)
            {
                if (__domains != null)
                {
                    for (int i = 0; i < _remotedEngineCount; i++)
                    {
                        AppDomain.Unload(__domains[i]);
                    }
                    __domains = null;
                }
                __remotedEngines = null;
            }
        }
        #endregion

        [TestMethod]
        public void Msmq_Config()
        {
            var msmq = DistributedApplication.ClusterChannel as MsmqChannelProvider;

            // check if receive queue is .\private$\ryan1
            Assert.IsTrue(msmq._receiveQueue.Path == ".\\private$\\ryan1", "Msmq local queue configuration fail.");

            // check if send queues are .\private$\ryan2 and .\private$\ryan3
            Assert.IsTrue(msmq._sendQueues.Count == 2, "Msmq remote queue configuration fail.");
            Assert.IsTrue(msmq._sendQueues[0].Path == ".\\private$\\ryan2", "Msmq remote queue configuration fail.");
            Assert.IsTrue(msmq._sendQueues[1].Path == ".\\private$\\ryan3", "Msmq remote queue configuration fail.");
        }


        /* ============================================================================= Msmq Send */
        [TestMethod]
        public void Msmq_Send()
        {
            var msmq = DistributedApplication.ClusterChannel as MsmqChannelProvider;

            // init: purge queues
            msmq._receiveQueue.Purge();
            for (var i = 0; i < msmq._sendQueues.Count; i++)
            {
                var sendqueue = msmq._sendQueues[i];
                sendqueue.Purge();
            }

            // distribute a single message, and check remote queues if message arrived
            var msg = new TestMessage();
            msg.Send();

            var formatter = new BinaryMessageFormatter();
            try
            {
                for (var i = 0; i < msmq._sendQueues.Count; i++)
                {
                    var sendqueue = msmq._sendQueues[i];
                    var recvmsg = sendqueue.Receive(TimeSpan.FromSeconds(3));
                    var recvtestmsg = formatter.Deserialize(recvmsg.Body as Stream) as TestMessage;
                    Assert.IsTrue(recvtestmsg.Message == msg.Message, "Received message differs from message that was sent.");  // check if we received the message that we have sent
                }
            }
            catch (System.Messaging.MessageQueueException mex)
            {
                if (mex.MessageQueueErrorCode == System.Messaging.MessageQueueErrorCode.IOTimeout)
                {
                    Assert.Fail("Receiving test message from remote queue timed out.");
                }
            }

        }

        [Serializable]
        public sealed class TestMessage : DebugMessage
        {
            public TestMessage()
            {
                Message = "Test message.";
            }
        }


        /* ============================================================================= Msmq Receive */
        [TestMethod]
        public void Msmq_Receive()
        {
            var msmq = DistributedApplication.ClusterChannel as MsmqChannelProvider;

            // init: purge queues
            msmq._receiveQueue.Purge();
            for (var i = 0; i < msmq._sendQueues.Count; i++)
            {
                var sendqueue = msmq._sendQueues[i];
                sendqueue.Purge();
            }

            // send a single message to the receive queue and check if it gets received and executed
            var message = new TestAction();

            var clusterMemberInfo = new ClusterMemberInfo();
            clusterMemberInfo.InstanceID = Guid.NewGuid().ToString();   // ensures message percieved as coming from other source

            message.SenderInfo = clusterMemberInfo;
            Stream messageStream = new BinaryMessageFormatter().Serialize(message);
            var m = new System.Messaging.Message(messageStream);
            m.TimeToBeReceived = TimeSpan.FromSeconds(RepositoryConfiguration.MessageRetentionTime);
            m.Formatter = new System.Messaging.BinaryMessageFormatter();

            _messageReceivedEvent = new AutoResetEvent(false);
            msmq._receiveQueue.Send(m); // send message to receivequeue

            // test for execution: _messageReceived should be set to true
            var received = _messageReceivedEvent.WaitOne(3000);
            Assert.IsTrue(received, "Distributed action was not received/executed within 3 seconds");
        }
        
        private static AutoResetEvent _messageReceivedEvent;

        [Serializable]
        private sealed class TestAction : DistributedAction
        {
            public TestAction()
            {
            }
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                _messageReceivedEvent.Set();
            }
        }


        /* ============================================================================= Send small/large indexdocument */
        [TestMethod]
        public void Msmq_SendSmallIndexDocument()
        {
            using (var loggedDataProvider = new LoggedDataProvider())
            {
                _indexDocumentContained = null;

                // create a single lucene message with SMALL indexdocument
                var sendmsg = CreateLucActivity(TestRoot.VersionId, false);

                // send it, receive it, process it
                TestIndexDocumentSending(sendmsg);

                Assert.IsTrue(_indexDocumentContained.Value == true, "Lucene activity does not contain index document, even though it should.");

                // check that retrieving index document from db never occurred
                var log = loggedDataProvider._GetLogAndClear();

                //LoadIndexDocumentByVersionId(versionId=<163>);
                Assert.IsFalse(log.Contains("LoadIndexDocumentByVersionId"), "Call to LoadIndexDocumentByVersionId happened, although it should not have.");

                //GetIndexDocumentDataFromReader(reader=<System.Data.SqlClient.SqlDataReader>);
                Assert.IsFalse(log.Contains("GetIndexDocumentDataFromReader"), "Call to GetIndexDocumentDataFromReader happened, although it should not have.");
            }
        }

        [TestMethod]
        public void Msmq_SendLargeIndexDocument()
        {
            using (var loggedDataProvider = new LoggedDataProvider())
            {
                _indexDocumentContained = null;

                // create a single lucene message with LARGE indexdocument
                var sendmsg = CreateLucActivity(TestRoot.VersionId, true);

                // send it, receive it, process it
                TestIndexDocumentSending(sendmsg);

                Assert.IsTrue(_indexDocumentContained.Value == false, "Lucene activity contains index document, even though it should not.");

                // check that retrieving index document from db occurred only once
                var log = loggedDataProvider._GetLogAndClear();

                //LoadIndexDocumentByVersionId(versionId=<163>);
                Assert.IsTrue(log.Split(new string[] { "LoadIndexDocumentByVersionId" }, StringSplitOptions.RemoveEmptyEntries).Length == 2, "Call to LoadIndexDocumentByVersionId happened more than once");

                //GetIndexDocumentDataFromReader(reader=<System.Data.SqlClient.SqlDataReader>);
                Assert.IsTrue(log.Split(new string[] { "GetIndexDocumentDataFromReader" }, StringSplitOptions.RemoveEmptyEntries).Length == 2, "Call to GetIndexDocumentDataFromReader happened more than once");
            }
        }

        [Serializable]
        private sealed class TestLuceneActivity : UpdateDocumentActivity
        {
            public TestLuceneActivity()
            {
            }
            internal override void Execute()
            {
                _indexDocumentContained = this.IndexDocumentData != null;
                base.Execute();
                _activityProcessedEvent.Set();
            }
        }

        private static AutoResetEvent _activityProcessedEvent;
        private static bool? _indexDocumentContained;
        private void TestIndexDocumentSending(DistributedLuceneActivity sendmsg)
        {
            // purge queues
            var msmq = DistributedApplication.ClusterChannel as MsmqChannelProvider;
            msmq._receiveQueue.Purge();
            for (var i = 0; i < msmq._sendQueues.Count; i++)
            {
                var sendqueue = msmq._sendQueues[i];
                sendqueue.Purge();
            }

            // distribute message
            sendmsg.Distribute();

            // grab message from send queue
            System.Messaging.Message recvmessage = null;
            try
            {
                recvmessage = msmq._sendQueues[0].Receive(TimeSpan.FromSeconds(3));
            }
            catch (System.Messaging.MessageQueueException mex)
            {
                if (mex.MessageQueueErrorCode == System.Messaging.MessageQueueErrorCode.IOTimeout)
                {
                    Assert.Fail("Receiving test message from remote queue timed out.");
                }
            }

            // check if message received is the one we have sent
            var lucmessage = CheckLucMessage(recvmessage);

            // simulate receiving/processing by calling OnMessageReceived
            _activityProcessedEvent = new AutoResetEvent(false);
            var remotedMessageStream = HackMessageAsRemote(lucmessage);
            msmq.OnMessageReceived(remotedMessageStream);

            // wait for message to be processed
            var processed = _activityProcessedEvent.WaitOne(3000);
            Assert.IsTrue(processed, "Lucene activity was not processed within 3 seconds");
        }
        private DistributedLuceneActivity.LuceneActivityDistributor CheckLucMessage(System.Messaging.Message recvmessage)
        {
            var formatter = new BinaryMessageFormatter();
            var lucmessage = formatter.Deserialize(recvmessage.Body as Stream) as DistributedLuceneActivity.LuceneActivityDistributor;
            var recactivity = lucmessage.Activity as TestLuceneActivity;
            Assert.IsTrue(recactivity != null, "Received message differs from message that was sent.");  // check if we received the message that we have sent
            return lucmessage;
        }
        private Stream HackMessageAsRemote(DistributedLuceneActivity.LuceneActivityDistributor message)
        {
            var formatter = new BinaryMessageFormatter();
            var clusterMemberInfo = new ClusterMemberInfo();
            clusterMemberInfo.InstanceID = Guid.NewGuid().ToString();   // ensures message percieved as coming from other source
            message.SenderInfo = clusterMemberInfo;
            return formatter.Serialize(message);
        }
        private TestLuceneActivity CreateLucActivity(int versionId, bool large)
        {
            var lucActivity = new TestLuceneActivity();
            lucActivity.VersionId = versionId;
            var node = Node.LoadNodeByVersionId(versionId) as GenericContent;
            var description = "a";
            if (large)
            {
                // create a large description
                var sb = new StringBuilder();
                var rnd = new Random();
                for (var i = 0; i < 200000; i++)
                {
                    sb.Append(CreateRandomWord(rnd.Next(20)));
                    sb.Append(" ");
                }
                description = sb.ToString();
            }

            node.Description = description;
            node.Save();
            node = Node.LoadNodeByVersionId(versionId) as GenericContent;

            bool hasBinary;
            lucActivity.IndexDocumentData = DataBackingStore.SaveIndexDocument(node, false, out hasBinary);

            if (large)
            {
                Assert.IsTrue(lucActivity.IndexDocumentData.IndexDocumentInfoSize > RepositoryConfiguration.MsmqIndexDocumentSizeLimit, "Created IndexDocument is expected to be large but it is only " + lucActivity.IndexDocumentData.IndexDocumentInfoSize.ToString() + " bytes");
            }
            else
            {
                Assert.IsTrue(lucActivity.IndexDocumentData.IndexDocumentInfoSize < RepositoryConfiguration.MsmqIndexDocumentSizeLimit, "Created IndexDocument is expected to be small but it is " + lucActivity.IndexDocumentData.IndexDocumentInfoSize.ToString() + " bytes");
            }

            return lucActivity;
        }
        private string CreateRandomWord(int length)
        {
            return Guid.NewGuid().ToString("N").Substring(0, length);
        }
    }
}
