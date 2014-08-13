using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Versioning;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Search.Parser;
using SenseNet.ContentRepository.i18n;
using System.IO;

namespace SenseNet.ContentRepository.Tests.Search
{
    [TestClass]
    public class SearchTest2 : TestBase
    {
        #region Test infrastructure
        public SearchTest2()
        {
        }

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
        private static string _testRootName = "_RepositoryTest_SearchTest2";
        private static string __testRootPath = String.Concat("/Root/", _testRootName);
        private static List<string> _installedContentTypes = new List<string>();

        private Folder __testRoot;
        private Folder TestRoot
        {
            get
            {
                if (__testRoot == null)
                {
                    __testRoot = (SystemFolder)Node.LoadNode(__testRootPath);
                    if (__testRoot == null)
                    {
                        Folder folder = new SystemFolder(Repository.Root);
                        folder.Name = _testRootName;
                        folder.Save();
                        __testRoot = (SystemFolder)Node.LoadNode(__testRootPath);
                    }
                }
                return __testRoot;
            }
        }

        [ClassCleanup]
        public static void RemoveContentTypes()
        {
            ContentType ct;
            if (Node.Exists(__testRootPath))
                Node.ForceDelete(__testRootPath);
            foreach (var ctName in _installedContentTypes)
            {
                ct = ContentType.GetByName(ctName);
                if (ct != null)
                    ct.Delete();
            }
            ct = ContentType.GetByName("Automobile1");
            if (ct != null)
                ct.Delete();
        }
        #endregion


        [TestMethod]
        public void ContentQuery_RecursiveQuery()
        {
            var q = new ContentQuery { Text = "+Members:{{Id:1}} +InTree:'/Root/IMS' .SORT:Id .AUTOFILTERS:OFF", Settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled } };
            var r = q.ExecuteToIds(ExecutionHint.ForceIndexedEngine);
            Assert.IsTrue(r.Count() > 0, "#01");
            var id = r.First();
            Assert.IsTrue(id == Group.Administrators.Id, "#02");

