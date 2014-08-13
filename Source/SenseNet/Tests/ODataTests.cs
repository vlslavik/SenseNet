using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Portal.Virtualization;
using System.Web;
using SenseNet.Portal;
using SenseNet.Portal.OData;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using SenseNet.Search;
using SenseNet.ContentRepository.Schema;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Storage.Security;
using System.Xml;
using SenseNet.ContentRepository.Tests.ContentHandlers;

namespace SenseNet.ContentRepository.Tests
{
    internal class ODataFilterTestHelper
    {
        public static string TestValue { get { return "Administrators"; } }
        internal class A
        {
            internal class B
            {
                public static string TestValue { get { return "Administrators"; } }
            }
        }
    }

    [ContentHandler]
    public class OData_Filter_ThroughReference_ContentHandler : GenericContent
    {
        public const string CTD = @"<?xml version='1.0' encoding='utf-8'?>
<ContentType name='OData_Filter_ThroughReference_ContentHandler' parentType='GenericContent' handler='SenseNet.ContentRepository.Tests.OData_Filter_ThroughReference_ContentHandler' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
  <Fields>
    <Field name='References' type='Reference'>
      <Configuration>
        <AllowMultiple>true</AllowMultiple>
        <AllowedTypes>
          <Type>OData_Filter_ThroughReference_ContentHandler</Type>
        </AllowedTypes>
      </Configuration>
    </Field>
  </Fields>
</ContentType>
";
        public OData_Filter_ThroughReference_ContentHandler(Node parent) : this(parent, null) { }
        public OData_Filter_ThroughReference_ContentHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected OData_Filter_ThroughReference_ContentHandler(NodeToken token) : base(token) { }

        public const string REFERENCES = "References";
        [RepositoryProperty(REFERENCES, RepositoryDataType.Reference)]
        public IEnumerable<Node> References
        {
            get { return this.GetReferences(REFERENCES); }
            set { this.SetReferences(REFERENCES, value); }
        }

    }


    [TestClass]
    public class ODataTests : TestBase
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

        private static string _testRootName = "_ODataTests";
        private static string _testRootPath = String.Concat("/Root/", _testRootName);
        /// <summary>
        /// Do not use. Instead of TestRoot property
        /// </summary>
        private static Node __testRoot;
        public static Node TestRoot
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

