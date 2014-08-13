using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Tests
{
    [ContentHandler]
    internal class NotLoadableAspect : Aspect
    {
        public const string CTD = @"<ContentType name='NotLoadableAspect' parentType='Aspect' handler='SenseNet.ContentRepository.Tests.NotLoadableAspect' xmlns='http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition'>
</ContentType>";
        public NotLoadableAspect(Node parent) : this(parent, null) { }
        public NotLoadableAspect(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected NotLoadableAspect(NodeToken nt) : base(nt) { throw new ApplicationException("##Forbidden"); }
    }

    [TestClass]
    public class SettingsTests : TestBase
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

        private static string _testRootName = "_SettingsTests";
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
        public void Settings_OverridingValues()
        {
            // R
            //     (settings)
            //         Settings1.settings
            //     A
            //         (settings)
            //             Settings1.settings
            //         B
            //             C
            //                 (settings)
            //                     Settings1.settings
            //                 D

            var setting1 = Tools.GetStreamFromString("{x: 1, y: 2, z: 3}");
            var setting2 = Tools.GetStreamFromString("{x: 2,       z: 6}");
            var setting3 = Tools.GetStreamFromString("{x: 3, y: 4      }");

            var r = new SystemFolder(TestRoot) { Name = "R" }; r.Save();
            try
            {
                var rS = new Folder(r) { Name = Settings.SETTINGSCONTAINERNAME }; rS.Save();
                var rS1 = new Settings(rS) { Name = "Settings1.settings" }; rS1.Binary.SetStream(setting1); rS1.Binary.FileName = rS1.Name; rS1.Save();

                var a = new SystemFolder(r) { Name = "A" }; a.Save();
                var aS = new Folder(a) { Name = Settings.SETTINGSCONTAINERNAME }; aS.Save();
                var aS1 = new Settings(aS) { Name = "Settings1.settings" }; aS1.Binary.SetStream(setting2); aS1.Binary.FileName = aS1.Name; aS1.Save();

                var b = new SystemFolder(a) { Name = "B" }; b.Save();

                var c = new SystemFolder(b) { Name = "C" }; c.Save();
                var cS = new Folder(c) { Name = Settings.SETTINGSCONTAINERNAME }; cS.Save();
                var cS1 = new Settings(cS) { Name = "Settings1.settings" }; cS1.Binary.SetStream(setting3); cS1.Binary.FileName = cS1.Name; cS1.Save();

                var d = new SystemFolder(c) { Name = "D" }; d.Save();

                // =============================== Testing Settings.GetValue API

                var rX = Settings.GetValue<int>("Settings1", "x", r.Path);
                var rY = Settings.GetValue<int>("Settings1", "y", r.Path);
                var rZ = Settings.GetValue<int>("Settings1", "z", r.Path);

                var aX = Settings.GetValue<int>("Settings1", "x", a.Path);
                var aY = Settings.GetValue<int>("Settings1", "y", a.Path);
                var aZ = Settings.GetValue<int>("Settings1", "z", a.Path);

                var bX = Settings.GetValue<int>("Settings1", "x", b.Path);
                var bY = Settings.GetValue<int>("Settings1", "y", b.Path);
                var bZ = Settings.GetValue<int>("Settings1", "z", b.Path);

                var cX = Settings.GetValue<int>("Settings1", "x", c.Path);
                var cY = Settings.GetValue<int>("Settings1", "y", c.Path);
                var cZ = Settings.GetValue<int>("Settings1", "z", c.Path);

                var dX = Settings.GetValue<int>("Settings1", "x", d.Path);
                var dY = Settings.GetValue<int>("Settings1", "y", d.Path);
                var dZ = Settings.GetValue<int>("Settings1", "z", d.Path);

                Assert.IsTrue(rX == 1, String.Format("rX is {0}, expected: 1", rX));
                Assert.IsTrue(rY == 2, String.Format("rY is {0}, expected: 2", rY));
                Assert.IsTrue(rZ == 3, String.Format("rZ is {0}, expected: 3", rZ));

                Assert.IsTrue(aX == 2, String.Format("aX is {0}, expected: 2", aX));
                Assert.IsTrue(aY == 2, String.Format("aY is {0}, expected: 2", aY));
                Assert.IsTrue(aZ == 6, String.Format("aZ is {0}, expected: 6", aZ));

                Assert.IsTrue(bX == 2, String.Format("bX is {0}, expected: 2", bX));
                Assert.IsTrue(bY == 2, String.Format("bY is {0}, expected: 2", bY));
                Assert.IsTrue(bZ == 6, String.Format("bZ is {0}, expected: 6", bZ));

                Assert.IsTrue(cX == 3, String.Format("cX is {0}, expected: 3", cX));
                Assert.IsTrue(cY == 4, String.Format("cY is {0}, expected: 4", cY));
                Assert.IsTrue(cZ == 6, String.Format("cZ is {0}, expected: 6", cZ));

                Assert.IsTrue(dX == 3, String.Format("dX is {0}, expected: 3", dX));
                Assert.IsTrue(dY == 4, String.Format("dY is {0}, expected: 4", dY));
                Assert.IsTrue(dZ == 6, String.Format("dZ is {0}, expected: 6", dZ));

                // =============================== Testing dynamic fields

                var rs1content = Content.Load(rS1.Id);
                var as1content = Content.Load(aS1.Id);
                var cs1content = Content.Load(cS1.Id);

                Assert.AreEqual(rs1content["x"], 1m, string.Format("rs1content[\"x\"] is {0}, expected: 1", rs1content["x"]));
                Assert.AreEqual(rs1content["y"], 2m, string.Format("rs1content[\"y\"] is {0}, expected: 2", rs1content["y"]));
                Assert.AreEqual(rs1content["z"], 3m, string.Format("rs1content[\"z\"] is {0}, expected: 3", rs1content["z"]));

                Assert.AreEqual(as1content["x"], 2m, string.Format("as1content[\"x\"] is {0}, expected: 2", as1content["x"]));
                Assert.AreEqual(as1content["y"], 2m, string.Format("as1content[\"y\"] is {0}, expected: 2", as1content["y"]));
                Assert.AreEqual(as1content["z"], 6m, string.Format("as1content[\"z\"] is {0}, expected: 6", as1content["z"]));

                Assert.AreEqual(cs1content["x"], 3m, string.Format("cs1content[\"x\"] is {0}, expected: 3", cs1content["x"]));
                Assert.AreEqual(cs1content["y"], 4m, string.Format("cs1content[\"y\"] is {0}, expected: 4", cs1content["y"]));
                Assert.AreEqual(cs1content["z"], 6m, string.Format("cs1content[\"z\"] is {0}, expected: 6", cs1content["z"]));
            }
            finally
            {
                r.ForceDelete();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidContentException))]
        public void Settings_WrongPlace()
        {
            var rS1 = new Settings(TestRoot) { Name = "Settings1.settings" };
            try
            {
                rS1.Binary.SetStream(Tools.GetStreamFromString("{x: 1, y: 2, z: 3}"));
                rS1.Save();
            }
            finally
            {
                rS1.ForceDelete();
            }
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentException))]
        public void Settings_WrongExtension()
        {
            var r = new SystemFolder(TestRoot) { Name = "R" }; r.Save();
            try
            {
                var rS = new Folder(r) { Name = Settings.SETTINGSCONTAINERNAME }; rS.Save();
                var rS1 = new Settings(rS) { Name = "Settings1" };
                rS1.Binary.SetStream(Tools.GetStreamFromString("{x: 1, y: 2, z: 3}"));
                rS1.Save();
            }
            finally
            {
                r.ForceDelete();
            }
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidContentException))]
        public void Settings_Duplication()
        {
            // R
            //     (settings)
            //         Settings1.settings
            //         F
            //             Settings1.settings (error: duplicated)

            var r = new SystemFolder(TestRoot) { Name = "R" }; r.Save();
            try
            {
                var rS = new Folder(r) { Name = Settings.SETTINGSCONTAINERNAME }; rS.Save();
                var rSf = new Folder(rS) { Name = "F" }; rSf.Save();

                var rS1 = new Settings(rS) { Name = "Settings1.settings" };
                rS1.Binary.SetStream(Tools.GetStreamFromString("{x: 1, y: 2, z: 3}"));
                rS1.Save();

                var rSf1 = new Settings(rS) { Name = "Settings1.settings" };
                rSf1.Binary.SetStream(Tools.GetStreamFromString("{x: 1, y: 2, z: 3}"));
                rSf1.Save();
            }
            finally
            {
                r.ForceDelete();
            }
        }

        [TestMethod]
        public void Settings_NestedObject()
        {
            ContentTypeInstaller.InstallContentType(NotLoadableAspect.CTD);
            var z = new NotLoadableAspect(Repository.AspectsFolder) { Name = "z" };
            z.Save();

            // R
            //     (settings)
            //         Settings1.settings
            //     C
            var n = Node.LoadNode(TestRoot.Path + "/R");
            if (n != null)
                n.ForceDelete();

            var x = 42m;
            var y = "'aa'";
            var a = 44m;
            var b = "'bb'";
            var r = new SystemFolder(TestRoot) { Name = "R" }; r.Save();
            try
            {
                var rS = new Folder(r) { Name = Settings.SETTINGSCONTAINERNAME }; rS.Save();
                var rSf = new Folder(rS) { Name = "F" }; rSf.Save();

                var rS1 = new Settings(rS) { Name = "Settings1.settings" };
                rS1.Binary.SetStream(Tools.GetStreamFromString("{x: " + x + ", y: " + y + ", z: {a:" + a + ", b: " + b + "}}"));
                rS1.Binary.FileName = rS1.Name;
                rS1.Save();

                var rc = Content.CreateNew("Car", r, null);
                rc.Save();

                Assert.AreEqual(x, Settings.GetValue<int>("Settings1", "x", rc.Path));
                Assert.AreEqual(y.Trim('\''), Settings.GetValue<string>("Settings1", "y", rc.Path));
                Assert.AreEqual(a, Settings.GetValue<int>("Settings1", "z.a", rc.Path));
                Assert.AreEqual(b.Trim('\''), Settings.GetValue<string>("Settings1", "z.b", rc.Path));

                var settingsContent = Content.Load(rS1.Id);

                Assert.AreEqual(x, (decimal)settingsContent["x"]);
                Assert.AreEqual(y.Trim('\''), (string)settingsContent["y"]);
                Assert.AreEqual(a, (decimal)settingsContent["z.a"]);
                Assert.AreEqual(b.Trim('\''), (string)settingsContent["z.b"]);

                // different internal content creation
                rS1 = (Settings)Node.LoadNode(rS1.Id);
                settingsContent = Content.Create(rS1);

                Assert.AreEqual(x, (decimal)settingsContent["x"]);
                Assert.AreEqual(y.Trim('\''), (string)settingsContent["y"]);
                Assert.AreEqual(a, (decimal)settingsContent["z.a"]);
                Assert.AreEqual(b.Trim('\''), (string)settingsContent["z.b"]);
            }
            finally
            {
                r.ForceDelete();
                z.ForceDelete();
            }
        }
    }
}
