using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ApplicationModel;

namespace SenseNet.ContentRepository.Security
{
    public static class PermissionQuery
    {
        public static IEnumerable<Content> GetRelatedIdentities(Content content, PermissionLevel level, IdentityKind identityKind)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.GetRelatedIdentities(content.Path.ToLower(), level, identityKind).Select(n => Content.Create(n));
        }
        public static IDictionary<PermissionType, int> GetRelatedPermissions(Content content, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<string> includedTypes)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.GetRelatedPermissions(content.Path.ToLower(), level, explicitOnly, member, includedTypes);
        }
        public static IEnumerable<Content> GetRelatedItems(Content content, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.GetRelatedNodes(content.Path.ToLower(), level, explicitOnly, member, permissions).Select(n => Content.Create(n));
        }

        public static IEnumerable<Content> GetRelatedIdentities(Content content, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionType> permissions)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            return SecurityHandler.GetRelatedIdentities(content.Path.ToLower(), level, identityKind, permissions).Select(n => Content.Create(n));
        }
        public static IDictionary<Content, int> GetRelatedItemsOneLevel(Content content, PermissionLevel level, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            content.ContentHandler.Security.AssertSubtree(PermissionType.SeePermissions);
            var nodes = SecurityHandler.GetRelatedNodesOneLevel(content.Path.ToLower(), level, member, permissions);
            var result = new Dictionary<Content, int>(nodes.Count);
            foreach(var item in nodes)
                result.Add(Content.Create(item.Key), item.Value);
            return result;
        }
    }
    public static class PermissionQueryForRest
    {
        [ODataFunction]
        public static IEnumerable<Content> GetRelatedIdentities(Content content, string permissionLevel, string identityKind)
        {
            var level = GetPermissionLevel(permissionLevel);
            var kind = GetIdentityKind(identityKind);
            return PermissionQuery.GetRelatedIdentities(content, level, kind);
        }
        [ODataFunction]
        public static IDictionary<PermissionType, int> GetRelatedPermissions(Content content, string permissionLevel, bool explicitOnly, string memberPath, IEnumerable<string> includedTypes)
        {
            var level = GetPermissionLevel(permissionLevel);
            var member = GetMember(memberPath);
            return PermissionQuery.GetRelatedPermissions(content, level, explicitOnly, member, includedTypes);
        }
        [ODataFunction]
        public static IEnumerable<Content> GetRelatedItems(Content content, string permissionLevel, bool explicitOnly, string memberPath, string[] permissions)
        {
            var level = GetPermissionLevel(permissionLevel);
            var member = GetMember(memberPath);
            var perms = GetPermissionTypes(permissions);
            return PermissionQuery.GetRelatedItems(content, level, explicitOnly, member, perms);
        }

        [ODataFunction]
        public static IEnumerable<Content> GetRelatedIdentities(Content content, string permissionLevel, string identityKind, string[] permissions)
        {
            var level = GetPermissionLevel(permissionLevel);
            var perms = GetPermissionTypes(permissions);
            var kind = GetIdentityKind(identityKind);
            return PermissionQuery.GetRelatedIdentities(content, level, kind, perms);
        }
        [ODataFunction]
        public static IDictionary<Content, int> GetRelatedItemsOneLevel(Content content, string permissionLevel, string memberPath, string[] permissions)
        {
            var level = GetPermissionLevel(permissionLevel);
            var member = GetMember(memberPath);
            var perms = GetPermissionTypes(permissions);
            return PermissionQuery.GetRelatedItemsOneLevel(content, level, member, perms);
        }

        private static PermissionLevel GetPermissionLevel(string permissionLevel)
        {
            PermissionLevel level;
            if (!Enum.TryParse<PermissionLevel>(permissionLevel, true, out level))
                throw new ArgumentException(String.Format("Invalid permissionLevel argument: {0}, expected one of the following: {1}", permissionLevel,
                    String.Join(", ", Enum.GetNames(typeof(PermissionLevel)))));
            return level;
        }
        private static IdentityKind GetIdentityKind(string identityKind)
        {
            IdentityKind result;
            if (!Enum.TryParse<IdentityKind>(identityKind, true, out result))
                throw new ArgumentException(String.Format("Invalid identityKind argument: {0}, expected one of the following: {1}", identityKind,
                    String.Join(", ", Enum.GetNames(typeof(IdentityKind)))));
            return result;
        }
        private static ISecurityMember GetMember(string path)
        {
            var member = Node.LoadNode(path) as ISecurityMember;
            if (member == null)
                throw new ArgumentException("Invalid memberPath argument. Result content is not an ISecurityMember: " + path);
            return member;
        }
        //private static ISecurityContainer GetGroup(string path)
        //{
        //    var member = Node.LoadNode(path) as ISecurityContainer;
        //    if (member == null)
        //        //throw new ArgumentException("Invalid groupPath argument. Result content is not an ISecurityContainer: " + path);
        //        throw new ArgumentException("Invalid groupPath argument. Result content is not Group or OrganizationalUnit: " + path);
        //    return member;
        //}
        private static IEnumerable<PermissionType> GetPermissionTypes(string[] names)
        {
            var types = new PermissionType[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var pt = PermissionType.GetByName(names[i]);
                if (pt == null)
                    throw new ArgumentException("Unknown permission: " + names[i]);
                types[i] = pt;
            }
            return types;
        }
    }
}
