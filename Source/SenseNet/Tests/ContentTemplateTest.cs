using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Portal;
using System.Threading;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class ContentTemplateTest : TestBase
    {
        #region Test infrastructure
        
        private TestContext _testContextInstance;
        public override TestContext TestContext
        {
            get
            {
                return _testContextInstance;
            }
            set
            {
                _testContextInstance = value;
            }
        }

        #endregion

        #region Test environment
        private static string _testRootName = "CTemplateTests";
        private static string _testRootPath = String.Concat("/Root/", _testRootName);

        private static string _testFileName = "testfiletemplate1.txt";
        private static string _testListTemplateName = "list1";

        private static string _testWsTemplateName = "WsTemplate1";
        private static string _testWsTemplatePath = RepositoryPath.Combine(_testRootPath, _testWsTemplateName);

        private static File _fileTemplate1;
        private static File _fileTemplate2;
        private static File _fileGlobalTemplate1;

        private static ContentList _listTemplate1;
        private static ContentList _listGlobalTemplate1;

        private static ContentList _list1;
        private static ContentList _list2;

        private static Node _testRoot;
        public static Node TestRoot
        {
            get
            {
                if (_testRoot == null)
                {
                    _testRoot = Node.LoadNode(_testRootPath);
                    if (_testRoot == null)
                    {
                        var node = NodeType.CreateInstance("Folder", Node.LoadNode("/Root"));
                        node.Name = _testRootName;
                        node.Save();
                        _testRoot = Node.LoadNode(_testRootPath);
                    }
                }
                return _testRoot;
            }
        }

        protected static Site TestSite
        {
            get { return Node.Load<Site>(RepositoryPath.Combine(_testRootPath, "CtSite")); }
        }

        private static Workspace _ws1;
        protected static Workspace TestWorkspace
        {
            get { return _ws1 ?? (_ws1 = Node.Load<Workspace>(RepositoryPath.Combine(TestSite.Path, "CtWorkspace1"))); }
        }

        private static Workspace _ws2;
        protected static Workspace TestWorkspace2
        {
            get { return _ws2 ?? (_ws2 = Node.Load<Workspace>(RepositoryPath.Combine(TestSite.Path, "CtWorkspace2"))); }
        }

        private static Workspace _ws3;
        protected static Workspace TestWorkspace3
        {
            get { return _ws3 ?? (_ws3 = Node.Load<Workspace>(RepositoryPath.Combine(TestRoot.Path, "CtWorkspace3"))); }
        }

        private static ContentList _tl1;
        protected static ContentList TestList
        {
            get { return _tl1 ?? (_tl1 = Node.Load<ContentList>(RepositoryPath.Combine(TestWorkspace.Path, "CtList1"))); }
        }

        protected static Workspace TestWorkspaceTemplate
        {
            get { return Node.Load<Workspace>(_testWsTemplatePath); }
        }

        //[TestInitialize]
        //public void PrepareTest()
        //{
        //    CreatePlayGround();
        //}

        [ClassCleanup]
        public static void DestroyPlayground()
        {
            //delete the site indidually to remove it from the PortalContext sites collection
            if (TestSite != null)
                TestSite.ForceDelete();

            if (Node.Exists(_testRootPath))
                Node.ForceDelete(_testRootPath);

            _fileTemplate1 = null;
            _fileTemplate2 = null;
            _fileGlobalTemplate1 = null;

            _listTemplate1 = null;
            _listGlobalTemplate1 = null;

            _list1 = null;
            _list2 = null;
            _ws1 = null;
            _ws2 = null;
            _ws3 = null;
            _tl1 = null;

            _testRoot = null;
        }

        private static void CreatePlayGround()
        {
            DestroyPlayground();

            // /Root
            //      |
            //      +-ContentTemplates
            //      |   |
            //      |   +-File
            //      |   |   |
            //      |   |   +-file1.txt
            //      |   |
            //      |   +-ContentList
            //      |       |
            //      |       +-list1
            //      |
            //      +-CTemplateTests
            //          |
            //          +-CtSite
            //          |   |
            //          |   +-ContentTemplates
            //          |   |   |
            //          |   |   +-File
            //          |   |       |
            //          |   |       +-file1.txt
            //          |   |
            //          |   +-CtWorkspace1
            //          |   |   |
            //          |   |   +-ContentTemplates
            //          |   |   |   |
            //          |   |   |   +-ContentList
            //          |   |   |       |
            //          |   |   |       +-list1
            //          |   |   +-CtList1
            //          |   |   |   |
            //          |   |   |   +-ContentTemplates
            //          |   |   |       |
            //          |   |   |       +-File
            //          |   |   |           |
            //          |   |   |           +-file1.txt
            //          |   |   +-CtList2
            //          |   | 
            //          |   +-CtWorkspace2
            //          | 
            //          +-CtWorkspace3

            //global template folder
            var ctfGlobal = Node.LoadNode(Repository.ContentTemplateFolderPath);
            if (ctfGlobal == null)
            {
                ctfGlobal = new SystemFolder(Node.LoadNode("/Root")) {Name = Repository.ContentTemplatesFolderName};
                ctfGlobal.Save();
            }

            //create GLOBAL content template type folders
            var folderGlobalCtFile1 = Node.Load<Folder>(RepositoryPath.Combine(ctfGlobal.Path, "File"));
            if (folderGlobalCtFile1 == null)
            {
                folderGlobalCtFile1 = new Folder(ctfGlobal) { Name = "File" };
                folderGlobalCtFile1.Save();
            }
            var folderGlobalCtList1 = Node.Load<Folder>(RepositoryPath.Combine(ctfGlobal.Path, "ContentList"));
            if (folderGlobalCtList1 == null)
            {
                folderGlobalCtList1 = new Folder(ctfGlobal) { Name = "ContentList" };
                folderGlobalCtList1.Save();
            }

            //create GLOBAL content templates
            _fileGlobalTemplate1 = Node.Load<File>(RepositoryPath.Combine(folderGlobalCtFile1.Path, _testFileName));
            if (_fileGlobalTemplate1 == null)
            {
                _fileGlobalTemplate1 = new File(folderGlobalCtFile1) { Name = _testFileName, Index = 30 };
                _fileGlobalTemplate1.Save();
            }
            _listGlobalTemplate1 = Node.Load<ContentList>(RepositoryPath.Combine(folderGlobalCtList1.Path, _testListTemplateName));
            if (_listGlobalTemplate1 == null)
            {
                _listGlobalTemplate1 = new ContentList(folderGlobalCtList1) { Name = _testListTemplateName, Index = 30 };
                _listGlobalTemplate1.Save();
            }

            //create site, workspace and list
            var site = new Site(TestRoot) {Name = "CtSite"};
            site.UrlList.Add("mytemplatetestinterneturl", "Forms");
            site.Save();

            var ws = new Workspace(site) { Name = "CtWorkspace2", AllowedChildTypes = new List<ContentType> { ContentType.GetByName("ContentList"), ContentType.GetByName("Workspace"), ContentType.GetByName("File") } };
            ws.Save();

            ws = new Workspace(TestRoot) { Name = "CtWorkspace3", AllowedChildTypes = new List<ContentType> { ContentType.GetByName("ContentList"), ContentType.GetByName("Workspace"), ContentType.GetByName("File") } };
            ws.Save();

            ws = new Workspace(site) { Name = "CtWorkspace1", AllowedChildTypes = new List<ContentType> { ContentType.GetByName("ContentList"), ContentType.GetByName("Workspace") } };
            ws.Save();

            _list1 = new ContentList(TestWorkspace) { Name = "CtList1", AllowedChildTypes = new List<ContentType> { ContentType.GetByName("File") } };
            _list1.Save();
            _list2 = new ContentList(TestWorkspace) { Name = "CtList2", AllowedChildTypes = new List<ContentType> { ContentType.GetByName("File") } };
            _list2.Save();

            //create content template folders
            var ctfSite = new SystemFolder(site) {Name = Repository.ContentTemplatesFolderName};
            ctfSite.Save();
            var ctfWs = new SystemFolder(TestWorkspace) { Name = Repository.ContentTemplatesFolderName };
            ctfWs.Save();
            var ctfList = new SystemFolder(_list1) { Name = Repository.ContentTemplatesFolderName };
            ctfList.Save();

            //create content template type folders
            var folderCtFile1 = new Folder(ctfSite) {Name = "File"};
            folderCtFile1.Save();

            var folderCtL1 = new Folder(ctfWs) { Name = "ContentList" };
            folderCtL1.Save();

            var folderCtFile2 = new Folder(ctfList) { Name = "File" };
            folderCtFile2.Save();

            //create content templates
            _fileTemplate1 = new File(folderCtFile1) {Name = _testFileName, Index = 10 };
            _fileTemplate1.Save();

            _fileTemplate2 = new File(folderCtFile2) { Name = _testFileName, Index = 20 };
            _fileTemplate2.Save();

            _listTemplate1 = new ContentList(folderCtL1) { Name = _testListTemplateName, Index = 10 };
            _listTemplate1.Save();
        }

        private static void CreateTemplateWithLocalReferences()
        {
            if (Node.Exists(_testWsTemplatePath))
                Node.ForceDelete(_testWsTemplatePath);

            var wst = new Workspace(TestRoot) { Name = _testWsTemplateName };
            wst.Save();

            var groups = new SystemFolder(wst) { Name = "Groups" };
            groups.Save();

            var g1 = new Group(groups) { Name = "g1" };
            g1.Save();

            var g2 = new Group(groups) { Name = "g2", Members = new List<Node> { g1 } };
            g2.Save();
        }

        #endregion

        [TestMethod()]
        [TestCategory("ContentTemplateBug")]
        public void ContentTemplate_GetNewItemNodes()
        {
            CreatePlayGround();

            Assert.IsTrue(GenericScenario.GetNewItemNodes(TestList, new ContentType[0]).Count() == 0, "#0 new item list is not empty");

            var itemsForList1 = GenericScenario.GetNewItemNodes(TestList).ToList();
            Assert.IsTrue(itemsForList1.Count(ct => ct.Path.Equals(_fileTemplate2.Path)) == 1, "#1 " + _fileTemplate2.Path + " not found");
            Assert.IsTrue(itemsForList1.Count(ct => ct.Path.Equals(_fileGlobalTemplate1.Path)) == 0, "#2 " + _fileGlobalTemplate1.Path + " found");

            var itemsForList2 = GenericScenario.GetNewItemNodes(_list2).ToList();
            Assert.IsTrue(itemsForList2.Count(ct => ct.Path.Equals(_fileTemplate2.Path)) == 0, "#3 " + _fileTemplate2.Path + " found");
            Assert.IsTrue(itemsForList2.Count(ct => ct.Path.Equals(_fileTemplate1.Path)) == 1, "#4 " + _fileTemplate1.Path + " not found");

            var itemsForWorkspace = GenericScenario.GetNewItemNodes(TestWorkspace).ToList();

            Assert.IsTrue(itemsForWorkspace.Count(ct => ct.Path.Equals(_listTemplate1.Path)) == 1, "#5 " + _listTemplate1.Path + " not found");
            Assert.IsTrue(itemsForWorkspace.Count(ct => ct.Path.Equals(_listGlobalTemplate1.Path)) == 0, "#6 " + _listGlobalTemplate1.Path + " found");

            var itemsForWorkspace2 = GenericScenario.GetNewItemNodes(TestWorkspace2).ToList();
            Assert.IsTrue(itemsForWorkspace2.Count(ct => ct.Path.Equals(_fileTemplate1.Path)) == 1, "#7 " + _fileTemplate1.Path + " not found");
            Assert.IsTrue(itemsForWorkspace2.Count(ct => ct.Path.Equals(_listGlobalTemplate1.Path)) == 1, "#8 " + _listGlobalTemplate1.Path + " not found");

            var itemsForWorkspace3 = GenericScenario.GetNewItemNodes(TestWorkspace3).ToList();
            Assert.IsTrue(itemsForWorkspace3.Count(ct => ct.Path.Equals(_fileGlobalTemplate1.Path)) == 1, "#9 " + _fileGlobalTemplate1.Path + " not found");
            Assert.IsTrue(itemsForWorkspace3.Count(ct => ct.Path.Equals(_listGlobalTemplate1.Path)) == 1, "#10 " + _listGlobalTemplate1.Path + " not found");
        }

        [TestMethod()]
        [TestCategory("ContentTemplateBug")]
        public void ContentTemplate_UpdateReferences()
        {
            CreateTemplateWithLocalReferences();

            var newContent = ContentTemplate.CreateTemplated(TestRoot, TestWorkspaceTemplate, "WsWithLocalReferences");
            newContent.Save();

            var g2 = Node.Load<Group>(RepositoryPath.Combine(newContent.Path, "Groups/g2"));
            var g1 = g2.Members.First() as Group;

            Assert.IsTrue(g1 != null && g1.InTree(newContent.ContentHandler), "Referenced group is not in the newly created subtree.");
            Assert.IsTrue(g1.Name == "g1", "Name of referenced group is not correct.");
            Assert.IsTrue(g2.Members.Count() == 1, "Members property contains too many nodes.");
        }

        [TestMethod]
        [TestCategory("ContentTemplateBug")]
        public void ContentTemplate_CreationDate()
        {
            if (_fileGlobalTemplate1 == null)
                CreatePlayGround();

            Thread.Sleep(1000);
            var t0 = DateTime.UtcNow;
            Thread.Sleep(10);

            var y = ContentTemplate.CreateFromTemplate(TestRoot, _fileGlobalTemplate1, null);
            y.Save();

            y = Node.LoadNode(y.Id);
            Assert.IsTrue(t0 < y.CreationDate);
            Assert.IsTrue(t0 < y.ModificationDate);
            Assert.IsTrue(t0 < y.VersionCreationDate);
            Assert.IsTrue(t0 < y.VersionModificationDate);
        }
    }
}
