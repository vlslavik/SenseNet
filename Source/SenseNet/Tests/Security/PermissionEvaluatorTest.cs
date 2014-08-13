using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Security;
using System.Linq;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using SenseNet.ContentRepository.Security;

namespace SenseNet.ContentRepository.Tests.Security
{
    [TestClass]
    public class PermissionEvaluatorTests : TestBase
    {
        #region test infrastructure
        private class SecurityAccessor : Accessor
        {
            public SecurityAccessor(object target) : base(target) { }

            public bool HasPermission(string path, IUser user, bool isOwner, bool isLastModifier, params PermissionType[] permissionTypes)
            {
                return (bool)CallPrivateMethod("HasPermission", path, user, isOwner, isLastModifier, permissionTypes);
            }
            public bool HasPermission(string path, int userId, bool isOwner, bool isLastModifier, params PermissionType[] permissionTypes)
            {
                return (bool)CallPrivateMethod("HasPermission", path, GetUser(userId), isOwner, isLastModifier, permissionTypes);
            }
            public bool HasSubTreePermission(string path, int userId, bool isOwner, bool isLastModifier, params PermissionType[] permissionTypes)
            {
                return (bool)CallPrivateMethod("HasSubTreePermission", path, GetUser(userId), isOwner, isLastModifier, permissionTypes);
            }
            public PermissionValue GetPermission(string path, int userId, bool isOwner, bool isLastModifier, params PermissionType[] permissionTypes)
            {
                return (PermissionValue)CallPrivateMethod("GetPermission", path, GetUser(userId), isOwner, isLastModifier, permissionTypes);
            }
            public PermissionValue GetSubtreePermission(string path, int userId, bool isOwner, bool isLastModifier, params PermissionType[] permissionTypes)
            {
                return (PermissionValue)CallPrivateMethod("GetSubtreePermission", path, GetUser(userId), isOwner, isLastModifier, permissionTypes);
            }
            public PermissionValue[] GetAllPermissions(string path, int userId, bool isOwner, bool isLastModifier)
            {
                return (PermissionValue[])CallPrivateMethod("GetAllPermissions", path, GetUser(userId), isOwner, isLastModifier);
            }
            public PermittedLevel GetPermittedLevel(string path, int userId, bool isOwner, bool isLastModifier)
            {
                return (PermittedLevel)CallPrivateMethod("GetPermittedLevel", path, GetUser(userId), isOwner, isLastModifier);
            }
            public SecurityEntry[] GetAllEntries(string path)
            {
                return (SecurityEntry[])CallPrivateMethod("GetAllEntries", path);
            }
            public SecurityEntry[] GetExplicitEntries(string path)
            {
                return (SecurityEntry[])CallPrivateMethod("GetExplicitEntries", path);
            }
            public SecurityEntry[] GetEffectiveEntries(string path)
            {
                return (SecurityEntry[])CallPrivateMethod("GetEffectiveEntries", path);
            }
            public SnAccessControlList GetAcl(int nodeId, string path, int ownerId, int lastModifierId)
            {
                return (SnAccessControlList)CallPrivateMethod("GetAcl", nodeId, path, ownerId);
            }

