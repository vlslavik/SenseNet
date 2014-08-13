using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Portal;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class GlobalHttpApptests : TestBase
    {
        #region test infrastructure
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
        //Use ClassInitialize to run code before running the first test in the class
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
        //Use TestInitialize to run code before running each test
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
        #endregion
        #region Playground
        private static string _testRootName = "_GlobalHttpApptests";
        private static string _testRootPath = String.Concat("/Root/", _testRootName);
        /// <summary>
        /// Do not use. Instead of TestRoot property
        /// </summary>
        private Node _testRoot;
        public Node TestRoot
        {
            get
            {
                if (_testRoot == null)
                {
                    _testRoot = Node.LoadNode(_testRootPath);
                    if (_testRoot == null)
                    {
                        Node node = NodeType.CreateInstance("SystemFolder", Node.LoadNode("/Root"));
                        node.Name = _testRootName;
                        node.Save();
                        _testRoot = Node.LoadNode(_testRootPath);
                    }
                }
                return _testRoot;
            }
        }
        [ClassCleanup]
        public static void DestroyPlayground()
        {
            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);
        }
        #endregion

        internal static EventArgs _testArgs = new EventArgs();

        [TestMethod]
        public void GlobalApp_OverriddenMethods()
        {
            var app = new Global();
            var appAcc = new PrivateObject(app);

            //appAcc.Invoke("Application_Start", this, _testArgs);
            appAcc.Invoke("Application_End", this, _testArgs);
            appAcc.Invoke("Application_Error", this, _testArgs);
            appAcc.Invoke("Application_BeginRequest", this, _testArgs);
            appAcc.Invoke("Application_EndRequest", this, _testArgs);

            var snGlobalAcc = new PrivateType(typeof(SenseNetGlobal));
            var snGlobalInstance = snGlobalAcc.GetStaticProperty("Instance") as TestGlobalHttpApp;
            Assert.IsNotNull(snGlobalInstance);

            var log = snGlobalInstance.Log.ToString();
            Assert.IsTrue(log == "Application_End,Application_Error,Application_BeginRequest,Application_EndRequest");
        }

        private class TestGlobalHttpApp : SenseNetGlobal
        {
            internal StringBuilder Log = new StringBuilder();

            private void Write(object msg)
            {
                if (Log.Length > 0)
                    Log.Append(",");
                Log.Append(msg);
            }
            private void Check(object sender, EventArgs e, System.Web.HttpApplication application)
            {
                if (e != GlobalHttpApptests._testArgs)
                    throw new InvalidOperationException("Invalid EventArgs");
                if(application.GetType() != typeof(Global))
                    throw new InvalidOperationException("The 'application' must be SenseNet.Portal.Global");
            }

            protected override void Application_Start(object sender, EventArgs e, System.Web.HttpApplication application)
            {
                Check(sender, e, application);
                Write("Application_Start");
            }
            protected override void Application_End(object sender, EventArgs e, System.Web.HttpApplication application)
            {
                Check(sender, e, application);
                Write("Application_End");
            }
            protected override void Application_Error(object sender, EventArgs e, System.Web.HttpApplication application)
            {
                Check(sender, e, application);
                Write("Application_Error");
            }
            protected override void Application_BeginRequest(object sender, EventArgs e, System.Web.HttpApplication application)
            {
                Check(sender, e, application);
                Write("Application_BeginRequest");
            }
            protected override void Application_EndRequest(object sender, EventArgs e, System.Web.HttpApplication application)
            {
                Check(sender, e, application);
                Write("Application_EndRequest");
            }
        }
    }
}
