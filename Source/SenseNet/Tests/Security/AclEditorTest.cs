using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Tests.Security
{
    [TestClass]
    public class AclEditorTest : TestBase
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
        private static string _testRootName = "_AclEditorTest";
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
        [ExpectedException(typeof(SenseNetSecurityException))]
        public void Login_CannotLoginWithSomebody()
        {
            User.Current = User.Somebody;
        }

        [TestMethod, Description("Bug reproduction.")]
        public void AclEditor_GettingWithoutVisibleModifiedBy()
        {
            var newUser = new User(User.Administrator.Parent)
            {
                Name = "UserFor_AclEditor_GettingWithoutVisibleModifiedBy",
                Email = "userfor_acleditor_gettingwithoutvisiblemodifiedby@example.com",
                Enabled = true
            };
            newUser.Save();
            newUser = Node.Load<User>(newUser.Id);

            var content = Content.CreateNew("Car", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var contentId = content.Id;

            new AclEditor(TestRoot)
                .SetPermission(newUser.Id, true, PermissionType.Open, PermissionValue.Allow)
                .SetPermission(newUser.Id, true, PermissionType.SetPermissions, PermissionValue.Allow)
                .Apply();
            new AclEditor(User.Administrator)
                .SetPermission(newUser.Id, true, PermissionType.See, PermissionValue.Deny)
                .Apply();

            var origuser = User.Current;
            User.Current = newUser;

            content = Content.Load(contentId);
            Assert.AreEqual(User.Somebody.Id, content.ContentHandler.CreatedBy.Id);
            Assert.AreEqual(User.Somebody.Id, content.ContentHandler.ModifiedBy.Id);
            Assert.AreEqual(User.Somebody.Id, content.ContentHandler.VersionCreatedBy.Id);
            Assert.AreEqual(User.Somebody.Id, content.ContentHandler.VersionModifiedBy.Id);

            var ok = false;
            try
            {
                var ed = new AclEditor(Node.LoadNode(content.Id));
                ok = true;
            }
            //catch (Exception e)
            //{
            //    int q = 1;
            //}
            finally
            {
                User.Current = origuser;
            }

            Assert.AreEqual(true, ok);
        }
    }
}
