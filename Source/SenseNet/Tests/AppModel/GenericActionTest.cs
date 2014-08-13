using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Portal;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;
using System.Web;
using System.IO;
using SenseNet.Portal.AppModel;
using SenseNet.Portal.ApplicationModel;
using SenseNet.ApplicationModel;
using SenseNet.Portal.OData;

namespace SenseNet.ContentRepository.Tests.AppModel
{
    internal static class GenericActionTest_Facade
    {
        // not granted
        public static string TestMethod0(Content content)
        {
            return "ok";
        }

        [ODataFunction]
        public static string TestMethod1(Content content)
        {
            return "ok";
        }
        [ODataFunction]
        public static string TestMethod1(Content content, string s, int i)
        {
            return string.Concat(s, ": ", i);
        }
        [ODataFunction]
        public static Content SingleContentTest(Content content, string path)
        {
            return Content.Load(path);
        }
        [ODataFunction]
        public static IEnumerable<Content> MultipleContentTest(Content content, string path)
        {
            var c = Content.Load(path);
            return c.Children;
        }
    }

    [TestClass]
    public class GenericActionTest : TestBase
    {
        #region Infrastructure

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
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
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
        #endregion
        #region Sandbox
        [ClassInitialize]
        public static void CreateSandbox(TestContext testContext)
        {
            var rootAppsFolder = Node.Load<Folder>("/Root/(apps)");
            if (rootAppsFolder == null)
            {
                rootAppsFolder = new SystemFolder(Repository.Root);
                rootAppsFolder.Name = "(apps)";
                rootAppsFolder.Save();
            }
            var rootAppsGenericContentFolder = Node.Load<Folder>("/Root/(apps)/GenericContent");
            if (rootAppsGenericContentFolder == null)
            {
                rootAppsGenericContentFolder = new SystemFolder(rootAppsFolder);
                rootAppsGenericContentFolder.Name = "GenericContent";
                rootAppsGenericContentFolder.Save();
            }
            var rootAppsGenericContent_TestAction0 = Node.Load<GenericODataApplication>("/Root/GenericActionTest/(apps)/GenericContent/TestAction1");
            if (rootAppsGenericContent_TestAction0 == null)
            {
                rootAppsGenericContent_TestAction0 = new GenericODataApplication(rootAppsGenericContentFolder);
                rootAppsGenericContent_TestAction0.Name = "TestAction0";
                rootAppsGenericContent_TestAction0.ClassName = "SenseNet.ContentRepository.Tests.AppModel.GenericActionTest_Facade";
                rootAppsGenericContent_TestAction0.MethodName = "TestMethod0";
                rootAppsGenericContent_TestAction0.Parameters = null;
                rootAppsGenericContent_TestAction0.Save();
            }
            var rootAppsGenericContent_TestAction1 = Node.Load<GenericODataApplication>("/Root/GenericActionTest/(apps)/GenericContent/TestAction1");
            if (rootAppsGenericContent_TestAction1 == null)
            {
                rootAppsGenericContent_TestAction1 = new GenericODataApplication(rootAppsGenericContentFolder);
                rootAppsGenericContent_TestAction1.Name = "TestAction1";
                rootAppsGenericContent_TestAction1.ClassName = "SenseNet.ContentRepository.Tests.AppModel.GenericActionTest_Facade";
                rootAppsGenericContent_TestAction1.MethodName = "TestMethod1";
                rootAppsGenericContent_TestAction1.Parameters = null;
                rootAppsGenericContent_TestAction1.Save();
            }
            var rootAppsGenericContent_TestAction2 = Node.Load<GenericODataApplication>("/Root/GenericActionTest/(apps)/GenericContent/TestAction2");
            if (rootAppsGenericContent_TestAction2 == null)
            {
                rootAppsGenericContent_TestAction2 = new GenericODataApplication(rootAppsGenericContentFolder);
                rootAppsGenericContent_TestAction2.Name = "TestAction2";
                rootAppsGenericContent_TestAction2.ClassName = "SenseNet.ContentRepository.Tests.AppModel.GenericActionTest_Facade";
                rootAppsGenericContent_TestAction2.MethodName = "TestMethod1";
                rootAppsGenericContent_TestAction2.Parameters = "string sss, int iii";
                rootAppsGenericContent_TestAction2.Save();
            }
            var rootAppsGenericContent_TestAction3 = Node.Load<GenericODataApplication>("/Root/GenericActionTest/(apps)/GenericContent/TestAction2");
            if (rootAppsGenericContent_TestAction3 == null)
            {
                rootAppsGenericContent_TestAction3 = new GenericODataApplication(rootAppsGenericContentFolder);
                rootAppsGenericContent_TestAction3.Name = "TestAction3";
                rootAppsGenericContent_TestAction3.ClassName = "SenseNet.ContentRepository.Tests.AppModel.GenericActionTest_Facade";
                rootAppsGenericContent_TestAction3.MethodName = "SingleContentTest";
                rootAppsGenericContent_TestAction3.Parameters = "string path";
                rootAppsGenericContent_TestAction3.Save();
            }
            var rootAppsGenericContent_TestAction4 = Node.Load<GenericODataApplication>("/Root/GenericActionTest/(apps)/GenericContent/TestAction2");
            if (rootAppsGenericContent_TestAction4 == null)
            {
                rootAppsGenericContent_TestAction4 = new GenericODataApplication(rootAppsGenericContentFolder);
                rootAppsGenericContent_TestAction4.Name = "TestAction4";
                rootAppsGenericContent_TestAction4.ClassName = "SenseNet.ContentRepository.Tests.AppModel.GenericActionTest_Facade";
                rootAppsGenericContent_TestAction4.MethodName = "MultipleContentTest";
                rootAppsGenericContent_TestAction4.Parameters = "string path";
                rootAppsGenericContent_TestAction4.Save();
            }
        }
        private static Page EnsureSiteStartPage(Site site)
        {
            var startPageName = "Home";
            var homePage = Node.Load<Page>(RepositoryPath.Combine(site.Path, startPageName));
            if (homePage == null)
            {
                homePage = new Page(site);
                homePage.Name = startPageName;
                homePage.GetBinary("Binary").SetStream(Tools.GetStreamFromString("<html><body><h1>TestPage</h1></body></html>"));
                homePage.Save();
                site.StartPage = homePage;
                site.Save();
            }
            else if (site.StartPage == null)
            {
                site.StartPage = homePage;
                site.Save();
            }

            return homePage;
        }
        private void RemoveSiteStartPage(Site site)
        {
            if (site.StartPage == null)
                return;
            site.StartPage = null;
            site.Save();
        }

