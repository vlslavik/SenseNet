using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Security;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Tests.Security
{
    [TestClass]
    public class PermissionQueryTests : TestBase
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
        private static string _testRootName = "_PermissionQueryTests";
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
                        CreatePlayground();
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
            foreach(var ident in _identities)
                if (Node.Exists(ident.Value.Path))
                    Node.ForceDelete(ident.Value.Path);
        }

        private static Dictionary<string, ISecurityMember> _identities; // name --> user
        private static Dictionary<string, PermissionType> _permissions; // P1..N --> perm type
        private void CreatePlayground()
        {
            var userRoot = Node.LoadNode("/Root/IMS/BuiltIn/Portal");
            var u1 = (User)Node.LoadNode("/Root/IMS/BuiltIn/Portal/UU1"); if (u1 == null) u1 = (User)Content.CreateNew("User", userRoot, "UU1").ContentHandler; u1.Save();
            var u2 = (User)Node.LoadNode("/Root/IMS/BuiltIn/Portal/UU2"); if (u2 == null) u2 = (User)Content.CreateNew("User", userRoot, "UU2").ContentHandler; u2.Save();
            var u3 = (User)Node.LoadNode("/Root/IMS/BuiltIn/Portal/UU3"); if (u3 == null) u3 = (User)Content.CreateNew("User", userRoot, "UU3").ContentHandler; u3.Save();
            var g1 = (Group)Node.LoadNode("/Root/IMS/BuiltIn/Portal/GG1"); if (g1 == null) g1 = (Group)Content.CreateNew("Group", userRoot, "GG1").ContentHandler; g1.Members = new[] { u1, u2 }; g1.Save();
            var g2 = (Group)Node.LoadNode("/Root/IMS/BuiltIn/Portal/GG2"); if (g2 == null) g2 = (Group)Content.CreateNew("Group", userRoot, "GG2").ContentHandler; g2.Members = new[] { u3 }; g2.Save();

            _identities = new Dictionary<string, ISecurityMember>();
            _identities.Add("U1", u1);
            _identities.Add("U2", u2);
            _identities.Add("U3", u3);
            _identities.Add("G1", g1);
            _identities.Add("G2", g2);

            _permissions = new Dictionary<string, PermissionType>();
            _permissions.Add("P1", PermissionType.Custom01);
            _permissions.Add("P2", PermissionType.Custom02);
            _permissions.Add("P3", PermissionType.Custom03);

            Node n;
            n = CreateNode("R");
            {
                n = CreateNode("RA"); Allow(n, "U1", "P1"); Allow(n, "U2", "P1"); Allow(n, "U3", "P1"); Allow(n, "G1", "P1");
                {
                    n = CreateNode("RAX");
                    {
                        n = CreateNode("RAXA"); Deny(n, "U3", "P1"); Allow(n, "G1", "P2");
                        {
                            n = CreateNode("RAXAA"); Allow(n, "U3", "P1"); Allow(n, "G2", "P3");
                            {
                                n = CreateNode("RAXAAA");
                                n = CreateNode("RAXAAB");
                                n = CreateNode("RAXAAC");
                            }
                            n = CreateNode("RAXAB");
                            {
                                n = CreateNode("RAXABA"); Allow(n, "G2", "P2");
                            }
                            n = CreateNode("RAXAC"); Allow(n, "G2", "P2");
                            {
                                n = CreateNode("RAXACA", "Car"); Allow(n, "U1", "P3");
                                n = CreateNode("RAXACB");
                                n = CreateNode("RAXACC");
                            }
                        }
                        n = CreateNode("RAXB"); Allow(n, "U1", "P2"); Allow(n, "G2", "P2");
                        {
                            n = CreateNode("RAXBA"); Allow(n, "U2", "P2");
                            n = CreateNode("RAXBB");
                            {
                                n = CreateNode("RAXBBA");
                                n = CreateNode("RAXBBB");
                                n = CreateNode("RAXBBC"); Allow(n, "U1", "P3");
                            }
                            n = CreateNode("RAXBC");
                        }
                        n = CreateNode("RAXC"); Break(n);
                        {
                            n = CreateNode("RAXCA");
                            {
                                n = CreateNode("RAXCAA");
                                n = CreateNode("RAXCAB"); Allow(n, "U2", "P1");
                                n = CreateNode("RAXCAC");
                            }
                            n = CreateNode("RAXCB");
                            n = CreateNode("RAXCC"); Allow(n, "U1", "P1"); Allow(n, "G1", "P2");
                            {
                                n = CreateNode("RAXCCA");
                                n = CreateNode("RAXCCB");
                                n = CreateNode("RAXCCC");
                            }
                        }
                    }
                }
            }
        }

        private Node CreateNode(string p, string type = null)
        {
            string name;
            var path = GetPathAndName(p, out name);
            var parent = Node.LoadNode(path);

            if (type == null)
                type = "Folder";

            var content = Content.CreateNew(type, parent, name);
            content.Save();
            return content.ContentHandler;
        }
        private string GetPathAndName(string p, out string name)
        {
            var path = new StringBuilder(TestRoot.Path);
            for (int i = 0; i < p.Length - 1; i++)
                path.Append("/").Append(p[i]);
            name = p[p.Length - 1].ToString();
            return path.ToString();
        }
        private Content GetContent(string p)
        {
            var path = new StringBuilder(TestRoot.Path);
            for (int i = 0; i < p.Length; i++)
                path.Append("/").Append(p[i]);
            return Content.Load(path.ToString());
        }

        private void Break(Node n)
        {
            n.Security.BreakInheritance();
            Clear(n, "U1", "P1");
            Clear(n, "U2", "P1");
            Clear(n, "U3", "P1");
        }
        private void Allow(Node n, string ident, string perm)
        {
            n.Security.SetPermission(_identities[ident], true, _permissions[perm], PermissionValue.Allow);
        }
        private void Clear(Node n, string ident, string perm)
        {
            n.Security.SetPermission(_identities[ident], true, _permissions[perm], PermissionValue.NonDefined);
        }
        private void Deny(Node n, string ident, string perm)
        {
            n.Security.SetPermission(_identities[ident], true, _permissions[perm], PermissionValue.Deny);
        }

        #endregion

        [TestMethod]
        public void Security_AclEditorContext()
        {
            var parent = new Folder(TestRoot) { Name = "Parent_" + Guid.NewGuid().ToString() };
            parent.Save();

            var child = new Folder(parent) { Name = "Child_" + Guid.NewGuid().ToString() };
            child.Save();

            var grandchild = new Folder(child) { Name = "GrandChild_" + Guid.NewGuid().ToString() };
            grandchild.Save();

            var identity = User.Visitor;

            for (int i = 0; i < 2; i++)
            {
                using (new SystemAccount())
                {
                    using (var aectx = new AclEditorContext())
                    {
                        var editor1 = aectx.GetAclEditor(parent);
                        editor1.SetPermission(identity, false, PermissionType.Custom01, PermissionValue.Allow);
                        var editor2 = aectx.GetAclEditor(child);
                        editor2.SetPermission(identity, false, PermissionType.Custom02, PermissionValue.Allow);
                        var editor3 = aectx.GetAclEditor(grandchild);
                        editor3.SetPermission(identity, true, PermissionType.Custom01, PermissionValue.Allow);
                    }
                }
                Assert.IsTrue(parent.Security.HasPermission((IUser)identity, PermissionType.Custom01), String.Concat(i,". run: parent hasn't Custom01"));
                Assert.IsFalse(parent.Security.HasPermission((IUser)identity, PermissionType.Custom02), String.Concat(i, ". run: parent has Custom01"));
                Assert.IsFalse(child.Security.HasPermission((IUser)identity, PermissionType.Custom01), String.Concat(i, ". run: child has Custom01"));
                Assert.IsTrue(child.Security.HasPermission((IUser)identity, PermissionType.Custom02), String.Concat(i, ". run: child hasn't Custom02"));
                Assert.IsTrue(grandchild.Security.HasPermission((IUser)identity, PermissionType.Custom01), String.Concat(i, ". run: grandchild hasn't Custom01"));
                Assert.IsFalse(grandchild.Security.HasPermission((IUser)identity, PermissionType.Custom02), String.Concat(i, ". run: grandchild has Custom02"));
            }
        }

        [TestMethod]
        public void PermissionQuery_GetRelatedIdentities()
        {
            var identities = new[] { "UU1", "UU2", "UU3", "GG1", "GG2" };
            var content = GetContent("RAX");
            var result = PermissionQuery.GetRelatedIdentities(content, PermissionLevel.AllowedOrDenied, IdentityKind.All);
            var names = result.Where(c => c.Name.StartsWith("UU") || c.Name.StartsWith("GG")).Select(c => c.Name);

            Assert.IsTrue(0 == names.Except(identities).Count(), String.Concat("Names are ", String.Join(", ", names)));
            Assert.IsTrue(0 == identities.Except(names).Count(), String.Concat("Names are ", String.Join(", ", names)));

            identities = new[] { "UU1", "UU3", "GG1", "GG2" };
            content = GetContent("RAXA");
            result = PermissionQuery.GetRelatedIdentities(content, PermissionLevel.AllowedOrDenied, IdentityKind.All);
            names = result.Where(c => c.Name.StartsWith("UU") || c.Name.StartsWith("GG")).Select(c => c.Name);

            Assert.AreEqual(0, names.Except(identities).Count());
            Assert.AreEqual(0, identities.Except(names).Count());
            Assert.IsTrue(0 == names.Except(identities).Count(), String.Concat("Names are ", String.Join(", ", names)));
            Assert.IsTrue(0 == identities.Except(names).Count(), String.Concat("Names are ", String.Join(", ", names)));

            identities = new[] { "UU1", "UU2", "UU3", "GG1" };
            content = GetContent("RAXC");
            result = PermissionQuery.GetRelatedIdentities(content, PermissionLevel.AllowedOrDenied, IdentityKind.All);
            names = result.Where(c => c.Name.StartsWith("UU") || c.Name.StartsWith("GG")).Select(c => c.Name);

            Assert.IsTrue(0 == names.Except(identities).Count(), String.Concat("Names are ", String.Join(", ", names)));
            Assert.IsTrue(0 == identities.Except(names).Count(), String.Concat("Names are ", String.Join(", ", names)));
        }
        [TestMethod]
        public void PermissionQuery_GetRelatedPermissions()
        {
            var content = GetContent("RAX");
            var result = PermissionQuery.GetRelatedPermissions(content, PermissionLevel.AllowedOrDenied, true, _identities["U1"], null);
            var resultString = String.Join(", ", result.Where(i => i.Value > 0).Select(i => String.Concat(i.Key.Name, ":", i.Value)));
            Assert.AreEqual("Custom01:2, Custom02:1, Custom03:2", resultString);

            content = GetContent("RAX");
            result = PermissionQuery.GetRelatedPermissions(content, PermissionLevel.AllowedOrDenied, true, Group.Administrators, null);
            resultString = String.Join(", ", result.Where(i => i.Value > 0).Select(i => String.Concat(i.Key.Name, ":", i.Value)));
            Assert.AreEqual("See:1, Preview:1, PreviewWithoutWatermark:1, PreviewWithoutRedaction:1, Open:1, OpenMinor:1, Save:1, Publish:1, ForceCheckin:1, AddNew:1, Approve:1, Delete:1, RecallOldVersion:1, DeleteOldVersion:1, SeePermissions:1, SetPermissions:1, RunApplication:1, ManageListsAndWorkspaces:1", resultString);
        }
        [TestMethod]
        public void PermissionQuery_GetRelatedPermissions_WithTypeFilter()
        {
            var content = GetContent("RAX");
            var result = PermissionQuery.GetRelatedPermissions(content, PermissionLevel.AllowedOrDenied, true, _identities["U1"], new[] { "Folder" });
            var resultString = String.Join(", ", result.Where(i => i.Value > 0).Select(i => String.Concat(i.Key.Name, ":", i.Value)));
            Assert.AreEqual("Custom01:2, Custom02:1, Custom03:1", resultString);

            content = GetContent("RAX");
            result = PermissionQuery.GetRelatedPermissions(content, PermissionLevel.AllowedOrDenied, true, _identities["U1"], new[] { "Car" });
            resultString = String.Join(", ", result.Where(i => i.Value > 0).Select(i => String.Concat(i.Key.Name, ":", i.Value)));
            Assert.AreEqual("Custom03:1", resultString);
        }
        [TestMethod]
        public void PermissionQuery_GetRelatedItems_AllowedOrDenied()
        {
            var content = GetContent("RAX");
            var perms = new[] { PermissionType.Custom01 };
            var result = PermissionQuery.GetRelatedItems(content, PermissionLevel.AllowedOrDenied, true, _identities["U1"], perms);
            var actual = String.Join(", ", result.Select(c => c.Path.Substring(28).Replace("/", "")));
            Assert.AreEqual("RAXC, RAXCC", actual);

            content = GetContent("RAX");
            perms = new[] { PermissionType.Custom02 };
            result = PermissionQuery.GetRelatedItems(content, PermissionLevel.AllowedOrDenied, true, _identities["U1"], perms);
            actual = String.Join(", ", result.Select(c => c.Path.Substring(28).Replace("/", "")));
            Assert.AreEqual("RAXB", actual);

            content = GetContent("RAX");
            perms = new[] { PermissionType.Custom03 };
            result = PermissionQuery.GetRelatedItems(content, PermissionLevel.AllowedOrDenied, true, _identities["U1"], perms);
            actual = String.Join(", ", result.Select(c => c.Path.Substring(28).Replace("/", "")));
            Assert.AreEqual("RAXACA, RAXBBC", actual);

            content = GetContent("RAX");
            perms = new[] { PermissionType.Custom01, PermissionType.Custom02, PermissionType.Custom03 };
            result = PermissionQuery.GetRelatedItems(content, PermissionLevel.AllowedOrDenied, true, _identities["U1"], perms);
            actual = String.Join(", ", result.Select(c => c.Path.Substring(28).Replace("/", "")));
            Assert.AreEqual("RAXACA, RAXB, RAXBBC, RAXC, RAXCC", actual);

            content = GetContent("RAX");
            perms = new[] { PermissionType.Custom01, PermissionType.Custom02, PermissionType.Custom03 };
            result = PermissionQuery.GetRelatedItems(content, PermissionLevel.AllowedOrDenied, true, _identities["U3"], perms);
            actual = String.Join(", ", result.Select(c => c.Path.Substring(28).Replace("/", "")));
            Assert.AreEqual("RAXA, RAXAA, RAXC", actual);
        }
        [TestMethod]
        public void PermissionQuery_GetRelatedItems_Allowed()
        {
            var content = GetContent("RAX");
            var perms = new[] { PermissionType.Custom01 };
            var result = PermissionQuery.GetRelatedItems(content, PermissionLevel.Allowed, true, _identities["U3"], perms);
            var actual = String.Join(", ", result.Select(c => c.Path.Substring(28).Replace("/", "")));
            Assert.AreEqual("RAXAA, RAXC", actual);
        }
        [TestMethod]
        public void PermissionQuery_GetRelatedItems_Denied()
        {
            var content = GetContent("RAX");
            var perms = new[] { PermissionType.Custom01 };
            var result = PermissionQuery.GetRelatedItems(content, PermissionLevel.Denied, true, _identities["U3"], perms);
            var actual = String.Join(", ", result.Select(c => c.Path.Substring(28).Replace("/", "")));
            Assert.AreEqual("RAXA", actual);
        }

        [TestMethod]
        public void PermissionQuery_GetRelatedGroups()
        {
            var permissionTypes = new[] { PermissionType.Custom01, PermissionType.Custom02, PermissionType.Custom03 };

            var identities = new[] { "GG1", "GG2", "Administrators", "Everyone" };
            var content = GetContent("RAX");
            var result = PermissionQuery.GetRelatedIdentities(content, PermissionLevel.AllowedOrDenied, IdentityKind.GroupsAndOrganizationalUnits, permissionTypes);
            var names = result.Select(c => c.Name);

            Assert.AreEqual(0, names.Except(identities).Count());
            Assert.AreEqual(0, identities.Except(names).Count());
        }
        [TestMethod]
        public void PermissionQuery_GetRelatedItemsOneLevel()
        {
            var permissionTypes = new[] { PermissionType.Custom01, PermissionType.Custom02, PermissionType.Custom03 };

            var content = GetContent("RA");
            var group = (Group)_identities["G1"];
            var result = PermissionQuery.GetRelatedItemsOneLevel(content, PermissionLevel.AllowedOrDenied, group, permissionTypes);
            var actual = String.Join(", ", result.Select(c => String.Concat(c.Key.Path.Substring(28).Replace("/", ""), ":", c.Value)));
            Assert.AreEqual("RAX:3", actual);

            content = GetContent("RAX");
            group = (Group)_identities["G2"];
            result = PermissionQuery.GetRelatedItemsOneLevel(content, PermissionLevel.AllowedOrDenied, group, permissionTypes);
            actual = String.Join(", ", result.Select(c => String.Concat(c.Key.Path.Substring(28).Replace("/", ""), ":", c.Value)));
            Assert.AreEqual("RAXA:3, RAXB:0, RAXC:0", actual);

            content = GetContent("RAX");
            group = (Group)_identities["G2"];
            result = PermissionQuery.GetRelatedItemsOneLevel(content, PermissionLevel.AllowedOrDenied, group, permissionTypes);
            actual = String.Join(", ", result.Select(c => String.Concat(c.Key.Path.Substring(28).Replace("/", ""), ":", c.Value)));
            Assert.AreEqual("RAXA:3, RAXB:0, RAXC:0", actual);

            content = GetContent("RAXA");
            group = (Group)_identities["G2"];
            result = PermissionQuery.GetRelatedItemsOneLevel(content, PermissionLevel.AllowedOrDenied, group, permissionTypes);
            actual = String.Join(", ", result.Select(c => String.Concat(c.Key.Path.Substring(28).Replace("/", ""), ":", c.Value)));
            Assert.AreEqual("RAXAA:0, RAXAB:1, RAXAC:0", actual);
        }

    }
}
