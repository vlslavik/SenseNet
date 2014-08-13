using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using SenseNet.Services.ContentStore;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Fields;
using SenseNet.Portal.OData;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class AspectTests : TestBase
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

        private static string _testRootName = "_AspectTests";
        private static string _testRootPath = String.Concat("/Root/", _testRootName);
        /// <summary>
        /// Do not use. Instead of TestRoot property
        /// </summary>
        private Node __testRoot;
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

        [ClassInitialize]
        public static void InitializePlayground(TestContext testContext)
        {
            //var content = Content.Create(User.Administrator);
            //if (((IEnumerable<Node>)content["Manager"]).Count() > 0)
            //    return;
            //content["Manager"] = User.Administrator;
            //content["Email"] = "a@b.c";
            //content.Save();
        }
        [ClassCleanup]
        public static void DestroyPlayground()
        {
            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);
        }

        #endregion

        [TestMethod]
        public void Aspect_HasFieldIfHasAspect()
        {
            Aspect aspect1 = null;
            Aspect aspect2 = null;
            try
            {
                aspect1 = EnsureAspect("Aspect_HasFieldIfHasAspect_Aspect1");
                aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
  </Fields>
</AspectDefinition>";
                aspect1.Save();

                aspect2 = EnsureAspect("Aspect_HasFieldIfHasAspect_Aspect2");
                aspect2.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field2' type='ShortText' />
  </Fields>
</AspectDefinition>";
                aspect2.Save();

                var fieldName1 = String.Concat(aspect1.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                var fieldName2 = String.Concat(aspect2.Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");

                var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#1");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#2");

                content.AddAspects(aspect1);
                Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#3");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#4");

                content.RemoveAspects(aspect1);
                Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#5");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#6");

                content.AddAspects(aspect1, aspect2);
                Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#7");
                Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#8");

                content.RemoveAspects(aspect2.Path);
                Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#9");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#10");

                content.AddAspects(aspect2.Id);
                Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#11");
                Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#12");

                content.RemoveAllAspects();
                Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#13");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#14");
            }
            finally
            {
                aspect1.ForceDelete();
                aspect2.ForceDelete();
            }
        }
        [TestMethod]
        public void Aspect_PropertySetAndPropertyNotCreated()
        {
            var contentListCount = ActiveSchema.ContentListTypes.Count;
            var propertyCount = ActiveSchema.PropertyTypes.Count;

            var aspect = EnsureAspect(Guid.NewGuid().ToString());
            aspect.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
    <AspectField name='Field2' type='ShortText' />
  </Fields>
</AspectDefinition>";
            aspect.Save();

            Assert.IsTrue(ActiveSchema.ContentListTypes.Count == contentListCount, "ContentListType is created.");
            Assert.IsTrue(ActiveSchema.PropertyTypes.Count == propertyCount, "PropertyTypes are created.");
        }
        [TestMethod]
        public void Aspect_Searchable()
        {
            Aspect aspect1 = null;
            Aspect aspect2 = null;
            try
            {
                aspect1 = EnsureAspect("Aspect1");
                aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
  </Fields>
</AspectDefinition>";
                aspect1.Save();

                aspect2 = EnsureAspect("Aspect2");
                aspect2.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field2' type='ShortText' />
  </Fields>
</AspectDefinition>";
                aspect2.Save();

                var fieldName1 = String.Concat(aspect1.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                var fieldName2 = String.Concat(aspect2.Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");

                var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects(aspect1, aspect2);
                content[fieldName1] = "Value1";
                content[fieldName2] = "Value2";
                content.Save();
                var id1 = content.Id;

                content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects(aspect1, aspect2);
                content[fieldName1] = "Value1a";
                content[fieldName2] = "Value2";
                content.Save();
                var id2 = content.Id;

                content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects(aspect1);
                content[fieldName1] = "Value1";
                content.Save();
                var id3 = content.Id;

                ContentTypeManager.Reset(); //---- must work with loaded indexing info table
                content = Content.Load(content.Id);

                var r1 = Content.All.DisableAutofilters().Where(c => (string)c[fieldName1] == "Value1").ToArray().Select(x => x.Id);
                var r2 = Content.All.DisableAutofilters().Where(c => (string)c[fieldName2] == "Value2").ToArray().Select(x => x.Id);
                var r3 = ContentQuery.Query(fieldName1 + ":Value1 .AUTOFILTERS:OFF").Identifiers;
                var r4 = ContentQuery.Query(fieldName2 + ":Value2 .AUTOFILTERS:OFF").Identifiers;

                var expected1 = String.Join(",", new[] { id1, id3 });
                var expected2 = String.Join(",", new[] { id1, id2 });
                var result1 = String.Join(",", r1);
                var result2 = String.Join(",", r2);
                var result3 = String.Join(",", r3);
                var result4 = String.Join(",", r4);

                Assert.AreEqual(expected1, result1);
                Assert.AreEqual(expected2, result2);
                Assert.AreEqual(expected1, result3);
                Assert.AreEqual(expected2, result4);
            }
            finally
            {
                aspect1.ForceDelete();
                aspect2.ForceDelete();
            }
        }
        [TestMethod]
        public void Aspect_Sortable()
        {
            Aspect aspect1 = null;
            try
            {
                aspect1 = EnsureAspect("Aspect_Sortable_Aspect1");
                aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
<Fields>
    <AspectField name='Field1' type='ShortText' />
  </Fields>
</AspectDefinition>";
                aspect1.Save();

                var fieldName1 = String.Concat(aspect1.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");

                var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects(aspect1);
                content[fieldName1] = "Aspect_Sortable1b";
                content.Save();
                var id1 = content.Id;

                content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects(aspect1);
                content[fieldName1] = "Aspect_Sortable1c";
                content.Save();
                var id2 = content.Id;

                content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects(aspect1);
                content[fieldName1] = "Aspect_Sortable1a";
                content.Save();
                var id3 = content.Id;

                ContentTypeManager.Reset(); //---- must work with loaded indexing info table
                content = Content.Load(content.Id);

                var r0 = Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot) && ((string)c[fieldName1]).StartsWith("Aspect_Sortable1")).ToArray().Select(x => x.Id);
                var r1 = Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot) && ((string)c[fieldName1]).StartsWith("Aspect_Sortable1")).OrderBy(c => c[fieldName1]).ToArray().Select(x => x.Id);
                var r2 = Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot) && ((string)c[fieldName1]).StartsWith("Aspect_Sortable1")).OrderByDescending(c => c[fieldName1]).ToArray().Select(x => x.Id);
                var r3 = ContentQuery.Query(String.Format("+InTree:'{0}' +{1}:Aspect_Sortable1* .AUTOFILTERS:OFF .SORT:{1}", TestRoot.Path, fieldName1)).Identifiers;
                var r4 = ContentQuery.Query(String.Format("+InTree:'{0}' +{1}:Aspect_Sortable1* .AUTOFILTERS:OFF .REVERSESORT:{1}", TestRoot.Path, fieldName1)).Identifiers;

                var expected1 = String.Join(",", new[] { id3, id1, id2 });
                var expected2 = String.Join(",", new[] { id2, id1, id3 });
                var result0 = String.Join(",", r0);
                var result1 = String.Join(",", r1);
                var result2 = String.Join(",", r2);
                var result3 = String.Join(",", r3);
                var result4 = String.Join(",", r4);

                //Assert.AreEqual(expected1, result1);
                //Assert.AreEqual(expected2, result2);
                Assert.AreEqual(expected1, result3);
                Assert.AreEqual(expected2, result4);
            }
            finally
            {
                aspect1.ForceDelete();
            }
        }
        [TestMethod]
        public void Aspect_UniqueName()
        {
            var folder1 = new Folder(Repository.AspectsFolder) { Name = Guid.NewGuid().ToString() };
            folder1.Save();
            var folder2 = new Folder(Repository.AspectsFolder) { Name = Guid.NewGuid().ToString() };
            folder2.Save();

            var aspect1 = new Aspect(folder1) { Name = Guid.NewGuid().ToString() };
            aspect1.Save();
            var aspect2 = new Aspect(folder2) { Name = aspect1.Name };
            try
            {
                aspect2.Save();
                Assert.Fail("Exception was not thrown");
            }
            catch (InvalidOperationException)
            {
            }
        }
        
        [TestMethod]
        public void Aspect_UniqueName_02()
        {
            var aspectName = Guid.NewGuid().ToString();
            var aspect1 = new Aspect(Repository.AspectsFolder) { Name = aspectName };
            aspect1.Save();

            Assert.AreEqual(aspect1.Id, Aspect.LoadAspectByName(aspectName).Id, "#1 load newly created aspect by name failed: a different aspect was loaded.");

            //delete aspect to make its name available
            aspect1.ForceDelete();

            //create aspect with the same name
            var aspect2 = new Aspect(Repository.AspectsFolder) { Name = aspectName };
            aspect2.Save();
        }

        [TestMethod]
        public void Aspect_SameFieldName()
        {
            var folder1 = new Folder(TestRoot) { Name = Guid.NewGuid().ToString() };
            folder1.Save();

            var aspect1 = EnsureAspect("Aspect_SameFieldName_Aspect1");
            aspect1.AddFields(new FieldInfo { Name = "Field1", Type = "ShortText" });
            var fn1 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + "Field1";

            var aspect2 = EnsureAspect("Aspect_SameFieldName_Aspect2");
            aspect2.AddFields(new FieldInfo { Name = "Field1", Type = "Integer" });
            var fn2 = aspect2.Name + Aspect.ASPECTFIELDSEPARATOR + "Field1";

            var content1 = Content.CreateNew("Car", folder1, Guid.NewGuid().ToString());
            content1.AddAspects(aspect1);
            content1[fn1] = "TextValue";
            content1.Save();

            var content2 = Content.CreateNew("Car", folder1, Guid.NewGuid().ToString());
            content2.AddAspects(aspect2);
            content2[fn2] = 42;
            content2.Save();

            var result1 = Content.All.DisableAutofilters().Where(c => (string)c[fn1] == "TextValue").Count();
            var result2 = Content.All.DisableAutofilters().Where(c => (int)c[fn2] == 42).Count();

            Assert.IsTrue(result1 == 1, String.Format("Result1 is {0}, expected: 1", result1));
            Assert.IsTrue(result2 == 1, String.Format("Result2 is {0}, expected: 1", result2));
        }
        [TestMethod]
        public void Aspect_CreateAddFieldsRemoveFields()
        {
            Aspect aspect = null;
            try
            {
                var aspectContent = Content.CreateNew("Aspect", Repository.AspectsFolder, "Aspect42");
                aspectContent.Save();
                aspect = (Aspect)aspectContent.ContentHandler;

                var fieldName1 = String.Concat(aspect.Name, Aspect.ASPECTFIELDSEPARATOR, "Field1");
                var fieldName2 = String.Concat(aspect.Name, Aspect.ASPECTFIELDSEPARATOR, "Field2");

                var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects((Aspect)aspect);
                content.Save();

                //var fs1 = new ShortTextFieldSetting { Name = "Field1", ShortName = "ShortText" };
                //var fs2 = new ShortTextFieldSetting { Name = "Field2", ShortName = "ShortText" };
                var fs1 = new FieldInfo { Name = "Field1", Type = "ShortText" };
                var fs2 = new FieldInfo { Name = "Field2", Type = "ShortText" };

                //-----------------------------------------------------------------------------------------------------

                Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#1");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#2");

                aspect.AddFields(fs1);
                content = Content.Load(content.Id);
                Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#11");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#12");

                aspect.AddFields(fs2);
                content = Content.Load(content.Id);
                Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#21");
                Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#22");

                aspect.RemoveFields(fieldName1);
                content = Content.Load(content.Id);
                Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#31");
                Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#32");

                aspect.RemoveFields(fieldName2);
                content = Content.Load(content.Id);
                Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#41");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#42");

                aspect.AddFields(fs1, fs2);
                content = Content.Load(content.Id);
                Assert.IsTrue(content.Fields.ContainsKey(fieldName1), "#51");
                Assert.IsTrue(content.Fields.ContainsKey(fieldName2), "#52");

                aspect.RemoveFields(fieldName1, fieldName2);
                content = Content.Load(content.Id);
                Assert.IsFalse(content.Fields.ContainsKey(fieldName1), "#61");
                Assert.IsFalse(content.Fields.ContainsKey(fieldName2), "#62");
            }
            finally
            {
                aspect.ForceDelete();
            }
        }


        [TestMethod]
        public void Aspect_OData_AddRemoveAspect()
        {
            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var resourcePath = ODataHandler.GetEntityUrl(content.Path);
            var aspect1 = Content.CreateNew("Aspect", Repository.AspectsFolder, Guid.NewGuid().ToString());
            aspect1.Save();
            var aspect1Path = aspect1.Path;
            var aspect2 = Content.CreateNew("Aspect", Repository.AspectsFolder, Guid.NewGuid().ToString());
            aspect2.Save();
            var aspect2Path = aspect2.Path;
            var aspect3 = Content.CreateNew("Aspect", Repository.AspectsFolder, Guid.NewGuid().ToString());
            aspect3.Save();
            var aspect3Path = aspect3.Path;

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new ODataTests.TestActionResolver());

            ODataTests.CreateTestSite();
            try
            {
                string result;

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/AddAspects"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{aspects:[\"", aspect1Path, "\"]}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect1.ContentHandler));
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect2.ContentHandler));
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect3.ContentHandler));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/AddAspects"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{aspects:[\"", aspect2Path, "\", \"", aspect3Path, "\"]}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect1.ContentHandler));
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect2.ContentHandler));
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect3.ContentHandler));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/RemoveAspects"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{aspects:[\"", aspect1Path, "\", \"", aspect3Path, "\"]}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect1.ContentHandler));
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect2.ContentHandler));
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect3.ContentHandler));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/RemoveAspects"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{aspects:[\"", aspect2Path, "\"]}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect1.ContentHandler));
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect2.ContentHandler));
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect3.ContentHandler));


                content.AddAspects((Aspect)aspect1.ContentHandler, (Aspect)aspect2.ContentHandler, (Aspect)aspect3.ContentHandler);
                content.Save();
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect1.ContentHandler));
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect2.ContentHandler));
                Assert.IsTrue(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect3.ContentHandler));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/RemoveAllAspects"), "", output);
                    var handler = new ODataHandler();
                    //var stream = ODataTests.CreateRequestStream(String.Concat("{aspects:[\"", aspect2Path, "\"]}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect1.ContentHandler));
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect2.ContentHandler));
                Assert.IsFalse(((GenericContent)content.ContentHandler).HasReference("Aspects", aspect3.ContentHandler));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                ODataTests.CleanupTestSite();
            }

        }
        [TestMethod]
        public void Aspect_OData_AddRemoveFields()
        {
            var aspect = Content.CreateNew("Aspect", Repository.AspectsFolder, Guid.NewGuid().ToString());
            aspect.Save();
            //var aspectPath = aspect.Path;
            var resourcePath = ODataHandler.GetEntityUrl(aspect.Path);

            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.AddAspects(aspect.Path);
            content.Save();

            var odataHandlerAcc = new PrivateType(typeof(ODataHandler));
            var originalActionResolver = odataHandlerAcc.GetStaticProperty("ActionResolver");
            odataHandlerAcc.SetStaticProperty("ActionResolver", new ODataTests.TestActionResolver());

            var fieldInfosJson = GetJson(new[]{
                new FieldInfo { Name="Field1", Type="ShortText" },
                new FieldInfo { Name="Field2", Type="ShortText" },
                new FieldInfo { Name="Field3", Type="ShortText" },
            });

            ODataTests.CreateTestSite();
            try
            {
                string result;
                Field field;

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/AddFields"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{fields:", fieldInfosJson, "}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field1", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field2", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field3", out field));
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field4", out field));

                fieldInfosJson = GetJson(new[]{
                    new FieldInfo { Name="Field4", Type="ShortText" },
                });

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/AddFields"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{fields:", fieldInfosJson, "}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field1", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field2", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field3", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field4", out field));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/RemoveFields"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{fields:[\"Field1\"]}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field1", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field2", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field3", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field4", out field));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/RemoveFields"), "", output);
                    var handler = new ODataHandler();
                    var stream = ODataTests.CreateRequestStream(String.Concat("{fields:[\"Field4\",\"Field2\"]}"));
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", stream);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field1", out field));
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field2", out field));
                Assert.IsTrue(content.Fields.TryGetValue(aspect.Name + ".Field3", out field));
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field4", out field));

                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(string.Concat("/OData.svc", resourcePath, "/RemoveAllFields"), "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "POST", null);
                    result = output.GetStringBuilder().ToString();
                }
                content = Content.Load(content.Path);
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field1", out field));
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field2", out field));
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field3", out field));
                Assert.IsFalse(content.Fields.TryGetValue(aspect.Name + ".Field4", out field));
            }
            finally
            {
                odataHandlerAcc.SetStaticProperty("ActionResolver", originalActionResolver);
                content.DeletePhysical();
                ODataTests.CleanupTestSite();
            }

        }


        //--------------------------------------------------------------------------------------------- Bug reproductions

        [TestMethod]
        public void Aspect_OData_LongTextDoesNotContainCdata()
        {
            var fieldValue = "<p>Field value</p>";
            var aspect = EnsureAspect("LongTextTest");
            aspect.AddFields(new FieldInfo { Name = "Field1", Type = "LongText" });

            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.AddAspects(aspect);
            content["LongTextTest.Field1"] = fieldValue;
            content.Save();

            var uri = ODataTools.GetODataUrl(content);

            ODataTests.CreateTestSite();
            ODataTests.Entity entity;
            try
            {
                using (var output = new System.IO.StringWriter())
                {
                    var pc = ODataTests.CreatePortalContext(uri, "", output);
                    var handler = new ODataHandler();
                    handler.ProcessRequest(pc.OwnerHttpContext, "GET", null);
                    entity = ODataTests.GetEntity(output);
                }
                var value = entity.AllProperties["LongTextTest.Field1"];
                var jvalue = value as JValue;
                var stringValue = (string)jvalue.Value;
                Assert.AreEqual(fieldValue, stringValue);
            }
            finally
            {
                ODataTests.CleanupTestSite();
            }
        }

        [TestMethod]
        public void Aspect_FieldAppinfoContainsXmlCharacters()
        {
            var appinfoValue = "asdf<qwer>yxcv";
            var fieldvalue = "Xy <b>asdf</b>.";
            var aspect = EnsureAspect("XmlCharTest");
            aspect.AddFields(
                new FieldInfo
                {
                    AppInfo = appinfoValue,
                    Name = "TestField",
                    Type = "ShortText"
                });
            var content = Content.CreateNew("Car", TestRoot, null);
            content.AddAspects(aspect);
            content["XmlCharTest.TestField"] = fieldvalue;
            content.Save();
            var id = content.Id;

            //--------

            content = Content.Load(id);
            Assert.AreEqual(appinfoValue, content.Fields["XmlCharTest.TestField"].FieldSetting.AppInfo);
            Assert.AreEqual(fieldvalue, (string)content["XmlCharTest.TestField"]);
        }

        [TestMethod]
        public void Aspect_ReferenceFields()
        {
            Aspect aspect1 = null;
            try
            {
                var fields1 = new List<FieldInfo>();
                fields1.Add(new FieldInfo()
                {
                    Name = "MyField1",
                    Type = "ShortText",
                });
                fields1.Add(new FieldInfo()
                {
                    Name = "MyField2",
                    Type = "Reference",
                });

                aspect1 = EnsureAspect("Aspect_ReferenceFields_Aspect1");
                aspect1.AddFields(fields1.ToArray());
                aspect1.Save();

                var fn11 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + fields1[0].Name;
                var fn12 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + fields1[1].Name;

                // -----------

                var content1 = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content1.AddAspects(aspect1);
                content1[fn11] = "Hello world this is a nice summer afternoon!";
                content1.Save();

                var content2 = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content2.AddAspects(aspect1);
                content2[fn11] = "Hello world this is a cold winter morning!";
                content2[fn12] = new List<Node> { content1.ContentHandler };
                content2.Save();

                // Test reference property value after reload

                content2 = Content.Load(content2.Id);
                IEnumerable<Node> references = (IEnumerable<Node>)content2[fn12];
                
                Assert.IsTrue(references.Any());
                Assert.IsTrue(references.Count() == 1);
                Assert.IsTrue(references.ElementAt(0).Id == content1.Id);

                // Test if the field can be queried with CQL

                var q = ContentQuery.Query(fn12 + ":" + content1.Id.ToString(), new QuerySettings { EnableAutofilters = FilterStatus.Disabled });
                IEnumerable<int> ids = q.Identifiers;

                Assert.IsTrue(ids.Any());
                Assert.IsTrue(ids.Contains(content2.Id));

                // TODO: uncomment when LINQ on reference fields is implemented

                //// Test if the field can be queried with LINQ
                //
                //var qq = Content.All.Where(c => (int)c[fn12] == content1.Id).ToList();
                //
                //Assert.IsTrue(qq.Any());
                //Assert.IsTrue(qq.Any(c => c.Id == content2.Id));

                // -----------

                content1.ForceDelete();
                content2.ForceDelete();
            }
            finally
            {
                aspect1.ForceDelete();
            }
        }

        /// <summary>
        /// Testing custom indexing information for aspect fields.
        /// Custom indexing information should be persisted for aspect fields and the fields should be analyzed according to those settings.
        /// </summary>
        [TestMethod]
        public void Aspect_CustomIndexing()
        {
            Aspect aspect1 = null;
            try
            {
                var fields1 = new List<FieldInfo>();
                fields1.Add(new FieldInfo()
                {
                    Name = "MyField1",
                    Type = "ShortText",
                    Configuration = new ConfigurationInfo(),
                    Indexing = new IndexingInfo()
                    {
                        Analyzer = "Lucene.Net.Analysis.SimpleAnalyzer"
                    }
                });
                fields1.Add(new FieldInfo()
                {
                    Name = "MyField2",
                    Type = "LongText",
                    Configuration = new ConfigurationInfo(),
                    Indexing = new IndexingInfo()
                    {
                        Analyzer = "Lucene.Net.Analysis.Standard.StandardAnalyzer"
                    }
                });
                fields1.Add(new FieldInfo()
                {
                    Name = "MyField3",
                    Type = "LongText",
                    Configuration = new ConfigurationInfo(),
                    Indexing = new IndexingInfo()
                    {
                        Mode = IndexingMode.No
                    }
                });

                aspect1 = EnsureAspect("Aspect_CustomIndexing_Aspect1");
                aspect1.AddFields(fields1.ToArray());
                aspect1.Save();

                var fn11 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + fields1[0].Name;
                var fn12 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + fields1[1].Name;
                var fn13 = aspect1.Name + Aspect.ASPECTFIELDSEPARATOR + fields1[2].Name;

                var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
                content.AddAspects(aspect1);
                content[fn11] = "Hello world this is a nice summer afternoon!";
                content[fn12] = "Yes it's really nice indeed!";
                content[fn13] = "aaa bbb ccc";
                content.Save();
                var id1 = content.Id;

                var lq1 = Content.All.DisableAutofilters().Where(c => (string)c[fn11] == "Hello").AsEnumerable().Select(c => c.Id).ToArray();
                var lq2 = Content.All.DisableAutofilters().Where(c => (string)c[fn12] == "really").AsEnumerable().Select(c => c.Id).ToArray();
                var lq3 = Content.All.DisableAutofilters().Where(c => (string)c[fn13] == "aaa").AsEnumerable().Select(c => c.Id).ToArray();
                var lq3_2 = Content.All.DisableAutofilters().Where(c => (string)c[fn13] == "aaa bbb ccc").AsEnumerable().Select(c => c.Id).ToArray();

                Assert.IsTrue(lq1.Contains(id1), "LINQ query should find the content by the first aspect field");
                Assert.IsTrue(lq2.Contains(id1), "LINQ query should find the content by the second aspect field");
                Assert.IsFalse(lq3.Contains(id1), "LINQ query should NOT find the content by the third aspect field (which is not indexed)");
                Assert.IsFalse(lq3_2.Contains(id1), "LINQ query should NOT find the content by the third aspect field (which is not indexed), not even with exact match");

                var cq1 = ContentQuery.Query(fn11 + ":Hello .AUTOFILTERS:OFF").Identifiers.ToArray();
                var cq2 = ContentQuery.Query(fn12 + ":really .AUTOFILTERS:OFF").Identifiers.ToArray();

                Assert.IsTrue(cq1.Contains(id1), "Content query should find the content by the first aspect field");
                Assert.IsTrue(cq2.Contains(id1), "Content query should find the content by the second aspect field");

                content.ForceDelete();
            }
            finally
            {
                aspect1.ForceDelete();
            }
        }

        //=============================================================================================

        internal static Aspect EnsureAspect(string name)
        {
            var r = ContentQuery.Query("+TypeIs:Aspect +Name:@0 .AUTOFILTERS:OFF", null, name);
            if (r.Count > 0)
                return (Aspect)r.Nodes.First();
            var aspectContent = Content.CreateNew("Aspect", Repository.AspectsFolder, name);
            aspectContent.Save();
            return (Aspect)aspectContent.ContentHandler;
        }
        private string GetJson(object o)
        {
            var writer = new StringWriter();
            Newtonsoft.Json.JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                .Serialize(writer, o);
            return writer.GetStringBuilder().ToString();
        }
    }
}
