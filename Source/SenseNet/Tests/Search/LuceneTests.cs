using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Versioning;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search.Parser;
using SenseNet.Search;
using System.Diagnostics;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Tests.Search
{
    [TestClass]
    public class LuceneTests : TestBase
    {
        #region Test infrastructure
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
        #region TestRoot - ClassInitialize - ClassCleanup
        private static string _testRootName = "_LuceneTest";
        private static string __testRootPath = String.Concat("/Root/", _testRootName);
        private Folder __testRoot;
        private Folder _testRoot
        {
            get
            {
                if (__testRoot == null)
                {
                    __testRoot = (Folder)Node.LoadNode(__testRootPath);
                    if (__testRoot == null)
                    {
                        Folder folder = new SystemFolder(Repository.Root);
                        folder.Name = _testRootName;
                        folder.Save();
                        __testRoot = (Folder)Node.LoadNode(__testRootPath);
                    }
                }
                return __testRoot;
            }
        }

        //[ClassInitialize]
        //public static void InitializeEngine(TestContext testContext)
        //{
        //    StorageContext.Search.IsOuterEngineEnabled = true;
        //}
        [ClassCleanup]
        public static void RemoveContentTypes()
        {
            try
            {
                //if (Node.Exists(__testRootPath))
                Node.ForceDelete(__testRootPath);
                foreach (string path in _pathsToDelete)
                {
                    try
                    {
                        Node n = Node.LoadNode(path);
                        if (n != null)
                            Node.ForceDelete(path);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                //HACK: Uncomment the 'if' and delete try block 
                int q = 1;
            }
        }

        #endregion


        //=======================================================================================

        [TestMethod]
        public void Lucene_Document()
        {
            var doc = IndexDocumentInfo.CreateDocument(Node.LoadNode("/Root"));
            var typeis = String.Join(" ", doc.GetValues("TypeIs"));
            Assert.IsTrue(doc.GetValues("Path")[0] == "/root", "#1");
            Assert.IsTrue(typeis.Contains("genericcontent"), "#2");
            Assert.IsTrue(typeis.Contains("folder"), "#3");
            Assert.IsTrue(typeis.Contains("portalroot"), "#4");
        }
        [TestMethod]
        public void Lucene_FullText2()
        {
            var content = Content.CreateNew("Car", _testRoot, Guid.NewGuid().ToString());
            var gc = (GenericContent)content.ContentHandler;
            gc.Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
            content.Save();

            var nodeQuery = new NodeQuery(
                new SearchExpression("\"dolor sit amet*\"")
            );
            nodeQuery.Orders.AddRange(new SearchOrder[]{
                new SearchOrder(IntAttribute.Id, OrderDirection.Asc)
            });
            nodeQuery.Top = 100;

            //var result1 = nodeQuery.Execute(ExecutionHint.ForceRelationalEngine);
            var result2 = nodeQuery.Execute(ExecutionHint.ForceIndexedEngine);
            Assert.IsTrue(result2.Count > 0, "No matches");
            //Assert.IsTrue(CompareResults(result1, result2), "Results are not equal");
        }

        [TestMethod]
        public void Lucene_WorkOnDocument_ApprovingOff_NoVersioning()
        {
            var dump = new StringBuilder();
            var checkers = new List<LucObjectChecker>();

            var folder = new Folder(_testRoot);
            folder.Name = "ApprovingOff_NoVersioning";
            folder.InheritableVersioningMode = InheritableVersioningType.None;
            folder.InheritableApprovingMode = ApprovingType.False;
            folder.Save();
            var folderId = folder.Id;

            var car = Content.CreateNew("Car", folder, "Car_Off_None");
            var gcar = (GenericContent)car.ContentHandler;
            gcar.Description = "1_Create";
            car.Save();
            var carId = car.Id;

            AddDump("1_Create", carId, dump);
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            var msg = CheckLucObjects(carId, "1_Create", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "2_CheckOut";
            gcar.CheckOut();

            AddDump("2_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "2_CheckOut" });
            msg = CheckLucObjects(carId, "2_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "3_Save";
            car.Save();

            AddDump("3_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "3_Save" });
            msg = CheckLucObjects(carId, "3_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "4_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("4_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            msg = CheckLucObjects(carId, "4_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "5_Save";
            car.Save();

            AddDump("5_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "5_Save" });
            msg = CheckLucObjects(carId, "5_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "6_CheckOut";
            gcar.CheckOut();

            AddDump("6_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "5_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "6_CheckOut" });
            msg = CheckLucObjects(carId, "6_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "7_Save";
            car.Save();

            AddDump("7_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "5_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "7_Save" });
            msg = CheckLucObjects(carId, "7_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "8_CheckIn";
            car.CheckIn();

            AddDump("8_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "8_CheckIn" });
            msg = CheckLucObjects(carId, "8_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);
        }
        [TestMethod]
        public void Lucene_WorkOnDocument_ApprovingOff_MajorVersioning()
        {
            var dump = new StringBuilder();
            var checkers = new List<LucObjectChecker>();

            var folder = new Folder(_testRoot);
            folder.Name = "ApprovingOff_MajorVersioning";
            folder.InheritableVersioningMode = InheritableVersioningType.MajorOnly;
            folder.InheritableApprovingMode = ApprovingType.False;
            folder.Save();
            var folderId = folder.Id;

            var car = Content.CreateNew("Car", folder, "Car_Off_Major");
            var gcar = (GenericContent)car.ContentHandler;
            gcar.Description = "1_Create";
            car.Save();
            var carId = car.Id;

            AddDump("1_Create", carId, dump);
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            var msg = CheckLucObjects(carId, "1_Create", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "2_CheckOut";
            gcar.CheckOut();

            AddDump("2_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "2_CheckOut" });
            msg = CheckLucObjects(carId, "2_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "3_Save";
            car.Save();

            AddDump("3_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "3_Save" });
            msg = CheckLucObjects(carId, "3_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "4_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("4_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            msg = CheckLucObjects(carId, "4_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "5_Save";
            car.Save();

            AddDump("5_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "5_Save" });
            msg = CheckLucObjects(carId, "5_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "6_CheckOut";
            gcar.CheckOut();

            AddDump("6_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "5_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.L", Data = "6_CheckOut" });
            msg = CheckLucObjects(carId, "6_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "7_Save";
            car.Save();

            AddDump("7_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "5_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.L", Data = "7_Save" });
            msg = CheckLucObjects(carId, "7_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "8_CheckIn";
            car.CheckIn();

            AddDump("8_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "5_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V3.0.A", Data = "8_CheckIn" });
            msg = CheckLucObjects(carId, "8_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);

        }
        [TestMethod]
        public void Lucene_WorkOnDocument_ApprovingOff_FullVersioning()
        {
            var dump = new StringBuilder();
            var checkers = new List<LucObjectChecker>();

            var folder = new Folder(_testRoot);
            folder.Name = "ApprovingOff_FullVersioning";
            folder.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
            folder.InheritableApprovingMode = ApprovingType.False;
            folder.Save();
            var folderId = folder.Id;

            var car = Content.CreateNew("Car", folder, "Car_Off_Full");
            var gcar = (GenericContent)car.ContentHandler;
            gcar.Description = "1_Create";
            gcar.Save();
            var carId = car.Id;

            AddDump("1_Create", carId, dump);
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            var msg = CheckLucObjects(carId, "1_Create", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "2_CheckOut";
            gcar.CheckOut();

            AddDump("2_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.L", Data = "2_CheckOut" });
            msg = CheckLucObjects(carId, "2_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "3_Save";
            gcar.Save();

            AddDump("3_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.L", Data = "3_Save" });
            msg = CheckLucObjects(carId, "3_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "4_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("4_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            msg = CheckLucObjects(carId, "4_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "5_Save";
            gcar.Save();

            AddDump("5_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.D", Data = "5_Save" });
            msg = CheckLucObjects(carId, "5_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "6_Publish";
            gcar.Publish();

            AddDump("6_Publish", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            msg = CheckLucObjects(carId, "6_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "7_Save";
            gcar.Save();

            AddDump("7_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            msg = CheckLucObjects(carId, "7_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "8_Save";
            gcar.Save();

            AddDump("8_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.2.D", Data = "8_Save" });
            msg = CheckLucObjects(carId, "8_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "9_Publish";
            gcar.Publish();

            AddDump("9_Publish", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            msg = CheckLucObjects(carId, "9_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "10_CheckOut";
            gcar.CheckOut();

            AddDump("10_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V2.1.L", Data = "10_CheckOut" });
            msg = CheckLucObjects(carId, "10_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "11_Save";
            gcar.Save();

            AddDump("11_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V2.1.L", Data = "11_Save" });
            msg = CheckLucObjects(carId, "11_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "12_Save";
            gcar.Save();

            AddDump("12_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V2.1.L", Data = "12_Save" });
            msg = CheckLucObjects(carId, "12_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "13_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("13_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            msg = CheckLucObjects(carId, "13_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "14_CheckOut";
            gcar.CheckOut();

            AddDump("14_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V2.1.L", Data = "14_CheckOut" });
            msg = CheckLucObjects(carId, "14_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "15_Save";
            car.Save();

            AddDump("15_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V2.1.L", Data = "15_Save" });
            msg = CheckLucObjects(carId, "15_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "16_CheckIn";
            gcar.CheckIn();

            AddDump("16_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V2.1.D", Data = "16_CheckIn" });
            msg = CheckLucObjects(carId, "16_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "17_Publish";
            gcar.Publish();

            AddDump("17_Publish", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "6_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "7_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "9_Publish" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V3.0.A", Data = "17_Publish" });
            msg = CheckLucObjects(carId, "17_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

        }
        [TestMethod]
        public void Lucene_WorkOnDocument_ApprovingOn_NoVersioning()
        {
            var dump = new StringBuilder();
            var checkers = new List<LucObjectChecker>();

            var folder = new Folder(_testRoot);
            folder.Name = "ApprovingOn_NoVersioning";
            folder.InheritableVersioningMode = InheritableVersioningType.None;
            folder.InheritableApprovingMode = ApprovingType.True;
            folder.Save();
            var folderId = folder.Id;

            var car = Content.CreateNew("Car", folder, "Car_On_None");
            var gcar = (GenericContent)car.ContentHandler;
            gcar.Description = "1_Create";
            gcar.Save();
            var carId = car.Id;

            AddDump("1_Create", carId, dump);
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            var msg = CheckLucObjects(carId, "1_Create", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "2_CheckOut";
            gcar.CheckOut();

            AddDump("2_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "2_CheckOut" });
            msg = CheckLucObjects(carId, "2_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "3_Save";
            car.Save();

            AddDump("3_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "3_Save" });
            msg = CheckLucObjects(carId, "3_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "4_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("4_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            msg = CheckLucObjects(carId, "4_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "5_Save";
            car.Save();

            AddDump("5_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "5_Save" });
            msg = CheckLucObjects(carId, "5_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "6_CheckOut";
            gcar.CheckOut();

            AddDump("6_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "5_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "6_CheckOut" });
            msg = CheckLucObjects(carId, "6_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);


            gcar.Description = "7_Save";
            car.Save();

            AddDump("7_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "5_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "7_Save" });
            msg = CheckLucObjects(carId, "7_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "8_CheckIn";
            car.CheckIn();

            AddDump("8_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "8_CheckIn" });
            msg = CheckLucObjects(carId, "8_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "9_CheckOut";
            gcar.CheckOut();

            AddDump("9_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "8_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "9_CheckOut" });
            msg = CheckLucObjects(carId, "9_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "10_Save";
            car.Save();

            AddDump("10_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "8_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "10_Save" });
            msg = CheckLucObjects(carId, "10_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "11_Save";
            car.Save();

            AddDump("11_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "8_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "11_Save" });
            msg = CheckLucObjects(carId, "11_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "12_CheckIn";
            gcar.CheckIn();

            AddDump("12_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "12_CheckIn" });
            msg = CheckLucObjects(carId, "12_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "13_Reject";
            gcar.Reject();

            AddDump("13_Reject", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "13_Reject" });
            msg = CheckLucObjects(carId, "13_Reject", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "14_Save";
            gcar.Save();

            AddDump("14_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "13_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "14_Save" });
            msg = CheckLucObjects(carId, "14_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "15_CheckOut";
            gcar.CheckOut();

            AddDump("15_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "13_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "14_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.L", Data = "15_CheckOut" });
            msg = CheckLucObjects(carId, "15_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "16_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("16_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "13_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "14_Save" });
            msg = CheckLucObjects(carId, "16_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "17_Reject";
            gcar.Reject();

            AddDump("17_Reject", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "13_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.R", Data = "17_Reject" });
            msg = CheckLucObjects(carId, "17_Reject", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "18_Save";
            gcar.Save();

            AddDump("18_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "13_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.R", Data = "17_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.P", Data = "18_Save" });
            msg = CheckLucObjects(carId, "18_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "19_Approve";
            gcar.Approve();

            AddDump("19_Approve", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "19_Approve" });
            msg = CheckLucObjects(carId, "19_Approve", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "20_CheckOut";
            gcar.CheckOut();

            AddDump("20_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "19_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "20_CheckOut" });
            msg = CheckLucObjects(carId, "20_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "21_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("21_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "19_Approve" });
            msg = CheckLucObjects(carId, "21_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "22_Save";
            gcar.Save();

            AddDump("22_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "19_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "22_Save" });
            msg = CheckLucObjects(carId, "22_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "23_CheckOut";
            gcar.CheckOut();

            AddDump("23_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "19_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "22_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.L", Data = "23_CheckOut" });
            msg = CheckLucObjects(carId, "23_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "24_Save";
            gcar.Save();

            AddDump("24_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "19_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "22_Save" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.L", Data = "24_Save" });
            msg = CheckLucObjects(carId, "24_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "25_CheckIn";
            gcar.CheckIn();

            AddDump("25_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "19_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "25_CheckIn" });
            msg = CheckLucObjects(carId, "25_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "26_Approve";
            gcar.Approve();

            AddDump("26_Approve", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "26_Approve" });
            msg = CheckLucObjects(carId, "26_Approve", checkers);
            if (msg != null)
                Assert.Fail(msg);
        }
        [TestMethod]
        /**/public void Lucene_WorkOnDocument_ApprovingOn_MajorVersioning()
        {
            var dump = new StringBuilder();
            var checkers = new List<LucObjectChecker>();

            var folder = new Folder(_testRoot);
            folder.Name = "ApprovingOn_MajorVersioning";
            folder.InheritableVersioningMode = InheritableVersioningType.MajorOnly;
            folder.InheritableApprovingMode = ApprovingType.True;
            folder.Save();
            var folderId = folder.Id;

            var car = Content.CreateNew("Car", folder, "Car_On_Major");
            var gcar = (GenericContent)car.ContentHandler;
            gcar.Description = "1_Create";
            gcar.Save();
            var carId = car.Id;

            AddDump("1_Create", carId, dump);
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            var msg = CheckLucObjects(carId, "1_Create", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "2_CheckOut";
            gcar.CheckOut();

            AddDump("2_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "2_CheckOut" });
            msg = CheckLucObjects(carId, "2_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "3_Save";
            car.Save();

            AddDump("3_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "3_Save" });
            msg = CheckLucObjects(carId, "3_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "4_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("4_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "1_Create" });
            msg = CheckLucObjects(carId, "4_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "5_Save";
            car.Save();

            AddDump("5_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.P", Data = "5_Save" });
            msg = CheckLucObjects(carId, "5_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "6_Reject";
            gcar.Reject();

            AddDump("6_Reject", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "6_Reject" });
            msg = CheckLucObjects(carId, "6_Reject", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "7_Save";
            car.Save();

            AddDump("7_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "7_Save" });
            msg = CheckLucObjects(carId, "7_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "8_Reject";
            gcar.Reject();

            AddDump("8_Reject", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.R", Data = "8_Reject" });
            msg = CheckLucObjects(carId, "8_Reject", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "9_Save";
            car.Save();

            AddDump("9_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V1.0.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.R", Data = "8_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.P", Data = "9_Save" });
            msg = CheckLucObjects(carId, "9_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "10_Approve";
            gcar.Approve();

            AddDump("10_Approve", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "10_Approve" });
            msg = CheckLucObjects(carId, "10_Approve", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "11_CheckOut";
            gcar.CheckOut();

            AddDump("11_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "10_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "11_CheckOut" });
            msg = CheckLucObjects(carId, "11_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "12_Save";
            gcar.Save();

            AddDump("12_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "10_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.L", Data = "12_Save" });
            msg = CheckLucObjects(carId, "12_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "13_CheckIn";
            car.CheckIn();

            AddDump("13_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "10_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.P", Data = "13_CheckIn" });
            msg = CheckLucObjects(carId, "13_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "14_Reject";
            gcar.Reject();

            AddDump("14_Reject", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "10_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.R", Data = "14_Reject" });
            msg = CheckLucObjects(carId, "14_Reject", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "15_Save";
            gcar.Save();

            AddDump("15_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "10_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V2.0.R", Data = "14_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = true, IsPublic = false, Version = "V3.0.P", Data = "15_Save" });
            msg = CheckLucObjects(carId, "15_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "16_Approve";
            gcar.Approve();

            AddDump("16_Approve", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "10_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "16_Approve" });
            msg = CheckLucObjects(carId, "16_Approve", checkers);
            if (msg != null)
                Assert.Fail(msg);

        }
        [TestMethod]
        public void Lucene_WorkOnDocument_ApprovingOn_FullVersioning()
        {
            var dump = new StringBuilder();
            var checkers = new List<LucObjectChecker>();

            var folder = new Folder(_testRoot);
            folder.Name = "ApprovingOn_FullVersioning";
            folder.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
            folder.InheritableApprovingMode = ApprovingType.True;
            folder.Save();
            var folderId = folder.Id;

            var car = Content.CreateNew("Car", folder, "Car_On_Full");
            var gcar = (GenericContent)car.ContentHandler;
            gcar.Description = "1_Create";
            car.Save();
            var carId = car.Id;

            AddDump("1_Create", carId, dump);
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            var msg = CheckLucObjects(carId, "1_Create", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "2_CheckOut";
            gcar.CheckOut();

            AddDump("2_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.L", Data = "2_CheckOut" });
            msg = CheckLucObjects(carId, "2_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "3_Save";
            car.Save();

            AddDump("3_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.L", Data = "3_Save" });
            msg = CheckLucObjects(carId, "3_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "4_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("4_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            msg = CheckLucObjects(carId, "4_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "5_Save";
            car.Save();

            AddDump("5_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.D", Data = "5_Save" });
            msg = CheckLucObjects(carId, "5_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "6_Publish";
            car.Publish();

            AddDump("6_Publish", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.P", Data = "6_Publish" });
            msg = CheckLucObjects(carId, "6_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "6_Reject";
            gcar.Reject();

            AddDump("6_Reject", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            msg = CheckLucObjects(carId, "6_Reject", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "7_Save";
            gcar.Save();

            AddDump("7_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.3.D", Data = "7_Save" });
            msg = CheckLucObjects(carId, "7_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "8_Publish";
            gcar.Publish();

            AddDump("8_Publish", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.3.P", Data = "8_Publish" });
            msg = CheckLucObjects(carId, "8_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "9_Approve";
            gcar.Approve();

            AddDump("9_Approve", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            msg = CheckLucObjects(carId, "8_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "10_CheckOut";
            gcar.CheckOut();

            AddDump("10_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.L", Data = "10_CheckOut" });
            msg = CheckLucObjects(carId, "10_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "11_UndoCheckOut";
            gcar.UndoCheckOut();

            AddDump("11_UndoCheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            msg = CheckLucObjects(carId, "11_UndoCheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "12_CheckOut";
            gcar.CheckOut();

            AddDump("12_CheckOut", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.L", Data = "12_CheckOut" });
            msg = CheckLucObjects(carId, "12_CheckOut", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "13_Save";
            gcar.Save();

            AddDump("13_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.L", Data = "13_Save" });
            msg = CheckLucObjects(carId, "13_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "14_Save";
            gcar.Save();

            AddDump("14_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.L", Data = "14_Save" });
            msg = CheckLucObjects(carId, "14_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "15_CheckIn";
            gcar.CheckIn();

            AddDump("15_CheckIn", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "15_CheckIn" });
            msg = CheckLucObjects(carId, "15_CheckIn", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "16_Save";
            gcar.Save();

            AddDump("16_Save", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "15_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.2.D", Data = "16_Save" });
            msg = CheckLucObjects(carId, "16_Save", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "17_Publish";
            gcar.Publish();

            AddDump("17_Publish", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "15_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.2.P", Data = "17_Publish" });
            msg = CheckLucObjects(carId, "17_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "18_Reject";
            gcar.Reject();

            AddDump("18_Reject", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "15_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.2.R", Data = "18_Reject" });
            msg = CheckLucObjects(carId, "18_Reject", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "19_Publish";
            gcar.Publish();

            AddDump("19_Publish", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "15_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.2.R", Data = "18_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.3.P", Data = "19_Publish" });
            msg = CheckLucObjects(carId, "19_Publish", checkers);
            if (msg != null)
                Assert.Fail(msg);

            gcar.Description = "20_Approve";
            gcar.Approve();

            AddDump("20_Approve", carId, dump);
            checkers.Clear();
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.1.D", Data = "1_Create" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V0.2.R", Data = "6_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = true, IsPublic = true, Version = "V1.0.A", Data = "9_Approve" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.1.D", Data = "15_CheckIn" });
            checkers.Add(new LucObjectChecker { IsLastDraft = false, IsLastPublic = false, IsMajor = false, IsPublic = false, Version = "V1.2.R", Data = "18_Reject" });
            checkers.Add(new LucObjectChecker { IsLastDraft = true, IsLastPublic = true, IsMajor = true, IsPublic = true, Version = "V2.0.A", Data = "20_Approve" });
            msg = CheckLucObjects(carId, "20_Approve", checkers);
            if (msg != null)
                Assert.Fail(msg);

            #region dump
            /*
            1_Create
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            -----------------
            2_CheckOut
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.L; VersionId: 00000000000054; 
            -----------------
            3_Save
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.L; VersionId: 00000000000054; 
            -----------------
            4_UndoCheckOut
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            -----------------
            5_Save
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.D; VersionId: 00000000000055; 
            -----------------
            6_Publish
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.P; VersionId: 00000000000055; 
            -----------------
            6_Reject
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            -----------------
            7_Save
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.3.D; VersionId: 00000000000056; 
            -----------------
            8_Publish
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.3.P; VersionId: 00000000000056; 
            -----------------
            9_Approve
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 1; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            -----------------
            10_CheckOut
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.L; VersionId: 00000000000057; 
            -----------------
            11_UndoCheckOut
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 1; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            -----------------
            12_CheckOut
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.L; VersionId: 00000000000058; 
            -----------------
            13_Save
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.L; VersionId: 00000000000058; 
            -----------------
            14_Save
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.L; VersionId: 00000000000058; 
            -----------------
            15_CheckIn
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.D; VersionId: 00000000000058; 
            -----------------
            16_Save
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.D; VersionId: 00000000000058; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.2.D; VersionId: 00000000000059; 
            -----------------
            17_Publish
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.D; VersionId: 00000000000058; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.2.P; VersionId: 00000000000059; 
            -----------------
            18_Reject
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.D; VersionId: 00000000000058; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.2.R; VersionId: 00000000000059; 
            -----------------
            19_Publish
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.D; VersionId: 00000000000058; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.2.R; VersionId: 00000000000059; 
            IsLastDraft: 1; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.3.P; VersionId: 0000000000005a; 
            -----------------
            20_Approve
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.1.P; VersionId: 00000000000053; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V0.2.R; VersionId: 00000000000055; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 1; IsPublic: 1; Version: V1.0.A; VersionId: 00000000000056; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.1.D; VersionId: 00000000000058; 
            IsLastDraft: 0; IsLastPublic: 0; IsMajor: 0; IsPublic: 0; Version: V1.2.R; VersionId: 00000000000059; 
            IsLastDraft: 1; IsLastPublic: 1; IsMajor: 1; IsPublic: 1; Version: V2.0.A; VersionId: 0000000000005a; 
            -----------------

            */
            #endregion
        }

        [TestMethod]
        public void Lucene_NotEqualOptimizer()
        {
            var query = new NodeQuery(
                new ReferenceExpression(ReferenceAttribute.CreatedBy, User.Administrator),
                new StringExpression(StringAttribute.Name, StringOperator.Equal, User.Administrator.Name)
            );
            var expectedQueryText = LucQuery.Create(query).QueryText.Replace("+Name:", "-Name:");

            query = new NodeQuery(
                new ReferenceExpression(ReferenceAttribute.CreatedBy, User.Administrator),
                new StringExpression(StringAttribute.Name, StringOperator.NotEqual, User.Administrator.Name)
            );
            var queryText = LucQuery.Create(query).QueryText;

            // +CreatedById:00000000000001 -Name:administrator
            Assert.AreEqual(expectedQueryText, queryText);
        }
        [TestMethod]
        public void Lucene_NotExpressionOptimizer()
        {
            var query = new NodeQuery(
                new ReferenceExpression(ReferenceAttribute.CreatedBy, User.Administrator),
                new StringExpression(StringAttribute.Name, StringOperator.Equal, User.Administrator.Name)
            );
            var expectedQueryText = LucQuery.Create(query).QueryText.Replace("+Name:", "-Name:");

            query = new NodeQuery(
                new ReferenceExpression(ReferenceAttribute.CreatedBy, User.Administrator),
                new NotExpression(
                    new StringExpression(StringAttribute.Name, StringOperator.Equal, User.Administrator.Name))
            );
            var queryText = LucQuery.Create(query).QueryText;

            // +CreatedById:00000000000001 -Name:administrator
            Assert.AreEqual(expectedQueryText, queryText);
        }

        [TestMethod]
        public void Lucene_ChildrenQuery()
        {
            var nodeId = RepositoryConfiguration.PortalRootId;
            var queryText = "ParentId:" + nodeId;
            var q = LucQuery.Parse(queryText);
            q.EnableAutofilters = FilterStatus.Disabled;
            q.EnableLifespanFilter = FilterStatus.Disabled;
            //var idArray = ((LuceneSearchEngine)StorageContext.Search.SearchEngine).Execute_NEW(queryText);
            var idArray = (from lucObject in q.Execute() select lucObject.NodeId).ToArray();
            var count = idArray.Length;

            var count2 = Repository.Root.Children.Count();

            Assert.IsTrue(count > 0, "Empty result.");
            Assert.IsTrue(count == count2, String.Concat("Results are not equal. Expected: ", count2, ", actual: ", count));
        }

        [TestMethod]
        public void Lucene_LifespanQueries()
        {
            var x0 = ContentQuery.Query("WebContentDemo .LIFESPAN:ON .AUTOFILTERS:OFF .SORT:Index");
            var n0 = x0.Nodes.Select(n => n.Index);
            var s0 = string.Join(",", n0);

            var lastYear = DateTime.UtcNow.AddYears(-1);
            var yesterday = DateTime.UtcNow.AddDays(-1.0);
            var tomorrow = DateTime.UtcNow.AddDays(1.0);
            var nextYear = DateTime.UtcNow.AddYears(1);

            CreateLifespanContent(yesterday, tomorrow, 101);
            CreateLifespanContent(tomorrow, nextYear, 102);
            CreateLifespanContent(lastYear, yesterday, 103);
            CreateLifespanContent(null, tomorrow, 104);
            CreateLifespanContent(null, yesterday, 105);
            CreateLifespanContent(yesterday, null, 106);
            CreateLifespanContent(tomorrow, null, 107);
            CreateLifespanContent(null, null, 108);

            //==================================================

            var x1 = ContentQuery.Query("WebContentDemo .AUTOFILTERS:OFF .SORT:Index");
            var n1 = x1.Nodes.Select(n => n.Index);
            var s1 = string.Join(",", n1);

            var x2 = ContentQuery.Query("WebContentDemo .LIFESPAN:ON .AUTOFILTERS:OFF .SORT:Index");
            var n2 = x2.Nodes.Select(n => n.Index);
            var s2 = string.Join(",", n2);

            var expected = s0 + ",101,104,106,108";

            Assert.AreEqual(expected, s2);
        }
        private void CreateLifespanContent(DateTime? from, DateTime? till, int index)
        {
            var c = Content.CreateNew("WebContentDemo", _testRoot, Guid.NewGuid().ToString());
            var gc = (GenericContent)c.ContentHandler;
            gc.ValidFrom = from ?? DateTime.MinValue;
            gc.ValidTill = till ?? DateTime.MinValue;
            gc.EnableLifespan = true;
            c["Header"] = "WebContentDemo";
            gc.Index = index;
            gc.Save();
        }

        //===================================================================

        private int[] GetIdArray(IEnumerable<LucObject> docs)
        {
            return docs.Select(d => d.NodeId).ToArray();
        }

        [TestMethod]
        public void Lucene_Depth()
        {
            var qtext = "InTree:/Root/IMS AND Depth:<4";
            var query = SenseNet.Search.ContentQuery.CreateQuery(qtext);
            query.Settings.EnableAutofilters = FilterStatus.Disabled;
            var result = query.Execute();
            var paths = GetPathArray(result);

            var maxDepth = paths.Select(x => x.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length).Max();

            Assert.AreEqual(4, maxDepth);
        }
        private string[] GetPathArray(SenseNet.Search.QueryResult result)
        {
            return result.Nodes.Select(n => n.Path).ToArray();
        }

        //[TestMethod]
        //public void Lucene_QueryExecutor20100701()
        //{
        //    var queryText = "InTree:/Root/IMS AND Depth:<4 .SORT:Id .AUTOFILTERS:OFF";
        //    var query = LucQuery.Parse(queryText);
        //    var result = query.Execute(new QueryExecutor20100701());
        //    var maxDepth = result.Select(o => o.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length).Max();
        //    Assert.AreEqual(4, maxDepth);
        //}
        //[TestMethod]
        //public void QueryExecutor20100701_Paging()
        //{
        //    Folder[] folders;
        //    var root = CreateStructureForQueryExecutor20100701_Paging(out folders);

        //    #region
        //    //var see0 = folders[0].Security.HasPermission((IUser)Developer1, PermissionType.See);
        //    //var see1 = folders[1].Security.HasPermission((IUser)Developer1, PermissionType.See);
        //    //var see2 = folders[2].Security.HasPermission((IUser)Developer1, PermissionType.See);
        //    //var see3 = folders[3].Security.HasPermission((IUser)Developer1, PermissionType.See);
        //    //var see4 = folders[4].Security.HasPermission((IUser)Developer1, PermissionType.See);

        //    ////var queryText = String.Format("InTree:{0} .SORT:Id .TOP:5 .SKIP:5", _testRoot.Path);
        //    //var queryText = String.Format("InTree:{0} AND Type:Car .SORT:Id", root.Path); //  InTree:/Root/_LuceneTest/QueryExecutor20100701_Paging .SORT:Id
        //    //var query = LucQuery.Parse(queryText);

        //    //query.User = null;
        //    //var result0 = query.Execute(new QueryExecutor20100630());
        //    //var idArray0 = GetIdArray(result0);
        //    //var nameString0 = GetNameString(GetNameArray(idArray0)); // Car0, Car1, Car2, Car3, Car4, Car5, Car6, Car7, Car8, Car9, Car10, Car11, Car12, Car13, Car14

        //    //query.User = Developer1;
        //    //var result1 = query.Execute(new QueryExecutor20100630());
        //    //var idArray1 = GetIdArray(result1);
        //    //var nameString1 = GetNameString(GetNameArray(idArray1)); // Car0, Car1, Car2, Car6, Car7, Car8, Car12, Car13, Car14

        //    //query.User = null;
        //    //var result2 = query.Execute(new QueryExecutor20100701());
        //    //var idArray2 = GetIdArray(result2);
        //    //var nameString2 = GetNameString(GetNameArray(idArray2)); // Car0, Car1, Car2, Car3, Car4, Car5, Car6, Car7, Car8, Car9, Car10, Car11, Car12, Car13, Car14

        //    //query.User = Developer1;
        //    //var result3 = query.Execute(new QueryExecutor20100701());
        //    //var idArray3 = GetIdArray(result3);
        //    //var nameString3 = GetNameString(GetNameArray(idArray3)); // Car0, Car1, Car2, Car6, Car7, Car8, Car12, Car13, Car14

        //    //----

        //    //queryText = String.Format("InTree:{0} AND Type:Car .SORT:Id .TOP:4 .SKIP:4", _testRoot.Path);
        //    //query = LucQuery.Parse(queryText);

        //    //query.User = null;
        //    //var result4 = query.Execute(new QueryExecutor20100630());
        //    //var idArray4 = GetIdArray(result4);
        //    //var nameString4 = GetNameString(GetNameArray(idArray4)); // Car5, Car6, Car7, Car8, Car9

        //    //query.User = Developer1;
        //    //var result5 = query.Execute(new QueryExecutor20100630());
        //    //var idArray5 = GetIdArray(result5);
        //    //var nameString5 = GetNameString(GetNameArray(idArray5)); // Car8, Car12, Car13, Car14

        //    //query.User = null;
        //    //var result6 = query.Execute(new QueryExecutor20100701());
        //    //var idArray6 = GetIdArray(result6);
        //    //var nameString6 = GetNameString(GetNameArray(idArray6)); // Car0, Car1, Car2, Car3, Car4

        //    //query.User = Developer1;
        //    //var result7 = query.Execute(new QueryExecutor20100701());
        //    //var idArray7 = GetIdArray(result7);
        //    //var nameString7 = GetNameString(GetNameArray(idArray7)); // Car0, Car1, Car2, Car6, Car7
        //    #endregion

        //    //----

        //    var queryText = String.Format("InTree:{0} AND Type:Car .SORT:Id", _testRoot.Path);
        //    var query = LucQuery.Parse(queryText);
        //    query.User = Developer1;
        //    var sb = new StringBuilder();
        //    for (int top = 1; top < 9; top++)
        //    {
        //        query.PageSize = top;
        //        for (int skip = 0; skip < 18; skip++)
        //        {
        //            query.Skip = skip;
        //            var result = query.Execute(new QueryExecutor20100701());
        //            var nameString = GetNameString(GetNameArray(GetIdArray(result)));
        //            sb.Append(top).Append("\t").Append(skip).Append("\t").AppendLine(nameString);
        //        }
        //    }
        //    var sss = sb.ToString();

        //    Assert.Inconclusive();
        //}
        private string[] GetNameArray(int[] idArray)
        {
            try
            {
                return idArray.Select(i => Node.LoadNode(i).Name).ToArray();
            }
            catch (Exception e)
            {
                throw;
            }
        }
        private string GetNameString(string[] names)
        {
            return String.Join(", ", names);
        }
        private Folder CreateStructureForQueryExecutor20100701_Paging(out Folder[] folders)
        {
            var rootPath = RepositoryPath.Combine(_testRoot.Path, "QueryExecutor20100701_Paging");
            var rootFolder = Node.Load<Folder>(rootPath);
            if (rootFolder != null)
            {
                folders = SenseNet.Search.ContentQuery.Query("InFolder:" + rootPath).Nodes.Cast<Folder>().ToArray();
                return rootFolder;
            }

            var root = Content.CreateNew("Folder", _testRoot, "QueryExecutor20100701_Paging");
            root.Save();
            rootFolder = (Folder)root.ContentHandler;

            folders = new Folder[5];
            var nodeCounter = 0;
            for (int i = 0; i < folders.Length; i++)
            {
                var f = Content.CreateNew("Folder", rootFolder, "Folder_" + i);
                f.Save();
                folders[i] = (Folder)f.ContentHandler;

                for (int j = 0; j < 4; j++)
                    Content.CreateNew("Car", f.ContentHandler, "Car" + nodeCounter++).Save();
            }
            folders[1].Security.SetPermission(Developer1, true, PermissionType.See, PermissionValue.Deny);
            folders[3].Security.SetPermission(Developer1, true, PermissionType.See, PermissionValue.Deny);
            return rootFolder;
        }

        [TestMethod]
        public void Lucene_SearchInOnlyLastVersions()
        {
            var rootPath = RepositoryPath.Combine(_testRoot.Path, "Lucene_SearchInVersions");
            var root = CreateBrandNewFolder(rootPath);
            root.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
            root.Save();

            var contentCount = 3;
            var contents = new List<Content>(contentCount);

            for (int i = 0; i < contentCount; i++)
            {
                var content = Content.CreateNew("Car", root, "Car" + i);
                content["Make"] = "V0.1.D";
                content.Save();
                contents.Add(content);
            }
            for (int i = 1; i < contents.Count; i++)
            {
                contents[i]["Make"] = "V0.2.D";
                contents[i].Save();
            }
            for (int i = 2; i < contents.Count; i++)
            {
                contents[i]["Make"] = "V1.0.A";
                contents[i].Save();
                contents[i].Publish();
            }

            var result0 = LucQuery.Parse(String.Concat("InTree:", rootPath, " AND Make:'V0.1.D' .SORT:Id .AUTOFILTERS:OFF")).Execute();
            var nameString0 = GetNameString(GetNameArray(GetIdArray(result0)));
            var expectedNameString0 = "Car0";

            var result1 = LucQuery.Parse(String.Concat("InTree:", rootPath, " AND Make:'V0.2.D' .SORT:Id .AUTOFILTERS:OFF")).Execute();
            
            var nameString1 = GetNameString(GetNameArray(GetIdArray(result1)));
            var expectedNameString1 = "Car1";

            var expectedNameStrings = String.Concat(expectedNameString0, " | ", expectedNameString1);
            var nameStrings = String.Concat(nameString0, " | ", nameString1);

            Assert.AreEqual(expectedNameStrings, nameStrings);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException), "Projection in top level query is not allowed (.SELECT:Name)")]
        public void Lucene_Projection_TopLevelIsNotAllowed()
        {
            var queryText = "+Name:{{Type:ContentType .SORT:Id .SELECT:Name}} -Type:ContentType .SELECT:Name";
            var contentQuery = ContentQuery.CreateQuery(queryText, new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled });
            var idSet = contentQuery.ExecuteToIds();
        }
        [TestMethod]
        public void Lucene_Projection_Executing()
        {
            var folderPath = RepositoryPath.Combine(_testRoot.Path, "Folder");
            var folderContent = Content.Load(folderPath);
            if (folderContent == null)
            {
                folderContent = Content.CreateNew("Folder", _testRoot, "Folder");
                folderContent.Save();
            }
            var folderId = folderContent.Id;

            var carPath = RepositoryPath.Combine(_testRoot.Path, "Car");
            var carContent = Content.Load(carPath);
            if (carContent == null)
            {
                carContent = Content.CreateNew("Car", _testRoot, "Car");
                carContent.Save();
            }
            var carId = carContent.Id;

            var queryText = "+Name:{{Type:ContentType .SORT:Id .SELECT:Name}} -Type:ContentType";

            var contentQuery = ContentQuery.CreateQuery(queryText, new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled });
            var idSet = contentQuery.ExecuteToIds();

            Assert.IsTrue(idSet.Contains(folderId) && idSet.Contains(carId));
        }
        [TestMethod]
        public void Lucene_Projection_InnerResultAutoEscape()
        {
            var folderPath = RepositoryPath.Combine(__testRootPath, "Folder");
            var folderContent = Content.Load(folderPath);
            if (folderContent == null)
            {
                folderContent = Content.CreateNew("Folder", _testRoot, "Folder");
                folderContent.Save();
            }
            var folderId = folderContent.Id;

            var carIds = new int[3];
            for (var i = 0; i < carIds.Length; i++)
            {
                var name = "Car-" + i;
                var carPath = RepositoryPath.Combine(_testRoot.Path, name);
                var carContent = Content.Load(carPath);
                if (carContent == null)
                {
                    carContent = Content.CreateNew("Car", _testRoot, name);
                    carContent.Save();
                }
                carIds[i] = carContent.Id;
            }

            var queryText = String.Concat("+Name:{{+InFolder:'", __testRootPath, "' +Type:Car .SORT:Id .SELECT:Name}}");

            var contentQuery = ContentQuery.CreateQuery(queryText, new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled });
            var idSet = contentQuery.ExecuteToIds();

            var error = carIds.Except(carIds.Intersect(idSet)).ToArray();
            Assert.IsTrue(error.Length == 0, "Missing ids: " + String.Join(", ", error));
        }

        //=======================================================================================

        Node __userFolder;
        User __boss1;
        User __manager1;
        User __developer1;

        Node UserFolder
        {
            get
            {
                if (__userFolder == null)
                {
                    var userFolder = Node.LoadNode(RepositoryPath.GetParentPath(User.Administrator.Path));
                    if (userFolder == null)
                        throw new ApplicationException("UserFolder cannot be found.");
                    __userFolder = userFolder as Node;
                }
                return __userFolder;
            }
        }
        User Boss1
        {
            get
            {
                if (__boss1 == null)
                    __boss1 = LoadOrCreateUser("boss1", "Boss 1", UserFolder);
                return __boss1;
            }
        }
        User Manager1
        {
            get
            {
                if (__manager1 == null)
                    __manager1 = LoadOrCreateUser("manager1", "Manager 1", UserFolder);
                return __manager1;
            }
        }
        User Developer1
        {
            get
            {
                if (__developer1 == null)
                    __developer1 = LoadOrCreateUser("developer1", "Developer 1", UserFolder);
                return __developer1;
            }
        }

        private User LoadOrCreateUser(string name, string fullName, Node parentFolder)
        {
            string path = RepositoryPath.Combine(parentFolder.Path, name);
            AddPathToDelete(path);

            User user = User.LoadNode(path) as User;
            if (user == null)
            {
                user = new User(parentFolder);
                user.Name = name;
            }
            user.Email = name + "@email.com";
            user.Enabled = true;
            user.FullName = fullName;
            user.Save();
            return user;
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

        [TestMethod]
        public void Lucene_Security_1()
        {
            IUser savedCurrentUser;
            NodeQuery nodeQuery;

            var folder1 = CreateBrandNewFolder(RepositoryPath.Combine(_testRoot.Path, "Folder1"));
            folder1.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
            folder1.Save();
            folder1.Security.SetPermission(Boss1, true, PermissionType.OpenMinor, PermissionValue.Allow);
            folder1.Security.SetPermission(Developer1, true, PermissionType.Open, PermissionValue.Allow);

            SecurityHandler.Reset();

            // v0.1
            var content = Content.CreateNew("Car", folder1, "Car1");
            content["Make"] = "Porsche959";
            content["Index"] = 1;
            content.Save();

            // v0.2
            content["Index"] = 2;
            content.Save();

            savedCurrentUser = AccessProvider.Current.GetCurrentUser();
            AccessProvider.Current.SetCurrentUser(Boss1);
            nodeQuery = new NodeQuery(new StringExpression(ActiveSchema.PropertyTypes["Make"], StringOperator.StartsWith, (string)"Porsche959"));
            var result1Boss = nodeQuery.Execute(ExecutionHint.ForceIndexedEngine).Nodes.ToList();
            AccessProvider.Current.SetCurrentUser(Developer1);
            var result1Developer = nodeQuery.Execute(ExecutionHint.ForceIndexedEngine).Nodes.ToList();
            AccessProvider.Current.SetCurrentUser(savedCurrentUser);

            // v1.0
            content.Publish();

            // v1.1
            content["Index"] = 3;
            content.Save();

            savedCurrentUser = AccessProvider.Current.GetCurrentUser();
            AccessProvider.Current.SetCurrentUser(Boss1);
            nodeQuery = new NodeQuery(new StringExpression(ActiveSchema.PropertyTypes["Make"], StringOperator.StartsWith, (string)"Porsche959"));
            var result2Boss = nodeQuery.Execute(ExecutionHint.ForceIndexedEngine).Nodes.ToList();
            AccessProvider.Current.SetCurrentUser(Developer1);
            var result2Developer = nodeQuery.Execute(ExecutionHint.ForceIndexedEngine).Nodes.ToList();
            AccessProvider.Current.SetCurrentUser(savedCurrentUser);

            //result2.Nodes.ToArray()
            //[0]: Id=91, Name="Car1", Version={V1.1.D}, Path="/Root/_LuceneTest/Folder1/Car1"
            //....

            Assert.IsTrue(result1Developer.Count == 0, "#1");
            Assert.IsTrue(result1Boss.Count > 0, "#2");
            Assert.IsTrue(result1Boss[0].Version.ToString() == "V0.2.D", "#3");
            Assert.IsTrue(result2Developer.Count > 0, "#4");
            Assert.IsTrue(result2Boss[0].Version.ToString() == "V1.1.D", "#5");
            Assert.IsTrue(result2Boss.Count > 0, "#6");
            Assert.IsTrue(result2Developer[0].Version.ToString() == "V1.0.A", "#7");
        }

        private Folder CreateBrandNewFolder(string path)
        {
            if (Node.Exists(path))
                Node.ForceDelete(path);
            return LoadOrCreateFolder(path);
        }
        private Folder LoadOrCreateFolder(string path)
        {
            Folder folder = (Folder)Node.LoadNode(path);
            if (folder != null)
                return folder;

            string parentPath = RepositoryPath.GetParentPath(path);
            Folder parentFolder = (Folder)Node.LoadNode(parentPath);
            if (parentFolder == null)
                parentFolder = LoadOrCreateFolder(parentPath);

            folder = new Folder(parentFolder);
            folder.Name = RepositoryPath.GetFileName(path);
            folder.Save();
            AddPathToDelete(path);

            return folder;
        }

        //=======================================================================================

        private bool CompareResults(NodeQueryResult result1, NodeQueryResult result2)
        {
            if (result1.Count != result2.Count)
                return false;
            var a = result1.Identifiers.ToArray();
            var b = result2.Identifiers.ToArray();
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
        private string ViewLucObjects(IEnumerable<SenseNet.Search.LucObject> docs)
        {
            var sb = new StringBuilder();
            foreach (var doc in docs)
            {
                var fnames = new string[] { "IsLastDraft", "IsLastPublic", "IsMajor", "IsPublic", "Version", "VersionId" };
                foreach (var fname in fnames /*doc.Names*/)
                    sb.Append(fname).Append(": ").Append(doc[fname]).Append("; ");
                sb.AppendLine();
            }
            return sb.ToString();
        }
        private void AddDump(string title, int nodeId, StringBuilder logger)
        {
            logger.AppendLine(title);
            logger.Append(ViewLucObjects(LuceneSearchEngine.GetAllDocumentVersionsByNodeId(nodeId).OrderBy(x => x["Version"])));
            logger.AppendLine("-----------------");
        }
        private string CheckLucObjects(int nodeId, string title, IEnumerable<LucObjectChecker> checkerList)
        {
            var lucObjects = LuceneSearchEngine.GetAllDocumentVersionsByNodeId(nodeId);
            if (checkerList.Count() != lucObjects.Count())
                return String.Format("After {0} lucObjects count is {1}. Expected: {2}", title, lucObjects.Count(), checkerList.Count());

            var lucObjectList = lucObjects.OrderBy(x => x["Version"]).ToList();
            for (int i = 0; i < lucObjectList.Count; i++)
            {
                var luc = lucObjectList.ElementAt(i);
                var chk = checkerList.ElementAt(i);
                if (chk.IsLastDraft != (luc["IsLastDraft"] == BooleanIndexHandler.YES))
                    return String.Format("After {0} lucObject[{1}].IsLastDraft is {2}. Expected: {3}.", title, i, luc["IsLastDraft"], chk.IsLastDraft ? BooleanIndexHandler.YES : BooleanIndexHandler.NO);
                if (chk.IsLastPublic != (luc["IsLastPublic"] == BooleanIndexHandler.YES))
                    return String.Format("After {0} lucObject[{1}].IsLastPublic is {2}. Expected: {3}.", title, i, luc["IsLastPublic"], chk.IsLastPublic ? BooleanIndexHandler.YES : BooleanIndexHandler.NO);
                if (chk.IsMajor != (luc["IsMajor"] == BooleanIndexHandler.YES))
                    return String.Format("After {0} lucObject[{1}].IsMajor is {2}. Expected: {3}.", title, i, luc["IsMajor"], chk.IsMajor ? BooleanIndexHandler.YES : BooleanIndexHandler.NO);
                if (chk.IsPublic != (luc["IsPublic"] == BooleanIndexHandler.YES))
                    return String.Format("After {0} lucObject[{1}].IsPublic is {2}. Expected: {3}.", title, i, luc["IsPublic"], chk.IsPublic ? BooleanIndexHandler.YES : BooleanIndexHandler.NO);
                if (chk.Version.ToLower() != luc["Version"])
                    return String.Format("After {0} lucObject[{1}].Version is {2}. Expected: {3}.", title, i, luc["Version"], chk.Version.ToLower());
                if (chk.Data != null)
                {
                    var version = VersionNumber.Parse(chk.Version);
                    var content = Node.LoadNode(nodeId, version) as GenericContent;
                    if(content == null)
                        return String.Format("After {0} cannot load the version {1}: {2}.", title, i, chk.Version);
                    if (content.Description != chk.Data)
                        return String.Format("After {0} Node[{1}].Data is {2}. Expected: {3}.", title, i, content.Description, chk.Data);
                }
            }
            return null;
        }

        private class LucObjectChecker
        {
            public bool IsLastDraft { get; set; }
            public bool IsLastPublic { get; set; }
            public bool IsMajor { get; set; }
            public bool IsPublic { get; set; }
            public string Version { get; set; }
            public string Data { get; set; }
        }
    }
}
