using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using System.Reflection;
using System.Diagnostics;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Tests.ContentHandlers;
using SenseNet.Search;
using SenseNet.ContentRepository.Linq;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class LinqTests : TestBase
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

        private static string _testRootName = "_LinqTests";
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


        private static string _testRootName2 = "_LinqTests2";
        private static string _testRootPath2 = String.Concat("/Root/", _testRootName2);
        /// <summary>
        /// Do not use. Instead of TestRoot property
        /// </summary>
        private static Node __testRoot2;
        public static Node TestRoot2
        {
            get
            {
                if (__testRoot2 == null)
                {
                    __testRoot2 = Node.LoadNode(_testRootPath2);
                    if (__testRoot2 == null)
                    {
                        Node node = NodeType.CreateInstance("SystemFolder", Node.LoadNode("/Root"));
                        node.Name = _testRootName2;
                        node.Save();
                        __testRoot2 = Node.LoadNode(_testRootPath2);
                    }
                }
                return __testRoot2;
            }
        }

        [ClassInitialize]
        public static void InitializePlayground(TestContext testContext)
        {
            var content = Content.Create(User.Administrator);
            if (((IEnumerable<Node>)content["Manager"]).Count() <= 0)
            {
                content["Manager"] = User.Administrator;
                content["Email"] = "anyuser@somewhere.com";
                content.Save();
            }

            //=======================================================================================

            var groups = Content.CreateNew("Folder", TestRoot, "Groups").ContentHandler;
            groups.Save();
            var htmlContents = Content.CreateNew("Folder", TestRoot, "HTMLContents").ContentHandler;
            htmlContents.Save();
            var cars = Content.CreateNew("Folder", TestRoot, "Cars").ContentHandler;
            cars.Save();
            for (int i = 1; i <= 4; i++)
            {
                content = Content.CreateNew("Group", groups, "Group" + i);
                content["Index"] = i;
                content.Save();

                content = Content.CreateNew("HTMLContent", htmlContents, "HTMLContent" + i);
                content["Index"] = i;
                content.Save();

                content = Content.CreateNew("Car", cars, "Car" + i);
                content["Index"] = i;
                content.Save();
            }

            //=======================================================================================

            if (ContentType.GetByName("RefTestNode") == null)
                ContentTypeInstaller.InstallContentType(RefTestNode.ContentTypeDefinition);

            var mother1 = new RefTestNode(TestRoot2);
            mother1.Name = "Mother1";
            mother1.Save();

            var mother2 = new RefTestNode(TestRoot2);
            mother2.Name = "Mother2";
            mother2.Save();

            var child1 = new RefTestNode(TestRoot2);
            child1.Name = "Child1";
            child1.Mother = mother1;
            child1.Save();

            var child2 = new RefTestNode(TestRoot2);
            child2.Name = "Child2";
            child2.Mother = mother2;
            child2.Save();

            var child3 = new RefTestNode(TestRoot2);
            child3.Name = "Child3";
            child3.Save();

            mother1.Index = 42;
            mother1.Save();
        }
        [ClassCleanup]
        public static void DestroyPlayground()
        {
            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);
            if (Node.Exists(_testRootPath2))
                Node.ForceDelete(_testRootPath2);
        }

        #endregion

        [TestMethod]
        public void Linq_IdRange_Order()
        {
            test(Content.All.Where(c => c.Id < 4).OrderBy(c => c.Id).ToList(),
                (from c in Content.All where c.Id < 4 orderby c.Id select c).ToList(),
                "Admin | Root | IMS");
            test(Content.All.Where(c => c.Id <= 4).OrderBy(c => c.Id).ToList(),
                (from c in Content.All where c.Id <= 4 orderby c.Id select c).ToList(),
                "Admin | Root | IMS | BuiltIn");
            test(Content.All.Where(c => c.Id <= 4).OrderByDescending(c => c.Id).ToList(),
                (from c in Content.All where c.Id <= 4 orderby c.Id descending select c).ToList(),
                "BuiltIn | IMS | Root | Admin");
            test(Content.All.Where(c => c.Id <= 4 && c.Id > 1).OrderBy(c => c.Id).ToList(),
                (from c in Content.All where c.Id <= 4 && c.Id > 1 orderby c.Id select c).ToList(),
                "Root | IMS | BuiltIn");
        }
        [TestMethod]
        public void Linq_SingleNegativeTerm()
        {
            var s = Lucene.Net.Util.Constants.LUCENE_VERSION;
            var ss = Lucene.Net.Util.Constants.LUCENE_MAIN_VERSION;
            if (ss != "2.9.4")
                Assert.Inconclusive("Need to revise 'full set query': LucQuery.FullSetQuery (be MachAllDocsQuery of version 3.0.3)");
            Assert.AreEqual("-Id:42 +Id:>0", GetQueryString(Content.All.Where(c => c.Id != 42)));
        }
        [TestMethod]
        public void Linq_StartsWithEndsWithContains()
        {
            Assert.AreEqual("Name:car*", GetQueryString(Content.All.Where(c => c.Name.StartsWith("Car"))));
            Assert.AreEqual("Name:*r2", GetQueryString(Content.All.Where(c => c.Name.EndsWith("r2"))));
            Assert.AreEqual("Name:*ro*", GetQueryString(Content.All.Where(c => c.Name.Contains("ro"))));
        }
        [TestMethod]
        public void Linq_CaseInsensitivity()
        {
            test(Content.All.Where(c => c.Name == "admin").OrderBy(c => c.Id).ToList(),
                (from c in Content.All where c.Name == "admin" orderby c.Id select c).ToList(),
                "Admin");
            test(Content.All.Where(c => c.Name == "Admin").OrderBy(c => c.Id).ToList(),
                (from c in Content.All where c.Name == "Admin" orderby c.Id select c).ToList(),
                "Admin");
        }
        [TestMethod]
        public void Linq_EmptyString()
        {
            Assert.AreEqual("DisplayName:''", GetQueryString(Content.All.Where(c => c.DisplayName == "")));
        }
        [TestMethod]
        public void Linq_NullString()
        {
            var cars = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString()).ContentHandler;
            cars.Save();
            for (int i = 1; i <= 4; i++)
            {
                var content = Content.CreateNew("Car", cars, "Car" + i);
                content["Index"] = i;
                if (i == 3)
                    content["Model"] = "959";
                content.Save();
            }

            var x = SenseNet.Search.ContentQuery.Query("Model:'' .AUTOFILTERS:OFF").Count;

            test(Content.All.DisableAutofilters().Where(c => c.InTree(cars) && (string)c["Model"] == null).OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(cars) && (string)c["Model"] == null orderby c.Name select c).ToList(),
                "Car1 | Car2 | Car4");
        }
        [TestMethod]
        public void Linq_DateTime()
        {
            for (var i = 1; i < 5; i++)
            {
                var node = Node.LoadNode(TestRoot.Path + "/Cars/Car" + i);
                node.ModificationDate = DateTime.UtcNow.AddDays(-i);
                node.Save();
            }

            test(Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot) && c.ModificationDate < DateTime.UtcNow.AddDays(-2)).OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(TestRoot) && c.ModificationDate < DateTime.UtcNow.AddDays(-2) orderby c.Name select c).ToList(),
                "Car2 | Car3 | Car4");
        }

        [TestMethod]
        public void Linq_NegativeTerm()
        {
            Assert.AreEqual("-Id:2 +Id:<=4", GetQueryString(Content.All.Where(c => c.Id <= 4 && c.Id != 2)));
            Assert.AreEqual("-Id:2 +Id:>0", GetQueryString(Content.All.Where(c => c.Id != 2)));
            Assert.AreEqual("-Id:2 +Id:>0", GetQueryString(Content.All.Where(c => c.Id > 0 && c.Id != 2)));
        }
        [TestMethod]
        public void Linq_Bool()
        {
            string q;

            q = GetQueryString(Content.All.Where(c => c.IsFolder == true));
            Assert.AreEqual("IsFolder:yes", q);

            q = GetQueryString(Content.All.Where(c => c.IsFolder == false));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(Content.All.Where(c => c.IsFolder != true));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(Content.All.Where(c => c.IsFolder));
            Assert.AreEqual("IsFolder:yes", q);

            q = GetQueryString(Content.All.Where(c => (bool)c["Hidden"]));
            Assert.AreEqual("Hidden:yes", q);

            q = GetQueryString(Content.All.OfType<SenseNet.Portal.Site>().Where(c => c.EnableClientBasedCulture));
            Assert.AreEqual("+TypeIs:site +EnableClientBasedCulture:yes", q);
        }
        [TestMethod]
        public void Linq_Bool_Negation()
        {
            string q;

            q = GetQueryString(Content.All.Where(c => !c.IsFolder));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(Content.All.Where(c => c.IsFolder != true));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(Content.All.Where(c => !(c.IsFolder == true)));
            Assert.AreEqual("IsFolder:no", q);

            q = GetQueryString(Content.All.Where(c => !(bool)c["Hidden"]));
            Assert.AreEqual("Hidden:no", q);

            q = GetQueryString(Content.All.OfType<SenseNet.Portal.Site>().Where(c => !c.EnableClientBasedCulture));
            Assert.AreEqual("+TypeIs:site +EnableClientBasedCulture:no", q);

            q = GetQueryString(Content.All.Where(c => !((SenseNet.Portal.Site)c.ContentHandler).EnableClientBasedCulture));
            Assert.AreEqual("EnableClientBasedCulture:no", q);
        }
        [TestMethod]
        public void Linq_Negation()
        {
            string q;

            q = GetQueryString(Content.All.Where(c => c.Index != 42));
            Assert.AreEqual("-Index:42 +Id:>0", q);

            q = GetQueryString(Content.All.Where(c => !(c.Index == 42)));
            Assert.AreEqual("-Index:42 +Id:>0", q);

            q = GetQueryString(Content.All.Where(c => !(!(c.Index == 42) && !c.IsFolder)));
            Assert.AreEqual("-(+IsFolder:no -Index:42) +Id:>0", q);

        }
        [TestMethod]
        public void Linq_SingleReference()
        {
            var mother1 = Node.LoadNode(TestRoot2.Path + "/Mother1");
            if (mother1 == null)
                Assert.Inconclusive("Cannot test with insufficient environment: expected referenced node is null.");

            Assert.AreEqual(
                String.Format("+TypeIs:repositorytest_reftestnode +Mother:{0}", mother1.Id),
                GetQueryString(Content.All.OfType<RefTestNode>().Where(c => c.Mother == mother1)));
        }
        [TestMethod]
        public void Linq_MultiReference()
        {
            var path1 = TestRoot2.Path + "/Neighbor1";
            var path2 = TestRoot2.Path + "/Neighbor2";
            var names = new[] { "Mother1", "Mother2", "Child1", "Child2", "Child3" };
            try
            {
                var node1 = Node.Load<RefTestNode>(path1);
                if (node1 == null)
                {
                    node1 = new RefTestNode(TestRoot2);
                    node1.Name = "Neighbor1";
                    node1.Save();
                    var refNodes = names.Select(n => Node.Load<RefTestNode>(RepositoryPath.Combine(TestRoot2.Path, n))).ToArray();
                    node1.Neighbors = refNodes;
                    node1.Save();
                }

                var node2 = Node.Load<RefTestNode>(path2);
                if (node2 == null)
                {
                    node2 = new RefTestNode(TestRoot2);
                    node2.Name = "Neighbor2";
                    node2.Save();
                    var refNodes = (new[] { "Mother1", "Child1" }).Select(n => Node.Load<RefTestNode>(RepositoryPath.Combine(TestRoot2.Path, n))).ToArray();
                    node2.Neighbors = refNodes;
                    node2.Save();
                }

                //--------
                var child2 = Node.LoadNode(TestRoot2.Path + "/Child2");
                var result = Content.All.DisableAutofilters().Where(c => ((RefTestNode)c.ContentHandler).Neighbors.Contains(child2)).ToArray();
                Assert.IsTrue(result.Length == 1, String.Format("result.Length is {0}, expected: 1.", result.Length));
                Assert.IsTrue(result.First().Name == "Neighbor1", String.Format("result.First().Name is {0}, expected: 'Neighbor1'.", result.First().Name));
            }
            finally
            {
                Node.ForceDelete(path1);
                Node.ForceDelete(path2);
            }
        }
        [TestMethod]
        public void Linq_EmptyReference()
        {
            Content[] result;
            string expected, actual;
            QueryResult qresult;

            var mother1 = Node.LoadNode(TestRoot2.Path + "/Mother1");
            var mother2 = Node.LoadNode(TestRoot2.Path + "/Mother2");
            var child1 = Node.LoadNode(TestRoot2.Path + "/Child1");
            var child2 = Node.LoadNode(TestRoot2.Path + "/Child2");
            var child3 = Node.LoadNode(TestRoot2.Path + "/Child3");

            qresult = ContentQuery.Query(String.Concat("+Mother:null +InTree:", TestRoot2.Path, " .AUTOFILTERS:OFF"));
            result = Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot2) && ((RefTestNode)c.ContentHandler).Mother == null).OrderBy(c => c.Name).ToArray();
            Assert.IsTrue(result.Length == 3, String.Format("#5: count is {0}, expected: 3", result.Length));
            expected = String.Concat(child3.Id, ", ", mother1.Id, ", ", mother2.Id);
            actual = String.Join(", ", result.Select(x => x.Id));
            Assert.IsTrue(expected == actual, String.Format("#6: actual is {0}, expected: {1}", actual, expected));

            qresult = ContentQuery.Query(String.Concat("-Mother:null +InTree:", TestRoot2.Path, " +TypeIs:repositorytest_reftestnode .AUTOFILTERS:OFF"));
            result = Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot2) && ((RefTestNode)c.ContentHandler).Mother != null && c.ContentHandler is RefTestNode).OrderBy(c => c.Name).ToArray();
            Assert.IsTrue(result.Length == 2, String.Format("#5: count is {0}, expected: 2", result.Length));
            expected = String.Concat(child1.Id, ", ", child2.Id);
            actual = String.Join(", ", result.Select(x => x.Id));
            Assert.IsTrue(expected == actual, String.Format("#6: actual is {0}, expected: {1}", actual, expected));
        }

        [TestMethod]
        public void Linq_Children()
        {
            var folderName = "Linq_Children_test";
            var folder = Folder.Load<Folder>(RepositoryPath.Combine(TestRoot.Path, folderName));
            if (folder == null)
            {
                folder = new Folder(TestRoot) { Name = folderName };
                folder.Save();
                for (int i = 0; i < 4; i++)
                {
                    var content = Content.CreateNew("Car", folder, "Car" + i);
                    content.ContentHandler.Index = i;
                    content.Save();
                }
            }
            var folderContent = Content.Create(folder);

            var enumerable = folderContent.Children.DisableAutofilters().Where(c => c.Index < 2).OrderBy(c => c.Name);
            var result = enumerable.ToArray();

            var paths = result.Select(c => c.Path).ToArray();

            Assert.IsTrue(result.Length == 2, String.Format("result.Length is {0}, expected: 2.", result.Length));
            Assert.IsTrue(result[0].Name == "Car0", String.Format("result[0].Name is {0}, expected: 'Car0'.", result[0].Name));
            Assert.IsTrue(result[1].Name == "Car1", String.Format("result[1].Name is {0}, expected: 'Car1'.", result[1].Name));

        }
        [TestMethod]
        public void Linq_Children_Count()
        {
            if (ContentQuery.Query(".AUTOFILTERS:OFF .COUNTONLY Infolder:" + TestRoot.Path).Count == 0)
                for (int i = 0; i < 3; i++)
                    Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString()).Save();
            var r = ContentQuery.Query(".AUTOFILTERS:OFF InFolder:" + TestRoot.Path);
            var expected = r.Count;
            var content = Content.Create(TestRoot);
            var actual = content.Children.DisableAutofilters().Count();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Linq_First()
        {
            //.First(c => ...), .FirstOrDefault(c => ...)
            var content = Content.All.DisableAutofilters().Where(c => c.Id < 6).OrderByDescending(c => c.Id).First();
            Assert.AreEqual(5, content.Id);
        }
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void Linq_First_OnEmpty()
        {
            var content = Content.All.DisableAutofilters().Where(c => c.Id < 0).OrderByDescending(c => c.Id).First();
        }
        [TestMethod]
        public void Linq_FirstOrDefault()
        {
            var content = Content.All.DisableAutofilters().Where(c => c.Id < 6).OrderByDescending(c => c.Id).FirstOrDefault();
            Assert.AreEqual(5, content.Id);
        }
        [TestMethod]
        public void Linq_FirstOrDefault_OnEmpty()
        {
            var content = Content.All.DisableAutofilters().Where(c => c.Id < 0).OrderByDescending(c => c.Id).FirstOrDefault();
            Assert.IsNull(content);
        }
        [TestMethod]
        public void Linq_First_WithPredicate()
        {
            var content = Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderByDescending(c => c.Id).First(c => c.Id < 4);
            Assert.AreEqual(3, content.Id);
        }
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void Linq_First_WithPredicate_EmptySource()
        {
            //var x = Enumerable.Range(1, 100).Where(i => i > 10).OrderByDescending(i => i).First(i => i < 4);
            var content = Content.All.DisableAutofilters().Where(c => c.Id > 10).OrderByDescending(c => c.Id).First(c => c.Id < 4);
            Assert.AreEqual(3, content.Id);
        }
        [TestMethod]
        public void Linq_FirstOrDefault_WithPredicate()
        {
            var content = Content.All.DisableAutofilters().Where(c => c.Id < 10).OrderByDescending(c => c.Id).FirstOrDefault(c => c.Id < 4);
            Assert.AreEqual(3, content.Id);
        }
        [TestMethod]
        public void Linq_FirstOrDefault_WithPredicate_EmptySource()
        {
            //var x = Enumerable.Range(1, 100).Where(i => i > 10).OrderByDescending(i => i).FirstOrDefault(i => i < 4);
            var content = Content.All.DisableAutofilters().Where(c => c.Id > 10).OrderByDescending(c => c.Id).FirstOrDefault(c => c.Id < 4);
            Assert.IsNull(content);
        }

        [TestMethod]
        public void Linq_CountOnly()
        {
            var qresult = ContentQuery.Query(string.Concat("InFolder:", TestRoot.Path, " .AUTOFILTERS:OFF .COUNTONLY"));
            var expected = qresult.Count;

            var actual = Content.All.DisableAutofilters().Where(c => c.InFolder(TestRoot)).Count();
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void Linq_CountIsDeferred()
        {
            string log = null;
            try
            {
                ContentSet<Content>.TracingEnabled = true;
                var count = Content.All.DisableAutofilters().Where(c => c.InFolder(TestRoot)).Count();
                log = ContentSet<Content>.TraceLog.ToString();
            }
            finally
            {
                ContentSet<Content>.TraceLog.Clear();
                ContentSet<Content>.TracingEnabled = false;
            }
            Assert.IsTrue(log.Contains(".COUNTONLY"));
        }
        [TestMethod]
        public void Linq_Count_WithPredicate()
        {
            Assert.AreEqual(6, Content.All.DisableAutofilters().Count(c => c.Id < 7));
            Assert.AreEqual(4, Content.All.DisableAutofilters().Where(c => c.Id > 2).Count(c => c.Id < 7));
        }

        [TestMethod]
        public void Linq_Any()
        {
            Assert.IsFalse(Content.All.DisableAutofilters().Any(c => c.Id == 0), "#1");
            Assert.IsTrue(Content.All.DisableAutofilters().Any(c => c.Id == 1), "#2");
            Assert.IsTrue(Content.All.DisableAutofilters().Any(c => c.Id > 0), "#3");
        }

        [TestMethod]
        public void Linq_InFolder()
        {
            test(Content.All.DisableAutofilters().Where(c => c.InFolder(TestRoot.Path + "/Cars")).OrderBy(c => c.Id).ToList(),
                (from x in Content.All.DisableAutofilters() where x.InFolder(TestRoot.Path + "/Cars") orderby x.Id select x).ToList(),
                "Car1 | Car2 | Car3 | Car4");
        }
        [TestMethod]
        public void Linq_InTree()
        {
            Assert.AreEqual("InTree:" + TestRoot.Path.ToLower(), GetQueryString(Content.All.Where(c => c.InTree(TestRoot))));
        }
        [TestMethod]
        public void Linq_TypeFilter_Strong()
        {
            //-- type that handles one content type
            test(Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot) && c.ContentHandler is Group).OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(TestRoot) && c.ContentHandler is Group orderby c.Name select c).ToList(),
                "Group1 | Group2 | Group3 | Group4");
            test(Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot) && typeof(Group).IsAssignableFrom(c.ContentHandler.GetType())).OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(TestRoot) && typeof(Group).IsAssignableFrom(c.ContentHandler.GetType()) orderby c.Name select c).ToList(),
                "Group1 | Group2 | Group3 | Group4");

            //-- type that handles more than one content type
            test(Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot.Path + "/Cars") && c.ContentHandler is GenericContent).OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(TestRoot.Path + "/Cars") && c.ContentHandler is GenericContent orderby c.Name select c).ToList(),
                "Car1 | Car2 | Car3 | Car4 | Cars");
            test(Content.All.DisableAutofilters().Where(c => c.InTree(TestRoot.Path + "/Cars") && typeof(GenericContent).IsAssignableFrom(c.ContentHandler.GetType())).OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(TestRoot.Path + "/Cars") && typeof(GenericContent).IsAssignableFrom(c.ContentHandler.GetType()) orderby c.Name select c).ToList(),
                "Car1 | Car2 | Car3 | Car4 | Cars");
        }
        [TestMethod]
        public void Linq_TypeFilter_String()
        {
            Assert.AreEqual("+Id:>0 +Type:group", GetQueryString(Content.All.Where(c => c.ContentType.Name == "Group" && c.Id > 0)));
            Assert.AreEqual("Type:group", GetQueryString(Content.All.Where(c => c.ContentType == ContentType.GetByName("Group"))));
            Assert.AreEqual("Type:car", GetQueryString(Content.All.Where(c => c.Type("Car"))));
            Assert.AreEqual("TypeIs:car", GetQueryString(Content.All.Where(c => c.TypeIs("Car"))));
        }

        [TestMethod]
        public void Linq_ConditionalOperator()
        {
            bool b;

            // First operand of the conditional operator is a constant
            b = true;
            Assert.AreEqual("DisplayName:car", GetQueryString(Content.All.Where(c => b ? c.DisplayName == "Car" : c.Index == 42)));
            b = false;
            Assert.AreEqual("Index:42", GetQueryString(Content.All.Where(c => b ? c.DisplayName == "Car" : c.Index == 42)));

            // First operand is not a constant
            Assert.AreEqual("(+Index:85 -Type:car) (+DisplayName:ferrari +Type:car)", GetQueryString(Content.All.Where(c => c.Type("Car") ? c.DisplayName == "Ferrari" : c.Index == 85)));
            Assert.AreEqual("(+Index:85 -Type:car) (+DisplayName:\"my nice ferrari\" +Type:car)", GetQueryString(Content.All.Where(c => c.Type("Car") ? c.DisplayName == "My nice Ferrari" : c.Index == 85)));
        }

        [TestMethod]
        public void Linq_FieldWithIndexer()
        {
            var cars = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString()).ContentHandler;
            cars.Save();
            var words = new[] { "Ferrari", "Porsche", "Porsche", "Ferrari" };
            for (int i = 1; i <= 4; i++)
            {
                var content = Content.CreateNew("Car", cars, "Car" + i);
                content["Index"] = i;
                content["Make"] = words[i - 1];
                content.Save();
            }

            test(Content.All.DisableAutofilters().Where(c => c.InTree(cars) && (string)c["Make"] == "Porsche").OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(cars) && (string)c["Make"] == "Porsche" orderby c.Name select c).ToList(),
                "Car2 | Car3");
        }
        [TestMethod]
        public void Linq_Boolean()
        {
            var cars = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString()).ContentHandler;
            cars.Save();
            var words = new[] { "Ferrari", "Porsche", "Porsche", "Ferrari" };
            for (int i = 1; i <= 4; i++)
            {
                var content = Content.CreateNew("Car", cars, "Car" + i);
                content["Index"] = i;
                content["Make"] = words[i - 1];
                content.Save();
            }

            test(Content.All.DisableAutofilters().Where(c => c.InTree(cars) && (((int)c["Index"] == 2 && (string)c["Make"] == "Porsche") || ((int)c["Index"] == 4 && (string)c["Make"] == "Ferrari"))).OrderBy(c => c.Name).ToList(),
                (from c in Content.All.DisableAutofilters() where c.InTree(cars) && (((int)c["Index"] == 2 && (string)c["Make"] == "Porsche") || ((int)c["Index"] == 4 && (string)c["Make"] == "Ferrari")) orderby c.Name select c).ToList(),
                "Car2 | Car4");
        }

        [TestMethod]
        public void Linq_AndOrPrecedence()
        {
            Assert.AreEqual("+(Index:3 (+Index:2 +TypeIs:group)) +InTree:/root/_linqtests", GetQueryString(Content.All.Where(c => c.InTree(TestRoot) && (c.ContentHandler is Group && c.Index == 2 || c.Index == 3))));
            Assert.AreEqual("+((+TypeIs:group +Index:3) Index:2) +InTree:/root/_linqtests", GetQueryString(Content.All.Where(c => c.InTree(TestRoot) && (c.Index == 2 || c.Index == 3 && c.ContentHandler is Group))));
        }

        [TestMethod]
        public void Linq_OrderBy()
        {
            var cars = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString()).ContentHandler;
            cars.Save();
            for (int i = 1; i < 10; i++)
            {
                var content = Content.CreateNew("Car", cars, "Car" + i);
                content["Index"] = (i - 1) % 3;
                content.Save();
            }

            var result = Content.All.DisableAutofilters().Where(c => c.InTree(cars) && c.TypeIs("Car")).OrderBy(c => c.Index).ThenByDescending(c => c.Name).ToArray();
            var names = String.Join(" | ", result.Select(c => c.Name));

            Assert.AreEqual("Car7 | Car4 | Car1 | Car8 | Car5 | Car2 | Car9 | Car6 | Car3", names);
        }

        [TestMethod]
        public void Linq_SelectSimple()
        {
            var names = String.Join(", ", Content.All.Where(c => c.Id < 10).OrderBy(c => c.Name).AsEnumerable().Select(c => c.Name));
            Assert.AreEqual("Admin, Administrators, BuiltIn, Creators, Everyone, IMS, Portal, Root, Visitor", names);
        }
        [TestMethod]
        public void Linq_Select_WithoutAsEnumerable()
        {
            try
            {
                var x = String.Join(", ", Content.All.Where(c => c.Id < 10).OrderBy(c => c.Name).Select(c => c.Name));
                Assert.Fail("An error must be thrown with exclamation: Use AsEnumerable ...");
            }
            catch (NotSupportedException e)
            {
                if (!e.Message.Contains("AsEnumerable"))
                    Assert.Fail("Exception message does not contain 'AsEnumerable'");
            }
        }
        [TestMethod]
        public void Linq_SelectNew()
        {
            var x = Content.All.Where(c => c.Id < 10).OrderBy(c => c.Id).AsEnumerable().Select(c => new { Id = c.Id, c.Name }).ToArray();
            var y = String.Join(", ", x.Select(a => String.Concat(a.Id, ", ", a.Name)));
            Assert.AreEqual("1, Admin, 2, Root, 3, IMS, 4, BuiltIn, 5, Portal, 6, Visitor, 7, Administrators, 8, Everyone, 9, Creators", y);
        }

        [TestMethod]
        public void Linq_OfType()
        {
            Assert.AreEqual("+TypeIs:site +EnableClientBasedCulture:yes",
                GetQueryString(Content.All.OfType<SenseNet.Portal.Site>().Where(c => c.EnableClientBasedCulture == true)));
        }

        [TestMethod]
        public void Linq_TakeSkip()
        {
            var q = GetQueryString(Content.All.Where(c => c.IsFolder == true).Skip(8).Take(5));
            Assert.AreEqual("IsFolder:yes .TOP:5 .SKIP:8", q);
        }

        [TestMethod]
        public void Linq_CombiningQueries()
        {
            var childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderOr, ContentQuery = "Id:>42", EnableAutofilters = FilterStatus.Disabled, Skip = 18, Top = 15 };
            var expr = Content.All.Where(c => c.IsFolder == true).Skip(8).Take(5).Expression;
            var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
            var expected = "(+IsFolder:yes +Id:>42) InFolder:/root/fakepath .TOP:15 .SKIP:18 .AUTOFILTERS:OFF";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Linq_API()
        {
            ContentSet<Content>[] contentSets = 
            {
                (ContentSet<Content>)Content.All.Where(c => c.Id < 6),                                           // -, -
                (ContentSet<Content>)Content.All.EnableAutofilters().Where(c => c.Id < 6),                       // -, -
                (ContentSet<Content>)Content.All.DisableAutofilters().Where(c => c.Id < 6),                      // 
                (ContentSet<Content>)Content.All.EnableLifespan().Where(c => c.Id < 6),                          // 
                (ContentSet<Content>)Content.All.DisableLifespan().Where(c => c.Id < 6),                         // 
                (ContentSet<Content>)Content.All.EnableAutofilters().EnableLifespan().Where(c => c.Id < 6),      // 
                (ContentSet<Content>)Content.All.EnableAutofilters().DisableLifespan().Where(c => c.Id < 6),     // 
                (ContentSet<Content>)Content.All.DisableAutofilters().EnableLifespan().Where(c => c.Id < 6),     // 
                (ContentSet<Content>)Content.All.DisableAutofilters().DisableLifespan().Where(c => c.Id < 6),    // 
                (ContentSet<Content>)Content.All.EnableLifespan().EnableAutofilters().Where(c => c.Id < 6),      // 
                (ContentSet<Content>)Content.All.DisableLifespan().EnableAutofilters().Where(c => c.Id < 6),     // 
                (ContentSet<Content>)Content.All.EnableLifespan().DisableAutofilters().Where(c => c.Id < 6),     // 
                (ContentSet<Content>)Content.All.DisableLifespan().DisableAutofilters().Where(c => c.Id < 6),    // 
            };
            var queries = new string[contentSets.Length];
            for (var i = 0; i < contentSets.Length; i++)
                queries[i] = SnExpression.BuildQuery(contentSets[i].Expression, typeof(Content), contentSets[i].ContextPath, contentSets[i].ChildrenDefinition).ToString();

            var expected = @"Id:<6
Id:<6
Id:<6 .AUTOFILTERS:OFF
Id:<6 .LIFESPAN:ON
Id:<6
Id:<6 .LIFESPAN:ON
Id:<6
Id:<6 .AUTOFILTERS:OFF .LIFESPAN:ON
Id:<6 .AUTOFILTERS:OFF
Id:<6 .LIFESPAN:ON
Id:<6
Id:<6 .AUTOFILTERS:OFF .LIFESPAN:ON
Id:<6 .AUTOFILTERS:OFF";
            var actual = String.Join("\r\n", queries);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Linq_OfTypeAndFirst()
        {
            var email = "admin@b.c";
            var user = new User(User.Administrator.Parent);
            user.Name = "testuser129";
            user.Email = email;
            user.Save();

            var result = Content.All.OfType<User>().FirstOrDefault(c => c.InTree(Repository.ImsFolderPath) && c.Email == email);
            Assert.IsTrue(result != null);
        }
        [TestMethod]
        public void Linq_OfTypeAndWhere()
        {
            string path = "/Root/IMS/BuiltIn/Portal";
            User user = User.Administrator;
            var ok = Content.All.OfType<Group>().Where(g => g.InTree(path)).AsEnumerable().Any(g => user.IsInGroup(g));
            Assert.IsTrue(ok);
        }

        //---------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void Linq_Error_UnknownField()
        {
            try
            {
                var x = Content.All.Where(c => (int)c["UnknownField"] == 42).ToArray();
                Assert.Fail("The expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException e)
            {
                var msg = e.Message;
                Assert.IsTrue(msg.ToLower().Contains("unknown field"), "Error message does not contain: 'unknown field'.");
                Assert.IsTrue(msg.Contains("UnknownField"), "Error message does not contain the field name: 'UnknownField'.");
            }
        }
        [TestMethod]
        public void Linq_Error_NotConstants()
        {
            try { var x = Content.All.Where(c => c.DisplayName.StartsWith(c.Name)).ToArray(); Assert.Fail("#1 Exception wasn't thrown"); }
            catch (NotSupportedException) { }
            try { var x = Content.All.Where(c => c.DisplayName.EndsWith(c.Name)).ToArray(); Assert.Fail("#2 Exception wasn't thrown"); }
            catch (NotSupportedException) { }
            try { var x = Content.All.Where(c => c.DisplayName.Contains(c.Name)).ToArray(); Assert.Fail("#3 Exception wasn't thrown"); }
            catch (NotSupportedException) { }

            try { var x = Content.All.Where(c => c.Type(c.DisplayName)).ToArray(); Assert.Fail("#4 Exception wasn't thrown"); }
            catch (NotSupportedException) { }
            try { var x = Content.All.Where(c => c.TypeIs(c.DisplayName)).ToArray(); Assert.Fail("#5 Exception wasn't thrown"); }
            catch (NotSupportedException) { }
            try { var x = Content.All.Where(c => c.InFolder(c.WorkspacePath)).ToArray(); Assert.Fail("#6 Exception wasn't thrown"); }
            catch (NotSupportedException) { }
            try { var x = Content.All.Where(c => c.InTree(c.WorkspacePath)).ToArray(); Assert.Fail("#7 Exception wasn't thrown"); }
            catch (NotSupportedException) { }
        }

        //---------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void Linq_OptimizeBooleans()
        {
            var folder = Repository.Root;

            var childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd };
            //var expr = Content.All.Where(c => c.Path != "/Root/A" && c.Path != "/Root/B" && c.Path != "/Root/C" && c.Type("Folder") && c.InFolder(folder)).Expression;
            var expr = Content.All.Where(c => c.Name != "A" && c.Name != "B" && c.Name != "C" && c.TypeIs("Folder")).Expression;
            var actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
            var expected = "+(+TypeIs:folder -Name:c -Name:b -Name:a) +InFolder:/root/fakepath";
            Assert.AreEqual(expected, actual);
        }

        //---------------------------------------------------------------------------------------------------------

        [TestMethod]
        public void Linq_AspectField()
        {
            var aspect1 = AspectTests.EnsureAspect("Linq_AspectField_Aspect1");
            aspect1.AspectDefinition = @"<AspectDefinition xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/AspectDefinition'>
                                            <Fields><AspectField name='Field1' type='ShortText' /></Fields></AspectDefinition>";
            aspect1.Save();

            Assert.AreEqual("Linq_AspectField_Aspect1.Field1:fieldvalue", GetQueryString(
                Content.All.OfType<Content>().Where(c => (string)c["Linq_AspectField_Aspect1.Field1"] == "fieldvalue")));
        }

        //========================================================================================================= bugz

        [TestMethod]
        public void Linq_OptimizeBooleans_1()
        {
            // +(TypeIs:group TypeIs:user) +InFolder:/root/ims/builtin/demo/managers
            ChildrenDefinition childrenDef;
            System.Linq.Expressions.Expression expr;
            string actual;
            string expected;

            childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd };
            expr = Content.All.Where(c => c.ContentHandler is Group || c.ContentHandler is User).Expression;
            actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
            expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
            Assert.AreEqual(expected, actual);

            childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "Id:>0" };
            expr = Content.All.Where(c => c.ContentHandler is Group || c.ContentHandler is User).Expression;
            actual = SnExpression.BuildQuery(expr, typeof(Content), "/Root/FakePath", childrenDef).ToString();
            expected = "+(TypeIs:user TypeIs:group) +Id:>0 +InFolder:/root/fakepath";
            Assert.AreEqual(expected, actual);

            childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "TypeIs:user TypeIs:group" };
            actual = SnExpression.BuildQuery(null, typeof(Content), "/Root/FakePath", childrenDef).ToString();
            expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
            Assert.AreEqual(expected, actual);

            childrenDef = new ChildrenDefinition { PathUsage = PathUsageMode.InFolderAnd, ContentQuery = "+(TypeIs:user TypeIs:group)" };
            actual = SnExpression.BuildQuery(null, typeof(Content), "/Root/FakePath", childrenDef).ToString();
            expected = "+(TypeIs:user TypeIs:group) +InFolder:/root/fakepath";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Linq_Bug_StackOverflow()
        {
            // There was a bug in a customer code that caused StackOverflowException but in Sense/Net this case has never been reproduced.

            var aspectName = "Aspect_Linq_Bug_StackOverflow";
            var aspect = Aspect.LoadAspectByName(aspectName);
            if (aspect == null)
            {
                aspect = new Aspect(Repository.AspectsFolder) { Name = aspectName };
                aspect.Save();
            }


            aspect = Content.All.DisableAutofilters().OfType<Aspect>().Where(x => x.Name == aspectName).FirstOrDefault();
            Assert.IsNotNull(aspect);

            aspect.ForceDelete();

            aspect = Content.All.DisableAutofilters().OfType<Aspect>().Where(x => x.Name == aspectName).FirstOrDefault();
            Assert.IsNull(aspect);
        }

        //[TestMethod]
        //public void Linq_ContentSet_CountVsTotalCount()
        //{
        //    var qtextBase = "+Name:(a* b*) +TypeIs:ContentType .AUTOFILTERS:OFF";
        //    var realLength = ContentQuery.Query(qtextBase).Identifiers.Count();
        //    if (realLength < 11)
        //        Assert.Inconclusive("Lenght of base set must be greater than 10.");

        //    //---------------------------------------------

        //    var top = 5;
        //    var qtext = qtextBase + " .TOP:" + top;
        //    var cset = new ContentSet<Content>(new ChildrenDefinition
        //    {
        //        AllChildren = false,
        //        ContentQuery = qtext,
        //        Top = 10
        //    }, null);

        //    var setLength = cset.Count();
        //    var resultLength = cset.ToArray().Length;

        //    Assert.AreEqual(top, setLength);
        //    Assert.AreEqual(top, resultLength);

        //    //---------------------------------------------

        //    top = realLength + 5;
        //    qtext = "+Name:(a* b*) +TypeIs:ContentType .TOP:" + top + " .AUTOFILTERS:OFF";
        //    cset = new ContentSet<Content>(new ChildrenDefinition
        //    {
        //        AllChildren = false,
        //        ContentQuery = qtext,
        //        Top = 10
        //    }, null);

        //    setLength = cset.Count();
        //    resultLength = cset.ToArray().Length;

        //    Assert.AreEqual(realLength, setLength);
        //    Assert.AreEqual(realLength, resultLength);

        //}

        //=========================================================================================================

        private string GetQueryString<T>(IQueryable<T> queryable)
        {
            var cs = queryable.Provider as ContentSet<T>;
            return cs.GetCompiledQuery().ToString();
        }

        private void test(List<Content> product1, List<Content> product2, string expected)
        {
            var trace = new StackTrace(1, true);
            var frame = trace.GetFrame(0);
            var methodName = frame.GetMethod().Name;
            var lineNumber = frame.GetFileLineNumber();

            var stringResult1 = String.Join(" | ", product1.Select(c => c.Name));
            Assert.IsTrue(expected == stringResult1, String.Format("{0} (line: {1}): Result#1 is {2}. Expected: {3}", methodName, lineNumber, stringResult1, expected));
            var stringResult2 = String.Join(" | ", product2.Select(c => c.Name));
            Assert.IsTrue(expected == stringResult2, String.Format("{0} (line: {1}): Result#2 is {2}. Expected: {3}", methodName, lineNumber, stringResult2, expected));
        }
        private void test(List<Content> product1, string expected)
        {
            var trace = new StackTrace(1, true);
            var frame = trace.GetFrame(0);
            var methodName = frame.GetMethod().Name;
            var lineNumber = frame.GetFileLineNumber();

            var stringResult1 = String.Join(" | ", product1.Select(c => c.Name));
            Assert.IsTrue(expected == stringResult1, String.Format("{0} (line: {1}): Result#1 is {2}. Expected: {3}", methodName, lineNumber, stringResult1, expected));
        }

    }
}
