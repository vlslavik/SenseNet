using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Data.Common;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Data;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Security
{
    public enum PermissionLevel { Allowed, Denied, AllowedOrDenied }
    public enum IdentityKind { All, Users, Groups, OrganizationalUnits, UsersAndGroups, UsersAndOrganizationalUnits, GroupsAndOrganizationalUnits }

    internal class PermissionEvaluator
    {
        #region //==================================================================== Distributed Action

        [Serializable]
        internal class PermissionEvaluatorResetDistributedAction : SenseNet.Communication.Messaging.DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                if (onRemote && isFromMe)
                    return;
                PermissionEvaluator.ResetPrivate();
            }
        }

        private static void DistributedReset()
        {
            new PermissionEvaluatorResetDistributedAction().Execute();
        }
        private static void ResetPrivate()
        {
            instance = null;
        }
        #endregion

        private static readonly SecurityEntry[] EmptyEntryArray = new SecurityEntry[0];

        //============================================================================= Singleton model

        private static PermissionEvaluator instance;
        private static object instanceLock = new object();

        internal static PermissionEvaluator Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                lock (instanceLock)
                {
                    if (instance != null)
                        return instance;
                    var inst = new PermissionEvaluator();
                    inst.Initialize();
                    instance = inst;
                    return instance;
                }
            }
        }

        private PermissionEvaluator() { }

        internal static PermissionEvaluator Parse(string src)
        {
            var result = new PermissionEvaluator();
            var sa = src.Trim().Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            ParseInfo(sa[0].Trim(), result);
            ParseMembership(sa[1].Trim(), result);
            return result;
        }
        private static void ParseInfo(string src, PermissionEvaluator newInstance)
        {
            var sa = src.Trim().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in sa)
            {
                var permInfo = PermissionInfo.Parse(s);

                var parent = newInstance.GetParentInfo(permInfo.Path);
                if (parent != null)
                    parent.Children.Add(permInfo);
                permInfo.Parent = parent;
                newInstance.permissionTable.Add(permInfo.Path, permInfo);
            }
        }
        private static void ParseMembership(string src, PermissionEvaluator newInstance)
        {
            var sa = src.Trim().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in sa)
            {
                var sb = s.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int user = Int32.Parse(sb[0]);
                var groups = new List<int>();
                for (int i = 1; i < sb.Length; i++)
                    groups.Add(Int32.Parse(sb[i]));
                newInstance.membership.Add(user, groups);
            }
        }


        //============================================================================= Instance implementation

        private Dictionary<string, PermissionInfo> permissionTable = new Dictionary<string, PermissionInfo>(); // Path --> 
        private Dictionary<int, List<int>> membership = new Dictionary<int, List<int>>(); // UserId --> list of ContainerId

        //============================================================================= Build structure

        private void Initialize()
        {
            using (var proc = DataProvider.CreateDataProcedure(DataProvider.Current.GetPermissionLoaderScript()))
            {
                proc.CommandType = CommandType.Text;
                using (var reader = proc.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var path = reader.GetString(1).ToLower();
                        var id = reader.GetInt32(6);
                        var creator = reader.GetInt32(2);
                        var lastModifier = reader.GetInt32(3);
                        var inherited = reader.GetByte(4) == 1;

                        AddPermissionSet(path, id, creator, lastModifier, inherited, CreatePermissionSet(reader));
                    }
                    reader.NextResult();
                    while (reader.Read())
                        AddMembershipRow(reader);
                }
            }
        }
        private PermissionSet CreatePermissionSet(DbDataReader reader)
        {
            var isSystem = reader.GetInt32(0) != 0;
            var allowBits = 0u;
            var denyBits = 0u;
            for (var i = 0; i < PermissionType.NumberOfPermissionTypes; i++)
            {
                var value = (PermissionValue)reader.GetByte(i + 9);
                var shift = i;
                if (!isSystem)
                    shift += PermissionType.NumberOfSystemPermissionTypes;
                if (value == PermissionValue.Allow)
                    allowBits |= 1u << shift;
                else if (value == PermissionValue.Deny)
                    denyBits |= 1u << shift;
            }

            var allowBits1 = PermissionType.ConvertBitsIdToIndex(allowBits);
            var denyBits1 = PermissionType.ConvertBitsIdToIndex(denyBits);

            return new PermissionSet(reader.GetInt32(7), reader.GetByte(8) != 0, allowBits1, denyBits1);
        }
        private void AddPermissionSet(string path, int id, int creator, int lastModifier, bool inherited, PermissionSet entry)
        {
            if (!permissionTable.ContainsKey(path))
                permissionTable.Add(path, CreatePermissionInfo(path, id, creator, lastModifier, inherited));
            var entity = permissionTable[path];
            
            var oldEntry = entity.PermissionSets.Where(x => x.PrincipalId == entry.PrincipalId && x.Propagates == entry.Propagates).FirstOrDefault();
            if (oldEntry == null)
                permissionTable[path].PermissionSets.Add(entry);
            else
                oldEntry.Combine(entry);
        }
        private PermissionInfo CreatePermissionInfo(string path, int id, int creator, int lastModifier, bool inherited)
        {
            var parent = GetParentInfo(path);
            var permInfo = new PermissionInfo
            {
                Path = path,
                Id = id,
                Creator = creator,
                LastModifier = lastModifier,
                Inherits = inherited,
                PermissionSets = new List<PermissionSet>(),
                Parent = parent,
                Children = new List<PermissionInfo>()
            };
            if (parent != null)
                parent.Children.Add(permInfo);
            return permInfo;
        }
        private PermissionInfo GetParentInfo(string path)
        {
            if (path.ToLower() == "/root")
                return null;

            return GetFirstInfo(RepositoryPath.GetParentPath(path));
        }
        private PermissionInfo GetFirstInfo(string path)
        {
            var p = path;
            PermissionInfo parent;
            while (true)
            {
                if (permissionTable.TryGetValue(p, out parent))
                    return parent;
                if (p.ToLower() == "/root")
                    break;
                p = RepositoryPath.GetParentPath(p);
            }
            return null;
        }

        private void AddMembershipRow(DbDataReader reader)
        {
            var containerId = reader.GetInt32(0);
            var userId = reader.GetInt32(1);
            var containerType = reader.GetString(2);
            if (!membership.ContainsKey(userId))
                membership.Add(userId, new List<int>());
            if (!membership[userId].Contains(containerId))
                membership[userId].Add(containerId);
        }

        //----------------------------------------------------------------------------- Permission queries

        private IEnumerable<Node> GetRelatedIdentitiesPrivate(string contentPath, PermissionLevel level, IdentityKind identityKind)
        {
            if (level != PermissionLevel.AllowedOrDenied)
                throw new NotImplementedException("Not implemented. Use 'AllowedOrDenied'");

            var firstPermInfo = GetFirstInfo(contentPath);
            var identityIds = new List<int>();

            if (contentPath == firstPermInfo.Path)
            {
                SearchRelatedIdentitiesInTree(level, identityKind, firstPermInfo, identityIds);
            }
            else
            {
                var contentPathSlash = contentPath + "/";
                foreach (var childPi in firstPermInfo.Children)
                {
                    if (childPi.Path == contentPath || childPi.Path.StartsWith(contentPathSlash))
                        SearchRelatedIdentitiesInTree(level, identityKind, childPi, identityIds);
                }
            }

            var identities = new NodeList<Node>(identityIds);
            return Filter(identities, identityKind);
        }
        private void SearchRelatedIdentitiesInTree(PermissionLevel level, IdentityKind identityKind, PermissionInfo node, List<int> ids)
        {
            // if breaked, adding existing parent-s effective identities
            if (!node.Inherits)
                if (node.Parent != null)
                    foreach (var entry in GetEffectiveEntries(node.Parent.Path))
                        if (!ids.Contains(entry.PrincipalId))
                            ids.Add(entry.PrincipalId);

            // adding explicite identities recursive
            foreach (var ps in node.PermissionSets)
                if (!ids.Contains(ps.PrincipalId))
                    ids.Add(ps.PrincipalId);
            foreach (var childNode in node.Children)
                SearchRelatedIdentitiesInTree(level, identityKind, childNode, ids);
        }

        private Dictionary<PermissionType, int> GetRelatedPermissionsPrivate(string contentPath, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<string> includedTypes)
        {
            if (!explicitOnly)
                throw new NotImplementedException("Not implemented. Use explicitOnly = true");

            var firstPermInfo = GetFirstInfo(contentPath);
            var counters = new Dictionary<PermissionType, int>();
            foreach (var pt in ActiveSchema.PermissionTypes)
                counters.Add(pt, 0);

            if (contentPath == firstPermInfo.Path)
            {
                SearchRelatedPermissionsInTree(firstPermInfo, identityId, counters, includedTypes, level);
            }
            else
            {
                var contentPathSlash = contentPath + "/";
                foreach (var childPi in firstPermInfo.Children)
                {
                    if (childPi.Path == contentPath || childPi.Path.StartsWith(contentPathSlash))
                        SearchRelatedPermissionsInTree(childPi, identityId, counters, includedTypes, level);
                }
            }

            return counters;
        }
        private void SearchRelatedPermissionsInTree(PermissionInfo node, int identityId, Dictionary<PermissionType, int> counters, IEnumerable<string> includedTypes, PermissionLevel level)
        {
            if (includedTypes == null || IsIncludedType(node.Path, includedTypes))
            {
                var breakedAllowed = 0u;
                var breakedDenied = 0u;
                if (!node.Inherits)
                {
                    foreach (var entry in GetEffectiveEntries(node.Parent.Path))
                    {
                        if (entry.PrincipalId == identityId)
                        {
                            breakedAllowed = entry.AllowBits;
                            breakedDenied = entry.DenyBits;
                            SetPermissionsCountersByPermissionLevel(counters, level, breakedAllowed, breakedDenied);
                        }
                    }
                }
                foreach (var ps in node.PermissionSets)
                {
                    if (ps.PrincipalId == identityId)
                    {
                        var allowBits = (uint)(ps.AllowBits & ~breakedAllowed); // breakedAllowed and breakedDenied bits have already been added.
                        var denyBits = (uint)(ps.DenyBits & ~breakedDenied);
                        SetPermissionsCountersByPermissionLevel(counters, level, allowBits, denyBits);
                    }
                }
            }

            foreach (var childNode in node.Children)
                SearchRelatedPermissionsInTree(childNode, identityId, counters, includedTypes, level);
        }
        private void SetPermissionsCountersByPermissionLevel(Dictionary<PermissionType, int> counters, PermissionLevel level, uint allowBits, uint denyBits)
        {
            switch (level)
            {
                case PermissionLevel.Allowed:
                    IncrementCounters(allowBits, counters);
                    break;
                case PermissionLevel.Denied:
                    IncrementCounters(denyBits, counters);
                    break;
                case PermissionLevel.AllowedOrDenied:
                    IncrementCounters(allowBits, counters);
                    IncrementCounters(denyBits, counters);
                    break;
                default:
                    break;
            }
        }
        private bool IsIncludedType(string path, IEnumerable<string> includedTypes)
        {
            var nt = NodeType.GetById(NodeHead.Get(path).NodeTypeId);
            foreach (var includedType in includedTypes)
                if (nt.IsInstaceOfOrDerivedFrom(includedType))
                    return true;
            return false;
        }
        private void IncrementCounters(uint bits, Dictionary<PermissionType, int> counters)
        {
            var mask = 1;
            foreach (var pt in ActiveSchema.PermissionTypes)
            {
                if ((bits & mask) > 0)
                    counters[pt]++;
                mask = mask << 1;
            }
        }

        private IEnumerable<Node> GetRelatedNodesPrivate(string contentPath, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<PermissionType> pts)
        {
            if (!explicitOnly)
                throw new NotImplementedException("Not implemented. Use explicitOnly = true");

            var firstPermInfo = GetFirstInfo(contentPath);
            var nodeIds = new List<int>();

            var permissionMask = 0u;
            foreach (var pt in pts)
                permissionMask |= 1u << (pt.Index - 1);

            if (contentPath == firstPermInfo.Path)
            {
                SearchRelatedNodesInTree(firstPermInfo, identityId, permissionMask, level, nodeIds);
            }
            else
            {
                var contentPathSlash = contentPath + "/";
                foreach (var childPi in firstPermInfo.Children)
                {
                    if (childPi.Path == contentPath || childPi.Path.StartsWith(contentPathSlash))
                        SearchRelatedNodesInTree(childPi, identityId, permissionMask, level, nodeIds);
                }
            }

            //--------------------------------
            var identities = new NodeList<Node>(nodeIds);
            return identities;
        }
        //private void SearchRelatedNodesInTree(PermissionInfo permInfo, int identityId, int permissionMask, List<int> nodeIds)
        //{
        //    if (!permInfo.Inherits)
        //    {
        //        if (permInfo.Parent != null)
        //            foreach (var entry in GetEffectiveEntries(permInfo.Parent.Path))
        //                if (entry.PrincipalId == identityId)
        //                    if (((entry.AllowBits | entry.DenyBits) & permissionMask) != 0)
        //                        if (!nodeIds.Contains(permInfo.Id))
        //                            nodeIds.Add(permInfo.Id);
        //    }
        //    else
        //    {
        //        foreach (var ps in permInfo.PermissionSets)
        //            if (ps.PrincipalId == identityId)
        //                if ((ps.AllowBits & permissionMask) > 0 || (ps.DenyBits & permissionMask) > 0)
        //                    if (!nodeIds.Contains(permInfo.Id))
        //                        nodeIds.Add(permInfo.Id);
        //    }
        //    foreach (var childNode in permInfo.Children)
        //        SearchRelatedNodesInTree(childNode, identityId, permissionMask, nodeIds);
        //}
        private void SearchRelatedNodesInTree(PermissionInfo permInfo, int identityId, uint permissionMask, PermissionLevel level, List<int> nodeIds)
        {
            if (!permInfo.Inherits)
            {
                if (permInfo.Parent != null)
                    foreach (var entry in GetEffectiveEntries(permInfo.Parent.Path))
                        if (entry.PrincipalId == identityId)
                            if (HasBits(entry, level, permissionMask))
                                if (!nodeIds.Contains(permInfo.Id))
                                    nodeIds.Add(permInfo.Id);
            }
            else
            {
                foreach (var ps in permInfo.PermissionSets)
                    if (ps.PrincipalId == identityId)
                        if(HasBits(ps, level, permissionMask))
                            if (!nodeIds.Contains(permInfo.Id))
                                nodeIds.Add(permInfo.Id);
            }
            foreach (var childNode in permInfo.Children)
                SearchRelatedNodesInTree(childNode, identityId, permissionMask, level, nodeIds);
        }
        private bool HasBits(PermissionBits permBits, PermissionLevel level, uint permissionMask )
        {
            switch (level)
            {
                case PermissionLevel.Allowed:
                    return (permBits.AllowBits & permissionMask) != 0;
                case PermissionLevel.Denied:
                    return (permBits.DenyBits & permissionMask) != 0;
                case PermissionLevel.AllowedOrDenied:
                    return ((permBits.AllowBits | permBits.DenyBits) & permissionMask) != 0;
                default:
                    throw new NotImplementedException("Unknown PermissionLevel: " + level);
            }
        }

        private IEnumerable<Node> GetRelatedIdentitiesPrivate(string contentPath, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionType> permissions)
        {
            if (level != PermissionLevel.AllowedOrDenied)
                throw new NotImplementedException("Not implemented. Use 'AllowedOrDenied'");

            var permissionMask = 0;
            foreach (var pt in permissions)
                permissionMask |= 1 << (pt.Index - 1);

            var firstPermInfo = GetFirstInfo(contentPath);
            var identityIds = new List<int>();

            if (contentPath == firstPermInfo.Path)
            {
                SearchRelatedIdentitiesInTree(level, firstPermInfo, identityKind, identityIds, permissionMask);
            }
            else
            {
                var contentPathSlash = contentPath + "/";
                foreach (var childPi in firstPermInfo.Children)
                {
                    if (childPi.Path == contentPath || childPi.Path.StartsWith(contentPathSlash))
                        SearchRelatedIdentitiesInTree(level, childPi, identityKind, identityIds, permissionMask);
                }
            }

            var identities = new NodeList<Node>(identityIds);
            return Filter(identities, identityKind);

        }
        private void SearchRelatedIdentitiesInTree(PermissionLevel level, PermissionInfo node, IdentityKind identityKind, List<int> ids, int permissionMask)
        {
            // if breaked, adding existing parent-s effective identities
            if (!node.Inherits)
                if (node.Parent != null)
                    foreach (var entry in GetEffectiveEntries(node.Parent.Path))
                        if (!ids.Contains(entry.PrincipalId))
                            ids.Add(entry.PrincipalId);

            // adding explicite identities recursive
            foreach (var ps in node.PermissionSets)
                if (((ps.AllowBits | ps.DenyBits) & permissionMask) != 0)
                    if (!ids.Contains(ps.PrincipalId))
                        ids.Add(ps.PrincipalId);
            foreach (var childNode in node.Children)
                SearchRelatedIdentitiesInTree(level, childNode, identityKind, ids, permissionMask);
        }

        private Dictionary<Node, int> GetRelatedNodesOneLevelPrivate(string contentPath, PermissionLevel level, int identityId, IEnumerable<PermissionType> permissions)
        {
            if (level != PermissionLevel.AllowedOrDenied)
                throw new NotImplementedException("Not implemented. Use 'AllowedOrDenied'");

            var permissionMask = 0;
            foreach (var pt in permissions)
                permissionMask |= 1 << (pt.Index - 1);

            var node = Node.LoadNode(contentPath);
            var folder = node as IFolder;
            if (folder == null)
                return new Dictionary<Node, int>();

            var result = new Dictionary<Node, int>();
            foreach (var child in folder.Children)
            {
                result.Add(child, 0);
                CollectRelatedItemsInSubtree(child, identityId, permissionMask, child, result);
            }

            return result;
        }
        private void CollectRelatedItemsInSubtree(Node content, int identityId, int permissionMask, Node rootContent, Dictionary<Node, int> contentCounters)
        {
            var contentPath = content.Path.ToLower();
            var permInfo = GetFirstInfo(contentPath);

            if (contentPath == permInfo.Path)
            {
                CollectRelatedItemsInSubtree(permInfo, content == rootContent, identityId, permissionMask, rootContent, contentCounters);
            }
            else
            {
                var contentPathSlash = contentPath + "/";
                foreach (var childPi in permInfo.Children)
                    if (childPi.Path == contentPath || childPi.Path.ToLower().StartsWith(contentPathSlash))
                        CollectRelatedItemsInSubtree(childPi, false, identityId, permissionMask, rootContent, contentCounters);
            }

            //var folder = content as IFolder;
            //if (folder != null)
            //    foreach (var child in folder.Children)
            //        CollectRelatedItemsInSubtree(child, identityId, permissionMask, rootNode, nodes);
        }
        private void CollectRelatedItemsInSubtree(PermissionInfo permInfo, bool isRoot, int identityId, int permissionMask, Node rootContent, Dictionary<Node, int> contentCounters)
        {
            if (!isRoot)
            {
                if (permInfo.Inherits)
                {
                    foreach (var ps in permInfo.PermissionSets)
                    {
                        if (ps.PrincipalId == identityId)
                        {
                            if ((ps.AllowBits & permissionMask) > 0 || (ps.DenyBits & permissionMask) > 0)
                            {
                                contentCounters[rootContent]++;
                                //Trace.WriteLine(String.Concat("#>", permInfo.Path, ", ", rootContent.Path, ": ", contentCounters[rootContent]));
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (permInfo.Parent != null)
                    {
                        foreach (var entry in GetEffectiveEntries(permInfo.Parent.Path))
                        {
                            if (entry.PrincipalId == identityId)
                            {
                                if (((entry.AllowBits | entry.DenyBits) & permissionMask) != 0)
                                {
                                    contentCounters[rootContent]++;
                                    //Trace.WriteLine(String.Concat("#>", permInfo.Path, ", ", rootContent.Path, ": ", contentCounters[rootContent]));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            foreach (var childPermInfo in permInfo.Children)
                CollectRelatedItemsInSubtree(childPermInfo, false, identityId, permissionMask, rootContent, contentCounters);
        }

        private IEnumerable<Node> Filter(NodeList<Node> nodeList, IdentityKind identityKind)
        {
            switch (identityKind)
            {
                case IdentityKind.All:                          return nodeList;
                case IdentityKind.Users:                        return nodeList.Where(n => n is IUser);
                case IdentityKind.Groups:                       return nodeList.Where(n => n is IGroup);
                case IdentityKind.OrganizationalUnits:          return nodeList.Where(n => n is IOrganizationalUnit);
                case IdentityKind.UsersAndGroups:               return nodeList.Where(n => n is IUser || n is IGroup);
                case IdentityKind.UsersAndOrganizationalUnits:  return nodeList.Where(n => n is IUser || n is IOrganizationalUnit);
                case IdentityKind.GroupsAndOrganizationalUnits: return nodeList.Where(n => n is ISecurityContainer);
                default:                                        throw new NotImplementedException("Unknown IdentityKind: " + identityKind);
            }
        }

        //============================================================================= Static interface

        public static void Reset()
        {
            DistributedReset();
            //instance = null;
        }

        internal bool HasPermission(string path, IUser user, bool isCreator, bool isLastModifier, PermissionType[] permissionTypes)
        {
            if (user.Id == -1)
                return true;
            var value = GetPermission(path, user, isCreator, isLastModifier, permissionTypes);

            if (RepositoryConfiguration.TracePermissionCheck)
                if (value != PermissionValue.Allow)
                    Debug.WriteLine(String.Format("HasPermission> {0}, {1}, {2}, {3}", value, String.Join("|", permissionTypes.Select(x => x.Name).ToArray()), user.Username, path));

            return value == PermissionValue.Allow;

        }
        internal bool HasSubTreePermission(string path, IUser user, bool isCreator, bool isLastModifier, PermissionType[] permissionTypes)
        {
            if (user.Id == -1)
                return true;
            var value = GetSubtreePermission(path, user, isCreator, isLastModifier, permissionTypes);
            return value == PermissionValue.Allow;
        }
        internal PermissionValue GetPermission(string path, IUser user, bool isCreator, bool isLastModifier, PermissionType[] permissionTypes)
        {
            if (user.Id == -1)
                return PermissionValue.Allow;

            //==>
            var principals = GetPrincipals(user, isCreator, isLastModifier);

            var allow = 0u;
            var deny = 0u;

            var firstPermInfo = GetFirstInfo(path);
            if (firstPermInfo.Path == path)
                firstPermInfo.AggregateLevelOnlyValues(principals, ref allow, ref deny);
            for (var permInfo = firstPermInfo; permInfo != null; permInfo = permInfo.Inherits ? permInfo.Parent : null)
                permInfo.AggregateEffectiveValues(principals, ref allow, ref deny);
            //==<

            var mask = GetPermissionMask(permissionTypes);
            if ((deny & mask) != 0)
                return PermissionValue.Deny;
            if ((allow & mask) != mask)
                return PermissionValue.NonDefined;
            return PermissionValue.Allow;
        }
        internal PermissionValue GetSubtreePermission(string path, IUser user, bool isCreator, bool isLastModifier, PermissionType[] permissionTypes)
        {
            if (user.Id == -1)
                return PermissionValue.Allow;

            //======== #1: startbits: getpermbits
            //==>
            var principals = GetPrincipals(user, isCreator, isLastModifier);

            var allow = 0u;
            var deny = 0u;

            var firstPermInfo = GetFirstInfo(path);
            if (firstPermInfo.Path == path)
                firstPermInfo.AggregateLevelOnlyValues(principals, ref allow, ref deny);
            for (var permInfo = firstPermInfo; permInfo != null; permInfo = permInfo.Inherits ? permInfo.Parent : null)
                permInfo.AggregateEffectiveValues(principals, ref allow, ref deny);
            //==<
            var mask = GetPermissionMask(permissionTypes);
            if ((deny & mask) != 0)
                return PermissionValue.Deny;
            if ((allow & mask) != mask)
                return PermissionValue.NonDefined;

            //  +r     +1+++ | +1_++ | +1+++
            //  +r/a   +1_++ | +1+++ | +1-++
            // ==============|=======|=======
            //           +++ |   _++ |   -++

            //  +r     +1+++ | +1_++ | +1+++
            //  +r/a   -1_++ | -1+++ | -1-++
            // ==============|=======|=======
            //           +++ |   _++ |   -++

            //  +r     +1+++ | +1_++ | +1+++
            //  -r/a   +1_++ | +1+++ | +1-++
            // ==============|=======|=======
            //           _++ |   _++ |   -++
            // nem fugg a permissionset.inheritable ertektol
            // denybits: or, break: nem kell ujraszamolni
            // allowbits or, break: ujraszamolni

            //PermissionInfo subTreePermInfo;
            //if (entries.TryGetValue(path, out subTreePermInfo))
            //{
            //    subTreePermInfo.GetSubtreePermission(path, principals, isCreator, isLastModifier, mask, ref allow, ref deny);
            //}
            //else
            //{
            var p = path + "/";
            var permInfos = from key in permissionTable.Keys where key.StartsWith(p) orderby key select permissionTable[key];
            foreach (var permInfo in permInfos)
            {
                if (!permInfo.Inherits)
                {
                    allow = 0;
                    foreach (var entry in permInfo.PermissionSets)
                    {
                        if (!principals.Contains(entry.PrincipalId))
                            continue;
                        allow |= entry.AllowBits;
                        deny |= entry.DenyBits;
                    }
                }
                foreach (var entry in permInfo.PermissionSets)
                {
                    if (!principals.Contains(entry.PrincipalId))
                        continue;
                    deny |= entry.DenyBits;
                }
            }
            //}

            if ((deny & mask) != 0)
                return PermissionValue.Deny;
            if ((allow & mask) != mask)
                return PermissionValue.NonDefined;
            return PermissionValue.Allow;

        }
        internal PermissionValue[] GetAllPermissions(string path, IUser user, bool isCreator, bool isLastModifier)
        {
            if (user.Id == -1)
                return GetPermissionValues(0xFFFFFFFFu, 0u);
            //==>
            var principals = GetPrincipals(user, isCreator, isLastModifier);

            var allow = 0u;
            var deny = 0u;

            var firstPermInfo = GetFirstInfo(path);
            if (firstPermInfo.Path == path)
                firstPermInfo.AggregateLevelOnlyValues(principals, ref allow, ref deny);
            for (var permInfo = firstPermInfo; permInfo != null; permInfo = permInfo.Inherits ? permInfo.Parent : null)
                permInfo.AggregateEffectiveValues(principals, ref allow, ref deny);
            //==<

            return GetPermissionValues(allow, deny);
        }
        internal PermittedLevel GetPermittedLevel(string path, IUser user, bool isCreator, bool isLastModifier)
        {
            if (user.Id == -1)
                return PermittedLevel.All;
            //==>
            var principals = GetPrincipals(user, isCreator, isLastModifier);

            var allow = 0u;
            var deny = 0u;

            var firstPermInfo = GetFirstInfo(path);
            if (firstPermInfo == null)
                throw new ApplicationException(String.Format("PermissionInfo was not found. Path: {0}, User: {1}, isCreator: {2}, isLastModifier: {3}", path, user.Username, isCreator, isLastModifier));

            if (firstPermInfo.Path == path)
                firstPermInfo.AggregateLevelOnlyValues(principals, ref allow, ref deny);
            for (var permInfo = firstPermInfo; permInfo != null; permInfo = permInfo.Inherits ? permInfo.Parent : null)
                permInfo.AggregateEffectiveValues(principals, ref allow, ref deny);
            //==<

            var x = allow & ~deny;
            PermittedLevel level;
            if ((x & PermissionBits.OpenMinorBit) != 0)
                level = PermittedLevel.All;
            else if ((x & PermissionBits.OpenBit) != 0)
                level = PermittedLevel.PublicOnly;
            else if ((x & PermissionBits.SeeBit) != 0)
                level = PermittedLevel.HeadOnly;
            else
                level = PermittedLevel.None;
            return level;

            ////HACK: harcoded implementation
            //if (userId == 1)
            //    return PermittedLevel.All;
            //if (path.StartsWith("/Root/System/ContentExplorer/explorer.aspx"))
            //    return PermittedLevel.None;
            //return PermittedLevel.PublicOnly;
        }
        internal SecurityEntry[] GetAllEntries(string path)
        {
            var firstPermInfo = GetFirstInfo(path);
            if (firstPermInfo == null)
                return EmptyEntryArray;
            return firstPermInfo.GetAllEntries().ToArray();
        }
        internal SecurityEntry[] GetExplicitEntries(string path)
        {
            //var firstPermInfo = GetFirstInfo(path);
            //if (firstPermInfo == null)
            //    return EmptyEntryArray;
            //return firstPermInfo.GetExplicitEntries().ToArray();

            PermissionInfo permInfo;
            if (!permissionTable.TryGetValue(path.ToLower(), out permInfo))
                return EmptyEntryArray;
            return permInfo.GetExplicitEntries().ToArray();
        }
        internal SecurityEntry GetExplicitEntry(string path, int identity)
        {
            PermissionInfo permInfo;
            if (!permissionTable.TryGetValue(path.ToLower(), out permInfo))
                return null;
            return permInfo.GetExplicitEntry(identity);
        }
        internal SecurityEntry[] GetEffectiveEntries(string path)
        {
            return GetEffectiveEntries(path, null);
        }
        internal SecurityEntry[] GetEffectiveEntries(string path, IEnumerable<int> relatedIdentities)
        {
            var firstPermInfo = GetFirstInfo(path);
            if (firstPermInfo == null)
                return EmptyEntryArray;
            return firstPermInfo.GetEffectiveEntries(firstPermInfo.Path == path, relatedIdentities).ToArray();
        }

        internal bool IsInGroup(int userId, int groupId)
        {
            if (groupId == RepositoryConfiguration.EveryoneGroupId)
                return userId != RepositoryConfiguration.VisitorUserId;

            if (membership.ContainsKey(userId))
            {
                bool hasStaticPermission = membership[userId].Contains(groupId);
                if (!hasStaticPermission) // check for dynamic extension permissions
                {
                    // Elevation: it does not matter if the current user has
                    // enough permissions for the requested user content or not.
                    using (new SystemAccount())
                    {
                        var user = Node.LoadNode(userId) as IUser;
                        if (user != null)
                        {
                            var extension = user.MembershipExtension;
                            if (extension != null)
                                return extension.ExtensionIds.Contains(groupId);
                        } 
                    }
                }
                return hasStaticPermission;
            }

            return false;
        }

        public static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind)
        {
            return Instance.GetRelatedIdentitiesPrivate(contentPath, level, identityKind);
        }
        public static Dictionary<PermissionType, int> GetRelatedPermissions(string contentPath, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<string> includedTypes)
        {
            return Instance.GetRelatedPermissionsPrivate(contentPath, level, explicitOnly, identityId, includedTypes);
        }
        public static IEnumerable<Node> GetRelatedNodes(string contentPath, PermissionLevel level, bool explicitOnly, int identityId, IEnumerable<PermissionType> permissionTypes)
        {
            return Instance.GetRelatedNodesPrivate(contentPath, level, explicitOnly, identityId, permissionTypes);
        }

        internal static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionType> permissions)
        {
            return Instance.GetRelatedIdentitiesPrivate(contentPath, level, identityKind, permissions);
        }
        internal static Dictionary<Node, int> GetRelatedNodesOneLevel(string contentPath, PermissionLevel level, int identityId, IEnumerable<PermissionType> permissions)
        {
            return Instance.GetRelatedNodesOneLevelPrivate(contentPath, level, identityId, permissions);
        }

        //=============================================================================

        internal List<int> GetPrincipals(IUser user, bool isCreator, bool isLastModifier)
        {
            var principals = new List<int>(new int[] { user.Id });
            if (user.Id != RepositoryConfiguration.VisitorUserId)
                principals.Add(RepositoryConfiguration.EveryoneGroupId);

            if (membership.ContainsKey(user.Id))
                principals.AddRange(membership[user.Id]);
            if (isCreator)
                principals.Add(RepositoryConfiguration.CreatorsGroupId);
            if (isLastModifier)
                principals.Add(RepositoryConfiguration.LastModifiersGroupId);
            var extension = user.MembershipExtension;
            if (extension != null)
                principals.AddRange(extension.ExtensionIds);
            return principals;
        }
        private PermissionValue[] GetPermissionValues(uint allowBits, uint denyBits)
        {
            var result = new PermissionValue[PermissionType.NumberOfPermissionTypes];
            for (int i = 0; i < PermissionType.NumberOfPermissionTypes; i++)
            {
                var allow = (allowBits & 1) == 1;
                var deny = (denyBits & 1) == 1;
                allowBits = allowBits >> 1;
                denyBits = denyBits >> 1;
                if (deny)
                    result[i] = PermissionValue.Deny;
                else if (allow)
                    result[i] = PermissionValue.Allow;
                else
                    result[i] = PermissionValue.NonDefined;
            }
            return result;
        }
        private int GetPermissionMask(PermissionType[] permissionTypes)
        {
            int mask = 0;
            foreach (var permissionType in permissionTypes)
                mask = mask | (1 << (permissionType.Index - 1));
            return mask;
        }

        //============================================================================= for editing

        internal SnAccessControlList GetAcl(int nodeId, string path, int creatorId, int lastModifierId)
        {
            var acl = new SnAccessControlList { Path = path, NodeId = nodeId, Creator = SnIdentity.Create(creatorId), LastModifier = SnIdentity.Create(lastModifierId) };
            var firstPermInfo = GetFirstInfo(path);
            if (firstPermInfo == null)
                return acl;
            return firstPermInfo.BuildAcl(acl);
        }
        internal SecurityEntry[] SetAcl(SnAccessControlList acl)
        {
            var result = new List<SecurityEntry>();

            //var acl0 = GetAcl(nodeId, path, creatorId);

            foreach (var entry in acl.Entries)
            {
                if (entry.Identity.NodeId == RepositoryConfiguration.SomebodyUserId)
                    continue;

                var values = new PermissionValue[ActiveSchema.PermissionTypes.Count];
                foreach (var perm in entry.Permissions)
                {
                    //var id = ActiveSchema.PermissionTypes[perm.Name].Id;
                    //var allow = perm.AllowFrom == null ? perm.Allow : false;
                    //var deny = perm.DenyFrom == null ? perm.Deny : false;
                    //var value = deny ? PermissionValue.Deny : (allow ? PermissionValue.Allow : PermissionValue.NonDefined);
                    //values[id - 1] = value;

                    var index = ActiveSchema.PermissionTypes[perm.Name].Index;
                    var value = perm.Deny ? PermissionValue.Deny : (perm.Allow ? PermissionValue.Allow : PermissionValue.NonDefined);
                    values[index - 1] = value;
                }

                result.Add(new SecurityEntry(acl.NodeId, entry.Identity.NodeId, entry.Propagates, values));
            }

            return result.ToArray();
        }

    }
}
