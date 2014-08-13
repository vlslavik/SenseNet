using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using Lucene.Net.Search;
using Lucene.Net.Index;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;

namespace SenseNet.ContentRepository.Tests.Security
{
    [TestClass]
    public class QueryAccessLevelTest : TestBase
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
        private static string _testRootName = "_QueryAccessLevelTest";
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

        static List<string> _pathsToDelete = new List<string>();
        static void AddPathToDelete(string path)
        {
            lock (_pathsToDelete)
            {
                if (_pathsToDelete.Contains(path))
                    return;
                _pathsToDelete.Add(path);
            }
        }

        [ClassCleanup]
        public static void DestroyPlayground()
        {
            foreach (string path in _pathsToDelete)
            {
                try
                {
                    Node n = Node.LoadNode(path);
                    if (n != null)
                        Node.ForceDelete(path);
                }
                catch // catch only
                {
                }
            }

            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);
        }

        #endregion

        Node __userFolder;
        Node UserFolder
        {
            get
            {
                if (__userFolder == null)
                {
                    //__userFolder = Folder.Load(Path.GetParentPath(User.Administrator.Path));
                    var userFolder = Node.LoadNode(RepositoryPath.GetParentPath(User.Administrator.Path));
                    if (userFolder == null)
                        throw new ApplicationException("UserFolder cannot be found.");
                    __userFolder = userFolder as Node;
                }
                return __userFolder;
            }
        }
        User __testUser;
        User TestUser
        {
            get
            {
                if (__testUser == null)
                    __testUser = LoadOrCreateUser("testUser", "Tes Tuser", UserFolder);
                return __testUser;
            }
        }
        User LoadOrCreateUser(string name, string fullName, Node parentFolder)
        {
            var path = RepositoryPath.Combine(parentFolder.Path, name);
            AddPathToDelete(path);

            var user = Node.LoadNode(path) as User ?? new User(parentFolder) { Name = name };
            user.Email = name + "@email.com";
            user.Enabled = true;
            user.FullName = fullName;
            user.Save();

            return user;
        }


        [TestMethod]
        public void Security_ContentQuery_FieldLevel_Parsing()
        {
            Assert.AreEqual(QueryFieldLevel.HeadOnly, LucQuery.Parse("+Name:a* +InTree:/Root/MyFolder -InFolder:/Root/MyFolder/x +Index:0").FieldLevel);
            Assert.AreEqual(QueryFieldLevel.NoBinaryOrFullText, LucQuery.Parse("+Name:a* +InTree:/Root/MyFolder +Description:value1").FieldLevel);
            Assert.AreEqual(QueryFieldLevel.BinaryOrFullText, LucQuery.Parse("+InTree:/Root/MyFolder searchword").FieldLevel);
            Assert.AreEqual(QueryFieldLevel.BinaryOrFullText, LucQuery.Parse("searchword").FieldLevel);
        }
        [TestMethod]
        public void Security_ContentQuery_FieldLevel_Executing()
        {
            var rootFolderName = "Security_ContentQuery_AccessLevel_ExecutingAndLinq";
            var rootFolder = EnsureTestStructure(rootFolderName);
            var savedUser = User.Current;
            User.Current = TestUser;
            try
            {
                var settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled, Sort = new[] { new SortInfo { FieldName = "Index", Reverse = false } } };

                var r1 = ContentQuery.Query("Name:(See Preview PreviewWithoutWatermark PreviewWithoutRedaction Open OpenMinor)", settings).Nodes.Select(x => x.Name).ToArray();
                var r2 = ContentQuery.Query("InFolder:@0", settings, rootFolder.Path).Nodes.Select(x => x.Name).ToArray();
                var r3 = ContentQuery.Query("+InFolder:@0 +Index:>0", settings, rootFolder.Path).Nodes.Select(x => x.Name).ToArray();
                var r4 = ContentQuery.Query("+InFolder:@0 +Description:value", settings, rootFolder.Path).Nodes.Select(x => x.Name).ToArray();
                var r5 = ContentQuery.Query("+InFolder:@0 value .SORT:Index", settings, rootFolder.Path).Nodes.Select(x => x.Name).ToArray();

                Assert.AreEqual("See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r1));
                Assert.AreEqual("See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r2));
                Assert.AreEqual("See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r3));
                Assert.AreEqual("Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r4));
                Assert.AreEqual("PreviewWithoutRedaction, OpenMinor", String.Join(", ", r5));
            }
            finally
            {
                User.Current = savedUser;
            }
        }
        [TestMethod]
        public void Security_ContentQuery_FieldLevel_Linq()
        {
            var rootFolderName = "Security_ContentQuery_AccessLevel_ExecutingAndLinq";
            var rootFolder = EnsureTestStructure(rootFolderName);
            var savedUser = User.Current;
            User.Current = TestUser;
            try
            {
                var settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled, Sort = new[] { new SortInfo { FieldName = "Index", Reverse = false } } };

                var r1 = Content.All.DisableAutofilters().Where(c => c.Name == "See" || c.Name == "Preview" || c.Name == "PreviewWithoutWatermark" || c.Name == "PreviewWithoutRedaction" || c.Name == "Open" || c.Name == "OpenMinor").OrderBy(c => c.Index).AsEnumerable().Select(x => x.Name).ToArray();
                var r2 = Content.All.DisableAutofilters().Where(c => c.InFolder(rootFolder.Path)).OrderBy(c => c.Index).AsEnumerable().Select(x => x.Name).ToArray();
                var r3 = Content.All.DisableAutofilters().Where(c => c.InFolder(rootFolder.Path) && c.Index > 0).OrderBy(c => c.Index).AsEnumerable().Select(x => x.Name).ToArray();
                var r4 = Content.All.DisableAutofilters().Where(c => c.InFolder(rootFolder.Path) && c.Description == "value").OrderBy(c => c.Index).AsEnumerable().Select(x => x.Name).ToArray();
                //var r5 = ContentQuery.Query("+InFolder:@0 value .SORT:Index", settings, rootFolder.Path).Nodes.Select(x => x.Name).ToArray();

                Assert.AreEqual("See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r1));
                Assert.AreEqual("See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r2));
                Assert.AreEqual("See, Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r3));
                Assert.AreEqual("Preview, PreviewWithoutWatermark, PreviewWithoutRedaction, OpenMinor", String.Join(", ", r4));
                //Assert.AreEqual("PreviewWithoutRedaction, OpenMinor", String.Join(", ", r5));
            }
            finally
            {
                User.Current = savedUser;
            }
        }

        private SystemFolder EnsureTestStructure(string rootFolderName)
        {
            var rootFolder = Node.Load<SystemFolder>(RepositoryPath.Combine(_testRootPath, rootFolderName));
            if (rootFolder == null)
            {
                rootFolder = new SystemFolder(TestRoot) { Name = rootFolderName };
                rootFolder.Save();
                rootFolder.VersioningMode = VersioningType.MajorAndMinor;
                rootFolder.Save();

                rootFolder.Security.BreakInheritance();
                rootFolder.Security.GetAclEditor()
                    .SetPermission(User.Visitor.Id, true, PermissionType.See, PermissionValue.NonDefined)
                    .SetPermission(Group.Everyone.Id, true, PermissionType.See, PermissionValue.NonDefined)
                    .Apply();

                Folder folder;

                folder = new Folder(rootFolder) { Name = "See", Description = "Description value", Index = 1 };
                folder.Save();
                folder.Security.GetAclEditor().SetPermission(TestUser, true, PermissionType.See, PermissionValue.Allow).Apply();

                folder = new Folder(rootFolder) { Name = "Preview", Description = "Description value", Index = 2 };
                folder.Save();
                folder.Security.GetAclEditor().SetPermission(TestUser, true, PermissionType.Preview, PermissionValue.Allow).Apply();

                folder = new Folder(rootFolder) { Name = "PreviewWithoutWatermark", Description = "Description value", Index = 3 };
                folder.Save();
                folder.Security.GetAclEditor().SetPermission(TestUser, true, PermissionType.PreviewWithoutWatermark, PermissionValue.Allow).Apply();

                folder = new Folder(rootFolder) { Name = "PreviewWithoutRedaction", Description = "Description value", Index = 4 };
                folder.Save();
                folder.Security.GetAclEditor().SetPermission(TestUser, true, PermissionType.PreviewWithoutRedaction, PermissionValue.Allow).Apply();

                folder = new Folder(rootFolder) { Name = "Open", Description = "Description value", Index = 5, VersioningMode = VersioningType.MajorAndMinor };
                folder.Save();
                folder.Security.GetAclEditor().SetPermission(TestUser, true, PermissionType.Open, PermissionValue.Allow).Apply();

                folder = new Folder(rootFolder) { Name = "OpenMinor", Description = "Description value", Index = 6, VersioningMode = VersioningType.MajorAndMinor };
                folder.Save();
                folder.Security.GetAclEditor().SetPermission(TestUser, true, PermissionType.OpenMinor, PermissionValue.Allow).Apply();
            }
            return rootFolder;
        }

    }
}
