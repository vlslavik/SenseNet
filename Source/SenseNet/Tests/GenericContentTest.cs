using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Portal;
using SenseNet.ContentRepository.Storage.Data;
using System.IO;

namespace SenseNet.ContentRepository.Tests
{
	/// <summary>
	///This is a test class for SenseNet.ContentRepository.GenericContent and is intended
	///to contain all SenseNet.ContentRepository.GenericContent Unit Tests
	///</summary>
	[TestClass()]
    public class GenericContentTest : TestBase
	{
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
		//Use ClassInitialize to run code before rucming the first test in the class
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
		//Use TestInitialize to run code before rucming each test
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


		private static string _testRootName = "_GenericContentTests";
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


		//-------------------------------------------------------------------- Save ---------------------

		[TestMethod()]
		public void GenericContent_Save_VersionModeNone_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.False;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.AreEqual(vn, test.Version, "#1");
			Assert.AreEqual(test.Version.Status, VersionStatus.Approved, "#2");
			Assert.AreNotEqual(cm, test.CustomMeta, "#3");
		}

		[TestMethod()]
		public void GenericContent_Save_VersionModeMajorOnly_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.False;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#3");
			Assert.AreNotEqual(cm, test.CustomMeta, "#4");
		}

		[TestMethod()]
		public void GenericContent_Save_VersionModeMajorAndMinor_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.False;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Draft, "#2");
			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == vn.Minor + 1, "#3");
			Assert.AreNotEqual(cm, test.CustomMeta, "#4");
		}

		[TestMethod()]
		public void GenericContent_Save_VersionModeInherited_ApprovingFalse_Test()
		{
			Page parent = CreatePage("GCSaveTestParent");
			parent.InheritableVersioningMode = InheritableVersioningType.MajorOnly;
			parent.Save();

			Page test = CreatePage("GCSaveTest", parent);

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.Inherited;
			test.ApprovingMode = ApprovingType.False;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#3");
			Assert.AreNotEqual(cm, test.CustomMeta, "#4");
		}

		[TestMethod()]
		public void GenericContent_Save_VersionModeNone_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#3");
			Assert.AreNotEqual(cm, test.CustomMeta, "#4");
		}

		[TestMethod()]
		public void GenericContent_Save_VersionModeMajorOnly_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#3");
			Assert.AreNotEqual(cm, test.CustomMeta, "#4");
		}

		[TestMethod()]
		public void GenericContent_Save_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Draft, "#2");
			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == vn.Minor + 1, "#3");
			Assert.AreNotEqual(cm, test.CustomMeta, "#4");
		}

		[TestMethod()]
		public void GenericContent_Save_VersionModeMajorOnly_ApprovingInherited_Test()
		{
			Page parent = CreatePage("GCSaveTestParent");
			parent.InheritableApprovingMode = ApprovingType.True;
			parent.Save();

			Page test = CreatePage("GCSaveTest", parent);

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.Inherited;

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Save();

			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#3");
			Assert.AreNotEqual(cm, test.CustomMeta, "#4");
		}

        [TestMethod()]
        public void GenericContent_SaveWithLock_VersionModeNone_ApprovingFalse_Test()
        {
            var textContent1 = "abc";
            var test = CreateFile(binaryText: textContent1, save: false);

            test.VersioningMode = VersioningType.None;
            test.ApprovingMode = ApprovingType.False;

            //save new node as locked
            test.Save(SavingMode.KeepVersionAndLock);

            var testId = test.Id;

            test = Node.Load<File>(testId);
            var actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after locked save.");
            Assert.AreEqual("V1.0.L", test.Version.ToString(), "Version number is not correct after locked save.");
            Assert.AreEqual(1, test.Versions.Count(), "Version count is not correct after locked save.");

            test.CheckIn();
            test = Node.Load<File>(testId);
            actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after checkin.");
            Assert.AreEqual("V1.0.A", test.Version.ToString(), "Version number is not correct after checkin.");
            Assert.AreEqual(1, test.Versions.Count(), "Version count is not correct after checkin.");
        }

        [TestMethod()]
        public void GenericContent_SaveWithLock_VersionModeMajorAndMinor_ApprovingTrue_Test()
        {
            var textContent1 = "abc";
            var test = CreateFile(binaryText: textContent1, save: false);

            test.VersioningMode = VersioningType.MajorAndMinor;
            test.ApprovingMode = ApprovingType.True;

            //save new node as locked
            test.Save(SavingMode.KeepVersionAndLock);

            var testId = test.Id;

            test = Node.Load<File>(testId);
            var actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after locked save.");
            Assert.AreEqual("V0.1.L", test.Version.ToString(), "Version number is not correct after locked save.");
            Assert.AreEqual(1, test.Versions.Count(), "Version count is not correct after locked save.");

            test.CheckIn();
            test = Node.Load<File>(testId);
            actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after checkin.");
            Assert.AreEqual("V0.1.D", test.Version.ToString(), "Version number is not correct after checkin.");
            Assert.AreEqual(1, test.Versions.Count(), "Version count is not correct after checkin.");

            test.Publish();
            test = Node.Load<File>(testId);
            actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after publish.");
            Assert.AreEqual("V0.1.P", test.Version.ToString(), "Version number is not correct after publish.");
            Assert.AreEqual(1, test.Versions.Count(), "Version count is not correct after publish.");
        }

        [TestMethod()]
        public void GenericContent_SaveWithLock_VersionModeMajorAndMinor_ApprovingFalse_Test()
        {
            var textContent1 = "abc";
            var test = CreateFile(binaryText: textContent1, save: false);

            test.VersioningMode = VersioningType.MajorAndMinor;
            test.ApprovingMode = ApprovingType.False;

            //save new node as locked
            test.Save(SavingMode.KeepVersionAndLock);

            var testId = test.Id;

            test = Node.Load<File>(testId);
            var actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after locked save.");
            Assert.AreEqual("V0.1.L", test.Version.ToString(), "Version number is not correct after locked save.");
            Assert.AreEqual(1, test.Versions.Count(), "Version count is not correct after locked save.");

            test.CheckIn();
            test = Node.Load<File>(testId);
            actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after checkin.");
            Assert.AreEqual("V0.1.D", test.Version.ToString(), "Version number is not correct after checkin.");
            Assert.AreEqual(1, test.Versions.Count(), "Version count is not correct after checkin.");
        }

        [TestMethod()]
        public void GenericContent_SaveWithLock_Exception_Test()
        {
            //============ Creating a test user
            var testUser = new User(User.Administrator.Parent)
            {
                Name = "UserFor_GenericContent_SaveWithLock_Exception_Test",
                Email = "userfor_genericcontent_savewithlock_exception_test@example.com",
                Enabled = true
            };
            testUser.Save();
            testUser = Node.Load<User>(testUser.Id);
            File testFile = null;
            var origUser = User.Current;
            try
            {
                try
                {
                    //============ Orig user creates a file
                    testFile = CreateFile(save: true);
                    testFile.Security.GetAclEditor()
                        .SetPermission(testUser, true, PermissionType.Save, ContentRepository.Storage.Security.PermissionValue.Allow)
                        .Apply();

                    //============ Orig user modifies and locks the file
                    testFile.Index++;
                    testFile.Save(SavingMode.KeepVersionAndLock); //(must work properly)

                    //============ Orig user makes free thr file
                    testFile.CheckIn();

                    //============ Test user locks the file
                    User.Current = testUser;
                    testFile.CheckOut();

                    //============ Test user tries to lock the file
                    User.Current = origUser;
                    testFile = Node.Load<File>(testFile.Id);
                    testFile.Index++;
                    try
                    {
                        testFile.Save(SavingMode.KeepVersionAndLock);

                        //============ forbidden code branch
                        Assert.Fail("InvalidContentActionException was not thrown");
                    }
                    catch (InvalidContentActionException)
                    {
                    }
                }
                finally
                {
                    //============ restoring content state
                    if (testFile.Locked)
                    {
                        User.Current = testFile.LockedBy;
                        testFile.CheckIn();
                    }
                }
            }
            finally
            {
                //============ restoring orig user
                User.Current = origUser;
            }
        }

        [TestMethod()]
        public void GenericContent_SaveWithLock_RestorePreviousVersion_Test()
        {
            var textContent1 = "abc";
            var test = CreateFile(save: true, binaryText: textContent1);
            var versionId = test.VersionId;

            var bd = new BinaryData { FileName = test.Binary.FileName };
            bd.SetStream(Tools.GetStreamFromString("def"));
            
            //save new binary
            test.Binary = bd;
            test.VersioningMode = VersioningType.MajorAndMinor;
            test.Save();

            var actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreNotEqual(textContent1, actualContent, "Binary text content has not changed!");

            test.CheckOut();
            test.CheckIn();

            //lock by the current user
            test.CheckOut();

            var lockedVersion = test.Version;

            //restore original content
            test = Node.LoadNodeByVersionId(versionId) as File;
            test.Save(SavingMode.KeepVersionAndLock);

            actualContent = Tools.GetStreamString(test.Binary.GetStream());

            Assert.AreEqual(textContent1, actualContent, "Binary text content is not correct after restore.");
            Assert.IsTrue(test.Locked, "File is not locked after restore.");
            Assert.IsTrue(test.IsLatestVersion, "File version is not correct after restore.");
            Assert.AreEqual(lockedVersion.ToString(), test.Version.ToString(), "File version is not correct after restore.");
        }

        [TestMethod()]
        public void GenericContent_SaveWithLock_Locked_Test()
        {
            var test = CreateFile(save: false);

            test.Save(SavingMode.KeepVersionAndLock);

            Assert.AreEqual(User.Current.Id, test.LockedById, "File is not locked for the current user.");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidContentActionException))]
        public void GenericContent_SaveWithLock_Undo_Test()
        {
            var test = CreateFile(save: false);

            test.Save(SavingMode.KeepVersionAndLock);

            test.UndoCheckOut();
        }
        
	    //-------------------------------------------------------------------- SaveSameVersion ----------

		[TestMethod()]
		public void GenericContent_SaveSameVersion_VersionModeNone_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.False;

            test.Save(SavingMode.KeepVersion);

			Assert.AreEqual(vn, test.Version, "#1");
			Assert.AreEqual(test.Version.Status, VersionStatus.Approved, "#2");
		}

		[TestMethod()]
		public void GenericContent_SaveSameVersion_VersionModeMajorOnly_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.False;

            test.Save(SavingMode.KeepVersion);

			Assert.AreEqual(vn, test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
		}

		[TestMethod()]
		public void GenericContent_SaveSameVersion_VersionModeMajorAndMinor_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.False;

            test.Save(SavingMode.KeepVersion);

			Assert.AreEqual(vn, test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
		}

		[TestMethod()]
		public void GenericContent_SaveSameVersion_VersionModeNone_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

            test.Save(SavingMode.KeepVersion);

			Assert.AreEqual(vn, test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
		}

		[TestMethod()]
		public void GenericContent_SaveSameVersion_VersionModeMajorOnly_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

            test.Save(SavingMode.KeepVersion);
			
			Assert.AreEqual(vn, test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
		}

		[TestMethod()]
		public void GenericContent_SaveSameVersion_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

            test.Save(SavingMode.KeepVersion);

			Assert.AreEqual(vn, test.Version, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
		}

        //-------------------------------------------------------------------- Start multistep save -----------------

        [TestMethod()]
        public void GenericContent_MultistepSave_VersionModeMajorAndMinor_ApprovingFalse_Test()
        {
            Page test = CreatePage("GCSaveTest");

            var vn = test.Version;

            test.VersioningMode = VersioningType.MajorAndMinor;
            test.ApprovingMode = ApprovingType.False;

            test.CustomMeta = Guid.NewGuid().ToString();

            test.Save(SavingMode.StartMultistepSave);

            Assert.IsTrue(vn < test.Version, "#1 version hasn't been raised.");
            Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#2 status is not locked");
            Assert.AreEqual(ContentSavingState.Modifying, test.SavingState, string.Format("#3 saving state is incorrect."));

            test.FinalizeContent();

            Assert.IsTrue(test.Version.Status == VersionStatus.Draft, "#4 status is not correct");
            Assert.AreEqual(ContentSavingState.Finalized, test.SavingState, string.Format("#5 saving state is incorrect."));

            test.CheckOut();

            vn = test.Version;

            test.Save(SavingMode.StartMultistepSave);

            Assert.AreEqual(vn, test.Version, "#6 version is not correct");
            Assert.AreEqual(ContentSavingState.ModifyingLocked, test.SavingState, string.Format("#7 saving state is incorrect."));

            test.FinalizeContent();

            Assert.AreEqual(vn, test.Version, "#8 version is not correct");
            Assert.AreEqual(ContentSavingState.Finalized, test.SavingState, string.Format("#9 saving state is incorrect."));
        }

        //[TestMethod]
        public void x()
        {
            var file = new File(TestRoot) { Name = Guid.NewGuid().ToString() };
            file.Binary.SetStream(Tools.GetStreamFromString("Test data" + file.Name));

            var savingAction = SavingAction.Create(file);
            savingAction.MultistepSaving = true;
            savingAction.SaveAndLock();
            savingAction.Execute();

            var fileId = file.Id;
            var fileVersionId = file.VersionId;

            //---- web request #1
            var pctx = TestTools.CreatePortalContext("/file/BinaryHandler.ashx", "propertyname=Binary&nodeid=" + fileId);
            var hctx = pctx.OwnerHttpContext;
            var bh = (System.Web.IHttpHandler)(new SenseNet.Portal.Handlers.BinaryHandler());
            bh.ProcessRequest(hctx);

            Assert.AreEqual(404, hctx.Response.StatusCode);

            file.FinalizeContent();

            //---- web request #2
            pctx = TestTools.CreatePortalContext("/file/BinaryHandler.ashx", "propertyname=Binary&nodeid=" + fileId);
            hctx = pctx.OwnerHttpContext;
            bh = (System.Web.IHttpHandler)(new SenseNet.Portal.Handlers.BinaryHandler());
            bh.ProcessRequest(hctx);

            Assert.AreEqual(200, hctx.Response.StatusCode);
        }
        //[TestMethod]
        public void y()
        {
            //---- creating
            var file = new File(TestRoot) { Name = Guid.NewGuid().ToString() };
            file.Binary.SetStream(Tools.GetStreamFromString("Test data. Version 1. " + file.Name));
            file.Save();
            var fileId = file.Id;

            //---- modifying
            file = Node.Load<File>(fileId);
            file.CheckOut();
            file.Binary.SetStream(Tools.GetStreamFromString("Test data. Version 2. " + file.Name));
            var savingAction = SavingAction.Create(file);
            savingAction.MultistepSaving = true;
            savingAction.SaveAndLock();
            savingAction.Execute();

            //---- web request #1
            var sb = new StringBuilder();
            var output = new StringWriter(sb);
            var pctx = TestTools.CreatePortalContext("/BinaryHandler.ashx", "propertyname=Binary&nodeid=" + fileId, output);
            var hctx = pctx.OwnerHttpContext;
            var bh = (System.Web.IHttpHandler)(new SenseNet.Portal.Handlers.BinaryHandler());
            bh.ProcessRequest(hctx);

            Assert.AreEqual(200, hctx.Response.StatusCode);
            Assert.IsTrue(sb.ToString().Contains("Test data. Version 1."));
            Assert.IsFalse(sb.ToString().Contains("Test data. Version 2."));

            file = Node.Load<File>(fileId);
            file.FinalizeContent();

            //---- web request #2
            sb = new StringBuilder();
            output = new StringWriter(sb);
            pctx = TestTools.CreatePortalContext("/BinaryHandler.ashx", "propertyname=Binary&nodeid=" + fileId, output);
            hctx = pctx.OwnerHttpContext;
            bh = (System.Web.IHttpHandler)(new SenseNet.Portal.Handlers.BinaryHandler());
            bh.ProcessRequest(hctx);

            Assert.AreEqual(200, hctx.Response.StatusCode);
            Assert.IsFalse(sb.ToString().Contains("Test data. Version 1."));
            Assert.IsTrue(sb.ToString().Contains("Test data. Version 2."));
        }

		//-------------------------------------------------------------------- CheckOut -----------------

		[TestMethod()]
		public void GenericContent_CheckOut_VersionModeNone_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#2");
			Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#3");
		}

		[TestMethod()]
		public void GenericContent_CheckOut_VersionModeMajorOnly_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#2");
			Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#3");
		}

		[TestMethod()]
		public void GenericContent_CheckOut_VersionModeMajorAndMinor_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor > 0, "#2");
			Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#3");
		}

		[TestMethod()]
		public void GenericContent_CheckOut_VersionModeNone_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#2");
			Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#3");
		}

		[TestMethod()]
		public void GenericContent_CheckOut_VersionModeMajorOnly_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#2");
			Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#3");
		}

		[TestMethod()]
		public void GenericContent_CheckOut_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			Assert.IsTrue(vn < test.Version, "#1");
			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor > 0, "#2");
			Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#3");
		}

		//-------------------------------------------------------------------- CheckIn ------------------

		[TestMethod()]
		public void GenericContent_CheckIn_VersionModeNone_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");

			List<VersionNumber> vnList = Node.GetVersionNumbers(test.Id);

			Assert.IsTrue(vnList.Count == 1, "#4");
		}

		[TestMethod()]
		public void GenericContent_CheckIn_VersionModeMajorOnly_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");

			List<VersionNumber> vnList = Node.GetVersionNumbers(test.Id);

			Assert.IsTrue(vnList.Count > 1, "#4");
		}

		[TestMethod()]
		public void GenericContent_CheckIn_VersionModeMajorAndMinor_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor > 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Draft, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");

			List<VersionNumber> vnList = Node.GetVersionNumbers(test.Id);

			Assert.IsTrue(vnList.Count > 1, "#4");
		}

		[TestMethod()]
		public void GenericContent_CheckIn_VersionModeNone_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");

			List<VersionNumber> vnList = Node.GetVersionNumbers(test.Id);

			Assert.IsTrue(vnList.Count > 1, "#4");
		}

		[TestMethod()]
		public void GenericContent_CheckIn_VersionModeMajorOnly_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");

			List<VersionNumber> vnList = Node.GetVersionNumbers(test.Id);

			Assert.IsTrue(vnList.Count > 1, "#4");
		}

		[TestMethod()]
		public void GenericContent_CheckIn_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor > 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Draft, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");

			List<VersionNumber> vnList = Node.GetVersionNumbers(test.Id);

			Assert.IsTrue(vnList.Count > 1, "#4");
		}

		//-------------------------------------------------------------------- UndoCheckOut -------------
		
		[TestMethod()]
		public void GenericContent_UndoCheckOut_VersionModeNone_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.UndoCheckOut();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreEqual(test.CustomMeta, cm, "#3");

			List<VersionNumber> vnList = Node.GetVersionNumbers(test.Id);

			Assert.IsTrue(vnList.Count == 1, "#4");
		}

		[TestMethod()]
		public void GenericContent_UndoCheckOut_VersionModeMajorOnly_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.UndoCheckOut();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_UndoCheckOut_VersionModeMajorAndMinor_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.UndoCheckOut();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_UndoCheckOut_VersionModeNone_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.UndoCheckOut();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_UndoCheckOut_VersionModeMajorOnly_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.UndoCheckOut();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_UndoCheckOut_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.UndoCheckOut();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreEqual(test.CustomMeta, cm, "#3");
		}

		//-------------------------------------------------------------------- Publish ------------------
		
		[TestMethod()]
        [ExpectedException(typeof(InvalidContentActionException))]
		public void GenericContent_Publish_VersionModeNone_ApprovingFalse_Test()
		{
            //Assert.Inconclusive("Approving off, None: CheckedOut ==> Publish");

			Page test = CreatePage("GCSaveTest");

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

            Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#2");

            //this throws an exception: cannot publish 
            //the content if approving is OFF
			test.Publish();
		}
        [TestMethod()]
        public void GenericContent_Save_CheckOutIn_VersionModeNone_ApprovingFalse_Bug3167()
        {
            var page = CreatePage("GCSaveTest");
            var pageId = page.Id;
            var versionString1 = page.Version.ToString();
            page.Binary.SetStream(Tools.GetStreamFromString("Binary1"));
            page.Binary.FileName = "1.html";
            page.PersonalizationSettings.SetStream(Tools.GetStreamFromString("PersonalizationSettings1"));
            page.PersonalizationSettings.FileName = "PersonalizationSettings";
            page.VersioningMode = VersioningType.None;
            page.ApprovingMode = ApprovingType.False;
            page.Save();

            page = Node.Load<Page>(pageId);
            page.CheckOut();
            var versionString2 = page.Version.ToString();
            page.Binary.SetStream(Tools.GetStreamFromString("Binary2"));
            page.PersonalizationSettings.SetStream(Tools.GetStreamFromString("PersonalizationSettings2"));
            page.Save();

            var versionString3 = page.Version.ToString();

            //page = Node.Load<Page>(pageId);

            page.CheckIn();

            page = Node.Load<Page>(pageId);

            var versionString4 = page.Version.ToString();
            var vnList = Node.GetVersionNumbers(page.Id);
            var binString = Tools.GetStreamString(page.Binary.GetStream());
            var persString = Tools.GetStreamString(page.PersonalizationSettings.GetStream());

            Assert.IsTrue(binString == "Binary2", "#1");
            Assert.IsTrue(page.Version.Major == 1 && page.Version.Minor == 0, "#2");
            Assert.IsTrue(persString == "PersonalizationSettings2", "#3");
            Assert.IsTrue(vnList.Count() == 1, "#3");
            Assert.IsTrue(versionString1 == "V1.0.A", "#4");
            Assert.IsTrue(versionString2 == "V2.0.L", "#5");
            Assert.IsTrue(versionString3 == "V2.0.L", "#6");
            Assert.IsTrue(versionString4 == "V1.0.A", "#7");
        }

		[TestMethod()]
        [ExpectedException(typeof(InvalidContentActionException))]
		public void GenericContent_Publish_VersionModeMajorOnly_ApprovingFalse_Test()
		{
            //Assert.Inconclusive("Approving off, MajorOnly: CheckedOut ==> Publish");

			Page test = CreatePage("GCSaveTest");

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

            Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
            Assert.IsTrue(test.Version.Status == VersionStatus.Locked, "#2");

            //this throws an exception: cannot publish 
            //the content if approving is OFF
			test.Publish();
		}

		[TestMethod()]
		public void GenericContent_Publish_VersionModeMajorAndMinor_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.False;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Publish();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_Checkin_VersionModeNone_ApprovingTrue_Test()
		{
            //Assert.Inconclusive("Approving on, None: CheckedOut ==> Publish");

            var test = CreatePage("GCSaveTest");
			var cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_Checkin_VersionModeMajorOnly_ApprovingTrue_Test()
		{
            //Assert.Inconclusive("Approving on, Major: CheckedOut ==> Publish");
            
            var test = CreatePage("GCSaveTest");

			var vn = test.Version;
			var cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.CheckIn();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_Publish_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

			test.CheckOut();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Publish();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 1, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Pending, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		//-------------------------------------------------------------------- Approve ------------------

		[TestMethod()]
		public void GenericContent_Approve_VersionModeNone_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

			test.Save();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Approve();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_Approve_VersionModeMajorOnly_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

			test.Save();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Approve();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_Approve_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

			test.Save();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Publish();

			test.Approve();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Approved, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		//-------------------------------------------------------------------- Reject -------------------

		[TestMethod()]
		public void GenericContent_Reject_VersionModeNone_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.True;

			test.Save();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Reject();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Rejected, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_Reject_VersionModeMajorOnly_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorOnly;
			test.ApprovingMode = ApprovingType.True;

			test.Save();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Reject();

			Assert.IsTrue(test.Version.Major == 2 && test.Version.Minor == 0, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Rejected, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		[TestMethod()]
		public void GenericContent_Reject_VersionModeMajorAndMinor_ApprovingTrue_Test()
		{
			Page test = CreatePage("GCSaveTest");

			VersionNumber vn = test.Version;
			string cm = test.CustomMeta;

			test.VersioningMode = VersioningType.MajorAndMinor;
			test.ApprovingMode = ApprovingType.True;

			test.Save();

			test.CustomMeta = Guid.NewGuid().ToString();

			test.Publish();

			test.Reject();

			Assert.IsTrue(test.Version.Major == 1 && test.Version.Minor == 1, "#1");
			Assert.IsTrue(test.Version.Status == VersionStatus.Rejected, "#2");
			Assert.AreNotEqual(test.CustomMeta, cm, "#3");
		}

		//-------------------------------------------------------------------- Exception ----------------

		[TestMethod()]
		[ExpectedException(typeof(InvalidContentActionException))]
		public void GenericContent_Exception_VersionModeNone_ApprovingFalse_Test()
		{
			Page test = CreatePage("GCSaveTest");

			test.VersioningMode = VersioningType.None;
			test.ApprovingMode = ApprovingType.False;

			test.Reject();
		}

		//-------------------------------------------------------------------- Others ----------------

        [TestMethod()]
        public void GenericContent_MajorAndMinor_CheckoutSaveCheckin_BinaryData()
        {
            //--------------------------------------------- prepare

            var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            var folder = (Folder)folderContent.ContentHandler;
            folder.InheritableVersioningMode = InheritableVersioningType.MajorAndMinor;
            folder.Save();

            var fileContent = Content.CreateNew("File", folder, null);
            var file = (File)fileContent.ContentHandler;

            var stream = Tools.GetStreamFromString("asdf qwer yxcv");
            var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream.Length };
            binaryData.SetStream(stream);

            file.SetBinary("Binary", binaryData);
            file.Save();

            var fileId = file.Id;

            //--------------------------------------------- operating

            file = Node.Load<File>(fileId);
            file.CheckOut();

            file = Node.Load<File>(fileId);
            file.Binary.SetStream(Tools.GetStreamFromString("asdf qwer yxcv 123"));
            file.Save();

            file = Node.Load<File>(fileId);
            file.CheckIn();

            file = Node.Load<File>(fileId);
            var s = Tools.GetStreamString(file.Binary.GetStream());

            Assert.IsTrue(s == "asdf qwer yxcv 123");
        }

        [TestMethod()]
        public void GenericContent_None_CheckoutSaveCheckin_BinaryData()
        {
            //--------------------------------------------- prepare

            var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            var folder = (Folder)folderContent.ContentHandler;
            folder.InheritableVersioningMode = InheritableVersioningType.None;
            folder.Save();

            var fileContent = Content.CreateNew("File", folder, null);
            var file = (File)fileContent.ContentHandler;

            var stream = Tools.GetStreamFromString("asdf qwer yxcv");
            var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream.Length };
            binaryData.SetStream(stream);

            file.SetBinary("Binary", binaryData);
            file.Save();

            var fileId = file.Id;

            //--------------------------------------------- operating

            file = Node.Load<File>(fileId);
            file.CheckOut();

            file = Node.Load<File>(fileId);
            file.Binary.SetStream(Tools.GetStreamFromString("asdf qwer yxcv 123"));
            file.Save();

            file = Node.Load<File>(fileId);
            file.CheckIn();

            file = Node.Load<File>(fileId);
            var s = Tools.GetStreamString(file.Binary.GetStream());

            Assert.IsTrue(s == "asdf qwer yxcv 123");
        }
        [TestMethod()]
        public void GenericContent_None_CheckoutSaveCheckin_BinaryData_OfficeProtocolBug()
        {
            //--------------------------------------------- prepare

            var folderContent = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            var folder = (Folder)folderContent.ContentHandler;
            folder.InheritableVersioningMode = InheritableVersioningType.None;
            folder.Save();

            var fileContent = Content.CreateNew("File", folder, null);
            var file = (File)fileContent.ContentHandler;

            var stream2 = Tools.GetStreamFromString("asdf qwer yxcv");
            var binaryData = new BinaryData { ContentType = "text/plain", FileName = "1.txt", Size = stream2.Length };
            binaryData.SetStream(stream2);

            file.SetBinary("Binary", binaryData);
            file.Save();

            var fileId = file.Id;

            //--------------------------------------------- operating

            var node = Node.LoadNode(fileId);
            var gc = node as GenericContent;
            gc.CheckOut();

            // save
            var node2 = Node.LoadNode(fileId);
            using (var stream = new System.IO.MemoryStream())
            {
                using (var streamwriter = new System.IO.StreamWriter(stream))
                {
                    streamwriter.Write("asdf qwer yxcv 123");
                    streamwriter.Flush();
                    stream.Seek(0, System.IO.SeekOrigin.Begin);

                    ((BinaryData)node2["Binary"]).SetStream(stream);

                    node2.Save();
                }
            }

            // load
            var node3 = Node.LoadNode(fileId);
            var bdata = (BinaryData)node3["Binary"];
            var expstream = bdata.GetStream();
            Assert.IsTrue(expstream.Length > 0, "stream length is 0");

            var s = Tools.GetStreamString(expstream);
            Assert.IsTrue(s == "asdf qwer yxcv 123", String.Format("content is '{0}'. expected: 'asdf qwer yxcv 123'", s));
        }

        [TestMethod]
        public void LoggedDataProvider_RightWrappingAndRestoring()
        {
            var dataProvider = DataProvider.Current;
            using (var loggedDataProvider = new LoggedDataProvider())
            {
                Assert.ReferenceEquals(loggedDataProvider, DataProvider.Current);
            }
            Assert.ReferenceEquals(dataProvider, DataProvider.Current);
        }

		//-------------------------------------------------------------------- Helper methods -----------

		public Page CreatePage(string name)
		{
			return CreatePage(name, this.TestRoot);
		}

		public static Page CreatePage(string name, Node parent)
		{
			Page page = Node.LoadNode(string.Concat(parent.Path, "/", name)) as Page;
            if (page != null)
                page.ForceDelete();
			page = new Page(parent);
			page.Name = name;
			page.Save();

			return page;
		}

        public File CreateFile(string name = "test.txt", string binaryText = null, bool save = true)
        {
            var fileNode = Node.LoadNode(string.Concat(this.TestRoot.Path, "/", name));
            if (fileNode != null)
                fileNode.ForceDelete();

            var file = new File(this.TestRoot) {Name = name};

            if (string.IsNullOrEmpty(binaryText))
                binaryText = "asd";

            var bd = new BinaryData {FileName = name};
            bd.SetStream(Tools.GetStreamFromString(binaryText));

            file.Binary = bd;

            if (save)
                file.Save();

            return file;
        }
	}
}