using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.ContentRepository.Tests.Security
{
    [TestClass]
    public class CodeInjectionTest : TestBase
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
        private static string _testRootName = "_CodeInjectionTest";
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
        public void CodeInjection_LoadUserByDomainAndName()
        {
            // CQL hacks
            //Test(@"BuiltIn"" -Name:x* (""", @""") ""x");
            //Test(@"BuiltIn"" Name:x* (""", @""") ""x");
            Test(@"BuiltIn", @"admin\"" +asdf:qwer");

            // SQL hacks
            Test(@"BuiltIn%') OR (Name != 'asdf", "");
            Test("BuiltIn", "admin' OR Name != 'asdf");
        }

        private void Test(string domain, string name)
        {
            User user;
            //Assert.IsNull(user = User.Load(name), "User: " + user.Username + ". Expected: null");
            Assert.IsNull(user = User.Load(domain, name), "User: " + (user == null ? "null" : user.Username) + ". Expected: null");
            Assert.IsNull(user = User.Load(domain, name, ExecutionHint.ForceIndexedEngine), "User via CQL: " + (user == null ? "null" : user.Username) + ". Expected: null");
            Assert.IsNull(user = User.Load(domain, name, ExecutionHint.ForceRelationalEngine), "User via SQL: " + (user == null ? "null" : user.Username) + ". Expected: null");
        }
    }
}
