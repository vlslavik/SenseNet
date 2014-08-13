using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using System.Diagnostics;
using System.Threading;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass]
    public class NodeDateTest : TestBase
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
        private static string _testRootName = "_NodeDateTest";
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

        [TestMethod]
        public void NodeDates_CreationDates()
        {
            Folder folder = null;
            try
            {
                var t00 = GetTimes(null);

                Thread.Sleep(100);

                folder = new Folder(TestRoot);
                var t01 = GetTimes(folder);

                Thread.Sleep(100);

                folder.Save();
                var t02 = GetTimes(folder);
                var folderId = folder.Id;

                Thread.Sleep(100);

                folder = Node.Load<Folder>(folderId);
                var t03 = GetTimes(folder);

                Thread.Sleep(100);

                folder.CheckOut();
                var t04 = GetTimes(folder);

                Thread.Sleep(100);

                folder.Index++;
                folder.Save();
                var t05 = GetTimes(folder);

                Thread.Sleep(100);

                folder.CheckIn();
                var t06 = GetTimes(folder);

                Thread.Sleep(100);


                folder = Node.Load<Folder>(folderId);
                folder.CheckOut();
                folder.Index++;
                folder.Save();
                var t07 = GetTimes(folder);

                Thread.Sleep(100);

                folder.UndoCheckOut();
                var t08 = GetTimes(folder);

                // CreationDate is earlier than after-construction time but older than test-start time.
                Assert.IsTrue(t00.CurrentTime.CompareTo(t01.CreationDate) < 0);
                Assert.IsTrue(t01.CurrentTime.CompareTo(t01.CreationDate) >= 0);

                // CreationDate is constant.
                Assert.AreEqual(t01.CreationDate, t02.CreationDate);
                Assert.AreEqual(t01.CreationDate, t03.CreationDate);
                Assert.AreEqual(t01.CreationDate, t04.CreationDate);
                Assert.AreEqual(t01.CreationDate, t05.CreationDate);
                Assert.AreEqual(t01.CreationDate, t06.CreationDate);
                Assert.AreEqual(t01.CreationDate, t07.CreationDate);
                Assert.AreEqual(t01.CreationDate, t08.CreationDate); // sql rounding problem

                // CreationDate and CreationDate are equal if there is only one version but different in case of more versions exist.
                Assert.AreEqual(t01.CreationDate, t01.VersionCreationDate);
                Assert.AreEqual(t01.VersionCreationDate, t02.VersionCreationDate);
                Assert.AreEqual(t02.VersionCreationDate, t03.VersionCreationDate);
                Assert.AreNotEqual(t03.VersionCreationDate, t04.VersionCreationDate);
                Assert.AreEqual(t04.VersionCreationDate, t05.VersionCreationDate);
                Assert.AreNotEqual(t05.VersionCreationDate, t06.VersionCreationDate);
                Assert.AreNotEqual(t06.VersionCreationDate, t07.VersionCreationDate);
                Assert.AreNotEqual(t07.VersionCreationDate, t08.VersionCreationDate);

                Assert.IsTrue(t01.CreationDate == t01.VersionCreationDate);
                Assert.IsTrue(t01.VersionCreationDate == t02.VersionCreationDate);
                Assert.IsTrue(t02.VersionCreationDate == t03.VersionCreationDate);
                Assert.IsTrue(t03.VersionCreationDate.CompareTo( t04.VersionCreationDate) < 0);
                Assert.IsTrue(t04.VersionCreationDate == t05.VersionCreationDate);
                Assert.IsTrue(t05.VersionCreationDate.CompareTo( t06.VersionCreationDate) < 0);
                Assert.IsTrue(t06.VersionCreationDate.CompareTo( t07.VersionCreationDate) < 0);
                Assert.IsTrue(t06.VersionCreationDate.CompareTo(t08.VersionCreationDate) <= 0 && t08.VersionCreationDate.CompareTo(t07.VersionCreationDate) <= 0);
            }
            finally
            {
                folder.ForceDelete();
            }
        }

        private struct Times
        {
            public string CurrentTime;
            public string CreationDate;
            public string VersionCreationDate;
            public string ModificationDate;
            public string VersionModificationDate;
        }
        private Times GetTimes(Node node)
        {
            var times = new Times { CurrentTime = DateToString(DateTime.UtcNow) };
            if (node == null)
                return times;

            times.CreationDate = DateToString(node.CreationDate);
            times.VersionCreationDate = DateToString(node.VersionCreationDate);
            times.ModificationDate = DateToString(node.ModificationDate);
            times.VersionModificationDate = DateToString(node.VersionModificationDate);
            return times;
        }
        private string DateToString(DateTime date)
        {
            return date.ToString("yyyyMMdd-HH:mm:ss.fff");
        }
    }
}
