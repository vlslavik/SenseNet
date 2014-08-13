using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Tests
{
    [TestClass()]
    public class GroupTest : TestBase
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
                if (Node.Exists(path))
                    Node.ForceDelete(path);
        }


        [TestMethod()]
        public void Group_Constructor()
        {
            Node parent = Repository.Root;
            Group target = new Group(parent);
            Assert.IsNotNull(target, "1. Group is null.");
        }

        [TestMethod]
        public void Group_Creation()
        {
            string testGroupAName = "TestGroupA";
            string testGroupAPath = RepositoryPath.Combine(Repository.Root.Path, testGroupAName);
            if (Node.Exists(testGroupAPath))
                Node.ForceDelete(testGroupAPath);

            string testGroupBName = "TestGroupB";
            string testGroupBPath = RepositoryPath.Combine(Repository.Root.Path, testGroupBName);
            if (Node.Exists(testGroupBPath))
                Node.ForceDelete(testGroupBPath);

            string testGroupCName = "TestGroupC";
            string testGroupCPath = RepositoryPath.Combine(Repository.Root.Path, testGroupCName);
            if (Node.Exists(testGroupCPath))
                Node.ForceDelete(testGroupCPath);

            string testUserAName = "TestUserA";
            string testUserAPath = RepositoryPath.Combine("/Root/IMS/BuiltIn/Portal", testUserAName);
            if (Node.Exists(testUserAPath))
                Node.ForceDelete(testUserAPath);
            User testUserA = new User(Node.LoadNode("/Root/IMS/BuiltIn/Portal"));
            testUserA.Name = testUserAName;
            testUserA.Save();

            AddPathToDelete(testGroupAPath);
            AddPathToDelete(testGroupBPath);
            AddPathToDelete(testGroupCPath);
            AddPathToDelete(testUserAPath);

            Group testGroupA = new Group(Repository.Root);
            testGroupA.Name = testGroupAName;
            testGroupA.AddMember(User.Administrator);
            testGroupA.AddMember(User.Visitor);
            testGroupA.Save();

            Group testGroupB = new Group(Repository.Root);
            testGroupB.Name = testGroupBName;
            testGroupB.AddMember(User.Administrator);
            testGroupB.AddMember(testGroupA);
            testGroupB.Save();

            Group testGroupC = new Group(Repository.Root);
            testGroupC.Name = testGroupCName;
            testGroupC.AddMember(testGroupB);
            testGroupC.Save();

            //testGroupA.AddMember(testGroupC);
            //testGroupA.Save();
        }


        /// <summary>
        ///A test for Administrators
        ///</summary>
        [TestMethod()]
        public void Group_HasAdministratorsGroup()
        {
            Assert.IsNotNull(Group.Administrators, "Group Administrators is null.");
        }

        /// <summary>
        ///A test for Everyone
        ///</summary>
        [TestMethod()]
        public void Group_HasEveryoneGroup()
        {
            Assert.IsNotNull(Group.Everyone, "Group Everyone is null.");
        }

        [TestMethod]
        public void Group_Bug1882_MixedMembership()
        {
            Group group;
            User user;

            var domainNode = Node.LoadNode("/Root/IMS/BuiltIn");

            var testFolder = Node.LoadNode("/Root/TestFolder_Bug1882");
            if (testFolder == null)
            {
                testFolder = new Folder(Repository.Root);
                testFolder.Name = "TestFolder_Bug1882";
                testFolder.Save();
                AddPathToDelete("/Root/TestFolder_Bug1882");
            }

            group = Node.Load<Group>("/Root/IMS/BuiltIn/G1");
            if (group == null)
            {
                group = new Group(domainNode);
                group.Name = "G1";
                group.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/G1");
            }
            testFolder.Security.SetPermission(group, true, PermissionType.AddNew, PermissionValue.Allow);

            group = Node.Load<Group>("/Root/IMS/BuiltIn/G2");
            if (group == null)
            {
                group = new Group(domainNode);
                group.Name = "G2";
                group.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/G2");
            }
            testFolder.Security.SetPermission(group, true, PermissionType.Approve, PermissionValue.Allow);

            user = Node.Load<User>("/Root/IMS/BuiltIn/U1");
            if (user == null)
            {
                user = new User(domainNode);
                user.Name = "U1";
                user.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/U1");
            }
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user, PermissionType.AddNew), "#1");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user, PermissionType.Approve), "#2");

            user = Node.Load<User>("/Root/IMS/BuiltIn/U2");
            if (user == null)
            {
                user = new User(domainNode);
                user.Name = "U2";
                user.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/U2");
            }
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user, PermissionType.AddNew), "#3");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user, PermissionType.Approve), "#4");

            //--

            var group1 = Node.Load<Group>("/Root/IMS/BuiltIn/G1");
            var group2 = Node.Load<Group>("/Root/IMS/BuiltIn/G2");
            var user1 = Node.Load<User>("/Root/IMS/BuiltIn/U1");
            var user2 = Node.Load<User>("/Root/IMS/BuiltIn/U2");

            group1.AddMember(user1);
            group1.Save();

            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user1, PermissionType.AddNew), "#5");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user1, PermissionType.Approve), "#6");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user2, PermissionType.AddNew), "#7");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user2, PermissionType.Approve), "#8");

            group2.AddMember(user2);
            group2.Save();

            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user1, PermissionType.AddNew), "#9");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user1, PermissionType.Approve), "#10");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user2, PermissionType.AddNew), "#11");
            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user2, PermissionType.Approve), "#12");

            group1 = Node.Load<Group>("/Root/IMS/BuiltIn/G1");
            group1.AddMember(Node.Load<Group>("/Root/IMS/BuiltIn/G2"));
            group1.Save();

            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user1, PermissionType.AddNew), "#13");
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user1, PermissionType.Approve), "#14");
            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user2, PermissionType.AddNew), "#15");
            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user2, PermissionType.Approve), "#16");

            //--

            group = Node.Load<Group>("/Root/IMS/BuiltIn/G3");
            if (group == null)
            {
                group = new Group(domainNode);
                group.Name = "G3";
                group.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/G3");
            }
            testFolder.Security.SetPermission(group, true, PermissionType.DeleteOldVersion, PermissionValue.Allow);

            user = Node.Load<User>("/Root/IMS/BuiltIn/U3");
            if (user == null)
            {
                user = new User(domainNode);
                user.Name = "U3";
                user.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/U3");
            }
            var group3 = Node.Load<Group>("/Root/IMS/BuiltIn/G3");
            var user3 = Node.Load<User>("/Root/IMS/BuiltIn/U3");
            group3.AddMember(user3);
            group3.Save();

            group3 = Node.Load<Group>("/Root/IMS/BuiltIn/G3");
            group2.AddMember(group3);
            group2.Save();

            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user3, PermissionType.AddNew), "#17");
            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user3, PermissionType.Approve), "#18");
            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user3, PermissionType.DeleteOldVersion), "#19");

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Group_AddSelf()
        {
            Group group;
            var domainNode = Node.LoadNode("/Root/IMS/BuiltIn");

            var name = "G_AddSelf";
            var path = "/Root/IMS/BuiltIn/" + name;
            group = Node.Load<Group>(path);
            if (group == null)
            {
                group = new Group(domainNode);
                group.Name = name;
                group.Save();
                AddPathToDelete(path);
            }

            group = Node.Load<Group>(group.Id);
            group.AddMember(group);
        }
        [TestMethod]
        public void Group_Bug740_Circular()
        {
            var domainNode = Node.LoadNode("/Root/IMS/BuiltIn");

            var groups = new Group[5];
            for (int i = 0; i < groups.Length; i++)
            {
                var name = "G_Bug740_" + i;
                var path = "/Root/IMS/BuiltIn/" + name;
                var group = Node.Load<Group>(path);
                if (group == null)
                {
                    group = new Group(domainNode);
                    group.Name = name;
                    group.Save();
                    AddPathToDelete(path);
                    groups[i] = group;
                }
            }

            var group0 = Node.Load<Group>(groups[0].Id);
            var group1 = Node.Load<Group>(groups[1].Id);
            var group2 = Node.Load<Group>(groups[2].Id);
            var group3 = Node.Load<Group>(groups[3].Id);
            var group4 = Node.Load<Group>(groups[4].Id);

            group0.AddMember(group1);
            group0.AddMember(group2);
            group1.AddMember(group2);
            group2.AddMember(group3);
            group3.AddMember(group4);

            //-- add circular ref
            try
            {
                group4.AddMember(group0);
            }
            catch (InvalidOperationException)
            {
                //-- ok
            }
        }

        [TestMethod]
        public void Group_DistinctedGetAllMemberUsers()
        {
            var userContainer = User.Administrator.Parent;
            var user1 = new User(userContainer) { Name = Guid.NewGuid().ToString(), Email = Guid.NewGuid().ToString() + "@example.com", Enabled = true }; user1.Save();
            var user2 = new User(userContainer) { Name = Guid.NewGuid().ToString(), Email = Guid.NewGuid().ToString() + "@example.com", Enabled = true }; user2.Save();
            var user3 = new User(userContainer) { Name = Guid.NewGuid().ToString(), Email = Guid.NewGuid().ToString() + "@example.com", Enabled = true }; user3.Save();
            var group1 = new Group(userContainer) { Name = Guid.NewGuid().ToString() }; group1.Save();
            var group2 = new Group(userContainer) { Name = Guid.NewGuid().ToString() }; group2.Save();
            var group3 = new Group(userContainer) { Name = Guid.NewGuid().ToString() }; group3.Save();

            group1.AddMember(user1);
            group2.AddMember(user1);
            group2.AddMember(user2);
            group3.AddMember(user1);
            group3.AddMember(user3);

            group1.AddMember(group2);
            group1.AddMember(group3);
            group1.Save();
            group2.Save();
            group3.Save();

            Assert.AreEqual(3, group1.GetAllMemberUsers().Count);

            user1.ForceDelete();
            user2.ForceDelete();
            user3.ForceDelete();
            group1.ForceDelete();
            group2.ForceDelete();
            group3.ForceDelete();
        }

        //=============================================================== Visitor doesn't member of Everyone

        [TestMethod]
        public void Group_VisitorDoesNotMemberOfEveryone()
        {
            Assert.IsFalse(User.Visitor.IsInGroup(Group.Everyone));
        }
        [TestMethod]
        public void Group_SpecialGroups_AddRemoveMember()
        {
            var userRoot = User.Administrator.Parent;
            var user = new User(userRoot);
            user.Name = Guid.NewGuid().ToString();
            user.Save();
            var group = new Group(userRoot);
            group.Name = Guid.NewGuid().ToString();
            group.Save();

            try
            {
                try { Group.Everyone.AddMember(user); Assert.Fail("Adding a user to Everyone doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.Everyone.AddMember(group); Assert.Fail("Adding a group to Everyone doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.Creators.AddMember(user); Assert.Fail("Adding a user to Creators doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.Creators.AddMember(group); Assert.Fail("Adding a group to Creators doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.LastModifiers.AddMember(user); Assert.Fail("Adding a user to LastModifiers doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.LastModifiers.AddMember(group); Assert.Fail("Adding a group to LastModifiers doesn't throws."); }
                catch (InvalidOperationException) { }

                try { Group.Everyone.RemoveMember(user); Assert.Fail("Removing test user from Everyone doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.Everyone.RemoveMember(group); Assert.Fail("Removing test group from Everyone doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.Creators.RemoveMember(user); Assert.Fail("Removing test user from Creators doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.Creators.RemoveMember(group); Assert.Fail("Removing test group from Creators doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.LastModifiers.RemoveMember(user); Assert.Fail("Removing test user from LastModifiers doesn't throws."); }
                catch (InvalidOperationException) { }
                try { Group.LastModifiers.RemoveMember(group); Assert.Fail("Removing test group from LastModifiers doesn't throws."); }
                catch (InvalidOperationException) { }
            }
            finally
            {
                user.ForceDelete();
                group.ForceDelete();
            }
        }

        //===============================================================

        [TestMethod]
        public void Group_RightFlatteningAfterDeletingAnInnerGroup()
        {
            Group group;
            User user;

            var testFolder = Node.LoadNode("/Root/TestFolder_RightFlatteningAfterDeletingAnInnerGroup");
            if (testFolder == null)
            {
                testFolder = new Folder(Repository.Root);
                testFolder.Name = "TestFolder_RightFlatteningAfterDeletingAnInnerGroup";
                testFolder.Save();
                AddPathToDelete("/Root/TestFolder_RightFlatteningAfterDeletingAnInnerGroup");
            }

            var domainNode = Node.LoadNode("/Root/IMS/BuiltIn");

            group = Node.Load<Group>("/Root/IMS/BuiltIn/G1");
            if (group == null)
            {
                group = new Group(domainNode);
                group.Name = "G1";
                group.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/G1");
            }

            group = Node.Load<Group>("/Root/IMS/BuiltIn/G2");
            if (group == null)
            {
                group = new Group(domainNode);
                group.Name = "G2";
                group.Save();
                //AddPathToDelete("/Root/IMS/BuiltIn/G2");
            }

            user = Node.Load<User>("/Root/IMS/BuiltIn/U1");
            if (user == null)
            {
                user = new User(domainNode);
                user.Name = "U1";
                user.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/U1");
            }

            user = Node.Load<User>("/Root/IMS/BuiltIn/U2");
            if (user == null)
            {
                user = new User(domainNode);
                user.Name = "U2";
                user.Save();
                AddPathToDelete("/Root/IMS/BuiltIn/U2");
            }

            //--

            var group1 = Node.Load<Group>("/Root/IMS/BuiltIn/G1");
            var group2 = Node.Load<Group>("/Root/IMS/BuiltIn/G2");
            var user1 = Node.Load<User>("/Root/IMS/BuiltIn/U1");
            var user2 = Node.Load<User>("/Root/IMS/BuiltIn/U2");

            group1.AddMember(user1);
            group1.AddMember(group2);
            group1.Save();

            group2.AddMember(user2);
            group2.Save();

            testFolder.Security.SetPermission(group1, true, PermissionType.Custom01, PermissionValue.Allow);

            Assert.IsTrue(user2.IsInGroup(group1));
            Assert.IsTrue(testFolder.Security.HasPermission((IUser)user2, PermissionType.Custom01));

            //---------------------------------------

            group2.ForceDelete();

            Assert.IsFalse(user2.IsInGroup(group1));
            Assert.IsFalse(testFolder.Security.HasPermission((IUser)user2, PermissionType.Custom01));
        }
    }
}