        [ClassInitialize]
        public static void InitializePlayground(TestContext testContext)
        {
            EnsureReferenceTestStructure();

            var content = Content.Create(User.Administrator);
            if (((IEnumerable<Node>)content["Manager"]).Count() > 0)
                return;
            content["Manager"] = User.Administrator;
            content["Email"] = "anybody@somewhere.com";
            content.Save();
        }
        [ClassCleanup]
        public static void DestroyPlayground()
        {
            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);
        }

        private const string TestSiteName = "ODataTestSite";
        private static string TestSitePath
        {
            get { return RepositoryPath.Combine("/Root/Sites", TestSiteName); }
        }

        private static void EnsureReferenceTestStructure()
        {
            if (ContentType.GetByName(typeof(OData_Filter_ThroughReference_ContentHandler).Name) == null)
                ContentTypeInstaller.InstallContentType(OData_Filter_ThroughReference_ContentHandler.CTD);

            var referrercontent = Content.Load(RepositoryPath.Combine(_testRootPath, "Referrer"));
            if (referrercontent == null)
            {
                var nodes = new Node[5];
                for (int i = 0; i < nodes.Length; i++)
                {
                    var content = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", TestRoot, "Referenced" + i);
                    content.Index = i + 1;
                    content.Save();
                    nodes[i] = content.ContentHandler;
                }

                referrercontent = Content.CreateNew("OData_Filter_ThroughReference_ContentHandler", TestRoot, "Referrer");
                var referrer = (OData_Filter_ThroughReference_ContentHandler)referrercontent.ContentHandler;
                referrer.References = nodes;
                referrercontent.Save();
            }
        }
        #endregion

        [TestMethod]
        public void SnJsonConverterTest_SimpleProjection()
        {
            // Create, save
            var content = Content.CreateNew("Car", __testRoot, "MyCar1");
            content["Make"] = "Citroen";
            content["Model"] = "C100";
            content["Price"] = 2399999.99;
            content.Save();

            // Reload
            content = Content.Load(content.Path);
            // Generate JSON
            var generatedJson = content.ToJson(new[] { "Id", "Path", "Name", "Make", "Model", "Price" }, null);

            // Run assertions
            var jobj = JObject.Parse(generatedJson);
            Assert.AreEqual(jobj["Id"], content.Id);
            Assert.AreEqual(jobj["Path"], content.Path);
            Assert.AreEqual(jobj["Name"], content.Name);
            Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
            Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
            Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);
        }

        [TestMethod]
        public void SnJsonConverterTest_WithExpand()
        {
            // Create, save
            var content = Content.CreateNew("Car", __testRoot, "MyCar2");
            content["Make"] = "Citroen";
            content["Model"] = "C101";
            content["Price"] = 4399999.99;
            content.Save();

            // Reload
            content = Content.Load(content.Path);
            // Generate JSON
            var generatedJson = content.ToJson(new[] { "Id", "Path", "Name", "Make", "Model", "Price", "CreatedBy/Id", "CreatedBy/Path" }, new[] { "CreatedBy" });

            // Run assertions
            var jobj = JObject.Parse(generatedJson);
            Assert.AreEqual(jobj["Id"], content.Id);
            Assert.AreEqual(jobj["Path"], content.Path);
            Assert.AreEqual(jobj["Name"], content.Name);
            Assert.AreEqual(jobj["Make"].Value<string>(), content["Make"]);
            Assert.AreEqual(jobj["Model"].Value<string>(), content["Model"]);
            Assert.AreEqual(jobj["Price"].Value<decimal>(), content["Price"]);
            Assert.AreEqual(jobj["CreatedBy"]["Id"], content.ContentHandler.CreatedBy.Id);
            Assert.AreEqual(jobj["CreatedBy"]["Path"], content.ContentHandler.CreatedBy.Path);
        }

        [TestMethod]
        public void OData_Urls_CurrentSite()
        {
            var site = CreateTestSite();
            var siteParentPath = RepositoryPath.GetParentPath(site.Path);
            var siteName = RepositoryPath.GetFileName(site.Path);
            try
            {
                string expectedJson = string.Concat(@"{""d"":{
                    ""__metadata"":{                    ""uri"":""/OData.svc", siteParentPath, @"('", siteName, @"')"",""type"":""Site""},
                    ""Manager"":{""__deferred"":{       ""uri"":""/OData.svc", siteParentPath, @"('", siteName, @"')/Manager""}},
                    ""CreatedBy"":{""__deferred"":{     ""uri"":""/OData.svc", siteParentPath, @"('", siteName, @"')/CreatedBy""}},
                    ""ModifiedBy"":{""__deferred"":{    ""uri"":""/OData.svc", siteParentPath, @"('", siteName, @"')/ModifiedBy""}}}}")
                    .Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                string json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(ODataTools.GetODataUrl(site.Path), "$select=Manager,CreatedBy,ModifiedBy&metadata=minimal", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    json = GetStringResult(output);
                }
                var result = json.Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                Assert.IsTrue(result == expectedJson, String.Format("Result is: {0}, expected: {1}", result, expectedJson));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Parsing_TopSkip()
        {
            CreateTestSite();
            try
            {
                PortalContext pc;
                ODataHandler handler;
                //---------------------------------------- without top, without skip
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.Top == 0, string.Format("Top is {0}, expected: 0.", handler.ODataRequest.Top));
                    Assert.IsTrue(handler.ODataRequest.Skip == 0, string.Format("Skip is {0}, expected: 0.", handler.ODataRequest.Skip));
                    Assert.IsTrue(!handler.ODataRequest.HasTop, "HasTop is true, expected: false.");
                    Assert.IsTrue(!handler.ODataRequest.HasSkip, "HasSkip is true, expected: false.");
                }

                //---------------------------------------- top 3, without skip
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$top=3", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.Top == 3, string.Format("Top is {0}, expected: 3.", handler.ODataRequest.Top));
                    Assert.IsTrue(handler.ODataRequest.Skip == 0, string.Format("Skip is {0}, expected: 0.", handler.ODataRequest.Skip));
                    Assert.IsTrue(handler.ODataRequest.HasTop, "HasTop is false, expected: true.");
                    Assert.IsTrue(!handler.ODataRequest.HasSkip, "HasSkip is true, expected: false.");
                }

                //---------------------------------------- without top, skip 4
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$skip=4", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.Top == 0, string.Format("Top is {0}, expected: 0.", handler.ODataRequest.Top));
                    Assert.IsTrue(handler.ODataRequest.Skip == 4, string.Format("Skip is {0}, expected: 4.", handler.ODataRequest.Skip));
                    Assert.IsTrue(!handler.ODataRequest.HasTop, "HasTop is true, expected: false.");
                    Assert.IsTrue(handler.ODataRequest.HasSkip, "HasSkip is false, expected: true.");
                }

                //---------------------------------------- top 3, skip 4
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$top=3&$skip=4", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.Top == 3, string.Format("Top is {0}, expected: 3.", handler.ODataRequest.Top));
                    Assert.IsTrue(handler.ODataRequest.Skip == 4, string.Format("Skip is {0}, expected: 4.", handler.ODataRequest.Skip));
                    Assert.IsTrue(handler.ODataRequest.HasTop, "HasTop is false, expected: true.");
                    Assert.IsTrue(handler.ODataRequest.HasSkip, "HasSkip is false, expected: true.");
                }

                //---------------------------------------- top 0, skip 0
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$top=0&$skip=0", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.Top == 0, string.Format("Top is {0}, expected: 0.", handler.ODataRequest.Top));
                    Assert.IsTrue(handler.ODataRequest.Skip == 0, string.Format("Skip is {0}, expected: 0.", handler.ODataRequest.Skip));
                    Assert.IsTrue(!handler.ODataRequest.HasTop, "HasTop is true, expected: false.");
                    Assert.IsTrue(!handler.ODataRequest.HasSkip, "HasSkip is true, expected: false.");
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_InvalidTop()
        {
            CreateTestSite();
            try
            {
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$top=-3", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.NegativeTopParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.NegativeTopParameter));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_InvalidSkip()
        {
            CreateTestSite();
            try
            {
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$skip=-4", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.NegativeSkipParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.NegativeSkipParameter));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_InlineCount()
        {
            CreateTestSite();
            try
            {
                PortalContext pc;
                ODataHandler handler;

                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=none", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.InlineCount == InlineCount.None, string.Format("InlineCount is {0}, expected: None.", handler.ODataRequest.InlineCount));
                    Assert.IsTrue(!handler.ODataRequest.HasInlineCount, "HasInlineCount is true, expected: false.");
                }

                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=0", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.InlineCount == InlineCount.None, string.Format("InlineCount is {0}, expected: None.", handler.ODataRequest.InlineCount));
                    Assert.IsTrue(!handler.ODataRequest.HasInlineCount, "HasInlineCount is true, expected: false.");
                }

                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=allpages", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.InlineCount == InlineCount.AllPages, string.Format("InlineCount is {0}, expected: AllPages.", handler.ODataRequest.InlineCount));
                    Assert.IsTrue(handler.ODataRequest.HasInlineCount, "HasInlineCount is false, expected: true.");
                }

                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=1", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.InlineCount == InlineCount.AllPages, string.Format("InlineCount is {0}, expected: AllPages.", handler.ODataRequest.InlineCount));
                    Assert.IsTrue(handler.ODataRequest.HasInlineCount, "HasInlineCount is false, expected: true.");
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_InvalidInlineCount()
        {
            CreateTestSite();
            try
            {
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=asdf", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidInlineCountParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.InvalidInlineCountParameter));
                }
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$inlinecount=2", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidInlineCountParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.InvalidInlineCountParameter));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_OrderBy()
        {
            CreateTestSite();
            try
            {
                PortalContext pc;
                ODataHandler handler;

                //----------------------------------------------------------------------------- sorting: -
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = handler.ODataRequest.Sort.ToArray();
                    Assert.IsTrue(handler.ODataRequest.HasSort == false, "HasSort is true.");
                    Assert.IsTrue(sort.Length == 0, string.Format("Sort.Count is {0}, expected: 0.", sort.Length));
                }

                //----------------------------------------------------------------------------- sorting: Id
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=Id", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = handler.ODataRequest.Sort.ToArray();
                    Assert.IsTrue(handler.ODataRequest.HasSort, "HasSort is false.");
                    Assert.IsTrue(sort.Length == 1, string.Format("Sort.Count is {0}, expected: 1.", sort.Length));
                    Assert.IsTrue(sort[0].FieldName == "Id", string.Format("Sort[0].Name is {0}, expected: 'Id'.", sort[0].FieldName));
                    Assert.IsTrue(sort[0].Reverse == false, "Sort[0].Reverse is true, expected: false.");
                }

                //----------------------------------------------------------------------------- sorting: Name asc
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=Name asc", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = handler.ODataRequest.Sort.ToArray();
                    Assert.IsTrue(handler.ODataRequest.HasSort, "HasSort is false.");
                    Assert.IsTrue(sort.Length == 1, string.Format("Sort.Count is {0}, expected: 1.", sort.Length));
                    Assert.IsTrue(sort[0].FieldName == "Name", string.Format("Sort[0].Name is {0}, expected: 'Name'.", sort[0].FieldName));
                    Assert.IsTrue(sort[0].Reverse == false, "Sort[0].Reverse is true, expected: false.");
                }

                //----------------------------------------------------------------------------- sorting: DisplayName desc
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=DisplayName desc", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = handler.ODataRequest.Sort.ToArray();
                    Assert.IsTrue(handler.ODataRequest.HasSort, "HasSort is false.");
                    Assert.IsTrue(sort.Length == 1, string.Format("Sort.Count is {0}, expected: 1.", sort.Length));
                    Assert.IsTrue(sort[0].FieldName == "DisplayName", string.Format("Sort[0].Name is {0}, expected: 'DisplayName'.", sort[0].FieldName));
                    Assert.IsTrue(sort[0].Reverse == true, "Sort[0].Reverse is true, expected: false.");
                }

                //----------------------------------------------------------------------------- sorting: ModificationDate desc, Category, Name
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$orderby=   ModificationDate desc    ,   Category   ,    Name", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var sort = handler.ODataRequest.Sort.ToArray();
                    Assert.IsTrue(handler.ODataRequest.HasSort, "HasSort is false.");
                    Assert.IsTrue(sort.Length == 3, string.Format("Sort.Count is {0}, expected: 3.", sort.Length));
                    Assert.IsTrue(sort[0].FieldName == "ModificationDate", string.Format("Sort[0].Name is {0}, expected: 'ModificationDate'.", sort[0].FieldName));
                    Assert.IsTrue(sort[0].Reverse == true, "Sort[0].Reverse is true, expected: false.");
                    Assert.IsTrue(sort[1].FieldName == "Category", string.Format("Sort[1].Name is {0}, expected: 'ModificationDate'.", sort[1].FieldName));
                    Assert.IsTrue(sort[1].Reverse == false, "Sort[1].Reverse is false, expected: true.");
                    Assert.IsTrue(sort[2].FieldName == "Name", string.Format("Sort[2].Name is {0}, expected: 'ModificationDate'.", sort[2].FieldName));
                    Assert.IsTrue(sort[2].Reverse == false, "Sort[2].Reverse is false, expected: true.");
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_InvalidOrderBy()
        {
            CreateTestSite();
            try
            {
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$orderby=asdf asd", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByDirectionParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.InvalidInlineCountParameter));
                }
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$orderby=asdf asc desc", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidOrderByParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.InvalidInlineCountParameter));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_Format()
        {
            CreateTestSite();
            try
            {
                PortalContext pc;
                ODataHandler handler;

                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$format=json", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.Format == "json", string.Format("Format is {0}, expected: json.", handler.ODataRequest.Format));
                }

                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$format=verbosejson", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    Assert.IsTrue(handler.ODataRequest.Format == "verbosejson", string.Format("Format is {0}, expected: verbosejson.", handler.ODataRequest.Format));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_InvalidFormat()
        {
            CreateTestSite();
            try
            {
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$format=atom", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidFormatParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.InvalidFormatParameter));
                }
                //using (var output = new System.IO.StringWriter())
                //{
                //    var pc = CreatePortalContext("/OData.svc/Root", "$format=xml", output);
                //    var handler = new ODataHandler();
                //    handler.ProcessRequest(pc.OwnerHttpContext);
                //    var code = GetExceptionCode(output);
                //    Assert.IsTrue(code == ODataExceptionCode.InvalidFormatParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.InvalidFormatParameter));
                //}
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$format=xxx", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var code = GetExceptionCode(output);
                    Assert.IsTrue(code == ODataExceptionCode.InvalidFormatParameter, string.Format("ErrorCode is {0}, expected: {1}.", code, ODataExceptionCode.InvalidFormatParameter));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Parsing_Select()
        {
            CreateTestSite();
            try
            {
                PortalContext pc;
                ODataHandler handler;

                //----------------------------------------------------------------------------- select: -
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var select = handler.ODataRequest.Select;
                    Assert.IsTrue(handler.ODataRequest.HasSelect == false, "HasSelect is true.");
                    Assert.IsTrue(select.Count == 0, string.Format("Select.Count is {0}, expected: 0.", select.Count));
                }

                //----------------------------------------------------------------------------- select: Id, DisplayName, ModificationDate
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$select=    Id  ,\tDisplayName\r\n\t,   ModificationDate   ", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var select = handler.ODataRequest.Select;
                    Assert.IsTrue(handler.ODataRequest.HasSelect, "HasSelect is false.");
                    Assert.IsTrue(select.Count == 3, string.Format("select.Count is {0}, expected: 1.", select.Count));
                    Assert.IsTrue(select[0] == "Id", string.Format("select[0].Name is {0}, expected: 'Id'.", select[0]));
                    Assert.IsTrue(select[1] == "DisplayName", string.Format("select[1].Name is {0}, expected: 'DisplayName'.", select[0]));
                    Assert.IsTrue(select[2] == "ModificationDate", string.Format("select[2].Name is {0}, expected: 'ModificationDate'.", select[0]));
                }

                //----------------------------------------------------------------------------- select: *
                using (var output = new System.IO.StringWriter())
                {
                    pc = CreatePortalContext("/OData.svc/Root", "$select=*", output);
                    handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var select = handler.ODataRequest.Select;
                    Assert.IsTrue(handler.ODataRequest.HasSelect == false, "HasSelect is true.");
                    Assert.IsTrue(select.Count == 0, string.Format("Select.Count is {0}, expected: 0.", select.Count));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Getting_Collection()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    //var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
                var origIds = folder.Children.Select(f => f.Id);
                var Ids = entities.Select(e => e.Id);
                Assert.IsTrue(origIds.Except(Ids).Count() == 0, "#1");
                Assert.IsTrue(Ids.Except(origIds).Count() == 0, "#2");
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_Entity()
        {
            CreateTestSite();
            try
            {
                Entity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entity = GetEntity(output);
                }
                var nodeHead = NodeHead.Get(entity.Path);
                Assert.IsTrue(nodeHead.Id == entity.Id, string.Format("nodeHead.Id ({0}) and entity.Id ({1}) are not equal", nodeHead.Id, entity.Id));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_NotExistentEntity()
        {
            CreateTestSite();
            try
            {
                string responseStatus = null;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('AAAA')", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    responseStatus = pc.OwnerHttpContext.Response.Status;
                }
                Assert.IsTrue(responseStatus == "404 Not Found", string.Format("responseStatus is {0}, expected {1}", responseStatus, "404 Not Found"));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_NotExistentProperty()
        {
            CreateTestSite();
            try
            {
                string errorCode;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/aaaa", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    errorCode = GetExceptionCodeText(output);
                }

                Assert.AreEqual("UnknownAction", errorCode, "errorCode is not correct");
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_SimplePropertyAndRaw()
        {
            CreateTestSite();
            try
            {
                string json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/Id", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    json = GetStringResult(output);
                }
                Assert.IsTrue(json.Replace("\r\n", "").Replace("\t", "").Replace(" ", "") == "{\"d\":{\"Id\":3}}", "#1");
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/Id/$value", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    json = GetStringResult(output);
                }
                Assert.IsTrue(json == "3", "#2");
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_CollectionProperty()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var group = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators");
                var origIds = group.Members.Select(f => f.Id);
                var Ids = entities.Select(e => e.Id);
                Assert.IsTrue(origIds.Except(Ids).Count() == 0, "#1");
                Assert.IsTrue(Ids.Except(origIds).Count() == 0, "#2");
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Getting_Collection_Projection()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    //var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "$select=Id,Name", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }

                var itemIndex = 0;
                foreach (var entity in entities)
                {
                    var props = entity.AllProperties.ToArray();
                    Assert.IsTrue(props.Length == 3, string.Format("Item#{0}: AllProperties.Count id ({1}), expected: 3", itemIndex, props.Length));
                    Assert.IsTrue(props[0].Key == "__metadata", string.Format("Item#{0}: AllProperties[0] is ({1}), expected: '__metadata'", itemIndex, props[0].Key));
                    Assert.IsTrue(props[1].Key == "Id", string.Format("Item#{0}: AllProperties[1] is ({1}), expected: 'Id'", itemIndex, props[1].Key));
                    Assert.IsTrue(props[2].Key == "Name", string.Format("Item#{0}: AllProperties[2] is ({1}), expected: 'Name'", itemIndex, props[2].Key));
                    itemIndex++;
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Getting_Entity_Projection()
        {
            CreateTestSite();
            try
            {
                Entity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')", "$select=Id,Name", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entity = GetEntity(output);
                }
                var props = entity.AllProperties.ToArray();
                Assert.IsTrue(props.Length == 3, string.Format("AllProperties.Count id ({0}), expected: 3", props.Length));
                Assert.IsTrue(props[0].Key == "__metadata", string.Format("AllProperties[0] is ({0}), expected: '__metadata'", props[0].Key));
                Assert.IsTrue(props[1].Key == "Id", string.Format("AllProperties[1] is ({0}), expected: 'Id'", props[1].Key));
                Assert.IsTrue(props[2].Key == "Name", string.Format("AllProperties[2] is ({0}), expected: 'Name'", props[2].Key));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_Entity_NoProjection()
        {
            CreateTestSite();
            try
            {
                Entity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entity = GetEntity(output);
                }
                var allowedFieldNames = new List<string>();
                var c = Content.Load("/Root/IMS");
                var ct = c.ContentType;
                var fieldNames = ct.FieldSettings.Select(f => f.Name);
                allowedFieldNames.AddRange(fieldNames);
                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder" });

                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

                var a = entityPropNames.Except(allowedFieldNames).ToArray();
                var b = allowedFieldNames.Except(entityPropNames).ToArray();

                Assert.IsTrue(a.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", a)));
                Assert.IsTrue(b.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", b)));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_ContentList_NoProjection()
        {
            string listDef = @"<?xml version='1.0' encoding='utf-8'?>
<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
	<Fields>
		<ContentListField name='#ListField1' type='ShortText'/>
		<ContentListField name='#ListField2' type='Integer'/>
		<ContentListField name='#ListField3' type='Reference'/>
	</Fields>
</ContentListDefinition>
";
            string path = RepositoryPath.Combine(TestRoot.Path, "Cars");
            if (Node.Exists(path))
                Node.ForceDelete(path);
            ContentList list = new ContentList(TestRoot);
            list.Name = "Cars";
            list.ContentListDefinition = listDef;
            list.AllowedChildTypes = new ContentType[] { ContentType.GetByName("Car") };
            list.Save();

            var car = Content.CreateNew("Car", list, "Car1");
            car.Save();
            car = Content.CreateNew("Car", list, "Car2");
            car.Save();

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    //var odataPath = ODataHandler.GetODataPath(list.Path, car.Name);
                    var pc = CreatePortalContext("/OData.svc" + list.Path, "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var entity = entities.First();
                var entityPropNames = entity.AllProperties.Select(y => y.Key).ToArray();

                var allowedFieldNames = new List<string>();
                allowedFieldNames.AddRange(ContentType.GetByName("Car").FieldSettings.Select(f => f.Name));
                allowedFieldNames.AddRange(ContentType.GetByName("File").FieldSettings.Select(f => f.Name));
                allowedFieldNames.AddRange(list.ListFieldNames);
                allowedFieldNames.AddRange(new[] { "__metadata", "IsFile", "Actions", "IsFolder" });
                allowedFieldNames = allowedFieldNames.Distinct().ToList();

                var a = entityPropNames.Except(allowedFieldNames).ToArray();
                var b = allowedFieldNames.Except(entityPropNames).ToArray();

                Assert.IsTrue(a.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", a)));
                Assert.IsTrue(b.Length == 0, String.Format("Expected empty but contains: '{0}'", string.Join("', '", b)));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_ContentQuery()
        {
            var folderName = "OData_ContentQuery";
            var site = CreateTestSite();
            try
            {
                var folder = Node.Load<Folder>(RepositoryPath.Combine(site.Path, folderName));
                if (folder == null)
                {
                    var f = Content.CreateNew("Folder", site, folderName);
                    f.Save();
                    folder = (Folder)f.ContentHandler;
                }

                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "qwer" } }).Save();
                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", site, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "qwer" } }).Save();

                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "asdf" }, { "Model", "qwer" } }).Save();
                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "asdf" } }).Save();
                Content.CreateNewAndParse("Car", folder, null, new Dictionary<string, string> { { "Make", "qwer" }, { "Model", "qwer" } }).Save();

                var expectedQueryGlobal = "asdf AND Type:Car .SORT:Path .AUTOFILTERS:OFF";
                var expectedGlobal = string.Join(", ", ContentQuery.Query(expectedQueryGlobal).Nodes.Select(n => n.Id.ToString()));

                var expectedQueryLocal = String.Format("asdf AND Type:Car AND InTree:'{0}' .SORT:Path .AUTOFILTERS:OFF", folder.Path);
                var expectedLocal = string.Join(", ", ContentQuery.Query(expectedQueryLocal).Nodes.Select(n => n.Id.ToString()));

                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root", "$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var realGlobal = String.Join(", ", entities.Select(e => e.Id));
                Assert.IsTrue(expectedGlobal == realGlobal, String.Format("Local: The result is {0}. Expected: {1}", realGlobal, expectedGlobal));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/" + folderName, "$select=Id,Path&query=asdf+AND+Type%3aCar+.SORT%3aPath+.AUTOFILTERS%3aOFF", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var realLocal = String.Join(", ", entities.Select(e => e.Id));
                Assert.IsTrue(expectedLocal == realLocal, String.Format("Local: The result is {0}. Expected: {1}", realLocal, expectedLocal));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Getting_Collection_OrderTopSkipCount()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/System/Schema/ContentTypes/GenericContent", "$orderby=Name desc&$skip=4&$top=3&$inlinecount=allpages", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var Ids = entities.Select(e => e.Id);
                var origIds = ContentQuery.Query("+InFolder:/Root/System/Schema/ContentTypes/GenericContent .REVERSESORT:Name .SKIP:4 .TOP:3 .AUTOFILTERS:OFF").Nodes.Select(n => n.Id);
                var expected = String.Join(", ", origIds);
                var actual = String.Join(", ", Ids);
                Assert.AreEqual(expected, actual);
                Assert.AreEqual(ContentType.GetByName("GenericContent").ChildTypes.Count, entities.TotalCount);
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Getting_Collection_Count()
        {
            CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    //var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    result = GetStringResult(output);
                }
                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
                Assert.AreEqual(folder.Children.Count().ToString(), result);
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Getting_Collection_CountTop()
        {
            CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    //var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Members", "", output);
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal/$count", "$top=3", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    result = GetStringResult(output);
                }
                var folder = Node.Load<Folder>("/Root/IMS/BuiltIn/Portal");
                Assert.AreEqual("3", result);
            }
            finally
            {
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Expand()
        {
            var count = ContentQuery.Query("InFolder:/Root/IMS/BuiltIn/Portal .COUNTONLY").Count;
            var expectedJson = String.Concat(@"
{
  ""d"": {
    ""__metadata"": {
      ""uri"": ""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')"",
      ""type"": ""Group""
    },
    ""Id"": 7,
    ""Members"": [
      {
        ""__metadata"": {
          ""uri"": ""/OData.svc/Root/IMS/BuiltIn/Portal('Admin')"",
          ""type"": ""User""
        },
        ""Id"": 1,
        ""Name"": ""Admin""
      }
    ],
    ""Name"": ""Administrators""
  }
}");

            CreateTestSite();
            try
            {
                string jsonText;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')", "$expand=Members,ModifiedBy&$select=Id,Members/Id,Name,Members/Name&metadata=minimal", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    jsonText = GetStringResult(output);
                }
                var raw = jsonText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.IsTrue(raw == exp, String.Format("Result and expected are not equal. Result: {0}, expected: {1}", raw, exp));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Expand_Level2_Noselect()
        {
            CreateTestSite();
            try
            {
                Entity entity;
                string json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);

                    json = GetStringResult(output);
                    entity = GetEntity(output);
                }
                var createdBy = entity.CreatedBy;
                var createdBy_manager = createdBy.Manager;
                var msg = "Property count of '{0}' is {1}, expected: more than 20";
                Assert.IsTrue(entity.AllPropertiesSelected, string.Format(msg, "entity", entity.AllProperties.Count));
                Assert.IsTrue(createdBy.AllPropertiesSelected, string.Format(msg, "createdBy", createdBy.AllProperties.Count));
                Assert.IsTrue(createdBy_manager.AllPropertiesSelected, string.Format(msg, "createdBy.Manager", createdBy_manager.AllProperties.Count));
                Assert.IsTrue(createdBy.Manager.CreatedBy.IsDeferred, "'createdBy.Manager.CreatedBy' is not deferred");
                Assert.IsTrue(createdBy.Manager.Manager.IsDeferred, "'createdBy.Manager.Manager' is not deferred");
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Expand_Level2_Select_Level1()
        {
            CreateTestSite();
            try
            {
                Entity entity;
                string json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);

                    json = GetStringResult(output);
                    entity = GetEntity(output);
                }
                Assert.IsTrue(!entity.AllPropertiesSelected, "'entity' is not expanded.");
                Assert.IsTrue(entity.CreatedBy.AllPropertiesSelected, "'entity.CreatedBy' is not expanded.");
                Assert.IsTrue(entity.CreatedBy.CreatedBy.IsDeferred, "'entity.CreatedBy.CreatedBy' is not deferred");
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected, "'entity.CreatedBy.Manager' is not expanded.");
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred, "'entity.CreatedBy.Manager.CreatedBy' is not deferred");
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred, "'entity.CreatedBy.Manager.Manager' is not deferred");
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Expand_Level2_Select_Level2()
        {
            CreateTestSite();
            try
            {
                Entity entity;
                string json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy/Manager", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);

                    json = GetStringResult(output);
                    entity = GetEntity(output);
                }
                Assert.IsTrue(!entity.AllPropertiesSelected, "'entity' is not expanded.");
                Assert.IsTrue(!entity.CreatedBy.AllPropertiesSelected, "'entity.CreatedBy' is not expanded.");
                Assert.IsTrue(entity.CreatedBy.CreatedBy == null, "'entity.CreatedBy.CreatedBy' is not null");
                Assert.IsTrue(entity.CreatedBy.Manager.AllPropertiesSelected, "'entity.CreatedBy.Manager' is not expanded.");
                Assert.IsTrue(entity.CreatedBy.Manager.CreatedBy.IsDeferred, "'entity.CreatedBy.Manager.CreatedBy' is not deferred");
                Assert.IsTrue(entity.CreatedBy.Manager.Manager.IsDeferred, "'entity.CreatedBy.Manager.Manager' is not deferred");
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Expand_Level2_Select_Level3()
        {
            CreateTestSite();
            try
            {
                Entity entity;
                string json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn('Portal')", "$expand=CreatedBy/Manager&$select=CreatedBy/Manager/Id", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);

                    json = GetStringResult(output);
                    entity = GetEntity(output);
                }
                var id = entity.CreatedBy.Manager.Id;
                Assert.IsTrue(!entity.AllPropertiesSelected, "'entity' is not expanded.");
                Assert.IsTrue(!entity.CreatedBy.AllPropertiesSelected, "'entity.CreatedBy' is not expanded.");
                Assert.IsTrue(entity.CreatedBy.CreatedBy == null, "'entity.CreatedBy.CreatedBy' is not null");
                Assert.IsTrue(!entity.CreatedBy.Manager.AllPropertiesSelected, "'entity.CreatedBy.Manager' is not expanded.");
                Assert.IsTrue(entity.CreatedBy.Manager.Id > 0, String.Format("'entity.CreatedBy.Manager.Id' is {0}, expected: > 0", entity.CreatedBy.Manager.Id));
                Assert.IsTrue(entity.CreatedBy.Manager.Path == null, "'entity.CreatedBy.Manager.Path' is not null");
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_ExpandErrors()
        {
            CreateTestSite();
            try
            {
                //------------------------------------------------------------------------------------------------------------------------ test 1
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "$expand=Members&$select=Members1/Id", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var error = GetError(output);
                    Assert.IsTrue(error.Code == ODataExceptionCode.InvalidSelectParameter, string.Format("ErrorCode is '{0}', expected: '{1}'.", error.Code, ODataExceptionCode.InvalidSelectParameter));
                    Assert.IsTrue(error.Message == "Bad item in $select: Members1/Id", string.Format("Message is '{0}', expected: '{1}'.", error.Message, "Bad item in $select: Members1/Id"));
                }

                //------------------------------------------------------------------------------------------------------------------------ test 2
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "&$select=Members/Id", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    var error = GetError(output);
                    Assert.IsTrue(error.Code == ODataExceptionCode.InvalidSelectParameter, string.Format("ErrorCode is '{0}', expected: '{1}'.", error.Code, ODataExceptionCode.InvalidSelectParameter));
                    Assert.IsTrue(error.Message == "Bad item in $select: Members/Id", string.Format("Message is '{0}', expected: '{1}'.", error.Message, "Bad item in $select: Members/Id"));
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Expand_Actions()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            var expectedJson = @"
{
    ""d"":{
        ""__metadata"":{
            ""uri"":""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')"",
            ""type"":""Group"",
            ""actions"":[
                {""title"":""Action3"",""name"":""Action3"",""target"":""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action3"",""forbidden"":false,""parameters"": []},
                {""title"":""Action4"",""name"":""Action4"",""target"":""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action4"",""forbidden"":false,""parameters"": []}],
            ""functions"":[
                {""title"":""Action2"",""name"":""Action2"",""target"":""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action2"",""forbidden"":false,""parameters"": []}]
        },
        ""Id"":7,
        ""Name"":
        ""Administrators"",
        ""Actions"":{""__deferred"":{""uri"":""/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Actions""}},
        ""Members"":[{
            ""__metadata"":{
                ""uri"":""/OData.svc/Root/IMS/BuiltIn/Portal('Admin')"",
                ""type"":""User"",
                ""actions"":[
                    {""title"":""Action3"",""name"":""Action3"",""target"":""/OData.svc/Root/IMS/BuiltIn/Portal('Admin')/Action3"",""forbidden"":false,""parameters"": []},
                    {""title"":""Action4"",""name"":""Action4"",""target"":""/OData.svc/Root/IMS/BuiltIn/Portal('Admin')/Action4"",""forbidden"":false,""parameters"": []}],
                ""functions"":[
                    {""title"":""Action2"",""name"":""Action2"",""target"":""/OData.svc/Root/IMS/BuiltIn/Portal('Admin')/Action2"",""forbidden"":false,""parameters"": []}]
            },
            ""Id"":1,
            ""Name"":""Admin"",
            ""Actions"":[
                {""Name"":""Action1"",""DisplayName"":""Action1"",""Index"":0,""Icon"":""ActionIcon1"",""Url"":""ActionIcon1_URI"",""IncludeBackUrl"":0,""ClientAction"":false,""Forbidden"":false},
                {""Name"":""Action2"",""DisplayName"":""Action2"",""Index"":0,""Icon"":""ActionIcon2"",""Url"":""ActionIcon2_URI"",""IncludeBackUrl"":0,""ClientAction"":false,""Forbidden"":false}]
            }
        ]
    }
}
";


            CreateTestSite();
            try
            {
                string jsonText;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')", "$expand=Members/Actions,ModifiedBy&$select=Id,Name,Actions,Members/Id,Members/Name,Members/Actions", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    jsonText = GetStringResult(output);
                }
                var raw = jsonText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.IsTrue(raw == exp, String.Format("Result and expected are not equal. Result: {0}, expected: {1}", raw, exp));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Invoking_Actions()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            var expectedJson = @"
{
  ""d"": {
    ""message"":""Action3 executed""
  }
}";

            CreateTestSite();
            try
            {
                string jsonText;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action3", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    jsonText = GetStringResult(output);
                }
                var raw = jsonText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                var exp = expectedJson.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
                Assert.IsTrue(raw == exp, String.Format("Result and expected are not equal. Result: {0}, expected: {1}", raw, exp));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Invoking_Actions_NoContent()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                HttpResponse response;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/Action4", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    response = pc.OwnerHttpContext.Response;
                }
                Assert.IsTrue(response.StatusCode == 204, String.Format("Response.Status is {0}, expected: 204 No content", response.Status));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }

        internal class TestActionResolver : IActionResolver
        {
            internal class Action1 : ActionBase
            {
                public override string Icon { get { return "ActionIcon1"; } set { } }
                public override string Name { get { return "Action1"; } set { } }
                public override string Uri { get { return "ActionIcon1_URI"; } }
                public override bool IsHtmlOperation { get { return true; } }
                public override bool IsODataOperation { get { return false; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action1 executed" } } } };
                }
            }
            internal class Action2 : ActionBase
            {
                public override string Icon { get { return "ActionIcon2"; } set { } }
                public override string Name { get { return "Action2"; } set { } }
                public override string Uri { get { return "ActionIcon2_URI"; } }
                public override bool IsHtmlOperation { get { return true; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return false; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action2 executed" } } } };
                }
            }
            internal class Action3 : ActionBase
            {
                public override string Icon { get { return "ActionIcon3"; } set { } }
                public override string Name { get { return "Action3"; } set { } }
                public override string Uri { get { return "ActionIcon3_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return new Dictionary<string, object> { { "d", new Dictionary<string, object> { { "message", "Action3 executed" } } } };
                }
            }
            internal class Action4 : ActionBase
            {
                public override string Icon { get { return "ActionIcon4"; } set { } }
                public override string Name { get { return "Action4"; } set { } }
                public override string Uri { get { return "ActionIcon4_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return null;
                }
            }

            internal class ChildrenDefinitionFilteringTestAction : ActionBase
            {
                public override string Icon { get { return "ChildrenDefinitionFilteringTestAction"; } set { } }
                public override string Name { get { return "ChildrenDefinitionFilteringTestAction"; } set { } }
                public override string Uri { get { return "ChildrenDefinitionFilteringTestAction_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return new ChildrenDefinition
                    {
                        ContentQuery = "InFolder:/Root/IMS/BuiltIn/Portal",
                        EnableAutofilters = FilterStatus.Disabled,
                        PathUsage = PathUsageMode.NotUsed,
                        Sort = new[] { new SortInfo { FieldName = "Name", Reverse = true } },
                        Skip = 2,
                        Top = 3
                    };
                }
            }
            internal class CollectionFilteringTestAction : ActionBase
            {
                public override string Icon { get { return "ActionIcon4"; } set { } }
                public override string Name { get { return "Action4"; } set { } }
                public override string Uri { get { return "ActionIcon4_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return ContentQuery.Query("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF").Nodes.Select(n => Content.Create(n));
                }
            }

            internal class ODataActionAction : ActionBase
            {
                public override string Icon { get { return "ODataActionAction"; } set { } }
                public override string Name { get { return "ODataActionAction"; } set { } }
                public override string Uri { get { return "ODataActionAction_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return true; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataAction executed.";
                }
            }
            internal class ODataFunctionAction : ActionBase
            {
                public override string Icon { get { return "ODataFunctionAction"; } set { } }
                public override string Name { get { return "ODataFunctionAction"; } set { } }
                public override string Uri { get { return "ODataFunctionAction_URI"; } }
                public override bool IsHtmlOperation { get { return false; } }
                public override bool IsODataOperation { get { return true; } }
                public override bool CausesStateChange { get { return false; } }
                public override object Execute(Content content, params object[] parameters)
                {
                    return "ODataFunction executed.";
                }
            }


            public GenericScenario GetScenario(string name, string parameters)
            {
                return null;
            }
            public IEnumerable<ActionBase> GetActions(Content context, string scenario, string backUri)
            {
                return new ActionBase[] { new Action1(), new Action2(), new Action3(), new Action4() };
            }
            public ActionBase GetAction(Content context, string scenario, string actionName, string backUri, object parameters)
            {
                switch (actionName)
                {
                    default: return null;
                    case "Action1": return new Action1();
                    case "Action2": return new Action2();
                    case "Action3": return new Action3();
                    case "Action4": return new Action4();
                    case "GetPermissions": return new GetPermissionsAction();
                    case "SetPermissions": return new SenseNet.Portal.ApplicationModel.SetPermissionsAction();
                    case "HasPermission": return new SenseNet.Portal.ApplicationModel.HasPermissionAction();
                    case "AddAspects": return new SenseNet.ApplicationModel.AspectActions.AddAspectsAction();
                    case "RemoveAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAspectsAction();
                    case "RemoveAllAspects": return new SenseNet.ApplicationModel.AspectActions.RemoveAllAspectsAction();
                    case "AddFields": return new SenseNet.ApplicationModel.AspectActions.AddFieldsAction();
                    case "RemoveFields": return new SenseNet.ApplicationModel.AspectActions.RemoveFieldsAction();
                    case "RemoveAllFields": return new SenseNet.ApplicationModel.AspectActions.RemoveAllFieldsAction();

                    case "ChildrenDefinitionFilteringTest": return new ChildrenDefinitionFilteringTestAction();
                    case "CollectionFilteringTest": return new CollectionFilteringTestAction();

                    case "ODataAction": return new ODataActionAction();
                    case "ODataFunction": return new ODataFunctionAction();
                }
            }
        }
        /*
        ActionBase
	        Action1
	        Action2
	        Action3
	        Action4
	        PortalAction
		        ClientAction
			        OpenPickerAction
				        CopyToAction
					        CopyBatchAction
				        ContentLinkBatchAction
				        MoveToAction
					        MoveBatchAction
			        ShareAction
			        DeleteBatchAction
				        DeleteAction
			        WebdavOpenAction
			        WebdavBrowseAction
		        UrlAction
			        SetAsDefaultViewAction
			        PurgeFromProxyAction
			        ExpenseClaimPublishAction
			        WorkflowsAction
			        OpenLinkAction
			        BinarySpecialAction
			        AbortWorkflowAction
			        UploadAction
			        ManageViewsAction
			        ContentTypeAction
			        SetNotificationAction
		        ServiceAction
			        CopyAppLocalAction
			        LogoutAction
			        UserProfileAction
			        CopyViewLocalAction
		        DeleteLocalAppAction
		        ExploreAction
        */

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Select_FieldMoreThanOnce()
        {
            var path = User.Administrator.Parent.Path;
            var nodecount = ContentQuery.Query(String.Format("InFolder:{0} .AUTOFILTERS:OFF .COUNTONLY", path)).Count;

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + path, "$orderby=Name asc&$select=Id,Id,Name,Name,Path", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    CheckError(output);
                    entities = GetEntities(output);
                }
                Assert.AreEqual(nodecount, entities.Count());
            }
            finally
            {
                CleanupTestSite();
            }

        }

        [TestMethod]
        public void OData_Select_AspectField()
        {
            var aspect1 = EnsureAspect("Aspect1");
            aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
  </Fields>
</AspectDefinition>";
            aspect1.Save();

            var folder = new Folder(TestRoot) { Name = Guid.NewGuid().ToString() };
            folder.Save();

            var content1 = Content.CreateNew("Car", folder, "Car1");
            content1.AddAspects(aspect1);
            content1["Aspect1.Field1"] = "asdf";
            content1.Save();

            var content2 = Content.CreateNew("Car", folder, "Car2");
            content2.AddAspects(aspect1);
            content2["Aspect1.Field1"] = "qwer";
            content2.Save();

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + folder.Path, "$orderby=Name asc&$select=Name,Aspect1.Field1", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Count() == 2, string.Format("entities.Count is ({0}), expected: 2", entities.Count()));
                Assert.IsTrue(entities[0].Name == "Car1", string.Format("entities[0].Name is ({0}), expected: 'Car1'", entities[0].Name));
                Assert.IsTrue(entities[1].Name == "Car2", string.Format("entities[1].Name is ({0}), expected: 'Car2'", entities[0].Name));
                Assert.IsTrue(entities[0].AllProperties.ContainsKey("Aspect1.Field1"), "entities[0] does not contain 'Aspect1.Field1'");
                Assert.IsTrue(entities[1].AllProperties.ContainsKey("Aspect1.Field1"), "entities[1] does not contain 'Aspect1.Field1'");
                var value1 = (string)((JValue)entities[0].AllProperties["Aspect1.Field1"]).Value;
                var value2 = (string)((JValue)entities[1].AllProperties["Aspect1.Field1"]).Value;
                Assert.IsTrue(value1 == "asdf", string.Format("entities[0].AllProperties[\"Aspect1.Field1\"] is ({0}), expected: 'asdf'", value1));
                Assert.IsTrue(value2 == "qwer", string.Format("entities[0].AllProperties[\"Aspect1.Field1\"] is ({0}), expected: 'qwer'", value2));
            }
            finally
            {
                CleanupTestSite();
            }

        }
        private Aspect EnsureAspect(string name)
        {
            //var r = ContentQuery.Query(String.Concat("Name:", name, " .AUTOFILTERS:OFF"));
            //if (r.Count > 0)
            //    return (Aspect)r.Nodes.First();
            //var aspectContent = Content.CreateNew("Aspect", TestRoot, name);
            //aspectContent.Save();
            //return (Aspect)aspectContent.ContentHandler;

            var aspect = Aspect.LoadAspectByName(name);
            if (aspect == null)
            {
                aspect = new Aspect(Repository.AspectsFolder) { Name = name };
                aspect.Save();
            }
            return aspect;
        }

        [TestMethod]
        public void OData_Rename_PUT()
        {
            var content = Content.CreateNew("Car", TestRoot, "ORIG_" + Guid.NewGuid().ToString());
            content.DisplayName = "Initial DisplayName";
            content.Save();
            var id = content.Id;
            var path = content.Path;

            var newName = "NEW_" + Guid.NewGuid().ToString();
            var newDisplayName = "New DisplayName";

            CreateTestSite();
            try
            {
                Entity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var json = String.Concat(@"models=[{
                      ""Name"": """, newName, @""",
                      ""DisplayName"": """, newDisplayName, @"""
                    }]");
                    var stream = CreateRequestStream(json);
                    var pc = CreatePortalContext("/OData.svc" + content.Path, "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);
                    entity = GetEntity(output);
                }
                var content1 = Content.Load(id);
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Rename_PATCH()
        {
            var content = Content.CreateNew("Car", TestRoot, "ORIG_" + Guid.NewGuid().ToString());
            content.DisplayName = "Initial DisplayName";
            content.Save();
            var id = content.Id;
            var path = content.Path;

            var newName = "NEW_" + Guid.NewGuid().ToString();
            var newDisplayName = "New DisplayName";

            CreateTestSite();
            try
            {
                Entity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var json = String.Concat(@"models=[{
                      ""Name"": """, newName, @""",
                      ""DisplayName"": """, newDisplayName, @"""
                    }]");
                    var stream = CreateRequestStream(json);
                    var pc = CreatePortalContext("/OData.svc" + content.Path, "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);
                    entity = GetEntity(output);
                }
                var content1 = Content.Load(id);
                Assert.AreEqual(newName, content1.Name);
                Assert.AreEqual(newDisplayName, content1.DisplayName);
            }
            finally
            {
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Put_Modifying()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
  ""DisplayName"": """, newDisplayName, @""",
  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
  ""Index"": 42
}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName, String.Format("The DisplayName is '{0}', expected: '{1}'", c.DisplayName, newDisplayName));
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370", String.Format("The CreationDate is '{0}', expected: '{1}'", modificationDateStr, "2012-10-11 03:52:01.6300"));
                Assert.IsTrue(c.ContentHandler.Index == 42, String.Format("The Index is '{0}', expected: '{1}'", c.ContentHandler.Index, 42));
                Assert.IsTrue((string)c["Make"] == null, String.Format("The Make field is '{0}', expected: null", c["Make"]));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Patch_Modifying()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
  ""DisplayName"": """, newDisplayName, @""",
  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
  ""Index"": 42
}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName, String.Format("The DisplayName is '{0}', expected: '{1}'", c.DisplayName, newDisplayName));
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370", String.Format("The CreationDate is '{0}', expected: '{1}'", modificationDateStr, "2012-10-11 03:52:01.6300"));
                Assert.IsTrue(c.ContentHandler.Index == 42, String.Format("The Index is '{0}', expected: '{1}'", c.ContentHandler.Index, 42));
                Assert.IsTrue((string)c["Make"] == "Not default", String.Format("The Make field is '{0}', expected: \"Not default\"", c["Make"]));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Merge_Modifying()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
  ""DisplayName"": """, newDisplayName, @""",
  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
  ""Index"": 42
}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "MERGE", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName, String.Format("The DisplayName is '{0}', expected: '{1}'", c.DisplayName, newDisplayName));
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370", String.Format("The CreationDate is '{0}', expected: '{1}'", modificationDateStr, "2012-10-11 03:52:01.6300"));
                Assert.IsTrue(c.ContentHandler.Index == 42, String.Format("The Index is '{0}', expected: '{1}'", c.ContentHandler.Index, 42));
                Assert.IsTrue((string)c["Make"] == "Not default", String.Format("The Make field is '{0}', expected: \"Not default\"", c["Make"]));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Posting_Creating()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var displayName = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(TestRoot.Path, name);
                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                var json = string.Concat(@"models=[{""Name"":""", name, @""",""DisplayName"":""", displayName, @""",""Index"":41}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + TestRoot.Path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.DisplayName == displayName, String.Format("The DisplayName is '{0}', expected: '{1}'", content.DisplayName, displayName));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Posting_Creating_ExplicitType()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(TestRoot.Path, name);
                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/" + TestRoot.Path, "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.ContentType.Name == "Car", String.Format("The ContentType is '{0}', expected: '{1}'", content.ContentType.Name, "Car"));
                Assert.IsTrue(content.Name == name, String.Format("The DisplayName is '{0}', expected: '{1}'", content.Name, name));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Deleting()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.Save();
                var path = string.Concat("/OData.svc/", TestRoot.Path, "('", name, "')");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext(path, "", output);
                var handler = new ODataHandler();
                handler.ProcessRequest(pc.OwnerHttpContext, "DELETE", null);

                var repoPath = string.Concat(TestRoot.Path, "/", name);
                Assert.IsTrue(Node.Exists(repoPath) == false);
            }
            finally
            {
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Security_GetPermissions_ACL()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetPermissions
            //Stream:
            //Result: {
            //    "id": 4108,
            //    "path": "/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Document_Library",
            //    "inherits": true,
            //    "entries": [
            //        {
            //            "identity": { "path": "/Root/Sites/Default_Site/workspaces/Project/budapestprojectworkspace/Groups/Owners", ...
            //            "permissions": {
            //                "See": {...

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                JContainer json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    json = Deserialize(output);
                }
                var entries = json["entries"];
                Assert.IsNotNull(entries);
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_GetPermissions_ACE()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/GetPermissions
            //Stream: {identity:"/root/ims/builtin/portal/visitor"}
            //Result: {
            //    "identity": { "id:": 7,  "path": "/Root/IMS/BuiltIn/Portal/Administrators",…},
            //    "permissions": {
            //        "See": { "value": "allow", "from": "/root" }
            //       ...

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                JContainer json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/GetPermissions", "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{identity:\"/root/ims/builtin/portal/visitor\"}");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    json = Deserialize(output);
                }
                var identity = json[0]["identity"];
                var permissions = json[0]["permissions"];
                Assert.IsTrue(identity != null, "Identity is null");
                Assert.IsTrue(permissions != null, "Permissions is null");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }

        [TestMethod]
        public void OData_Security_HasPermission_Administrator()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
            //Stream: {user:"/root/ims/builtin/portal/admin", permissions:["Open","Save"] }
            //result: true

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream(String.Concat("{user:\"", User.Administrator.Path, "\", permissions:[\"Open\",\"Save\"] }"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result == "true", "Result is false");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_HasPermission_Visitor()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
            //Stream: {user:"/root/ims/builtin/portal/visitor", permissions:["Open","Save"] }
            //result: false

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result = null;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{user:\"/root/ims/builtin/portal/visitor\", permissions:[\"Open\",\"Save\"] }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result == "false", "Result is false");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_HasPermission_NullUser()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
            //Stream: {user:null, permissions:["Open","Save"] }
            //result: true

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result = null;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{user:null, permissions:[\"Open\",\"Save\"] }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result == "true", "Result is false");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_HasPermission_WithoutUser()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
            //Stream: {permissions:["Open","Save"] }
            //result: true

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result = null;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{permissions:[\"Open\",\"Save\"] }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result == "true", "Result is false");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_HasPermission_Error_IdentityNotFound()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
            //Stream: {user:"/root/ims/builtin/portal/nobody", permissions:["Open","Save"] }
            //result: ERROR: ODataException: Content not found: /root/ims/builtin/portal/nobody

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{user:\"/root/ims/builtin/portal/nobody\", permissions:[\"Open\",\"Save\"] }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    error = GetError(output);
                }
                Assert.IsTrue(error.Code == ODataExceptionCode.ResourceNotFound, String.Format("Code is {0}, expected: {1}", error.Code, ODataExceptionCode.ResourceNotFound));
                Assert.IsTrue(error.Message == "Identity not found: /root/ims/builtin/portal/nobody", String.Format("Message is '{0}', expected: 'Identity not found: /root/ims/builtin/portal/nobody'", error.Message));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_HasPermission_Error_UnknownPermission()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
            //Stream: {permissions:["Open","Save1"] }
            //result: ERROR: ODataException: Unknown permission: Save1

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{permissions:[\"Open\",\"Save1\"] }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    error = GetError(output);
                }
                Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified, String.Format("Code is {0}, expected: {1}", error.Code, ODataExceptionCode.NotSpecified));
                Assert.IsTrue(error.Message == "Unknown permission: Save1", String.Format("Message is '{0}', expected: 'Unknown permission: Save1'", error.Message));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_HasPermission_Error_MissingParameter()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/HasPermission
            //Stream:
            //result: ERROR: "ODataException: Value cannot be null.\\nParameter name: permissions

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal('Administrators')/HasPermission", "", output);
                    var handler = new ODataHandler();
                    //var stream = CreateRequestStream("{user:\"/root/ims/builtin/portal/nobody\", permissions:[\"Open\",\"Save\"] }");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
                    error = GetError(output);
                }
                Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified, String.Format("Code is {0}, expected: {1}", error.Code, ODataExceptionCode.NotSpecified));
                Assert.IsTrue(error.Message == "Value cannot be null.\\nParameter name: permissions", String.Format("Message is '{0}', expected: 'Value cannot be null.\\nParameter name: permissions'", error.Message));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }

        }

        [TestMethod]
        public void OData_Security_SetPermissions()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermission
            //Stream: {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
            //result: (nothing)

            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var resourcePath = ODataHandler.GetEntityUrl(content.Path);

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\", Save:\"deny\"},{identity:\"/Root/IMS/BuiltIn/Portal/Creators\", Custom16:\"A\", Custom17:\"1\"}]}");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result.Length == 0, "Result is not empty");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_SetPermissions_NotPropagates()
        {
            //URL: /OData.svc/workspaces/Project/budapestprojectworkspace('Document_Library')/SetPermission
            //Stream: {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", Custom16:"A", Custom17:"1"}]}
            //result: (nothing)

            var content = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var folderPath = ODataHandler.GetEntityUrl(content.Path);
            var folderRepoPath = content.Path;
            content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var carRepoPath = content.Path;

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(string.Concat("/OData.svc/", folderPath, "/SetPermissions"), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\", propagates:false}]}");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result.Length == 0, "Result is not empty");
                var folder = Node.LoadNode(folderRepoPath);
                var car = Node.LoadNode(carRepoPath);

                Assert.IsTrue(folder.Security.HasPermission((IUser)User.Visitor, PermissionType.OpenMinor), "Not allowed on the folder.");
                Assert.IsFalse(car.Security.HasPermission((IUser)User.Visitor, PermissionType.OpenMinor), "Allowed on the car.");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_Break()
        {
            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var resourcePath = ODataHandler.GetEntityUrl(content.Path);

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{inheritance:\"break\"}");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result.Length == 0, "Result is not empty");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_Unbreak()
        {
            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var resourcePath = ODataHandler.GetEntityUrl(content.Path);

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{inheritance:\"unbreak\"}");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = GetStringResult(output);
                }
                Assert.IsTrue(result.Length == 0, "Result is not empty");
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                CleanupTestSite();
            }

        }
        [TestMethod]
        public void OData_Security_Error_MissingStream()
        {
            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var resourcePath = ODataHandler.GetEntityUrl(content.Path);

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "", output);
                    var handler = new ODataHandler();
                    Stream stream = null;
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    error = GetError(output);
                }
                var expectedMessage = "Value cannot be null.\\nParameter name: stream";
                Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified, String.Format("Code is {0}, expected: {1}", error.Code, ODataExceptionCode.NotSpecified));
                Assert.IsTrue(error.Message == expectedMessage, String.Format("Message is '{0}', expected: '{1}'", error.Message, expectedMessage));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Security_Error_BothParameters()
        {
            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var resourcePath = ODataHandler.GetEntityUrl(content.Path);

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{r:[{identity:\"/Root/IMS/BuiltIn/Portal/Visitor\", OpenMinor:\"allow\"}], inheritance:\"break\"}");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    error = GetError(output);
                }
                var expectedMessage = "Cannot use  r  and  inheritance  parameters at the same time.";
                Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified, String.Format("Code is {0}, expected: {1}", error.Code, ODataExceptionCode.NotSpecified));
                Assert.IsTrue(error.Message == expectedMessage, String.Format("Message is '{0}', expected: '{1}'", error.Message, expectedMessage));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Security_Error_InvalidInheritanceParam()
        {
            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var resourcePath = ODataHandler.GetEntityUrl(content.Path);

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(string.Concat("/OData.svc/", resourcePath, "/SetPermissions"), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream("{inheritance:\"dance\"}");
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    error = GetError(output);
                }
                var expectedMessage = "The value of the  inheritance  must be  break  or  unbreak .";
                Assert.IsTrue(error.Code == ODataExceptionCode.NotSpecified, String.Format("Code is {0}, expected: {1}", error.Code, ODataExceptionCode.NotSpecified));
                Assert.IsTrue(error.Message == expectedMessage, String.Format("Message is '{0}', expected: '{1}'", error.Message, expectedMessage));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_ServiceDocument()
        {
            var site = CreateTestSite();
            try
            {
                var folder1 = Content.CreateNew("Folder", site, "Folder_" + Guid.NewGuid().ToString());
                folder1.Save();
                var car1 = Content.CreateNew("Car", site, "Car_" + Guid.NewGuid().ToString());
                car1.Save();
                var workspace1 = Content.CreateNew("DocumentWorkspace", site, "DocumentWorkspace_" + Guid.NewGuid().ToString());
                workspace1.Save();
                var file1 = Content.CreateNew("File", site, "File_" + Guid.NewGuid().ToString());
                file1.Save();

                var containers = new[] { folder1, car1, workspace1, file1 };
                var names = String.Join(",", containers.Select(c => String.Concat("\"", c.Name, "\"")));
                var expectedJson = String.Concat("{\"d\":{\"EntitySets\":[", names, "]}}");

                string json;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    json = GetStringResult(output);
                }
                var result = json.Replace("\r\n", "").Replace("\t", "").Replace(" ", "");
                Assert.IsTrue(result == expectedJson, String.Format("Result is: {0}, expected: {1}", result, expectedJson));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Metadata_Global()
        {
            CreateTestSite();
            try
            {
                XmlNamespaceManager nsmgr;
                XmlDocument metaXml;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/$metadata", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    metaXml = GetMetadataXml(output.GetStringBuilder().ToString(), out nsmgr);
                }
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count == ContentType.GetContentTypes().Count(), "#1");
                var rootTypes = metaXml.SelectNodes("//x:EntityType[not(@BaseType)]", nsmgr);
                foreach (XmlElement node in rootTypes)
                {
                    var hasKey = node.SelectSingleNode("x:Key", nsmgr) != null;
                    var hasId = node.SelectSingleNode("x:Property[@Name = 'Id']", nsmgr) != null;
                    Assert.IsTrue(hasId == hasKey, "#2");
                }
                foreach (XmlElement node in metaXml.SelectNodes("//x:EntityType[@BaseType]", nsmgr))
                {
                    var hasKey = node.SelectSingleNode("x:Key", nsmgr) != null;
                    var hasId = node.SelectSingleNode("x:Property[@Name = 'Id']", nsmgr) != null;
                    Assert.IsFalse(hasKey, "#3");
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Metadata_Instance_Entity()
        {
            string listDef = @"<?xml version='1.0' encoding='utf-8'?>
<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
	<Fields>
		<ContentListField name='#ListField1' type='ShortText'>
			<Configuration>
				<MaxLength>42</MaxLength>
			</Configuration>
		</ContentListField>
	</Fields>
</ContentListDefinition>
";

            var listContent = Content.CreateNew("ContentList", TestRoot, Guid.NewGuid().ToString());
            var list = (ContentList)listContent.ContentHandler;
            list.AllowChildTypes(new[] { ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car") });
            list.ContentListDefinition = listDef;
            listContent.Save();

            var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
            itemFolder.Save();
            var itemContent = Content.CreateNew("Car", itemFolder.ContentHandler, Guid.NewGuid().ToString());
            itemContent.Save();

            CreateTestSite();
            try
            {
                XmlNamespaceManager nsmgr;
                XmlDocument metaXml;
                string src = null;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(String.Concat("/OData.svc", ODataHandler.GetEntityUrl(itemContent.Path), "/$metadata"), "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    output.Flush();
                    src = GetStringResult(output);
                    metaXml = GetMetadataXml(src, out nsmgr);
                }
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count == 1, "#1");
                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
                Assert.IsTrue(listProps.Count == 1, "#2");
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Metadata_Instance_Collection()
        {
            string listDef = @"<?xml version='1.0' encoding='utf-8'?>
<ContentListDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentListDefinition'>
	<Fields>
		<ContentListField name='#ListField1' type='ShortText'>
			<Configuration>
				<MaxLength>42</MaxLength>
			</Configuration>
		</ContentListField>
	</Fields>
</ContentListDefinition>
";

            var listContent = Content.CreateNew("ContentList", TestRoot, Guid.NewGuid().ToString());
            var list = (ContentList)listContent.ContentHandler;
            list.AllowChildTypes(new[] { ContentType.GetByName("Folder"), ContentType.GetByName("File"), ContentType.GetByName("Car") });
            list.ContentListDefinition = listDef;
            listContent.Save();

            var itemFolder = Content.CreateNew("Folder", listContent.ContentHandler, Guid.NewGuid().ToString());
            itemFolder.Save();

            CreateTestSite();
            try
            {
                XmlNamespaceManager nsmgr;
                XmlDocument metaXml;
                string src = null;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(String.Concat("/OData.svc", listContent.Path, "/$metadata"), "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    output.Flush();
                    src = GetStringResult(output);
                    metaXml = GetMetadataXml(src, out nsmgr);
                }
                var allTypes = metaXml.SelectNodes("//x:EntityType", nsmgr);
                Assert.IsTrue(allTypes.Count > 1, "#1");
                var listProps = metaXml.SelectNodes("//x:EntityType/x:Property[@Name='#ListField1']", nsmgr);
                Assert.IsTrue(listProps.Count == allTypes.Count, "#2");
            }
            finally
            {
                CleanupTestSite();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void OData_Filter_ContentField()
        {
            var a = new[] { "Ferrari", "Porsche", "Ferrari", "Mercedes" };
            var names = new List<string>();
            foreach (var x in new[] { "Ferrari", "Porsche", "Ferrari", "Mercedes" })
            {
                var car = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                car["Make"] = x;
                car.Save();
                names.Add(car.Name);
            }

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + TestRoot.Path, "$filter=Make eq 'Ferrari'&enableautofilters=false", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Filter_InFolder()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "$orderby=Id&$filter=Id lt (9 sub 2)", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
                Assert.IsTrue(entities[0].Id == 1, String.Format("entities[0].Id is {0}, expected: 1", entities[0].Id));
                Assert.IsTrue(entities[1].Id == 6, String.Format("entities[1].Id is {0}, expected: 6", entities[1].Id));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Filter_ThroughReference()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var resourcePath = ODataHandler.GetEntityUrl(_testRootPath + "/Referrer");
                    var url = String.Format("/OData.svc{0}/References", resourcePath);
                    var pc = CreatePortalContext(url, "$orderby=Index&$filter=Index lt 5 and Index gt 2", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
                Assert.IsTrue(entities[0].Index == 3, String.Format("entities[0].Index is {0}, expected: 3", entities[0].Index));
                Assert.IsTrue(entities[1].Index == 4, String.Format("entities[1].Index is {0}, expected: 4", entities[1].Index));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Filter_ThroughReference_TopSkip()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var resourcePath = ODataHandler.GetEntityUrl(_testRootPath + "/Referrer");
                    var url = String.Format("/OData.svc{0}/References", resourcePath);
                    var pc = CreatePortalContext(url, "$orderby=Index&$filter=Index lt 10&$top=3&$skip=1", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var actual = String.Join(",", entities.Select(e => e.Index).ToArray());
                Assert.AreEqual("2,3,4", actual);
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Filter_IsFolder()
        {
            var folder = new Folder(TestRoot);
            folder.Name = Guid.NewGuid().ToString();
            folder.Save();

            var folder1 = new Folder(folder);
            folder1.Name = "Folder1";
            folder1.Save();

            var folder2 = new Folder(folder);
            folder2.Name = "Folder2";
            folder2.Save();

            var content = Content.CreateNew("Car", folder, null);
            content.Save();

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=IsFolder eq true", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
                Assert.IsTrue(entities[0].Id == folder1.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, folder1.Id));
                Assert.IsTrue(entities[1].Id == folder2.Id, String.Format("entities[1].Id is {0}, expected: {1}", entities[1].Id, folder2.Id));
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=IsFolder eq false", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Length == 1, String.Format("Count is {0}, expected: 1", entities.Length));
                Assert.IsTrue(entities[0].Id == content.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, content.Id));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_FilteringAndPartitioningOperationResult_ChildrenDefinition()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/ChildrenDefinitionFilteringTest", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    entities = GetEntities(output);
                }
                var ids = String.Join(", ", entities.Select(e => e.Id));
                var expids = String.Join(", ", ContentQuery.Query("InFolder:/Root/IMS/BuiltIn/Portal .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:2 .TOP:3").Identifiers);
                // 8, 9, 7
                Assert.AreEqual(expids, ids);
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_FilteringAndPartitioningOperationResult_ContentCollection()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest", "$skip=1&$top=3&$orderby=Name desc&$select=Id,Name&$filter=Id ne 10&metadata=no", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    entities = GetEntities(output);
                }
                var ids = String.Join(", ", entities.Select(e => e.Id));
                var expids = String.Join(", ", ContentQuery.Query("+InFolder:/Root/IMS/BuiltIn/Portal -Id:10 .AUTOFILTERS:OFF .REVERSESORT:Name .SKIP:1 .TOP:3").Identifiers);
                // 8, 9, 7
                Assert.AreEqual(expids, ids);
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Filter_IsOf()
        {
            var folder = new Folder(TestRoot);
            folder.Name = Guid.NewGuid().ToString();
            folder.Save();

            var folder1 = new Folder(folder);
            folder1.Name = "Folder1";
            folder1.Save();

            var folder2 = new Folder(folder);
            folder2.Name = "Folder2";
            folder2.Save();

            var content = Content.CreateNew("Car", folder, null);
            content.Save();

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=isof('Folder')", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Length == 2, String.Format("Count is {0}, expected: 2", entities.Length));
                Assert.IsTrue(entities[0].Id == folder1.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, folder1.Id));
                Assert.IsTrue(entities[1].Id == folder2.Id, String.Format("entities[1].Id is {0}, expected: {1}", entities[1].Id, folder2.Id));
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + folder.Path, "&$filter=not isof('Folder')", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                Assert.IsTrue(entities.Length == 1, String.Format("Count is {0}, expected: 1", entities.Length));
                Assert.IsTrue(entities[0].Id == content.Id, String.Format("entities[0].Id is {0}, expected: {1}", entities[0].Id, content.Id));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_FilteringCollection_IsOf()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest", "&$select=Id,Name&metadata=no&$filter=isof('User')", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    entities = GetEntities(output);
                }
                var ids = String.Join(", ", entities.Select(e => e.Id));
                var expids = String.Join(", ", ContentQuery.Query("+InFolder:/Root/IMS/BuiltIn/Portal +TypeIs:User .AUTOFILTERS:OFF").Identifiers);
                // 6, 1
                Assert.AreEqual(expids, ids);

                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/CollectionFilteringTest", "&$select=Id,Name&metadata=no&$filter=not isof('User')", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    entities = GetEntities(output);
                }
                ids = String.Join(", ", entities.Select(e => e.Id));
                expids = String.Join(", ", ContentQuery.Query("+InFolder:/Root/IMS/BuiltIn/Portal -TypeIs:User .AUTOFILTERS:OFF").Identifiers);
                // 8, 9, 7
                Assert.AreEqual(expids, ids);

            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Filter_NamespaceAndMemberChain()
        {
            CreateTestSite();
            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root/IMS/BuiltIn/Portal", "$filter=SenseNet.ContentRepository.Tests.ODataFilterTestHelper/TestValue eq Name", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var group = Node.Load<Group>("/Root/IMS/BuiltIn/Portal/Administrators");
                Assert.AreEqual(1, entities.Count());
                Assert.AreEqual(group.Path, entities.First().Path);
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Filter_AspectField()
        {
            var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
            var aspect = new Aspect(Repository.AspectsFolder) { Name = aspectName };
            aspect.AddFields(new FieldInfo
            {
                Name = "Field1",
                DisplayName = "Field1 DisplayName",
                Description = "Field1 description",
                Type = "ShortText",
                Indexing = new IndexingInfo
                {
                    IndexHandler = "SenseNet.Search.Indexing.LowerStringIndexHandler"
                }
            });

            var aspectFieldName = aspectName + ".Field1";
            var aspectFieldODataName = aspectName + "/Field1";

            var site = CreateTestSite();
            var content1 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
            content1.Index = 1;
            content1.Save();
            var content2 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
            content2.Index = 2;
            content2.Save();
            var content3 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
            content3.Index = 3;
            content3.Save();
            var content4 = Content.CreateNew("SystemFolder", site, Guid.NewGuid().ToString());
            content4.Index = 4;
            content4.Save();

            content2.AddAspects(aspect);
            content2[aspectFieldName] = "Value2";
            content2.Save();
            content3.AddAspects(aspect);
            content3[aspectFieldName] = "Value3";
            content3.Save();
            content4.AddAspects(aspect);
            content4[aspectFieldName] = "Value2";
            content4.Save();

            try
            {
                Entities entities;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + site.Path, "$orderby=Index&$filter=" + aspectFieldODataName + " eq 'Value2'", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    entities = GetEntities(output);
                }
                var expected = String.Join(", ", (new[] { content2.Name, content4.Name }));
                var names = String.Join(", ", entities.Select(e => e.Name));
                Assert.AreEqual(expected, names);
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Filter_AspectField_FieldNotFound()
        {
            var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
            var aspect = new Aspect(Repository.AspectsFolder) { Name = aspectName };
            aspect.AddFields(new FieldInfo
            {
                Name = "Field1",
                DisplayName = "Field1 DisplayName",
                Description = "Field1 description",
                Type = "ShortText",
                Indexing = new IndexingInfo
                {
                    IndexHandler = "SenseNet.Search.Indexing.LowerStringIndexHandler"
                }
            });

            var site = CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + site.Path, "$orderby=Index&$filter=" + aspectName + "/Field2 eq 'Value2'", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    error = GetError(output);
                }
                Assert.IsTrue(error.Message.Contains("Field not found"));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Filter_AspectField_FieldNotFoundButAspectFound()
        {
            var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
            var aspect = new Aspect(Repository.AspectsFolder) { Name = aspectName };
            aspect.AddFields(new FieldInfo
            {
                Name = "Field1",
                DisplayName = "Field1 DisplayName",
                Description = "Field1 description",
                Type = "ShortText",
                Indexing = new IndexingInfo
                {
                    IndexHandler = "SenseNet.Search.Indexing.LowerStringIndexHandler"
                }
            });

            var site = CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + site.Path, "$orderby=Index&$filter=" + aspectName + " eq 'Value2'", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    error = GetError(output);
                }
                Assert.IsTrue(error.Message.Contains("Field not found"));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Filter_AspectField_AspectNotFound()
        {
            var aspectName = "Aspect" + Guid.NewGuid().ToString("N");
            var site = CreateTestSite();
            try
            {
                Error error;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc" + site.Path, "$orderby=Index&$filter=" + aspectName + "/Field1 eq 'Value1'", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    error = GetError(output);
                }
                Assert.IsTrue(error.Message.Contains("Field not found"));
            }
            finally
            {
                CleanupTestSite();
            }
        }


        [TestMethod]
        public void OData_InvokeAction_Post_GetPutMergePatchDelete()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result = null;
                Error error;

                //------------------------------------------------------------ POST: ok
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    result = GetStringResult(output);
                }
                Assert.AreEqual("ODataAction executed.", result);

                //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
                var verbs = new[] { "GET", "PUT", "MERGE", "PATCH", "DELETE" };
                foreach (var verb in verbs)
                {
                    using (var output = new System.IO.StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, verb, MemoryStream.Null);
                        error = GetError(output);
                        if (error == null)
                            Assert.Fail("Exception was not thrown: " + verb);
                    }
                    Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code, String.Format("Error code is {0}, expected: {1}, verb: {2}"
                        , error.Code, ODataExceptionCode.IllegalInvoke, verb));
                }
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_InvokeFunction_PostGet_PutMergePatchDelete()
        {
            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new TestActionResolver());

            CreateTestSite();
            try
            {
                string result = null;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataFunction", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", MemoryStream.Null);
                    result = GetStringResult(output);
                }
                Assert.AreEqual("ODataFunction executed.", result);

                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataFunction", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "GET", MemoryStream.Null);
                    result = GetStringResult(output);
                }
                Assert.AreEqual("ODataFunction executed.", result);

                //------------------------------------------------------------ GET PUT MERGE PATCH DELETE: error
                var verbs = new[] { "PUT", "MERGE", "PATCH", "DELETE" };
                foreach (var verb in verbs)
                {
                    Error error = null;
                    using (var output = new System.IO.StringWriter())
                    {
                        var pc = CreatePortalContext("/OData.svc/Root('IMS')/ODataAction", "", output);
                        var handler = new ODataHandler();
                        handler.ProcessRequest(pc.OwnerHttpContext, verb, MemoryStream.Null);
                        error = GetError(output);
                        if (error == null)
                            Assert.Fail("Exception was not thrown: " + verb);
                    }
                    Assert.AreEqual(ODataExceptionCode.IllegalInvoke, error.Code, String.Format("Error code is {0}, expected: {1}, verb: {2}"
                        , error.Code, ODataExceptionCode.IllegalInvoke, verb));
                }
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                CleanupTestSite();
            }
        }

        /*=========================================================================================================================================*/

        [TestMethod]
        public void OData_GetEntityById()
        {
            try
            {
                //var name = Guid.NewGuid().ToString();
                //var content = Content.CreateNew("Car", TestRoot, name);
                //content.Save();
                //var contentId = content.Id;
                var content = Content.Load(1);
                var id = content.Id;

                Entity entity;
                CreateTestSite();
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Content(" + id + ")", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    //handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    output.Flush();
                    entity = GetEntity(output);
                }
                Assert.AreEqual(id, entity.Id);
                Assert.AreEqual(content.Path, entity.Path);
                Assert.AreEqual(content.Name, entity.Name);
                Assert.AreEqual(content.ContentType, entity.ContentType);
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_GetEntityById_InvalidId()
        {
            try
            {
                Error err;
                CreateTestSite();
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Content(qwer)", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    output.Flush();
                    err = GetError(output);
                }
                Assert.AreEqual(ODataExceptionCode.InvalidId, err.Code);
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_GetPropertyOfEntityById()
        {
            try
            {
                //var name = Guid.NewGuid().ToString();
                //var content = Content.CreateNew("Car", TestRoot, name);
                //content.Save();
                //var contentId = content.Id;
                var content = Content.Load(1);
                var id = content.Id;

                string result;
                CreateTestSite();
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext("/OData.svc/Content(" + id + ")/Name", "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext);
                    //handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    output.Flush();
                    result = GetStringResult(output);
                }
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Put_ModifyingById()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
  ""DisplayName"": """, newDisplayName, @""",
  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
  ""Index"": 42
}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "PUT", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName, String.Format("The DisplayName is '{0}', expected: '{1}'", c.DisplayName, newDisplayName));
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370", String.Format("The CreationDate is '{0}', expected: '{1}'", modificationDateStr, "2012-10-11 03:52:01.6300"));
                Assert.IsTrue(c.ContentHandler.Index == 42, String.Format("The Index is '{0}', expected: '{1}'", c.ContentHandler.Index, 42));
                Assert.IsTrue((string)c["Make"] == null, String.Format("The Make field is '{0}', expected: null", c["Make"]));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Patch_ModifyingById()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
  ""DisplayName"": """, newDisplayName, @""",
  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
  ""Index"": 42
}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName, String.Format("The DisplayName is '{0}', expected: '{1}'", c.DisplayName, newDisplayName));
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370", String.Format("The CreationDate is '{0}', expected: '{1}'", modificationDateStr, "2012-10-11 03:52:01.6300"));
                Assert.IsTrue(c.ContentHandler.Index == 42, String.Format("The Index is '{0}', expected: '{1}'", c.ContentHandler.Index, 42));
                Assert.IsTrue((string)c["Make"] == "Not default", String.Format("The Make field is '{0}', expected: \"Not default\"", c["Make"]));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Merge_ModifyingById()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.DisplayName = "vadalma";
                var defaultMake = (string)content["Make"];
                content["Make"] = "Not default";
                content.Save();
                var id = content.Id;
                var path = content.Path;
                var url = GetUrl(content.Path);

                var newDisplayName = "szelídgesztenye";

                var json = String.Concat(@"models=[{
  ""DisplayName"": """, newDisplayName, @""",
  ""ModificationDate"": ""2012-10-11T03:52:01.637Z"",
  ""Index"": 42
}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "MERGE", stream);

                var c = Content.Load(id);
                var creationDateStr = c.ContentHandler.CreationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                var modificationDateStr = c.ContentHandler.ModificationDate.ToString("yyyy-MM-dd HH:mm:ss.ffff");

                Assert.IsTrue(c.DisplayName == newDisplayName, String.Format("The DisplayName is '{0}', expected: '{1}'", c.DisplayName, newDisplayName));
                Assert.IsTrue(modificationDateStr == "2012-10-11 03:52:01.6370", String.Format("The CreationDate is '{0}', expected: '{1}'", modificationDateStr, "2012-10-11 03:52:01.6300"));
                Assert.IsTrue(c.ContentHandler.Index == 42, String.Format("The Index is '{0}', expected: '{1}'", c.ContentHandler.Index, 42));
                Assert.IsTrue((string)c["Make"] == "Not default", String.Format("The Make field is '{0}', expected: \"Not default\"", c["Make"]));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_Posting_Creating_UnderById()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var displayName = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(TestRoot.Path, name);
                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                var json = string.Concat(@"models=[{""Name"":""", name, @""",""DisplayName"":""", displayName, @""",""Index"":41}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + TestRoot.Id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.DisplayName == displayName, String.Format("The DisplayName is '{0}', expected: '{1}'", content.DisplayName, displayName));
            }
            finally
            {
                CleanupTestSite();
            }
        }
        [TestMethod]
        public void OData_Posting_Creating_ExplicitType_UnderById()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(TestRoot.Path, name);
                //var json = string.Concat(@"models=[{""Id"":"""",""IsFile"":false,""Name"":"""",""DisplayName"":""", displayName, @""",""ModifiedBy"":null,""ModificationDate"":null,""CreationDate"":null,""Actions"":null}]");
                var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext("/OData.svc/content(" + TestRoot.Id + ")", "", output);
                var handler = new ODataHandler();
                var stream = CreateRequestStream(json);

                handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);

                var content = Content.Load(path);
                Assert.IsTrue(content.ContentType.Name == "Car", String.Format("The ContentType is '{0}', expected: '{1}'", content.ContentType.Name, "Car"));
                Assert.IsTrue(content.Name == name, String.Format("The DisplayName is '{0}', expected: '{1}'", content.Name, name));
            }
            finally
            {
                CleanupTestSite();
            }
        }

        [TestMethod]
        public void OData_DeletingBy()
        {
            CreateTestSite();
            try
            {
                var name = Guid.NewGuid().ToString();
                var content = Content.CreateNew("Car", TestRoot, name);
                content.Save();
                var path = string.Concat("/OData.svc/content(" + content.Id + ")");

                var output = new System.IO.StringWriter();
                var pc = CreatePortalContext(path, "", output);
                var handler = new ODataHandler();
                handler.ProcessRequest(pc.OwnerHttpContext, "DELETE", null);

                var repoPath = string.Concat(TestRoot.Path, "/", name);
                Assert.IsTrue(Node.Exists(repoPath) == false);
            }
            finally
            {
                CleanupTestSite();
            }
        }

        //========================================================================================================================================= Bug reproductions

        [TestMethod]
        public void OData_InconsistentNameAfterCreating()
        {
            try
            {
                var name = Guid.NewGuid().ToString();
                var path = RepositoryPath.Combine(TestRoot.Path, name);
                var json = string.Concat(@"models=[{""__ContentType"":""Car"",""Name"":""", name, @"""}]");

                Entity entity;
                string result;
                CreateTestSite();
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(String.Concat("/OData.svc", ODataHandler.GetEntityUrl(TestRoot.Path)), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream(json);
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    output.Flush();
                    result = GetStringResult(output);
                    entity = GetEntity(output);
                }
                var name1 = entity.Name;
                using (var output = new System.IO.StringWriter())
                {
                    var pc = CreatePortalContext(String.Concat("/OData.svc", ODataHandler.GetEntityUrl(TestRoot.Path)), "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream(json);
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    output.Flush();
                    result = GetStringResult(output);
                    entity = GetEntity(output);
                }
                var name2 = entity.Name;

                Assert.AreNotEqual(name1, name2);
            }
            finally
            {
                CleanupTestSite();
            }

        }

        [TestMethod]
        public void OData_ModifyWithInvisibleParent()
        {
            var root = new Folder(TestRoot) { Name = Guid.NewGuid().ToString() };
            root.Save();
            var node = new Folder(root) { Name = Guid.NewGuid().ToString() };
            node.Save();

            root.Security.BreakInheritance();
            root.Security.GetAclEditor().SetPermission(User.Visitor, true, PermissionType.See, PermissionValue.NonDefined).Apply();
            node.Security.GetAclEditor().SetPermission(User.Visitor, true, PermissionType.Save, PermissionValue.Allow).Apply();

            var savedUser = User.Current;

            CreateTestSite();
            try
            {
                User.Current = User.Visitor;

                Entity entity;
                using (var output = new System.IO.StringWriter())
                {
                    var json = String.Concat(@"models=[{""Index"": 42}]");
                    var pc = CreatePortalContext("/OData.svc" + node.Path, "", output);
                    var handler = new ODataHandler();
                    var stream = CreateRequestStream(json);
                    handler.ProcessRequest(pc.OwnerHttpContext, "PATCH", stream);
                    CheckError(output);
                    entity = GetEntity(output);
                }
                node = Node.Load<Folder>(node.Id);
                Assert.AreEqual(42, entity.Index);
                Assert.AreEqual(42, node.Index);
            }
            finally
            {
                User.Current = savedUser;
                CleanupTestSite();
            }

        }

        /*=========================================================================================================================================*/
        /*=========================================================================================================================================*/
        /*=========================================================================================================================================*/

        internal static Site CreateTestSite()
        {
            var node = Node.Load<Site>(TestSitePath);
            if (node != null)
                return node;

            var sites = Node.LoadNode("/Root/Sites");
            if (sites == null)
            {
                sites = new Folder(Repository.Root) { Name = "Sites" };
                sites.Save();
            }

            var site = new Site(sites) { Name = TestSiteName };
            var urlList = new Dictionary<string, string>(3)
                              {
                                  {"localhost", "None"},
                                  //{"localhost/fakesiteforms", "Forms"},
                                  //{"localhost/fakesitewindows", "Windows"},
                                  //{"localhost/fakesitenone", "None"}
                              };
            site.UrlList = urlList;
            site.AllowChildType("Car");
            site.Save();

            return site;
        }
        internal static void CleanupTestSite()
        {
            var node = Node.LoadNode("/Root/Sites/" + TestSiteName);
            if (node != null)
                node.ForceDelete();
        }

        internal static Stream CreateRequestStream(string request)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(request);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        internal static PortalContext CreatePortalContext(string pagePath, string queryString, System.IO.TextWriter output)
        {
            var simulatedWorkerRequest = new SimulatedHttpRequest(@"\", @"C:\Inetpub\wwwroot", pagePath, queryString, output, "localhost");
            var simulatedHttpContext = new HttpContext(simulatedWorkerRequest);
            HttpContext.Current = simulatedHttpContext;
            var portalContext = PortalContext.Create(simulatedHttpContext);
            return portalContext;
        }
        internal static ODataExceptionCode GetExceptionCode(StringWriter output)
        {
            ODataExceptionCode oecode;
            Enum.TryParse<ODataExceptionCode>(GetExceptionCodeText(output), out oecode);
            return oecode;
        }
        internal static string GetExceptionCodeText(StringWriter output)
        {
            var json = Deserialize(output);
            var error = json["error"] as JObject;
            return error["code"].Value<string>();
        }
        internal static void CheckError(StringWriter output)
        {
            var text = GetStringResult(output);
            if (text.IndexOf("error") < 0)
                return;
            Error e = null;
            try
            {
                e = GetError(output);
            }
            catch { } // does nothing
            if (e != null)
                throw new ApplicationException(String.Format("Code: {0}, Message: {1}", e.Code, e.Message));
        }
        internal static Error GetError(StringWriter output)
        {
            var json = Deserialize(output);
            var error = json["error"] as JObject;
            if (error == null)
                throw new Exception("Object is not an error");
            var code = error["code"].Value<string>();
            var message = error["message"] as JObject;
            var value = message["value"].Value<string>();
            ODataExceptionCode oecode;
            Enum.TryParse<ODataExceptionCode>(code, out oecode);
            return new Error { Code = oecode, Message = value };
        }
        internal static Entity GetEntity(StringWriter output)
        {
            var result = new Dictionary<string, object>();
            var jo = (JObject)Deserialize(output);
            return CreateEntity((JObject)jo["d"]);
        }
        internal static Entities GetEntities(StringWriter output)
        {
            var result = new List<Entity>();
            var jo = (JObject)Deserialize(output);
            var d = (JObject)jo["d"];
            var count = d["__count"].Value<int>();
            var jarray = (JArray)d["results"];
            for (int i = 0; i < jarray.Count; i++)
                result.Add(CreateEntity((JObject)jarray[i]));
            return new Entities(result.ToList(), count);
        }
        internal static Entity CreateEntity(JObject obj)
        {
            var props = new Dictionary<string, object>();
            obj.Properties().Select(y => { props.Add(y.Name, y.Value.Value<object>()); return true; }).ToList();
            return new Entity(props);
        }

        private static JContainer Deserialize(StringWriter output)
        {
            var text = GetStringResult(output);
            JContainer json;
            using (var reader = new StringReader(text))
                json = Deserialize(reader);
            return json;
        }
        private static JContainer Deserialize(TextReader reader)
        {
            string models;
            models = reader.ReadToEnd();

            var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
            var serializer = JsonSerializer.Create(settings);
            var jreader = new JsonTextReader(new StringReader(models));
            var x = (JContainer)serializer.Deserialize(jreader);
            return x;

        }
        internal static JValue GetSimpleResult(StringWriter output)
        {
            var result = new Dictionary<string, object>();
            var jo = (JObject)Deserialize(output);
            var value = jo["d"]["result"];
            return (JValue)value;
        }
        internal static string GetStringResult(StringWriter output)
        {
            return output.GetStringBuilder().ToString();
        }
        private object GetUrl(string path)
        {
            return string.Format("http://localhost/OData.svc/{0}('{1}')", RepositoryPath.GetParentPath(path), RepositoryPath.GetFileName(path));
        }

        private XmlDocument GetMetadataXml(string src, out XmlNamespaceManager nsmgr)
        {
            var xml = new XmlDocument();
            nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2007/06/edmx");
            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsmgr.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsmgr.AddNamespace("x", "http://schemas.microsoft.com/ado/2007/05/edm");
            xml.LoadXml(src);
            return xml;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------
        internal class Entity
        {
            Dictionary<string, object> _data;
            public Entity(Dictionary<string, object> data)
            {
                _data = data;
            }

            public int Id
            {
                get
                {
                    if (!_data.ContainsKey("Id"))
                        return 0;
                    return ((JValue)_data["Id"]).Value<int>();
                }
            }
            public string Name
            {
                get
                {
                    if (!_data.ContainsKey("Name"))
                        return null;
                    return ((JValue)_data["Name"]).Value<string>();
                }
            }
            public string Path
            {
                get
                {
                    if (!_data.ContainsKey("Path"))
                        return null;
                    return ((JValue)_data["Path"]).Value<string>();
                }
            }
            public ContentType ContentType
            {
                get
                {
                    // ((JValue)((JObject)entity.AllProperties["__metadata"])["type"]).Value
                    object meta;
                    if (!_data.TryGetValue("__metadata", out meta))
                        return null;
                    return ContentType.GetByName((string)((JValue)((JObject)meta)["type"]).Value);
                }
            }

            Entity _createdBy;
            public Entity CreatedBy
            {
                get
                {
                    if (_createdBy == null)
                        _createdBy = GetEntity("CreatedBy");
                    return _createdBy;
                }
            }
            Entity _manager;
            public Entity Manager
            {
                get
                {
                    if (_manager == null)
                        _manager = GetEntity("Manager");
                    return _manager;
                }
            }

            public int Index
            {
                get
                {
                    if (!_data.ContainsKey("Index"))
                        return 0;
                    return ((JValue)_data["Index"]).Value<int>();
                }
            }

            public Dictionary<string, object> AllProperties { get { return _data; } }
            public bool IsDeferred { get { return AllProperties.Count == 1 && AllProperties.Keys.First() == "__deferred"; } }
            public bool IsExpanded { get { return AllProperties.Count > 1; } }
            public bool AllPropertiesSelected { get { return AllProperties.Count > 20; } }

            private Entity GetEntity(string name)
            {
                if (!_data.ContainsKey(name))
                    return null;
                var obj = _data[name];
                var jobj = obj as JObject;
                if (jobj != null)
                    return jobj == null ? (Entity)null : ODataTests.CreateEntity(jobj);

                var jvalue = obj as JValue;
                if (jvalue.Type == JTokenType.Null)
                    return null;

                throw new NotImplementedException();
            }
        }
        internal class Entities : IEnumerable<Entity>
        {
            List<Entity> _entities;
            public int TotalCount { get; private set; }
            public int Length { get { return _entities.Count; } }

            public Entities(List<Entity> entities, int count)
            {
                _entities = entities;
                this.TotalCount = count;
            }

            public Entity this[int index]
            {
                get { return _entities[index]; }
            }

            public IEnumerator<Entity> GetEnumerator()
            {
                return _entities.GetEnumerator();
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        internal class Error
        {
            public ODataExceptionCode Code;
            public string Message;
        }
    }
}