            private IUser GetUser(int userId)
            {
                return new TestUser(userId);
            }
        }
        private class TestUser : IUser
        {
            private int _userId;
            public TestUser(int userId)
            {
                _userId = userId;
            }
            //====================================== IUser Members
            public bool Enabled
            {
                get
                {
                    return true;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
            public string Domain { get { return "TEST"; } }
            public string Email { get; set; }
            public string FullName
            {
                get
                {
                    return Name;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
            public string Password
            {
                set { throw new NotImplementedException(); }
            }
            public string PasswordHash
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
            public string Username
            {
                get { return Domain + "\\" + Name; }
            }
            public bool IsInGroup(IGroup group)
            {
                return PermissionEvaluator.Instance.IsInGroup(this._userId, group.Id);
            }
            public bool IsInOrganizationalUnit(IOrganizationalUnit orgUnit)
            {
                return PermissionEvaluator.Instance.IsInGroup(this._userId, orgUnit.Id);
            }
            public bool IsInContainer(ISecurityContainer container)
            {
                return PermissionEvaluator.Instance.IsInGroup(this._userId, container.Id);
            }
            public MembershipExtension MembershipExtension { get; set; }
            //====================================== ISecurityMember Members
            public int Id
            {
                get { return _userId; }
            }
            public string Path
            {
                get { return "/TestUsers/" + Name; }
            }
            //====================================== IIdentity Members
            public string AuthenticationType
            {
                get { throw new NotImplementedException(); }
            }
            public bool IsAuthenticated
            {
                get { throw new NotImplementedException(); }
            }
            public string Name
            {
                get { return "TestUser" + Id; }
            }
        }

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
        private static string _testRootName = "_InMemoryPermissionTests";
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
            if (Node.Exists("/Root/IMS/BuiltIn/ImpexUser"))
                Node.ForceDelete("/Root/IMS/BuiltIn/ImpexUser");
        }
        #endregion

        // "-/root/folder|+1345__+__|+0450__+__"
        // 1 Admin
        // 6 Visitor
        // 7 Administrators
        // 8 Everyone
        // 9 Creators

        [TestMethod]
        public void Security_OpenLevel_AllowOnly1()
        {
            var security = GetSecurity(@"
                +/root           |+0010__________;
                +/root/f1        |+0010_________+;
                +/root/f1/f2     |+0010_____+++++;
                +/root/f1/f2/f3  |+0010____++++++;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = security.GetPermittedLevel("/root", 11, false, false);
            var levelF1 = security.GetPermittedLevel("/root/f1", 11, false, false);
            var levelF2 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var levelF3 = security.GetPermittedLevel("/root/f1/f2/f3", 11, false, false);
            var levelF4 = security.GetPermittedLevel("/root/f1/f2/f3/f4", 11, false, false);
            Assert.AreEqual(PermittedLevel.None, levelRoot);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF1);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF2);
            Assert.AreEqual(PermittedLevel.All, levelF3);
            Assert.AreEqual(PermittedLevel.All, levelF4);
        }
        [TestMethod]
        public void Security_OpenLevel_AllowOnly2()
        {
            var security = GetSecurity(@"
                +/root           |+0010____++++++;
                +/root/f1        |+0010_____+++++;
                +/root/f1/f2     |+0010_________+;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = security.GetPermittedLevel("/root", 11, false, false);
            var levelF1 = security.GetPermittedLevel("/root/f1", 11, false, false);
            var levelF2 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var levelF3 = security.GetPermittedLevel("/root/f1/f2/f3", 11, false, false);
            Assert.AreEqual(PermittedLevel.All, levelRoot);
            Assert.AreEqual(PermittedLevel.All, levelF1);
            Assert.AreEqual(PermittedLevel.All, levelF2);
            Assert.AreEqual(PermittedLevel.All, levelF3);
        }
        [TestMethod]
        public void Security_AllowDeny1()
        {
            var security = GetSecurity(@"
                +/root           |+0010____-_____;
                +/root/f1        |+0010_________+;
                +/root/f1/f2     |+0010_____+++++;
                +/root/f1/f2/f3  |+0010____++++++;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = security.GetPermittedLevel("/root", 11, false, false);
            var levelF1 = security.GetPermittedLevel("/root/f1", 11, false, false);
            var levelF2 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var levelF3 = security.GetPermittedLevel("/root/f1/f2/f3", 11, false, false);
            var levelF4 = security.GetPermittedLevel("/root/f1/f2/f3/f4", 11, false, false);
            Assert.AreEqual(PermittedLevel.None, levelRoot);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF1);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF2);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF3);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF4);
        }
        [TestMethod]
        public void Security_AllowDeny2()
        {
            var security = GetSecurity(@"
                +/root           |+0010__________
                                 |+0011____--____;
                +/root/f1        |+0010______++++;
                +/root/f1/f2     |+0010_____+++++;
                +/root/f1/f2/f3  |+0010____++++++;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = security.GetPermittedLevel("/root", 11, false, false);
            var levelF1 = security.GetPermittedLevel("/root/f1", 11, false, false);
            var levelF2 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var levelF3 = security.GetPermittedLevel("/root/f1/f2/f3", 11, false, false);
            var levelF4 = security.GetPermittedLevel("/root/f1/f2/f3/f4", 11, false, false);
            Assert.AreEqual(PermittedLevel.None, levelRoot);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF1);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF2);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF3);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF4);
        }
        [TestMethod]
        public void Security_OpenLevel_Break_AllowOnly()
        {
            var security = GetSecurity(@"
                +/root           |+0010____++++++;
                -/root/f1        |+0010_____+++++;
                +/root/f1/f2     |+0010_________+;
                -/root/f1/f2/f3  |+0010_________+;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = security.GetPermittedLevel("/root", 11, false, false);
            var levelF1 = security.GetPermittedLevel("/root/f1", 11, false, false);
            var levelF2 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var levelF3 = security.GetPermittedLevel("/root/f1/f2/f3", 11, false, false);
            var levelF4 = security.GetPermittedLevel("/root/f1/f2/f3/f4", 11, false, false);
            Assert.AreEqual(PermittedLevel.All, levelRoot);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF1);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF2);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF3);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF4);
        }
        [TestMethod]
        public void Security_OpenLevel_Break_AllowDeny()
        {
            var security = GetSecurity(@"
                +/root           |+0010____++++++
                                 |+0011____--____;
                -/root/f1        |+0010____++++++;
                +/root/f1/f2     |+0010____-_____;
                -/root/f1/f2/f3  |+0010____++++++;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = security.GetPermittedLevel("/root", 11, false, false);
            var levelF1 = security.GetPermittedLevel("/root/f1", 11, false, false);
            var levelF2 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var levelF3 = security.GetPermittedLevel("/root/f1/f2/f3", 11, false, false);
            var levelF4 = security.GetPermittedLevel("/root/f1/f2/f3/f4", 11, false, false);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelRoot);
            Assert.AreEqual(PermittedLevel.All, levelF1);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF2);
            Assert.AreEqual(PermittedLevel.All, levelF3);
            Assert.AreEqual(PermittedLevel.All, levelF4);
        }
        [TestMethod]
        public void Security_OpenLevel_OneLevelDeny()
        {
            var security = GetSecurity(@"
                +/root                    |+0010____++___+;
                +/root/f1/f2              |-0010____------;
                +/root/f1/f2/f3/f4        |-0010____-----_;
                +/root/f1/f2/f3/f4/f5/f6  |-0010____-_____;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = security.GetPermittedLevel("/root", 11, false, false);
            var levelF1 = security.GetPermittedLevel("/root/f1", 11, false, false);
            var levelF2 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var levelF3 = security.GetPermittedLevel("/root/f1/f2/f3", 11, false, false);
            var levelF4 = security.GetPermittedLevel("/root/f1/f2/f3/f4", 11, false, false);
            var levelF5 = security.GetPermittedLevel("/root/f1/f2/f3/f4/f5", 11, false, false);
            var levelF6 = security.GetPermittedLevel("/root/f1/f2/f3/f4/f5/f6", 11, false, false);
            var levelF7 = security.GetPermittedLevel("/root/f1/f2/f3/f4/f5/f6/f7", 11, false, false);
            Assert.AreEqual(PermittedLevel.All, levelRoot);
            Assert.AreEqual(PermittedLevel.All, levelF1);
            Assert.AreEqual(PermittedLevel.None, levelF2);
            Assert.AreEqual(PermittedLevel.All, levelF3);
            Assert.AreEqual(PermittedLevel.HeadOnly, levelF4);
            Assert.AreEqual(PermittedLevel.All, levelF5);
            Assert.AreEqual(PermittedLevel.PublicOnly, levelF6);
            Assert.AreEqual(PermittedLevel.All, levelF7);
        }
        [TestMethod]
        public void Security_OpenLevel_Ownership()
        {
            var security = GetSecurity(@"
                +/root                    |+0008_____+++++
                                          |+0009____++++++;
                #
                 1,  7;
                11, 10;
            ");

            var level0 = security.GetPermittedLevel("/root", 11, false, false);
            var level1 = security.GetPermittedLevel("/root/f1/f2", 11, false, false);
            var level2 = security.GetPermittedLevel("/root/f1/f2", 11, true, false);
            Assert.AreEqual(PermittedLevel.PublicOnly, level0);
            Assert.AreEqual(PermittedLevel.PublicOnly, level1);
            Assert.AreEqual(PermittedLevel.All, level2);
        }

        [TestMethod]
        public void Security_GetAllPermissions()
        {
            var security = GetSecurity(@"
                +/root           |+0010____-_____;
                +/root/f1        |+0010_________+;
                +/root/f1/f2     |+0010_____+++++;
                +/root/f1/f2/f3  |+0010____++++++;
                #
                 1,  7;
                11, 10;
            ");

            var levelRoot = PermissionsToString(security.GetAllPermissions("/root", 11, false, false));
            var levelF1 = PermissionsToString(security.GetAllPermissions("/root/f1", 11, false, false));
            var levelF2 = PermissionsToString(security.GetAllPermissions("/root/f1/f2", 11, false, false));
            var levelF3 = PermissionsToString(security.GetAllPermissions("/root/f1/f2/f3", 11, false, false));
            var levelF4 = PermissionsToString(security.GetAllPermissions("/root/f1/f2/f3/f4", 11, false, false));

            Assert.AreEqual("__________________________-_____", levelRoot);
            Assert.AreEqual("__________________________-____+", levelF1);
            Assert.AreEqual("__________________________-+++++", levelF2);
            Assert.AreEqual("__________________________-+++++", levelF3);
            Assert.AreEqual("__________________________-+++++", levelF4);
        }
        [TestMethod]
        public void Security_GetPermissionAndHasPermission()
        {
            var security = GetSecurity(
                //-- PermissionType.Id: 987654321
              @"-/root           |+0010_____+_______
                                 |+0009____+________;
                +/root/f1        |+0010__+_________+;
                +/root/f1/f2     |+0010___-____+++++;
                +/root/f1/f2/f3  |+0010_______++++++;
                #
                 1,  7;
                11, 10;
            ");

            var path = "/root/f1/f2/f3/f4";

            var perm1 = security.GetPermission(path, 11, false, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 8, 5 }));
            var perm2 = security.GetPermission(path, 11, false, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 7, 5 }));
            var perm3 = security.GetPermission(path, 11, false, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 6, 5 }));
            var perm4 = security.GetPermission(path, 11, true, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 6, 5 }));
            var has1 = security.HasPermission(path, 11, false, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 8, 5 }));
            var has2 = security.HasPermission(path, 11, false, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 7, 5 }));
            var has3 = security.HasPermission(path, 11, false, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 6, 5 }));
            var has4 = security.HasPermission(path, 11, true, false, GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 6, 5 }));

            Assert.IsTrue(perm1 == PermissionValue.Allow, "#1");
            Assert.IsTrue(perm2 == PermissionValue.Deny, "#2");
            Assert.IsTrue(perm3 == PermissionValue.NonDefined, "#3");
            Assert.IsTrue(perm4 == PermissionValue.Allow, "#4");
            Assert.IsTrue(has1, "#5");
            Assert.IsTrue(!has2, "#6");
            Assert.IsTrue(!has3, "#7");
            Assert.IsTrue(has4, "#8");
        }
        [TestMethod]
        public void Security_SubTree_GetPermissionAndHasPermission()
        {
            var security = GetSecurity(
                //-- PermissionType.Index:     10987654321
              @"-/root                 |+0001__+++++++++++
                                       |+0007__+++++++++++
                                       |+0006________+++++
                                       |+0008________+++++;
                +/root/users           |-0008___+_________
                                       |+0009___+__+++++++;
                +/root/users/u1/xx/aa  |+0012_______++++++;
                +/root/users/u1/xx/bb  |+0012_______++++++;
                -/root/users/u2/xx     |+0001__+++++++++++
                                       |+0007__+++++++++++
                                       |+0008____________+
                                       |+0010___+__+++++++;
                +/root/users/u2/xx/aa  |+0011______---____;
                +/root/users/u2/xx/bb  |+0011_______++++++;
                #
                 1,  7;
                11, 10;
                12, 10;
            ");

            var permset = GetPermissionTypeFromIdArray(new int[] { 1, 2, 3, 4, 7 });

            var perms1 = PermissionsToString(security.GetAllPermissions("/root/users", 11, false, false));
            var perms2 = PermissionsToString(security.GetAllPermissions("/root/users/u1", 11, true, false));
            var perms3 = PermissionsToString(security.GetAllPermissions("/root/users/u2", 11, false, false));
            var perms4 = PermissionsToString(security.GetAllPermissions("/root/users", 12, false, false));
            var perms5 = PermissionsToString(security.GetAllPermissions("/root/users/u1", 12, true, false));
            var perms6 = PermissionsToString(security.GetAllPermissions("/root/users/u2", 12, false, false));
            var perms7 = PermissionsToString(security.GetAllPermissions("/root/users/u2/xx", 11, false, false));

            var perm1 = security.GetSubtreePermission("/root/users", 11, false, false, permset);
            var perm2 = security.GetSubtreePermission("/root/users/u1", 11, true, false, permset);
            var perm3 = security.GetSubtreePermission("/root/users/u2", 11, false, false, permset);
            var perm4 = security.GetSubtreePermission("/root/users", 12, false, false, permset);
            var perm5 = security.GetSubtreePermission("/root/users/u1", 12, false, false, permset);
            var perm6 = security.GetSubtreePermission("/root/users/u2", 12, true, false, permset);
            var perm7 = security.GetSubtreePermission("/root/users/u2/xx", 11, false, false, permset);

            var has1 = security.HasSubTreePermission("/root/users", 11, false, false, permset);
            var has2 = security.HasSubTreePermission("/root/users/u1", 11, true, false, permset);
            var has3 = security.HasSubTreePermission("/root/users/u2", 11, false, false, permset);
            var has4 = security.HasSubTreePermission("/root/users", 12, false, false, permset);
            var has5 = security.HasSubTreePermission("/root/users/u1", 12, false, false, permset);
            var has6 = security.HasSubTreePermission("/root/users/u2", 12, true, false, permset);
            var has7 = security.HasSubTreePermission("/root/users/u2/xx", 11, false, false, permset);

            Assert.IsTrue(perms1 == "______________________+____+++++", "GetAll#1");
            Assert.IsTrue(perms2 == "______________________+__+++++++", "GetAll#2");
            Assert.IsTrue(perms3 == "___________________________+++++", "GetAll#3");
            Assert.IsTrue(perms4 == "______________________+____+++++", "GetAll#4");
            Assert.IsTrue(perms5 == "______________________+__+++++++", "GetAll#5");
            Assert.IsTrue(perms6 == "___________________________+++++", "GetAll#6");
            Assert.IsTrue(perms7 == "______________________+__+++++++", "GetAll#7");

            Assert.IsTrue(perm1 == PermissionValue.NonDefined, "Get#1" + perm1);
            Assert.IsTrue(perm2 == PermissionValue.Allow, "Get#2" + perm2);
            Assert.IsTrue(perm3 == PermissionValue.NonDefined, "Get#3: " + perm3);
            Assert.IsTrue(perm4 == PermissionValue.NonDefined, "Get#4" + perm4);
            Assert.IsTrue(perm5 == PermissionValue.NonDefined, "Get#5" + perm5);
            Assert.IsTrue(perm6 == PermissionValue.Allow, "Get#6" + perm6);
            Assert.IsTrue(perm7 == PermissionValue.Deny, "Get#7" + perm6);

            Assert.IsTrue(!has1, "Has#1");
            Assert.IsTrue(has2, "Has#2");
            Assert.IsTrue(!has3, "Has#3");
            Assert.IsTrue(!has4, "Has#4");
            Assert.IsTrue(!has5, "Has#5");
            Assert.IsTrue(has6, "Has#6");
            Assert.IsTrue(!has7, "Has#6");
        }

        [TestMethod]
        public void Security_GetAllEntries()
        {
            var security = GetSecurity(
                //-- PermissionType.Id:       987654321
                @"-01/root               |+0001_______+++++++++++
                                         |+0007_______+++++++++++
                                         |+0006_____________+++++
                                         |+0008_____________+++++;
                +02/root/users           |-0008________+_________
                                         |+0009________+__+++++++;
                +03/root/users/u1/xx/aa  |+0012____________++++++;
                +04/root/users/u1/xx/bb  |+0012____________++++++;
                -05/root/users/u2/xx     |+0001_______+++++++++++
                                         |+0007_______+++++++++++
                                         |+0008_________________+
                                         |+0010________+__+++++++;
                +06/root/users/u2/xx/aa  |+0011-__________---____;
                +07/root/users/u2/xx/bb  |+0011____________++++++;
                #
                 1,  7;
                11, 10;
                12, 10;
            ");

            var expectedEntriesString = @"
                DefinedOn=6, Principal=11, Propagates=true, Values=______________-__________---____
                DefinedOn=5, Principal=1, Propagates=true, Values=_____________________+++++++++++
                DefinedOn=5, Principal=7, Propagates=true, Values=_____________________+++++++++++
                DefinedOn=5, Principal=8, Propagates=true, Values=_______________________________+
                DefinedOn=5, Principal=10, Propagates=true, Values=______________________+__+++++++
                ".Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var entriesString = GetEntriesToString(security.GetAllEntries("/root/users/u2/xx/aa"));
            var ok = entriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "") == expectedEntriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            Assert.IsTrue(ok);
        }
        [TestMethod]
        public void Security_GetExplicitEntries()
        {
            var security = GetSecurity(
                //-- PermissionType.Id:       987654321
                @"-01/root               |+0001_______++++++++
                                         |+0007_______++++++++
                                         |+0006_____________++
                                         |+0008_____________++;
                +02/root/users           |-0008________+______
                                         |+0009________+__++++;
                +03/root/users/u1/xx/aa  |+0012____________+++;
                +04/root/users/u1/xx/bb  |+0012____________+++;
                -05/root/users/u2/xx     |+0001_______++++++++
                                         |+0007_______++++++++
                                         |+0008______________+
                                         |+0010________+__++++;
                +06/root/users/u2/xx/aa  |+0011-__________---_;
                +07/root/users/u2/xx/bb  |+0011____________+++;
                #
                 1,  7;
                11, 10;
                12, 10;
            ");

            var expectedEntriesString = @"
                DefinedOn=6, Principal=11, Propagates=true, Values=_________________-__________---_
                ".Replace("\r", "").Replace("\n", "").Replace(" ", "");

            var entriesString = GetEntriesToString(security.GetExplicitEntries("/root/users/u2/xx/aa"));
            var ok = entriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "") == expectedEntriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            Assert.IsTrue(ok);
        }
        [TestMethod]
        public void Security_GetEffectiveEntries()
        {
            var security = GetSecurity(
                //-- PermissionType.Id:         987654321
                @"-01/root               |+0001__++++++++
                                         |+0007__++++++++
                                         |+0006________++
                                         |+0008________++;
                +02/root/users           |-0008___+______
                                         |+0009___+__++++;
                +03/root/users/u1/xx/aa  |+0012_______+++;
                +04/root/users/u1/xx/bb  |+0012_______+++;
                -05/root/users/u2/xx     |+0001__++++++++
                                         |+0007__++++++++
                                         |+0008_________+
                                         |+0010___+__++++;
                +06/root/users/u2/xx/aa  |+0008____++__+_
                                         |+0011______---_;
                +07/root/users/u2/xx/bb  |+0011_______+++;
                #
                 1,  7;
                11, 10;
                12, 10;
            ");

            //-- PermissionType.Id:                                     987654321
            var expectedEntriesString = @"
                DefinedOn=3, Principal=12, Propagates=true, Values=_____________________________+++
                DefinedOn=3, Principal= 8, Propagates=true, Values=______________________________++
                DefinedOn=3, Principal= 9, Propagates=true, Values=_________________________+__++++
                DefinedOn=3, Principal= 1, Propagates=true, Values=________________________++++++++
                DefinedOn=3, Principal= 7, Propagates=true, Values=________________________++++++++
                DefinedOn=3, Principal= 6, Propagates=true, Values=______________________________++
                ".Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var entriesString = GetEntriesToString(security.GetEffectiveEntries("/root/users/u1/xx/aa"));
            var ok = entriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "") == expectedEntriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            Assert.IsTrue(ok);
        }
        [TestMethod]
        public void Security_GetEffectiveEntries_EntryAggregation()
        {
            var security = GetSecurity(
                //-- PermissionType.Id:         987654321
                @"-01/root               |+0008________++;
                +02/root/users           |+0008___+______;
                +03/root/users/u1        |+0008_______+++;
                +04/root/users/u1/aa     |+0010_________+;
                #
                 1,  7;
                11, 10;
                12, 10;
            ");


            //-- PermissionType.Id:                                     987654321
            var expectedEntriesString = @"
                DefinedOn=4, Principal=10, Propagates=true, Values=_______________________________+
                DefinedOn=4, Principal=8, Propagates=true, Values=_________________________+___+++
                ".Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var entriesString = GetEntriesToString(security.GetEffectiveEntries("/root/users/u1/aa"));
            var ok = entriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "") == expectedEntriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            Assert.IsTrue(ok);
        }
        [TestMethod]
        public void Security_GetEffectiveEntries_Propagation()
        {
            var security = GetSecurity(
                //-- PermissionType.Id:         987654321
                @"-01/root               |+0001__++++++++
                                         |+0007__++++++++
                                         |+0006________++
                                         |+0008________++;
                +02/root/users           |-0008___+______
                                         |+0009___+__++++;
                +03/root/users/u1/xx/aa  |+0012_______+++;
                +04/root/users/u1/xx/bb  |+0012_______+++;
                -05/root/users/u2/xx     |+0001__++++++++
                                         |+0007__++++++++
                                         |+0008_________+
                                         |+0010___+__++++;
                +06/root/users/u2/xx/aa  |+0008____++__+_
                                         |+0011______---_;
                +07/root/users/u2/xx/bb  |+0011_______+++;
                #
                 1,  7;
                11, 10;
                12, 10;
            ");


            //-- PermissionType.Id:                                     987654321
            var expectedEntriesString1 = @"
                DefinedOn=2, Principal=8, Propagates= true, Values=_________________________+____++
                DefinedOn=2, Principal=9, Propagates= true, Values=_________________________+__++++
                DefinedOn=2, Principal=1, Propagates= true, Values=________________________++++++++
                DefinedOn=2, Principal=7, Propagates= true, Values=________________________++++++++
                DefinedOn=2, Principal=6, Propagates= true, Values=______________________________++
                ".Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var entriesString1 = GetEntriesToString(security.GetEffectiveEntries("/root/users"));
            var ok1 = entriesString1.Replace("\r", "").Replace("\n", "").Replace(" ", "") == expectedEntriesString1.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            //-- PermissionType.Id:                                     987654321
            var expectedEntriesString2 = @"
                DefinedOn=2, Principal=8, Propagates= true, Values=______________________________++
                DefinedOn=2, Principal=9, Propagates= true, Values=_________________________+__++++
                DefinedOn=2, Principal=1, Propagates= true, Values=________________________++++++++
                DefinedOn=2, Principal=7, Propagates= true, Values=________________________++++++++
                DefinedOn=2, Principal=6, Propagates= true, Values=______________________________++
                ".Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var entriesString2 = GetEntriesToString(security.GetEffectiveEntries("/root/users/u3"));
            var ok2 = entriesString2.Replace("\r", "").Replace("\n", "").Replace(" ", "") == expectedEntriesString2.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            Assert.IsTrue(ok1, "#1");
            Assert.IsTrue(ok2, "#2");
        }
        [TestMethod]
        public void Security_GetEffectiveEntries_ThroughBreak()
        {
            var security = GetSecurity(
                //-- PermissionType.Id:         987654321
                @"-01/root               |+0001_______++++++++
                                         |+0007_______++++++++
                                         |+0006_____________++
                                         |+0008_____________++;
                +02/root/users           |-0008________+______
                                         |+0009________+__++++;
                +03/root/users/u1/xx/aa  |+0012____________+++;
                +04/root/users/u1/xx/bb  |+0012____________+++;
                -05/root/users/u2/xx     |+0001_______++++++++
                                         |+0007_______++++++++
                                         |+0008______________+
                                         |+0010________+__++++;
                +06/root/users/u2/xx/aa  |+0008_________++__+_
                                         |+0011-__________---_;
                +07/root/users/u2/xx/bb  |+0011____________+++;
                #
                 1,  7;
                11, 10;
                12, 10;
            ");

            //-- PermissionType.Id:                                     987654321
            var expectedEntriesString = @"
                DefinedOn=6, Principal= 8, Propagates=true, Values=__________________________++__++
                DefinedOn=6, Principal=11, Propagates=true, Values=_________________-__________---_
                DefinedOn=6, Principal= 1, Propagates=true, Values=________________________++++++++
                DefinedOn=6, Principal= 7, Propagates=true, Values=________________________++++++++
                DefinedOn=6, Principal=10, Propagates=true, Values=_________________________+__++++
                ".Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var entriesString = GetEntriesToString(security.GetEffectiveEntries("/root/users/u2/xx/aa"));
            var ok = entriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "") == expectedEntriesString.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void Security_MembershipExtender()
        {
            var extAcc = new PrivateType(typeof(MembershipExtenderBase));
            var extenderSave = extAcc.GetStaticField("_instance");
            extAcc.SetStaticField("_instance", null);
            var user = User.Visitor;

            try
            {
                user.Index++; //--- forced reloading
                user.Save();

                var ext0 = user.MembershipExtension;
                string ext0str = string.Empty;
                if (ext0 != null)
                    ext0str = string.Join(", ", ext0.ExtensionIds);

                extAcc.SetStaticField("_instance", new TestMembershipExtender());

                MembershipExtenderBase.Extend(user);

                var ext1 = user.MembershipExtension;
                string ext1str = string.Empty;
                if (ext1 != null)
                    ext1str = string.Join(", ", ext1.ExtensionIds);

                Assert.IsTrue(ext0 == null, "#1: Extension is not null: " + ext0str);
                Assert.IsTrue(ext1.ExtensionIds.Count() == 1, "#2: Extension.Count is not 1 but: " + ext1.ExtensionIds.Count());
                Assert.IsTrue(ext1.ExtensionIds.ElementAt(0) == 111, "#3: Extension is not [111]: " + ext1str);
            }
            finally
            {
                extAcc.SetStaticField("_instance", extenderSave);
                user.Index++; //--- forced reloading
                user.Save();
            }
        }
        [TestMethod]
        public void Security_Haspermission_WithSecurity_MembershipExtender()
        {
            var security = GetSecurity(@"
                +/root           |+0007_++++++
                                 |+0008______+;
                #
                 1,  7;
                11, 10;
            ");
            // +/root/folder|+1345__+__|+0450__+__
            // 1 Admin
            // 6 Visitor
            // 7 Administrators
            // 8 Everyone
            // 9 Creators

            var user = new TestUser(111);

            var levelRoot = security.HasPermission("/root", user, false, false, PermissionType.Open);
            var levelF1 = security.HasPermission("/root/f1", user, false, false, PermissionType.Open);
            var levelF2 = security.HasPermission("/root/f1/f2", user, false, false, PermissionType.Open);
            var levelF3 = security.HasPermission("/root/f1/f2/f3", user, false, false, PermissionType.Open);

            Assert.IsFalse(levelRoot, "#1");
            Assert.IsFalse(levelF1, "#2");
            Assert.IsFalse(levelF2, "#3");
            Assert.IsFalse(levelF3, "#4");

            user.MembershipExtension = new MembershipExtension(new IGroup[] { new TestGroup { Id = 7 } });

            levelRoot = security.HasPermission("/root", user, false, false, PermissionType.Open);
            levelF1 = security.HasPermission("/root/f1", user, false, false, PermissionType.Open);
            levelF2 = security.HasPermission("/root/f1/f2", user, false, false, PermissionType.Open);
            levelF3 = security.HasPermission("/root/f1/f2/f3", user, false, false, PermissionType.Open);

            Assert.IsTrue(levelRoot, "#5");
            Assert.IsTrue(levelF1, "#6");
            Assert.IsTrue(levelF2, "#7");
            Assert.IsTrue(levelF3, "#8");
        }

        //============================================================================================ Permission export / import

        //[TestMethod]
        //public void ExportPermisions()
        //{
        //    var s = ExportPermisions(Repository.Root);

        //    Assert.Inconclusive();
        //}
        [TestMethod]
        public void ImportPermissions_WithoutClear()
        {
            var impexUser = Node.Load<User>("/Root/IMS/BuiltIn/ImpexUser");
            if (impexUser == null)
            {
                impexUser = new User(Node.LoadNode("/Root/IMS/BuiltIn"));
                impexUser.Name = "ImpexUser";
                impexUser.Save();
            }

            //-- initializing
            var node = Node.LoadNode("/Root/IMS");
            node.Security.SetPermission(User.Visitor, true, PermissionType.RunApplication, PermissionValue.Allow);
            node.Security.SetPermission(impexUser, true, PermissionType.Open, PermissionValue.Allow);

            //-- import without clear
            var xml1 = new XmlDocument();
            xml1.LoadXml(String.Format(@"<ContentMetaData>
                  <Permissions>
                    <Identity path='{0}'>
                      <RunApplication>Allow</RunApplication>
                    </Identity>
                  </Permissions>
                </ContentMetaData>", impexUser.Path));

            var permissionsElement1 = (XmlElement)xml1.SelectSingleNode("/ContentMetaData/Permissions");
            node.Security.ImportPermissions(permissionsElement1, "drive:\\path");

            var entries = node.Security.GetExplicitEntries();
            var dump = entries.OrderBy(e => e.PrincipalId).Select(e => String.Format("{0}:{1}", e.PrincipalId, PermissionsToString(e.PermissionValues))).ToArray();
            Assert.IsTrue(dump.Length == 2, "#1");
            Assert.IsTrue(dump[0] == String.Concat(User.Visitor.Id, ":_______________+________________"), "#2");
            Assert.IsTrue(dump[1] == String.Concat(impexUser.Id, ":_______________+________________"), "#3");
        }
        [TestMethod]
        public void ImportPermissions_WithClear()
        {
            var impexUser = Node.Load<User>("/Root/IMS/BuiltIn/ImpexUser");
            if (impexUser == null)
            {
                impexUser = new User(Node.LoadNode("/Root/IMS/BuiltIn"));
                impexUser.Name = "ImpexUser";
                impexUser.Save();
            }

            //-- initializing
            var node = Node.LoadNode("/Root/IMS");
            node.Security.SetPermission(User.Visitor, true, PermissionType.RunApplication, PermissionValue.Allow);
            node.Security.SetPermission(impexUser, true, PermissionType.Open, PermissionValue.Allow);

            //-- import with clear
            var xml1 = new XmlDocument();
            xml1.LoadXml(String.Format(@"<ContentMetaData>
                  <Permissions>
                    <Clear />
                    <Identity path='{0}'>
                      <RunApplication>Allow</RunApplication>
                    </Identity>
                  </Permissions>
                </ContentMetaData>", impexUser.Path));

            var permissionsElement1 = (XmlElement)xml1.SelectSingleNode("/ContentMetaData/Permissions");
            node.Security.ImportPermissions(permissionsElement1, "drive:\\path");

            var entries = node.Security.GetExplicitEntries();
            var dump = entries.OrderBy(e => e.PrincipalId).Select(e => String.Format("{0}:{1}", e.PrincipalId, PermissionsToString(e.PermissionValues))).ToArray();
            Assert.IsTrue(dump.Length == 1, "#1");
            Assert.IsTrue(dump[0] == String.Concat(impexUser.Id, ":_______________+________________"), "#2");
        }
        [TestMethod]
        public void ImportPermissions_NullPath()
        {
            //-- initializing
            var node = Node.LoadNode("/Root/IMS");

            //-- import with clear
            var xml = new XmlDocument();
            xml.LoadXml(@"<ContentMetaData>
                  <Permissions>
                    <Clear />
                    <Identity>
                      <RunApplication>Allow</RunApplication>
                    </Identity>
                  </Permissions>
                </ContentMetaData>");

            var permissionsElement = (XmlElement)xml.SelectSingleNode("/ContentMetaData/Permissions");
            try
            {
                node.Security.ImportPermissions(permissionsElement, "drive:\\path");
                Assert.Fail("Exception was not thrown.");
            }
            catch (Exception e)
            {
                int q = 1;
            }
        }
        [TestMethod]
        public void ImportPermissions_InvalidPath()
        {
            //-- initializing
            var node = Node.LoadNode("/Root/IMS");

            //-- import with clear
            var xml = new XmlDocument();
            xml.LoadXml(@"<ContentMetaData>
                  <Permissions>
                    <Clear />
                    <Identity path='root/ims'>
                      <RunApplication>Allow</RunApplication>
                    </Identity>
                  </Permissions>
                </ContentMetaData>");

            var permissionsElement = (XmlElement)xml.SelectSingleNode("/ContentMetaData/Permissions");
            try
            {
                node.Security.ImportPermissions(permissionsElement, "drive:\\path");
                Assert.Fail("Exception was not thrown.");
            }
            catch (Exception e)
            {
                int q = 1;
            }
        }
        [TestMethod]
        public void ImportPermissions_IdentityNotFound()
        {
            //-- initializing
            var node = Node.LoadNode("/Root/IMS");

            //-- import with clear
            var xml = new XmlDocument();
            xml.LoadXml(@"<ContentMetaData>
                  <Permissions>
                    <Clear />
                    <Identity path='/Root/IMS/BuiltIn/Portal/xy'>
                      <RunApplication>Allow</RunApplication>
                    </Identity>
                  </Permissions>
                </ContentMetaData>");

            var permissionsElement = (XmlElement)xml.SelectSingleNode("/ContentMetaData/Permissions");
            try
            {
                node.Security.ImportPermissions(permissionsElement, "drive:\\path");
                Assert.Fail("Exception was not thrown.");
            }
            catch (Exception e)
            {
                int q = 1;
            }
        }
        [TestMethod]
        public void ImportPermissions_PermissionTypeNotFound()
        {
            //-- initializing
            var node = Node.LoadNode("/Root/IMS");

            //-- import with clear
            var xml = new XmlDocument();
            xml.LoadXml(@"<ContentMetaData>
                  <Permissions>
                    <Clear />
                    <Identity path='/Root/IMS/BuiltIn/Portal/Visitor'>
                      <Unknown>Allow</Unknown>
                    </Identity>
                  </Permissions>
                </ContentMetaData>");

            var permissionsElement = (XmlElement)xml.SelectSingleNode("/ContentMetaData/Permissions");
            try
            {
                node.Security.ImportPermissions(permissionsElement, "drive:\\path");
                Assert.Fail("Exception was not thrown.");
            }
            catch (Exception e)
            {
                int q = 1;
            }
        }
        [TestMethod]
        public void ImportPermissions_InvalidPermissionValue()
        {
            //-- initializing
            var node = Node.LoadNode("/Root/IMS");

            //-- import with clear
            var xml = new XmlDocument();
            xml.LoadXml(@"<ContentMetaData>
                  <Permissions>
                    <Clear />
                    <Identity path='/Root/IMS/BuiltIn/Portal/Visitor'>
                      <See>Deny</See>
                      <Open>NotDefined</Open>
                    </Identity>
                  </Permissions>
                </ContentMetaData>");

            var permissionsElement = (XmlElement)xml.SelectSingleNode("/ContentMetaData/Permissions");
            try
            {
                node.Security.ImportPermissions(permissionsElement, "drive:\\path");
                Assert.Fail("Exception was not thrown.");
            }
            catch (Exception e)
            {
                int q = 1;
            }
        }

        //============================================================================================ See, Open, OpenMinor & Allow, Deny

        [TestMethod]
        public void Security_SetPerms_ForcedSettings()
        {
            var visitor = User.Visitor;
            var everyone = Group.Everyone;

            var content = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var folder = content.ContentHandler;

            content = Content.CreateNew("Car", folder, Guid.NewGuid().ToString());
            content.Save();
            var node = content.ContentHandler;

            folder.Security.BreakInheritance();
            ResetSeeOpenOpenMinorPermissions(folder, everyone);
            ResetSeeOpenOpenMinorPermissions(folder, visitor);

            string msg = null;
            //---------------------------------------------------------------- Run <----- See
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(1, node, visitor, "________________________________", "________________________________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(2, node, visitor, "_______________________________+", "_______________________________+")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(3, node, visitor, "_______________________________-", "______________-___--------------")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(4, node, visitor, "______________________________+_", "______________________________++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(5, node, visitor, "______________________________++", "______________________________++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(6, node, visitor, "______________________________+-", "______________-___--------------")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(7, node, visitor, "______________________________-_", "______________-___-------------_")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(8, node, visitor, "______________________________-+", "______________-___-------------+")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(9, node, visitor, "______________________________--", "______________-___--------------")) == null, msg);

            Assert.IsTrue((msg = SetPermsForcedSettingsTest(10, node, visitor, "_____________________________+__", "_____________________________+++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(11, node, visitor, "_____________________________+_+", "_____________________________+++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(12, node, visitor, "_____________________________+_-", "______________-___--------------")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(13, node, visitor, "_____________________________++_", "_____________________________+++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(14, node, visitor, "_____________________________+++", "_____________________________+++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(15, node, visitor, "_____________________________++-", "______________-___--------------")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(16, node, visitor, "_____________________________+-_", "______________-___-------------+")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(17, node, visitor, "_____________________________+-+", "______________-___-------------+")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(18, node, visitor, "_____________________________+--", "______________-___--------------")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(19, node, visitor, "_____________________________-__", "______________-___----------_-__")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(20, node, visitor, "_____________________________-_+", "______________-___----------_-_+")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(21, node, visitor, "_____________________________-_-", "______________-___--------------")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(22, node, visitor, "_____________________________-+_", "______________-___----------_-++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(23, node, visitor, "_____________________________-++", "______________-___----------_-++")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(24, node, visitor, "_____________________________-+-", "______________-___--------------")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(25, node, visitor, "_____________________________--_", "______________-___-------------_")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(26, node, visitor, "_____________________________--+", "______________-___-------------+")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(27, node, visitor, "_____________________________---", "______________-___--------------")) == null, msg);

            Assert.IsTrue((msg = SetPermsForcedSettingsTest(28, node, visitor, "_________________+______________", "_________________+______________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(29, node, visitor, "________________+_______________", "________________++______________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(30, node, visitor, "________________++______________", "________________++______________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(31, node, visitor, "_________________-______________", "________________--______________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(32, node, visitor, "________________-_______________", "________________-_______________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(33, node, visitor, "________________--______________", "________________--______________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(34, node, visitor, "________________+-______________", "________________--______________")) == null, msg);
            Assert.IsTrue((msg = SetPermsForcedSettingsTest(35, node, visitor, "________________-+______________", "________________-+______________")) == null, msg);
        }
        private string SetPermsForcedSettingsTest(int operationNumber, Node node, ISecurityMember member, string setting, string expectedState)
        {
            ResetSeeOpenOpenMinorPermissions(node, member);

            var perms = new PermissionValue[ActiveSchema.PermissionTypes.Count];
            var permCount = perms.Length;
            for (int i = 0; i < setting.Length; i++)
            {
                if (setting[i] == '+') perms[permCount - i - 1] = PermissionValue.Allow;
                else if (setting[i] == '-') perms[permCount - i - 1] = PermissionValue.Deny;
            }

            string result = null;
            node.Security.SetPermissions(member.Id, true, perms);
            SecurityEntry entry = null;
            foreach (var e in node.Security.GetExplicitEntries())
            {
                if (e.PrincipalId == member.Id)
                {
                    entry = e;
                    break;
                }
            }

            if (entry == null)
            {
                result = "________________________________";
            }
            else
            {
                var chars = new char[permCount];
                var values = entry.PermissionValues;
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == PermissionValue.Allow) chars[permCount - i - 1] = '+';
                    else if (values[i] == PermissionValue.Deny) chars[permCount - i - 1] = '-';
                    else chars[permCount - i - 1] = '_';
                }
                result = new String(chars);
            }
            if (result == expectedState)
                return null;
            return String.Concat("State is '", result, "', expected '", expectedState, "' at operation ", operationNumber);
        }

        [TestMethod]
        public void Security_SetAcl_ForcedSettings()
        {
            var visitor = User.Visitor;
            var everyone = Group.Everyone;

            var content = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var folder = content.ContentHandler;

            content = Content.CreateNew("Car", folder, Guid.NewGuid().ToString());
            content.Save();
            var node = content.ContentHandler;

            folder.Security.BreakInheritance();
            ResetSeeOpenOpenMinorPermissions(folder, everyone);
            ResetSeeOpenOpenMinorPermissions(folder, visitor);

            string msg = null;
            Assert.IsTrue((msg = SetAclForcedSettingsTest(01, node, visitor, "________________________________", "_______________________________+", PermissionType.See, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(02, node, visitor, "________________________________", "______________-___--------------", PermissionType.See, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(03, node, visitor, "______________------------------", "______________-----------------+", PermissionType.See, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(04, node, visitor, "______________------------------", "______________-----------------_", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(05, node, visitor, "______________++++++++++++++++++", "______________-+++--------------", PermissionType.See, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(06, node, visitor, "______________++++++++++++++++++", "_______________+++______________", PermissionType.See, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetAclForcedSettingsTest(11, node, visitor, "________________________________", "______________________________++", PermissionType.Preview, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(12, node, visitor, "________________________________", "______________-___-------------_", PermissionType.Preview, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(13, node, visitor, "______________------------------", "______________----------------++", PermissionType.Preview, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(14, node, visitor, "______________------------------", "______________----------------__", PermissionType.Preview, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(15, node, visitor, "______________++++++++++++++++++", "______________-+++-------------+", PermissionType.Preview, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(16, node, visitor, "______________++++++++++++++++++", "_______________+++_____________+", PermissionType.Preview, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetAclForcedSettingsTest(21, node, visitor, "________________________________", "_____________________________+++", PermissionType.PreviewWithoutWatermark, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(22, node, visitor, "________________________________", "______________-___----------_-__", PermissionType.PreviewWithoutWatermark, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(23, node, visitor, "______________------------------", "______________---------------+++", PermissionType.PreviewWithoutWatermark, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(24, node, visitor, "______________------------------", "______________---------------___", PermissionType.PreviewWithoutWatermark, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(25, node, visitor, "______________++++++++++++++++++", "______________-+++----------+-++", PermissionType.PreviewWithoutWatermark, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(26, node, visitor, "______________++++++++++++++++++", "_______________+++__________+_++", PermissionType.PreviewWithoutWatermark, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetAclForcedSettingsTest(31, node, visitor, "________________________________", "____________________________+_++", PermissionType.PreviewWithoutRedaction, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(32, node, visitor, "________________________________", "______________-___-----------___", PermissionType.PreviewWithoutRedaction, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(33, node, visitor, "______________------------------", "______________--------------+-++", PermissionType.PreviewWithoutRedaction, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(34, node, visitor, "______________------------------", "______________--------------_-__", PermissionType.PreviewWithoutRedaction, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(35, node, visitor, "______________++++++++++++++++++", "______________-+++-----------+++", PermissionType.PreviewWithoutRedaction, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(36, node, visitor, "______________++++++++++++++++++", "_______________+++___________+++", PermissionType.PreviewWithoutRedaction, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetAclForcedSettingsTest(41, node, visitor, "________________________________", "___________________________+++++", PermissionType.Open, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(42, node, visitor, "________________________________", "______________-___----------____", PermissionType.Open, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(43, node, visitor, "______________------------------", "______________-------------+++++", PermissionType.Open, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(44, node, visitor, "______________------------------", "______________-------------_____", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(45, node, visitor, "______________++++++++++++++++++", "______________-+++----------++++", PermissionType.Open, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(46, node, visitor, "______________++++++++++++++++++", "_______________+++__________++++", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetAclForcedSettingsTest(51, node, visitor, "________________________________", "__________________________++++++", PermissionType.OpenMinor, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(52, node, visitor, "________________________________", "______________-___---------_____", PermissionType.OpenMinor, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(53, node, visitor, "______________------------------", "______________------------++++++", PermissionType.OpenMinor, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(54, node, visitor, "______________------------------", "______________------------______", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(55, node, visitor, "______________++++++++++++++++++", "______________-+++---------+++++", PermissionType.OpenMinor, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(56, node, visitor, "______________++++++++++++++++++", "_______________+++_________+++++", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetAclForcedSettingsTest(61, node, visitor, "________________________________", "_________________________+++++++", PermissionType.Save, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(62, node, visitor, "________________________________", "______________-__________-______", PermissionType.Save, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(63, node, visitor, "______________------------------", "______________-----------+++++++", PermissionType.Save, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(64, node, visitor, "______________------------------", "______________-----------_______", PermissionType.Save, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(65, node, visitor, "______________++++++++++++++++++", "______________-++++++++++-++++++", PermissionType.Save, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetAclForcedSettingsTest(66, node, visitor, "______________++++++++++++++++++", "_______________++++++++++_++++++", PermissionType.Save, PermissionValue.NonDefined)) == null, msg);

        }
        private string SetAclForcedSettingsTest(int operationNumber, Node node, ISecurityMember member, string startState, string expectedState, PermissionType permType, PermissionValue value)
        {
            return SetForcedSettingsTest(operationNumber, node, member, startState, expectedState, permType, value, true);
        }

        [TestMethod]
        public void Security_SetPerm_ForcedSettings()
        {
            var visitor = User.Visitor;
            var everyone = Group.Everyone;

            var content = Content.CreateNew("Folder", TestRoot, Guid.NewGuid().ToString());
            content.Save();
            var folder = content.ContentHandler;

            content = Content.CreateNew("Car", folder, Guid.NewGuid().ToString());
            content.Save();
            var node = content.ContentHandler;

            folder.Security.BreakInheritance();
            //ResetSeeOpenOpenMinorPermissions(folder, everyone);
            ResetSeeOpenOpenMinorPermissions(folder, visitor);

            string msg = null;
            /*
            Assert.IsTrue((msg = SetPermForcedSettingsTest(1, node, visitor, "_______________-________________", "_______________-_______________+", PermissionType.See, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(2, node, visitor, "_______________-________________", "_______________-___________+++++", PermissionType.Open, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(3, node, visitor, "_______________-________________", "_______________-__________++++++", PermissionType.OpenMinor, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(4, node, visitor, "_______________-__________------", "______________--_____----------+", PermissionType.See, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(5, node, visitor, "_______________-__________------", "______________--__---------+++++", PermissionType.Open, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(6, node, visitor, "_______________-__________------", "______________--__--------++++++", PermissionType.OpenMinor, PermissionValue.Allow)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(7, node, visitor, "_______________-________________", "______________-----__-----------", PermissionType.See, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(8, node, visitor, "_______________-________________", "_________________--__----------_", PermissionType.Open, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(9, node, visitor, "_______________-________________", "_________________--__---------__", PermissionType.OpenMinor, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(10, node, visitor, "_______________-__________++++++", "______________-----__-----------", PermissionType.See, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(11, node, visitor, "_______________-__________++++++", "_________________--__----------+", PermissionType.Open, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(12, node, visitor, "_______________-__________++++++", "_________________--__---------++", PermissionType.OpenMinor, PermissionValue.Deny)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(13, node, visitor, "_______________-_______________+", "__________________-_____________", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(14, node, visitor, "_______________-_______________+", "__________________-____________+", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(15, node, visitor, "_______________-________+______+", "__________________-____________+", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(16, node, visitor, "_______________-___________+++++", "__________________-_____________", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(17, node, visitor, "_______________-___________+++++", "__________________-____________+", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(18, node, visitor, "_______________-________+__+++++", "__________________-___________++", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(19, node, visitor, "_______________-__________++++++", "__________________-_____________", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(20, node, visitor, "_______________-__________++++++", "__________________-____________+", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(21, node, visitor, "_______________-________+_++++++", "__________________-___________++", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(22, node, visitor, "_______________-__________-_____", "_________________--__---------__", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(23, node, visitor, "_______________-__________-_____", "_________________--__---------__", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(24, node, visitor, "_______________-__________-_____", "_________________--__--------___", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(25, node, visitor, "_______________-__________--____", "_________________--__----------_", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(26, node, visitor, "_______________-__________--____", "_________________--__---------__", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(27, node, visitor, "_______________-__________--____", "_________________--__--------___", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(28, node, visitor, "_______________-__________------", "_________________--__----------_", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(29, node, visitor, "_______________-__________------", "_________________--__---------__", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(30, node, visitor, "_______________-__________------", "_________________--__--------___", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(31, node, visitor, "_______________-__________-____+", "_________________--__---------__", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(32, node, visitor, "_______________-__________-____+", "_________________--__---------_+", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(33, node, visitor, "_______________-__________-____+", "_________________--__--------__+", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(34, node, visitor, "_______________-__________-+++++", "_________________--__---------__", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(35, node, visitor, "_______________-__________-+++++", "_________________--__---------_+", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(36, node, visitor, "_______________-__________-+++++", "_________________--__--------_++", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(37, node, visitor, "_______________-__________--___+", "_________________--__----------_", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(38, node, visitor, "_______________-__________--___+", "_________________--__---------_+", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(39, node, visitor, "_______________-__________--___+", "_________________--__--------__+", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(40, node, visitor, "_______________-________________", "_______________-________+_++++++", PermissionType.Publish, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(41, node, visitor, "_______________-________________", "_______________-________________", PermissionType.Publish, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(42, node, visitor, "_______________-________________", "_______________-________-_______", PermissionType.Publish, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(43, node, visitor, "_______________-___________+++++", "_______________-________+_++++++", PermissionType.Publish, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(44, node, visitor, "_______________-___________+++++", "_______________-___________+++++", PermissionType.Publish, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(45, node, visitor, "_______________-___________+++++", "_______________-________-__+++++", PermissionType.Publish, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(46, node, visitor, "_______________-________+_++++++", "_______________-________+_++++++", PermissionType.Publish, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(47, node, visitor, "_______________-________+_++++++", "_______________-__________++++++", PermissionType.Publish, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(48, node, visitor, "_______________-________+_++++++", "_______________-________-_++++++", PermissionType.Publish, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(49, node, visitor, "_______________-__--------------", "______________--__------+-++++++", PermissionType.Publish, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(50, node, visitor, "_______________-__--------------", "______________--__------_-______", PermissionType.Publish, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(51, node, visitor, "_______________-__--------------", "______________--__--------------", PermissionType.Publish, PermissionValue.Deny)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(52, node, visitor, "_______________-________________", "_______________-_+______________", PermissionType.SeePermissions, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(53, node, visitor, "_______________-________________", "_______________-++______________", PermissionType.SetPermissions, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(54, node, visitor, "_______________---______________", "_______________--+______________", PermissionType.SeePermissions, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(55, node, visitor, "_______________---______________", "_______________-++______________", PermissionType.SetPermissions, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(56, node, visitor, "_______________-++______________", "_______________-________________", PermissionType.SeePermissions, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(57, node, visitor, "_______________-++______________", "_______________-_+______________", PermissionType.SetPermissions, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(58, node, visitor, "_______________---______________", "_______________--_______________", PermissionType.SeePermissions, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(59, node, visitor, "_______________---______________", "_______________-________________", PermissionType.SetPermissions, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(60, node, visitor, "_______________-________________", "_______________---______________", PermissionType.SeePermissions, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(61, node, visitor, "_______________-________________", "_______________--_______________", PermissionType.SetPermissions, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(62, node, visitor, "_______________-++______________", "_______________---______________", PermissionType.SeePermissions, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(63, node, visitor, "_______________-++______________", "_______________--+______________", PermissionType.SetPermissions, PermissionValue.Deny)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(64, node, visitor, "________________________________", "______________++++_____+_+__++++", PermissionType.ManageListsAndWorkspaces, PermissionValue.Allow)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(65, node, visitor, "________________________________", "_________________-__________-___", PermissionType.Save, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(66, node, visitor, "________________________________", "_________________-_______-______", PermissionType.AddNew, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(67, node, visitor, "________________________________", "_________________-_____-________", PermissionType.Delete, PermissionValue.Deny)) == null, msg);
            */


            Assert.IsTrue((msg = SetPermForcedSettingsTest(01, node, visitor, "________________________________", "_______________________________+", PermissionType.See, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(02, node, visitor, "________________________________", "______________-___--------------", PermissionType.See, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(03, node, visitor, "______________------------------", "______________-----------------+", PermissionType.See, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(04, node, visitor, "______________------------------", "______________-----------------_", PermissionType.See, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(05, node, visitor, "______________++++++++++++++++++", "______________-+++--------------", PermissionType.See, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(06, node, visitor, "______________++++++++++++++++++", "_______________+++______________", PermissionType.See, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(11, node, visitor, "________________________________", "______________________________++", PermissionType.Preview, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(12, node, visitor, "________________________________", "______________-___-------------_", PermissionType.Preview, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(13, node, visitor, "______________------------------", "______________----------------++", PermissionType.Preview, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(14, node, visitor, "______________------------------", "______________----------------__", PermissionType.Preview, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(15, node, visitor, "______________++++++++++++++++++", "______________-+++-------------+", PermissionType.Preview, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(16, node, visitor, "______________++++++++++++++++++", "_______________+++_____________+", PermissionType.Preview, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(21, node, visitor, "________________________________", "_____________________________+++", PermissionType.PreviewWithoutWatermark, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(22, node, visitor, "________________________________", "______________-___----------_-__", PermissionType.PreviewWithoutWatermark, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(23, node, visitor, "______________------------------", "______________---------------+++", PermissionType.PreviewWithoutWatermark, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(24, node, visitor, "______________------------------", "______________---------------___", PermissionType.PreviewWithoutWatermark, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(25, node, visitor, "______________++++++++++++++++++", "______________-+++----------+-++", PermissionType.PreviewWithoutWatermark, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(26, node, visitor, "______________++++++++++++++++++", "_______________+++__________+_++", PermissionType.PreviewWithoutWatermark, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(31, node, visitor, "________________________________", "____________________________+_++", PermissionType.PreviewWithoutRedaction, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(32, node, visitor, "________________________________", "______________-___-----------___", PermissionType.PreviewWithoutRedaction, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(33, node, visitor, "______________------------------", "______________--------------+-++", PermissionType.PreviewWithoutRedaction, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(34, node, visitor, "______________------------------", "______________--------------_-__", PermissionType.PreviewWithoutRedaction, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(35, node, visitor, "______________++++++++++++++++++", "______________-+++-----------+++", PermissionType.PreviewWithoutRedaction, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(36, node, visitor, "______________++++++++++++++++++", "_______________+++___________+++", PermissionType.PreviewWithoutRedaction, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(41, node, visitor, "________________________________", "___________________________+++++", PermissionType.Open, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(42, node, visitor, "________________________________", "______________-___----------____", PermissionType.Open, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(43, node, visitor, "______________------------------", "______________-------------+++++", PermissionType.Open, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(44, node, visitor, "______________------------------", "______________-------------_____", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(45, node, visitor, "______________++++++++++++++++++", "______________-+++----------++++", PermissionType.Open, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(46, node, visitor, "______________++++++++++++++++++", "_______________+++__________++++", PermissionType.Open, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(51, node, visitor, "________________________________", "__________________________++++++", PermissionType.OpenMinor, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(52, node, visitor, "________________________________", "______________-___---------_____", PermissionType.OpenMinor, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(53, node, visitor, "______________------------------", "______________------------++++++", PermissionType.OpenMinor, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(54, node, visitor, "______________------------------", "______________------------______", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(55, node, visitor, "______________++++++++++++++++++", "______________-+++---------+++++", PermissionType.OpenMinor, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(56, node, visitor, "______________++++++++++++++++++", "_______________+++_________+++++", PermissionType.OpenMinor, PermissionValue.NonDefined)) == null, msg);

            Assert.IsTrue((msg = SetPermForcedSettingsTest(61, node, visitor, "________________________________", "_________________________+++++++", PermissionType.Save, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(62, node, visitor, "________________________________", "______________-__________-______", PermissionType.Save, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(63, node, visitor, "______________------------------", "______________-----------+++++++", PermissionType.Save, PermissionValue.Allow)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(64, node, visitor, "______________------------------", "______________-----------_______", PermissionType.Save, PermissionValue.NonDefined)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(65, node, visitor, "______________++++++++++++++++++", "______________-++++++++++-++++++", PermissionType.Save, PermissionValue.Deny)) == null, msg);
            Assert.IsTrue((msg = SetPermForcedSettingsTest(66, node, visitor, "______________++++++++++++++++++", "_______________++++++++++_++++++", PermissionType.Save, PermissionValue.NonDefined)) == null, msg);

        }
        private string SetPermForcedSettingsTest(int operationNumber, Node node, ISecurityMember member, string startState, string expectedState, PermissionType permType, PermissionValue value)
        {
            return SetForcedSettingsTest(operationNumber, node, member, startState, expectedState, permType, value, false);
        }
        private string SetForcedSettingsTest(int operationNumber, Node node, ISecurityMember member, string startState, string expectedState, PermissionType permType, PermissionValue value, bool acl)
        {
            ResetSeeOpenOpenMinorPermissions(node, member);

            var perms = new PermissionValue[ActiveSchema.PermissionTypes.Count];
            var permCount = perms.Length;
            for (int i = 0; i < startState.Length; i++)
            {
                if (startState[i] == '+') perms[permCount - i - 1] = PermissionValue.Allow;
                else if (startState[i] == '-') perms[permCount - i - 1] = PermissionValue.Deny;
            }

            //---- prerequisit
            node.Security.SetPermissions(member.Id, true, perms);
            //---- test operation
            if (acl)
                new AclEditor(node).SetPermission(member, true, permType, value).Apply();
            else
                node.Security.SetPermission(member, true, permType, value);

            string result = null;
            SecurityEntry entry = null;
            foreach (var e in node.Security.GetExplicitEntries())
            {
                if (e.PrincipalId == member.Id)
                {
                    entry = e;
                    break;
                }
            }

            if (entry == null)
            {
                result = "________________________________";
            }
            else
            {
                var chars = new char[permCount];
                var values = entry.PermissionValues;
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == PermissionValue.Allow) chars[permCount - i - 1] = '+';
                    else if (values[i] == PermissionValue.Deny) chars[permCount - i - 1] = '-';
                    else chars[permCount - i - 1] = '_';
                }
                result = new String(chars);
            }
            if (result == expectedState)
                return null;
            return String.Concat("State is '", result, "', expected '", expectedState, "' at operation ", operationNumber);


        }

        private void ResetSeeOpenOpenMinorPermissions(Node node, ISecurityMember user)
        {
            new AclEditor(node).
                SetPermission(user, true, PermissionType.See, PermissionValue.NonDefined).
                SetPermission(user, true, PermissionType.Open, PermissionValue.NonDefined).
                SetPermission(user, true, PermissionType.OpenMinor, PermissionValue.NonDefined).
                Apply();
        }

        [TestMethod] // bug reproduction
        public void Security_Inheritance_CannotSetNotEditableBit()
        {
            var rootFolder = new Folder(TestRoot);
            rootFolder.Name = Guid.NewGuid().ToString();
            rootFolder.Save();
            var folder1 = new Folder(rootFolder);
            folder1.Name = Guid.NewGuid().ToString();
            folder1.Save();
            var folder2 = new Folder(folder1);
            folder2.Name = Guid.NewGuid().ToString();
            folder2.Save();

            rootFolder.Security.BreakInheritance();

            folder1.Security.SetPermission(User.Visitor, true, PermissionType.SeePermissions, PermissionValue.Allow);

            var ed = folder2.Security.GetAclEditor();
            ed.SetPermission(User.Visitor.Id, true, PermissionType.RunApplication, PermissionValue.Allow);
            ed.Apply();

            folder1.Security.SetPermission(User.Visitor, true, PermissionType.SeePermissions, PermissionValue.NonDefined);

            var hasSeePerms = folder2.Security.HasPermission((IUser)User.Visitor, PermissionType.SeePermissions);
            Assert.IsFalse(hasSeePerms);
        }

        //============================================================================================ SetAcl

        private class AclEditorAccessor : Accessor
        {
            public AclEditorAccessor(AclEditor target) : base(target) { }
            public SnAccessControlEntry GetEntry(ISecurityMember principal, bool propagates)
            {
                return (SnAccessControlEntry)CallPrivateMethod("GetEntry", principal, propagates);
            }
            public SnAccessControlList GetAcl()
            {
                return (SnAccessControlList)GetPrivateField("acl");
            }
        }
        private class SecurityHandlerAccessor : Accessor
        {
            public SecurityHandlerAccessor(SecurityHandler target) : base(target) { }
            public IEnumerable<SecurityEntry> GetEntriesFromAcl(AclEditor ed, SnAccessControlList origAcl, SnAccessControlList acl)
            {
                return (IEnumerable<SecurityEntry>)CallPrivateStaticMethod("GetEntriesFromAcl", new Type[] { typeof(AclEditor), typeof(SnAccessControlList), typeof(SnAccessControlList) }, ed, origAcl, acl);
            }
        }

        [TestMethod]
        public void Acl_GuiSimulation()
        {
            string msg;
            //  Run SetPerm | SeePerm DeleteOld RecallOld Delete | Approve AddNew Force Publish | Save OpenMinor Open See

            Assert.IsNull(msg = SetAclTest(1, "___________________________+++++", "___________________________rrrrr", "________________________+_+_____", "________________________+_++++++"), msg);
            Assert.IsNull(msg = SetAclTest(2, "________________________+_++++++", "___________________________rrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(3, "________________________+_++++++", "___________________________rrrrr", "___________________________+++++", "________________________________"), msg);

            Assert.IsNull(msg = SetAclTest(4, "________________________+_++++++", "________________________r_rrrrrr", "______________________+_________", "______________________+___++++++"), msg);
            Assert.IsNull(msg = SetAclTest(5, "______________________+_+_++++++", "________________________r_rrrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(6, "______________________+_+_++++++", "________________________r_rrrrrr", "________________________+_++++++", "________________________________"), msg);

            Assert.IsNull(msg = SetAclTest(7, "_______________+___________+++++", "___________________________rrrrr", "________________________________", "________________________________"), msg);

            //--- ilo
            //leveszem az open minort, akkor megszunik az entry
            Assert.IsNull(msg = SetAclTest(8,  "________________________++++++++", "___________________________rrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(9,  "_______________________+++++++++", "___________________________rrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(10, "______________________++++++++++", "___________________________rrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(11, "_____________________+++++++++++", "___________________________rrrrr", "________________________________", "________________________________"), msg);
            //leveszem az open minort, de van run, megmarad a bejegyzes (nem biztos, hogy kell az osszes)
            Assert.IsNull(msg = SetAclTest(12, "_______________+________++++++++", "___________________________rrrrr", "_______________+________________", "_______________+________________"), msg);
            Assert.IsNull(msg = SetAclTest(13, "_______________+_______+++++++++", "___________________________rrrrr", "_______________+________________", "_______________+________________"), msg);
            Assert.IsNull(msg = SetAclTest(14, "_______________+______++++++++++", "___________________________rrrrr", "_______________+________________", "_______________+________________"), msg);
            Assert.IsNull(msg = SetAclTest(15, "_______________+_____+++++++++++", "___________________________rrrrr", "_______________+________________", "_______________+________________"), msg);
            //(nem biztos, hogy kell az osszes)
            Assert.IsNull(msg = SetAclTest(16, "_______________+________++++++++", "_______________r___________rrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(17, "_______________+_______+++++++++", "_______________r___________rrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(18, "_______________+______++++++++++", "_______________r___________rrrrr", "________________________________", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(19, "_______________+_____+++++++++++", "_______________r___________rrrrr", "________________________________", "________________________________"), msg);
            //(nem biztos, hogy kell az osszes)
            Assert.IsNull(msg = SetAclTest(20, "_______________+________++++++++", "_______________r___________rrrrr", "________________________++______", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(21, "_______________+_______+++++++++", "_______________r___________rrrrr", "_______________________+++______", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(22, "_______________+______++++++++++", "_______________r___________rrrrr", "______________________++++______", "________________________________"), msg);
            Assert.IsNull(msg = SetAclTest(23, "_______________+_____+++++++++++", "_______________r___________rrrrr", "_____________________+++++______", "________________________________"), msg);
            //osszes jog viszonya az open minorhoz kepest (mind kell)
            Assert.IsNull(msg = SetAclTest(25, "_______________+__________++++++", "_______________r___________rrrrr", "________________________+_______", "________________________+_++++++"), msg);
            Assert.IsNull(msg = SetAclTest(26, "_______________+__________++++++", "_______________r___________rrrrr", "_______________________+________", "_______________________+__++++++"), msg);
            Assert.IsNull(msg = SetAclTest(27, "_______________+__________++++++", "_______________r___________rrrrr", "______________________+_________", "______________________+___++++++"), msg);
            Assert.IsNull(msg = SetAclTest(28, "_______________+__________++++++", "_______________r___________rrrrr", "_____________________+__________", "_____________________+____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(29, "_______________+__________++++++", "_______________r___________rrrrr", "____________________+___________", "____________________+_____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(30, "_______________+__________++++++", "_______________r___________rrrrr", "___________________+____________", "___________________+______++++++"), msg);
            Assert.IsNull(msg = SetAclTest(31, "_______________+__________++++++", "_______________r___________rrrrr", "__________________+_____________", "__________________+_______++++++"), msg);
            //(open minor) 
            Assert.IsNull(msg = SetAclTest(24, "_______________+__________++++++", "_______________r___________rrrrr", "_________________________+______", "_________________________+++++++"), msg);
            Assert.IsNull(msg = SetAclTest(32, "________________________________", "________________________________", "__________________________+_____", "__________________________++++++"), msg);
            //(mind kell)
            Assert.IsNull(msg = SetAclTest(33, "________________________________", "________________________________", "_________________________+______", "_________________________+++++++"), msg);
            Assert.IsNull(msg = SetAclTest(34, "________________________________", "________________________________", "________________________+_______", "________________________+_++++++"), msg);
            Assert.IsNull(msg = SetAclTest(35, "________________________________", "________________________________", "_______________________+________", "_______________________+__++++++"), msg);
            Assert.IsNull(msg = SetAclTest(36, "________________________________", "________________________________", "______________________+_________", "______________________+___++++++"), msg);
            Assert.IsNull(msg = SetAclTest(37, "________________________________", "________________________________", "_____________________+__________", "_____________________+____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(38, "________________________________", "________________________________", "____________________+___________", "____________________+_____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(39, "________________________________", "________________________________", "___________________+____________", "___________________+______++++++"), msg);
            Assert.IsNull(msg = SetAclTest(40, "________________________________", "________________________________", "__________________+_____________", "__________________+_______++++++"), msg);
            //
            Assert.IsNull(msg = SetAclTest(41, "_______________+__________++++++", "_______________r___________rrrrr", "_________________________+-_____", "______________-___---------+++++"), msg);
            Assert.IsNull(msg = SetAclTest(42, "_______________+__________++++++", "_______________r___________rrrrr", "________________________+_-_____", "______________-___---------+++++"), msg);
            Assert.IsNull(msg = SetAclTest(43, "_______________+__________++++++", "_______________r___________rrrrr", "_______________________+__-_____", "______________-___---------+++++"), msg);
            Assert.IsNull(msg = SetAclTest(44, "_______________+__________++++++", "_______________r___________rrrrr", "______________________+___-_____", "______________-___---------+++++"), msg);
            Assert.IsNull(msg = SetAclTest(45, "_______________+__________++++++", "_______________r___________rrrrr", "_____________________+____-_____", "______________-___---------+++++"), msg);
            Assert.IsNull(msg = SetAclTest(46, "_______________+__________++++++", "_______________r___________rrrrr", "____________________+_____-_____", "______________-___---------+++++"), msg);
            Assert.IsNull(msg = SetAclTest(47, "_______________+__________++++++", "_______________r___________rrrrr", "___________________+______-_____", "______________-___---------+++++"), msg);
            Assert.IsNull(msg = SetAclTest(48, "_______________+__________++++++", "_______________r___________rrrrr", "__________________+_______-_____", "______________-___---------+++++"), msg);
            //
            Assert.IsNull(msg = SetAclTest(49, "_______________+__________-+++++", "_______________r__________rrrrrr", "_________________________+______", "_________________________+++++++"), msg);
            Assert.IsNull(msg = SetAclTest(50, "_______________+__________-+++++", "_______________r__________rrrrrr", "________________________+_______", "________________________+_++++++"), msg);
            Assert.IsNull(msg = SetAclTest(51, "_______________+__________-+++++", "_______________r__________rrrrrr", "_______________________+________", "_______________________+__++++++"), msg);
            Assert.IsNull(msg = SetAclTest(52, "_______________+__________-+++++", "_______________r__________rrrrrr", "______________________+_________", "______________________+___++++++"), msg);
            Assert.IsNull(msg = SetAclTest(53, "_______________+__________-+++++", "_______________r__________rrrrrr", "_____________________+__________", "_____________________+____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(54, "_______________+__________-+++++", "_______________r__________rrrrrr", "____________________+___________", "____________________+_____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(55, "_______________+__________-+++++", "_______________r__________rrrrrr", "___________________+____________", "___________________+______++++++"), msg);
            Assert.IsNull(msg = SetAclTest(56, "_______________+__________-+++++", "_______________r__________rrrrrr", "__________________+_____________", "__________________+_______++++++"), msg);
            //
            Assert.IsNull(msg = SetAclTest(57, "_______________+___________-++++", "_______________r___________rrrrr", "_________________________++_____", "_________________________+++++++"), msg);
            Assert.IsNull(msg = SetAclTest(58, "_______________+___________-++++", "_______________r___________rrrrr", "________________________+_+_____", "________________________+_++++++"), msg);
            Assert.IsNull(msg = SetAclTest(59, "_______________+___________-++++", "_______________r___________rrrrr", "_______________________+__+_____", "_______________________+__++++++"), msg);
            Assert.IsNull(msg = SetAclTest(60, "_______________+___________-++++", "_______________r___________rrrrr", "______________________+___+_____", "______________________+___++++++"), msg);
            Assert.IsNull(msg = SetAclTest(61, "_______________+___________-++++", "_______________r___________rrrrr", "_____________________+____+_____", "_____________________+____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(62, "_______________+___________-++++", "_______________r___________rrrrr", "____________________+_____+_____", "____________________+_____++++++"), msg);
            Assert.IsNull(msg = SetAclTest(63, "_______________+___________-++++", "_______________r___________rrrrr", "___________________+______+_____", "___________________+______++++++"), msg);
            Assert.IsNull(msg = SetAclTest(64, "_______________+___________-++++", "_______________r___________rrrrr", "__________________+_______+_____", "__________________+_______++++++"), msg);
            //
        }
        private string SetAclTest(int operationNumber, string initial, string readOnlyMask, string set, string expected)
        {
            if (readOnlyMask == null)
                readOnlyMask = initial.Replace("+", "r").Replace("-", "r");

            //Trace.WriteLine(String.Format("@> TEST #{0}: {1} | {2} | {3} | {4}", operationNumber, initial, readOnlyMask, set, expected));

            var node = TestRoot;
            var visitor = User.Visitor;
            var ident = new SnIdentity { Kind = SnIdentityKind.User, Name = "Visitor", NodeId = visitor.Id, Path = visitor.Path };

            var permsEd = GetPermsFromString(initial, readOnlyMask);
            var entryEd = new SnAccessControlEntry { Identity = ident, Propagates = true, Permissions = permsEd };
            var aclEd = new SnAccessControlList { NodeId = 9999, Creator = ident, Inherits = true, LastModifier = ident, Path = "asdf", Entries = new[] { entryEd } };

            var perms0 = GetPermsFromString(initial, readOnlyMask);
            var entry0 = new SnAccessControlEntry { Identity = ident, Propagates = true, Permissions = perms0 };
            var acl0 = new SnAccessControlList { NodeId = 9999, Creator = ident, Inherits = true, LastModifier = ident, Path = "asdf", Entries = new[] { entry0 } };

            var perms1 = GetPermsFromString(set, readOnlyMask);
            var entry1 = new SnAccessControlEntry { Identity = ident, Propagates = true, Permissions = perms1 };
            var acl1 = new SnAccessControlList { NodeId = 9999, Creator = ident, Inherits = true, LastModifier = ident, Path = "asdf", Entries = new[] { entry1 } };

            var ed = node.Security.GetAclEditor();
            ed.Acl = aclEd; // clone of acl0
            var edAcc = new AclEditorAccessor(ed);
            var secAcc = new SecurityHandlerAccessor(node.Security);
            var entries = secAcc.GetEntriesFromAcl(ed, acl0, acl1);

            var resultEntry = SearchEntry(entries, User.Visitor, true);
            var result = resultEntry.ValuesToString();
            if (result == expected)
                return null;

            return String.Concat("State is '", result, "', expected '", expected, "' at operation ", operationNumber);
        }
        private SnPermission[] GetPermsFromString(string values, string readOnlyMask)
        {
            var permTypes = ActiveSchema.PermissionTypes;
            var perms = new SnPermission[permTypes.Count];
            var c = values.ToCharArray();
            var r = readOnlyMask.ToCharArray();
            for (int i = 0; i < permTypes.Count; i++)
            {
                var ci = c.Length - 1 - i;
                var pt = permTypes[i];
                perms[i] = new SnPermission
                {
                    Name = permTypes[i].Name,
                    Allow = c[ci] == '+',
                    AllowFrom = r[ci] == 'r' ? "/path" : null,
                    Deny = c[ci] == '-',
                    DenyFrom = r[ci] == 'r' ? "/path" : null
                };
            }
            return perms;
        }
        private SecurityEntry SearchEntry(IEnumerable<SecurityEntry> entries, ISecurityMember member, bool propagates)
        {
            return entries.Where(x => x.PrincipalId == member.Id && x.Propagates == propagates).FirstOrDefault();
        }

        //============================================================================================ Tools

        private SecurityAccessor GetSecurity(string src)
        {
            var pt = new PrivateType(typeof(PermissionEvaluator));
            var instance = pt.InvokeStatic("Parse", src);
            return new SecurityAccessor((PermissionEvaluator)instance);
        }
        internal static string PermissionsToString(PermissionValue[] permissionValues)
        {
            var chars = new char[permissionValues.Length];
            for (int i = 0; i < permissionValues.Length; i++)
            {
                var ii = permissionValues.Length - i - 1;
                switch (permissionValues[i])
                {
                    case PermissionValue.NonDefined: chars[ii] = '_'; break;
                    case PermissionValue.Allow: chars[ii] = '+'; break;
                    case PermissionValue.Deny: chars[ii] = '-'; break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return new String(chars);
        }
        internal static string GetEntriesToString(SecurityEntry[] entries)
        {
            return String.Join("\r\n", entries.Select(x => x.ToString()).ToArray());
        }
        internal static PermissionType[] GetPermissionTypeFromIdArray(int[] idArray)
        {
            var result = new PermissionType[idArray.Length];
            for (int i = 0; i < idArray.Length; i++)
                result[i] = PermissionType.GetById(idArray[i]);
            return result;
        }
        internal static string ExportPermisions(Node node)
        {
            var sb = new StringBuilder();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.IndentChars = "  ";
            var writer = XmlWriter.Create(sb, settings);

            writer.WriteStartElement("Permissions");
            node.Security.ExportPermissions(writer);
            writer.WriteEndElement();
            writer.Flush();
            writer.Close();

            return sb.ToString();
        }

        private class TestMembershipExtender : MembershipExtenderBase
        {
            public override MembershipExtension GetExtension(IUser user)
            {
                return new MembershipExtension(new TestGroup[]
                {
                    new TestGroup{Id=111, Path="/a/b/111"}
                });
            }
        }
        private class TestGroup : IGroup
        {
            public IEnumerable<Node> Members
            {
                get { throw new NotImplementedException(); }
            }
            public int Id { get; set; }
            public string Path { get; set; }
        }

    }
}