            q = new ContentQuery { Text = "+Members:{{Name:admin*}} +InTree:'/Root/IMS' .SORT:Id .AUTOFILTERS:OFF", Settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled } };
            r = q.ExecuteToIds(ExecutionHint.ForceIndexedEngine);
            Assert.IsTrue(r.Count() > 0, "#11");
            id = r.First();
            Assert.IsTrue(id == Group.Administrators.Id, "#12");
        }
        [TestMethod]
        public void ContentQuery_RecursiveQuery_Empty()
        {
            ContentQuery q;
            IEnumerable<int> r;
            int id;

            q = new ContentQuery { Text = "+Members:{{Name:NOBODY42}} +Name:Administrators .AUTOFILTERS:OFF" };
            r = q.ExecuteToIds(ExecutionHint.ForceIndexedEngine);
            Assert.IsTrue(r.Count() == 0, "#05");

            q = new ContentQuery { Text = "Members:{{Name:NOBODY42}} Name:Administrators .AUTOFILTERS:OFF" };
            r = q.ExecuteToIds(ExecutionHint.ForceIndexedEngine);
            Assert.IsTrue(r.Count() > 0, "#07");
            id = r.First();
            Assert.IsTrue(id == Group.Administrators.Id, "#08");
        }

        [TestMethod]
        public void ContentQuery_LucQueryAddSimpleAndClause()
        {
            var inputText = ".SKIP:10 Name:My* .TOP:5 Meta:'.TOP:6' Type:Folder";
            var extensionText = "InTree:/Root/JohnSmith";
            var inputQuery = LucQuery.Parse(inputText);
            var extensionQuery = LucQuery.Parse(extensionText);
            inputQuery.AddAndClause(extensionQuery);
            var combinedAndText = inputQuery.ToString();

            var expectedAndText = "+(Name:my* Meta:.TOP:6 Type:folder) +InTree:/root/johnsmith .TOP:5 .SKIP:10";

            Assert.AreEqual(expectedAndText, combinedAndText);
        }
        [TestMethod]
        public void ContentQuery_LucQueryAddDoubleAndClause()
        {
            var inputText = ".SKIP:10 Name:My* .TOP:5 Meta:'.TOP:6' Type:Folder";
            var extensionText = ".AUTOFILTERS:OFF InTree:/Root/JohnSmith .TOP:100 InTree:/Root/System";
            var inputQuery = LucQuery.Parse(inputText);
            var extensionQuery = LucQuery.Parse(extensionText);
            inputQuery.AddAndClause(extensionQuery);
            var combinedAndText = inputQuery.ToString();

            var expectedAndText = "+(Name:my* Meta:.TOP:6 Type:folder) +(InTree:/root/johnsmith InTree:/root/system) .TOP:5 .SKIP:10";

            Assert.AreEqual(expectedAndText, combinedAndText);
        }
        [TestMethod]
        public void ContentQuery_LucQueryAddSimpleOrClause()
        {
            var inputText = ".SKIP:10 Name:My* .TOP:5 Meta:'.TOP:6' Type:Folder";
            var extensionText = "InTree:/Root/JohnSmith";
            var inputQuery = LucQuery.Parse(inputText);
            var extensionQuery = LucQuery.Parse(extensionText);
            inputQuery.AddOrClause(extensionQuery);
            var combinedAndText = inputQuery.ToString();

            var expectedAndText = "(Name:my* Meta:.TOP:6 Type:folder) InTree:/root/johnsmith .TOP:5 .SKIP:10";

            Assert.AreEqual(expectedAndText, combinedAndText);
        }
        [TestMethod]
        public void ContentQuery_LucQueryAddDoubleOrClause()
        {
            var inputText = ".SKIP:10 Name:My* .TOP:5 Meta:'.TOP:6' Type:Folder";
            var extensionText = ".AUTOFILTERS:OFF InTree:/Root/JohnSmith .TOP:100 InTree:/Root/System";
            var inputQuery = LucQuery.Parse(inputText);
            var extensionQuery = LucQuery.Parse(extensionText);
            inputQuery.AddOrClause(extensionQuery);
            var combinedAndText = inputQuery.ToString();

            var expectedAndText = "(Name:my* Meta:.TOP:6 Type:folder) (InTree:/root/johnsmith InTree:/root/system) .TOP:5 .SKIP:10";

            Assert.AreEqual(expectedAndText, combinedAndText);
        }

        [TestMethod]
        public void ContentQuery_CountOnly()
        {
            var expectedCount = ContentType.GetContentTypeNames().Length;
            var query = LucQuery.Parse("Type:ContentType .COUNTONLY .AUTOFILTERS:OFF .LIFESPAN:OFF");
            var result = query.Execute().ToArray();
            var totalCount = query.TotalCount;

            Assert.IsTrue(result.Length == 0, String.Format("Result length is: {0}, expected: 0.", result.Length));
            Assert.IsTrue(expectedCount == totalCount, String.Format("TotalCount is: {0}, expected: {1}.", totalCount, expectedCount));
        }

        [TestMethod]
        public void ContentQuery_SeePreviewOnly()
        {
            var root = new SystemFolder(TestRoot) { Name = "ContentQuery_SeePreviewOnly" };
            root.Save();
            var node1 = new SystemFolder(root) { Name = "N1", Index = 42 }; // See deny
            node1.Save();
            var node2 = new SystemFolder(root) { Name = "N2", Index = 42 }; // See
            node2.Save();
            var node3 = new SystemFolder(root) { Name = "N3", Index = 42 }; // Preview
            node3.Save();
            var node4 = new SystemFolder(root) { Name = "N4", Index = 42 }; // PreviewWithoutWatermark
            node4.Save();
            var node5 = new SystemFolder(root) { Name = "N5", Index = 42 }; // PreviewWithoutRedaction
            node5.Save();
            var node6 = new SystemFolder(root) { Name = "N6", Index = 42 }; // Open
            node6.Save();
            var node7 = new SystemFolder(root) { Name = "N7", Index = 43 }; // -
            node7.Save();
            var node8 = new SystemFolder(root) { Name = "N8", Index = 43 }; // -
            node8.Save();

            using (new SystemAccount())
            {
                node1.Security.BreakInheritance();
                //node1.Security.RemoveExplicitEntries();
                node1.Security.GetAclEditor()
                    .SetPermission(Group.Administrators, true, PermissionType.OpenMinor, PermissionValue.Allow)
                    .SetPermission(Group.Administrators, true, PermissionType.Delete, PermissionValue.Allow)
                    .SetPermission(User.Visitor, true, PermissionType.See, PermissionValue.Deny)
                    .Apply();

                node2.Security.BreakInheritance();
                node2.Security.GetAclEditor()
                    .SetPermission(Group.Administrators, true, PermissionType.OpenMinor, PermissionValue.Allow)
                    .SetPermission(Group.Administrators, true, PermissionType.Delete, PermissionValue.Allow)
                    .SetPermission(User.Visitor, true, PermissionType.Preview, PermissionValue.NonDefined)
                    .Apply();

                node3.Security.BreakInheritance();
                node3.Security.GetAclEditor()
                    .SetPermission(Group.Administrators, true, PermissionType.OpenMinor, PermissionValue.Allow)
                    .SetPermission(Group.Administrators, true, PermissionType.Delete, PermissionValue.Allow)
                    .SetPermission(User.Visitor, true, PermissionType.PreviewWithoutWatermark, PermissionValue.NonDefined)
                    .SetPermission(User.Visitor, true, PermissionType.PreviewWithoutRedaction, PermissionValue.NonDefined)
                    .Apply();

                node4.Security.BreakInheritance();
                node4.Security.GetAclEditor()
                    .SetPermission(Group.Administrators, true, PermissionType.OpenMinor, PermissionValue.Allow)
                    .SetPermission(Group.Administrators, true, PermissionType.Delete, PermissionValue.Allow)
                    .SetPermission(User.Visitor, true, PermissionType.PreviewWithoutRedaction, PermissionValue.NonDefined)
                    .Apply();

                node5.Security.BreakInheritance();
                node5.Security.GetAclEditor()
                    .SetPermission(Group.Administrators, true, PermissionType.OpenMinor, PermissionValue.Allow)
                    .SetPermission(Group.Administrators, true, PermissionType.Delete, PermissionValue.Allow)
                    .SetPermission(User.Visitor, true, PermissionType.Open, PermissionValue.NonDefined)
                    .Apply();

                node6.Security.BreakInheritance();
                node6.Security.GetAclEditor()
                    .SetPermission(Group.Administrators, true, PermissionType.OpenMinor, PermissionValue.Allow)
                    .SetPermission(Group.Administrators, true, PermissionType.Delete, PermissionValue.Allow)
                    .SetPermission(User.Visitor, true, PermissionType.Open, PermissionValue.Allow)
                    .Apply();

            }

            var origUser = User.Current;
            try
            {
                User.Current = User.Visitor;
                var query = string.Format("+InFolder:\"{0}\" +Index:42 .AUTOFILTERS:OFF", root.Path);
                var result = ContentQuery.Query(query);

                Assert.AreEqual(5, result.Identifiers.Count());

                Assert.AreEqual(5, result.Nodes.Count());
            }
            finally
            {
                User.Current = origUser;
            }
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ContentQuery_ChoiceFieldLocalizedOptions_ReservedOptionName()
        {
            ContentTypeInstaller.InstallContentType(String.Format(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ContentQuery_ChoiceField' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowExtraValue>true</AllowExtraValue>
								<AllowMultiple>true</AllowMultiple>
								<Options>
									<Option value='0'>$TestChoice,Text1</Option>
									<Option value='1'>$TestChoice,Text2</Option>
									<Option value='2'>$TestChoice,Text3</Option>
									<Option value='{0}3'>Text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>", SenseNet.ContentRepository.Fields.ChoiceField.EXTRAVALUEPREFIX));
        }

        [TestMethod]
        public void ContentQuery_ChoiceFieldLocalizedOptions()
        {
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ContentQuery_ChoiceField' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowExtraValue>true</AllowExtraValue>
								<AllowMultiple>true</AllowMultiple>
								<Options>
									<Option value='0'>$TestChoice,Text1</Option>
									<Option value='1'>$TestChoice,Text2</Option>
									<Option value='2'>$TestChoice,Text3</Option>
									<Option value='3'>Text4</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            SaveResources("TestChoice", @"<?xml version=""1.0"" encoding=""utf-8""?>
                <Resources>
                  <ResourceClass name=""TestChoice"">
                    <Languages>
                      <Language cultureName=""en"">
                        <data name=""Text1"" xml:space=""preserve""><value>Text-1</value></data>
                        <data name=""Text2"" xml:space=""preserve""><value>Text-2</value></data>
                        <data name=""Text3"" xml:space=""preserve""><value>Text-3</value></data>
                      </Language>
                      <Language cultureName=""hu"">
                        <data name=""Text1"" xml:space=""preserve""><value>Szöveg-1</value></data>
                        <data name=""Text2"" xml:space=""preserve""><value>Szöveg-2</value></data>
                        <data name=""Text3"" xml:space=""preserve""><value>Szöveg-3</value></data>
                      </Language>
                    </Languages>
                  </ResourceClass>
                </Resources>");

            SenseNetResourceManager.Reset();

            //var x = SenseNetResourceManager.Current.GetString("$TestChoice,Text1");
            //var y = SenseNetResourceManager.Current.GetStrings("TestChoice", "Text1");

            Content content = Content.CreateNew("ContentQuery_ChoiceField", TestRoot, Guid.NewGuid().ToString());
            content.ContentHandler["ChoiceTest"] = "2;3;~other.other text";

            content.Save();

            Assert.IsTrue(ContentQuery.Query("'Szöveg-3' .AUTOFILTERS:OFF").Count > 0);
            Assert.IsTrue(ContentQuery.Query("'Text-3' .AUTOFILTERS:OFF").Count > 0);
            Assert.IsTrue(ContentQuery.Query("'Text4' .AUTOFILTERS:OFF").Count > 0);
            Assert.IsTrue(ContentQuery.Query("'other text' .AUTOFILTERS:OFF").Count > 0);

            Assert.IsTrue(ContentQuery.Query("ChoiceTest:$2 .AUTOFILTERS:OFF").Count > 0);
            Assert.IsTrue(ContentQuery.Query("ChoiceTest:$3 .AUTOFILTERS:OFF").Count > 0);
            Assert.IsTrue(ContentQuery.Query("ChoiceTest:@0 .AUTOFILTERS:OFF", null, "~other").Count > 0);
            Assert.IsTrue(ContentQuery.Query("+ChoiceTest:@0 +ChoiceTest:@1 .AUTOFILTERS:OFF", null, "~other", "other text").Count > 0);

            var x = ContentQuery.Query("ChoiceTest:@0 .AUTOFILTERS:OFF", null, "Text-3").Count;

            Assert.IsTrue(ContentQuery.Query("ChoiceTest:@0 .AUTOFILTERS:OFF", null, "Text-3").Count > 0);
            Assert.IsTrue(ContentQuery.Query("ChoiceTest:@0 .AUTOFILTERS:OFF", null, "Szöveg-3").Count > 0);
            Assert.IsTrue(ContentQuery.Query("ChoiceTest:@0 .AUTOFILTERS:OFF", null, "Text4").Count > 0);
            Assert.IsTrue(ContentQuery.Query("ChoiceTest:@0 .AUTOFILTERS:OFF", null, "other text").Count > 0);
        }
        [TestMethod]
        public void ContentQuery_ChoiceFieldLocalizedOptions_Sorting()
        {
            ContentTypeInstaller.InstallContentType(@"<?xml version='1.0' encoding='utf-8'?>
				<ContentType name='ContentQuery_ChoiceField' parentType='GenericContent' handler='SenseNet.ContentRepository.GenericContent' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
					<Fields>
						<Field name='ChoiceTest' type='Choice'>
							<Configuration>
								<AllowExtraValue>true</AllowExtraValue>
								<AllowMultiple>true</AllowMultiple>
								<Options>
									<Option value='2'>$TestChoice,Text3</Option>
									<Option value='0'>$TestChoice,Text1</Option>
									<Option value='3'>Text4</Option>
									<Option value='1'>$TestChoice,Text2</Option>
								</Options>
							</Configuration>
						</Field>
					</Fields>
				</ContentType>");

            SaveResources("TestChoice", @"<?xml version=""1.0"" encoding=""utf-8""?>
                <Resources>
                  <ResourceClass name=""TestChoice"">
                    <Languages>
                      <Language cultureName=""en"">
                        <data name=""Text1"" xml:space=""preserve""><value>C-Text-1</value></data>
                        <data name=""Text2"" xml:space=""preserve""><value>A-Text-2</value></data>
                        <data name=""Text3"" xml:space=""preserve""><value>B-Text-3</value></data>
                      </Language>
                      <Language cultureName=""hu"">
                        <data name=""Text1"" xml:space=""preserve""><value>C-Szöveg-1</value></data>
                        <data name=""Text2"" xml:space=""preserve""><value>A-Szöveg-2</value></data>
                        <data name=""Text3"" xml:space=""preserve""><value>B-Szöveg-3</value></data>
                      </Language>
                    </Languages>
                  </ResourceClass>
                </Resources>");

            SenseNetResourceManager.Reset();

            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "2;3;~other.other text", 3);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "1", 2);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "3", 4);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "~other.other text 3", 9);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "0", 1);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "~other.other text 2", 8);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "~other.cccc", 7);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "~other.aaaa", 5);
            CreateTestContentForChoiceFieldLocalizedOptions_Sorting("Ordering-", "~other.bbbb", 6);

            var result = ContentQuery.Query("+Name:'Ordering-*' .AUTOFILTERS:OFF .SORT:ChoiceTest");
            Assert.AreEqual(9, result.Count);
            var indexes = String.Join(" ", result.Nodes.Select(n => n.Index.ToString()));
            Assert.AreEqual("1 2 3 4 5 6 7 8 9", indexes);
        }
        private void CreateTestContentForChoiceFieldLocalizedOptions_Sorting(string namePrefix, string term, int index)
        {
            var content = Content.CreateNew("ContentQuery_ChoiceField", TestRoot, namePrefix + Guid.NewGuid());
            content.ContentHandler["ChoiceTest"] = term;
            content["Index"] = index;
            content.Save();
        }
        private void SaveResources(string name, string xml)
        {
            var resNode = Node.Load<Resource>("/Root/Localization/" + name);
            if (resNode == null)
            {
                var parentNode = Node.LoadNode("/Root/Localization");
                if (parentNode == null)
                {
                    parentNode = new SystemFolder(Repository.Root, "Resources") { Name = "Localization" };
                    parentNode.Save();
                }
                resNode = new Resource(parentNode) { Name = name };
            }
            using (var stream = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
                writer.Write(xml);
                writer.Flush();
                resNode.Binary.SetStream(stream);
                resNode.Binary.FileName = "test";
                resNode.Save();
            }

        }
    }
}