        [ClassCleanup]
        public static void DestroySandbox()
        {
            var site = Node.Load<Site>("/Root/GenericActionTest");
            if (site != null)
                site.ForceDelete();
        }
        #endregion

        [TestMethod]
        public void GenericAction_RightOverload()
        {
            ODataTests.CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext("/OData.svc/Root('IMS')/TestAction2", "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream("{iii: 42, sss: 'asdf' }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = ODataTests.GetStringResult(output);
                }
                Assert.AreEqual("asdf: 42", result);
            }
            finally
            {
                ODataTests.CleanupTestSite();
            }
        }
        [TestMethod]
        public void GenericAction_SingleContent()
        {
            ODataTests.CreateTestSite();
            try
            {
                ODataTests.Entity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext("/OData.svc/Root('IMS')/TestAction3", "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{ path: '", User.Administrator.Path, "' }"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    entity = ODataTests.GetEntity(output);
                }
                Assert.AreEqual(1, entity.Id);
            }
            finally
            {
                ODataTests.CleanupTestSite();
            }
        }
        [TestMethod]
        public void GenericAction_MultipleContents()
        {
            ODataTests.CreateTestSite();
            try
            {
                ODataTests.Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext("/OData.svc/Root('IMS')/TestAction4", "$orderby=Name&$filter=Id lt 11", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream("{ path: '/Root/IMS/BuiltIn/Portal' }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    entities = ODataTests.GetEntities(output);
                }
                var actual = String.Join(", ", entities.Select(e => e.Name));
                Assert.AreEqual("Admin, Administrators, Creators, Everyone, LastModifiers, Visitor", actual);
            }
            finally
            {
                ODataTests.CleanupTestSite();
            }
        }
        [TestMethod]
        public void GenericAction_NotAnnotated()
        {
            ODataTests.CreateTestSite();
            try
            {
                ODataTests.Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext("/OData.svc/Root('IMS')/TestAction0", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
                    error = ODataTests.GetError(output);
                }
                Assert.IsTrue(error.Message.ToLower().Contains("access denied"));
            }
            finally
            {
                ODataTests.CleanupTestSite();
            }
        }

        private PortalContext CreatePortalContext(string page, string query)
        {
            var request = CreateRequest(page, query);
            var httpContext = new HttpContext(request);
            var portalContext = PortalContext.Create(httpContext);
            return portalContext;
        }
        private SimulatedHttpRequest CreateRequest(string page, string query)
        {
            var writer = new StringWriter();
            var request = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", page, query, writer, "testhost");
            return request;
        }

    }
}
