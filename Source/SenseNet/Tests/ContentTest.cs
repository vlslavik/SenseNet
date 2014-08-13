using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass()]
    public class ContentTest : TestBase
    {
        public override TestContext TestContext { get; set; }

        private static string _testRootName = "_ContentTests";
        private static string _testRootPath = String.Concat("/Root/", _testRootName);

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
                        var node = NodeType.CreateInstance("SystemFolder", Node.LoadNode("/Root"));
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

        //-------------------------------------------------------------------- Children ---------------------

        [TestMethod]
        public void Content_Children_CustomList()
        {
            var c1 = Content.CreateNew("Folder", TestRoot, null);
            var c2 = Content.CreateNew("Folder", TestRoot, null);
            var parentFolder = Content.CreateNew("Folder", TestRoot, null);

            parentFolder.ChildrenDefinition.BaseCollection = new [] {c1.ContentHandler, c2.ContentHandler};

            Assert.AreEqual(2, parentFolder.Children.Count(), "#1 child count is incorrect");
            Assert.AreEqual(c1.Name, parentFolder.Children.ToList().FirstOrDefault().Name, "#2 name is incorrect in case of first item");
        }
    }
}
