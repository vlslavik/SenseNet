using System;
using System.Collections;
using System.Resources;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using System.Globalization;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Xml;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Events;

namespace SenseNet.ContentRepository.Storage.Security
{
    public enum CopyPermissionMode { NoBreak, BreakWithoutClear, BreakAndClear }

    public sealed class SecurityHandler
	{
		private Node _node;

		private int NodeId
		{
			get { return _node.Id; }
		}

		internal SecurityHandler(Node node)
		{
			if (node == null)
				throw new ArgumentNullException("node");

			_node = node;
		}

        public static void Reset()
        {
            PermissionEvaluator.Reset();
        }

        internal static void Move(string sourcePath, string targetPath)
        {
            Reset();
        }
        internal static void Delete(string sourcePath)
        {
            Reset();
        }
        internal static void Rename(string originalPath, string newPath)
        {
            Reset();
        }

        //======================================================================================================== Administration methods

        public void SetPermission(IUser user, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
		{
			if (user == null)
				throw new ArgumentNullException("user");
            SetPermission(user as ISecurityMember, isInheritable, permissionType, permissionValue);
		}
        public void SetPermission(IGroup group, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
		{
			if (group == null)
				throw new ArgumentNullException("group");
            SetPermission(group as ISecurityMember, isInheritable, permissionType, permissionValue);
		}
        public void SetPermission(IOrganizationalUnit orgUnit, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
        {
            if (orgUnit == null)
                throw new ArgumentNullException("orgUnit");
            SetPermission(orgUnit as ISecurityMember, isInheritable, permissionType, permissionValue);
        }
        public void SetPermission(ISecurityMember securityMember, bool isInheritable, PermissionType permissionType, PermissionValue permissionValue)
        {
            if (securityMember == null)
                throw new ArgumentNullException("securityMember");
            if (permissionType == null)
                throw new ArgumentNullException("permissionType");

            Assert(PermissionType.SetPermissions);

            var changedData = new[] { new ChangedData { Name = "SetPermission", Original = GetAcl() } };

            var args = new CancellableNodeEventArgs(this._node, CancellableNodeEvent.PermissionChanging, changedData);
            _node.FireOnPermissionChanging(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this._node);
            var customData = args.CustomData;
	
            var entry = PermissionEvaluator.Instance.GetExplicitEntry(this._node.Path, securityMember.Id);
            var allowBits = 0u;
            var denyBits = 0u;
            if (entry != null)
            {
                allowBits = entry.AllowBits;
                denyBits = entry.DenyBits;
            }
            PermissionBits.SetBits(ref allowBits, ref denyBits, permissionType, permissionValue);

            var memberId = securityMember.Id;
            var permSet = new PermissionSet(memberId, isInheritable, allowBits, denyBits);
            entry = permSet.ToEntry(this.NodeId);

            DataProvider.Current.SetPermission(entry);

            Reset();

            changedData[0].Value = GetAcl();
            _node.FireOnPermissionChanged(changedData, customData);
        }

        public void SetPermissions(int principalId, bool isInheritable, PermissionValue[] permissionValues)
        {
            Assert(PermissionType.SetPermissions);

            var changedData = new[] { new ChangedData { Name = "SetPermissions", Original = GetAcl() } };

            var args = new CancellableNodeEventArgs(this._node, CancellableNodeEvent.PermissionChanging, changedData);
            _node.FireOnPermissionChanging(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this._node);
            var customData = args.CustomData;

            SetPermissionsWithoutReset(principalId, isInheritable, permissionValues);
            Reset();

            changedData[0].Value = GetAcl();
            _node.FireOnPermissionChanged(changedData, customData);
        }
        private void SetPermissionsWithoutReset(int principalId, bool isInheritable, PermissionValue[] permissionValues)
        {
            var permSet = new PermissionSet(principalId, isInheritable, permissionValues);
            var allowBits = permSet.AllowBits;
            var denyBits = permSet.DenyBits;

            PermissionBits.SetBits(ref allowBits, ref denyBits);
            permSet = new PermissionSet(principalId, isInheritable, allowBits, denyBits);
            var entry = permSet.ToEntry(NodeId);

            DataProvider.Current.SetPermission(entry);
        }

        public void RemoveExplicitEntries()
        {
            if (GetExplicitEntries().Length == 0)
                return;

            var changedData = new[] { new ChangedData { Name = "RemoveExplicitEntries", Original = GetAcl() } };

            var args = new CancellableNodeEventArgs(this._node, CancellableNodeEvent.PermissionChanging, changedData);
            _node.FireOnPermissionChanging(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this._node);
            var customData = args.CustomData;

            RemoveExplicitEntriesWithoutReset();
            Reset();

            changedData[0].Value = GetAcl();
            _node.FireOnPermissionChanged(changedData, customData);
        }
        private void RemoveExplicitEntriesWithoutReset()
        {
            foreach (var entry in GetExplicitEntries())
            {
                var e = new PermissionSet(entry.PrincipalId, entry.Propagates, 0, 0).ToEntry(entry.DefinedOnNodeId);
                DataProvider.Current.SetPermission(e);
            }
        }

        //public void ReplacePermissionsOnChildNodes()
        //{
        //    DataProvider.Current.ReplacePermissionsOnChildNodes(_node.Id);
        //    Reset();
        //}
        //public void BreakInheritance(int inheritanceSourceNodeId)
        //{
        //    DataProvider.Current.BreakInheritance(_node.Id, inheritanceSourceNodeId);
        //    Reset();
        //}
        public void BreakInheritance()
        {
            if (!_node.IsInherited)
                return;

            var changedData = new[] { new ChangedData { Name = "BreakInheritance", Original = GetAcl() } };

            var args = new CancellableNodeEventArgs(this._node, CancellableNodeEvent.PermissionChanging, changedData);
            _node.FireOnPermissionChanging(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this._node);
            var customData = args.CustomData;

            BreakInheritanceWithoutReset(true);
            Reset();

            changedData[0].Value = GetAcl();
            _node.FireOnPermissionChanged(changedData, customData);
        }
        private void BreakInheritanceWithoutReset(bool copy)
        {
            if (copy)
                foreach (var entry in GetEffectiveEntries())
                    SetPermissionsWithoutReset(entry.PrincipalId, entry.Propagates, entry.PermissionValues);
            DataBackingStore.BreakPermissionInheritance(_node);
        }
        public void RemoveBreakInheritance()
        {
            if (_node.IsInherited)
                return;

            var changedData = new[] { new ChangedData { Name = "RemoveBreakInheritance" } };
            var args = new CancellableNodeEventArgs(this._node, CancellableNodeEvent.PermissionChanging, changedData);
            _node.FireOnPermissionChanging(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this._node);
            var customData = args.CustomData;

            RemoveBreakInheritanceWithoutReset();
            Reset();

            _node.FireOnPermissionChanged(changedData, customData);
        }
        private void RemoveBreakInheritanceWithoutReset()
        {
            //foreach (var entry in GetEffectiveEntries())
            //    SetPermissions(entry.PrincipalId, entry.Propagates, entry.PermissionValues);
            DataBackingStore.RemoveBreakPermissionInheritance(_node);
        }

        public static void ExplicateGroupMembership()
		{
			DataProvider.Current.ExplicateGroupMemberships();
            Reset();
        }
        public static void ExplicateOrganizationUnitMemberships(IUser user)
        {
            DataProvider.Current.ExplicateOrganizationUnitMemberships(user);
            Reset();
        }

        public void ImportPermissions(XmlNode permissionsNode, string metadataPath)
        {
            Assert(PermissionType.SetPermissions);

            var changedData = new[] { new ChangedData { Name = "ImportPermissions", Original = GetAcl() } };

            var args = new CancellableNodeEventArgs(this._node, CancellableNodeEvent.PermissionChanging, changedData);
            _node.FireOnPermissionChanging(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this._node);
            var customData = args.CustomData;

            var permissionTypes = ActiveSchema.PermissionTypes;

            //-- parsing and executing 'Break' and 'Clear'
            var breakNode = permissionsNode.SelectSingleNode("Break");
            var clearNode = permissionsNode.SelectSingleNode("Clear");
            if (breakNode != null)
            {
                if (_node.IsInherited)
                    BreakInheritanceWithoutReset(clearNode == null);
            }
            else
            {
                if (!_node.IsInherited)
                    RemoveBreakInheritanceWithoutReset();
            }
            //-- executing 'Clear'
            if (clearNode != null)
                RemoveExplicitEntriesWithoutReset();

            var identityElementIndex = 0;
            foreach (XmlElement identityElement in permissionsNode.SelectNodes("Identity"))
            {
                identityElementIndex++;

                //-- checking identity path
                var path = identityElement.GetAttribute("path");
                var propagationAttr = identityElement.GetAttribute("propagation");
                var propagated = propagationAttr == null ? true : propagationAttr.ToLower() != "localonly";
                if (String.IsNullOrEmpty(path))
                    throw ImportPermissionExceptionHelper(String.Concat("Missing or empty path attribute of the Identity element ", identityElementIndex, "."), metadataPath, null);
                var pathCheck = RepositoryPath.IsValidPath(path);
                if (pathCheck != RepositoryPath.PathResult.Correct)
                    throw ImportPermissionExceptionHelper(String.Concat("Invalid path of the Identity element ", identityElementIndex, ": ", path, " (", pathCheck, ")."), metadataPath, null);

                //-- getting identity node
                var identityNode = Node.LoadNode(path);
                if (identityNode == null)
                    throw ImportPermissionExceptionHelper(String.Concat("Identity ", identityElementIndex, " was not found: ", path, "."), metadataPath, null);

                //-- initializing value array
                var values = new PermissionValue[permissionTypes.Count];
                foreach (var permType in permissionTypes)
                    values[permType.Index - 1] = PermissionValue.NonDefined;

                //-- parsing value array
                foreach (XmlElement permissionElement in identityElement.SelectNodes("*"))
                {
                    var permName = permissionElement.LocalName;
                    var permType = permissionTypes.Where(p => String.Compare(p.Name, permName, true) == 0).FirstOrDefault();
                    if (permType == null)
                        throw ImportPermissionExceptionHelper(String.Concat("Permission type was not found in Identity ", identityElementIndex, "."), metadataPath, null);

                    var permValue = PermissionValue.NonDefined;
                    switch (permissionElement.InnerText.ToLower())
                    {
                        case "allow": permValue = PermissionValue.Allow; break;
                        case "deny": permValue = PermissionValue.Deny; break;
                        default:
                            throw ImportPermissionExceptionHelper(String.Concat("Invalid permission value in Identity ", identityElementIndex, ": ", permissionElement.InnerText, ". Allowed values: Allow, Deny"), metadataPath, null);
                    }

                    values[permType.Index - 1] = permValue;
                }

                //-- setting permissions
                SetPermissionsWithoutReset(identityNode.Id, propagated, values);
            }

            Reset();

            changedData[0].Value = GetAcl();
            _node.FireOnPermissionChanged(changedData, customData);
        }
        private Exception ImportPermissionExceptionHelper(string message, string metadataPath, Exception innerException)
        {
            var msg = String.Concat("Importing permissions failed. Metadata: ", metadataPath, ". Reason: ", message);
            return new ApplicationException(msg, innerException);
        }
        public void ExportPermissions(XmlWriter writer)
        {
            if (!_node.IsInherited)
                writer.WriteElementString("Break", null);
            var entries = _node.Security.GetExplicitEntries();
            foreach (var entry in entries)
                entry.Export(writer);
        }

        public void Assert(params PermissionType[] permissionTypes)
        {
            Assert(_node, permissionTypes);
        }
        public void Assert(string message, params PermissionType[] permissionTypes)
        {
            Assert(_node, message, permissionTypes);
        }
        public static void Assert(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            Assert(node.Path, node.CreatedById, node.ModifiedById, null, permissionTypes);
        }
        public static void Assert(Node node, string message, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            Assert(node.Path, node.CreatedById, node.ModifiedById, message, permissionTypes);
        }
        public static void Assert(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            Assert(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, null, permissionTypes);
        }
        public static void Assert(NodeHead nodeHead, string message, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            Assert(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, message, permissionTypes);
        }
        private static void Assert(string path, int creatorId, int lastModifierId, string message, params PermissionType[] permissionTypes)
        {
            //if (path == RepositoryConfiguration.VisitorUserId)
            //    return;
            IUser user = AccessProvider.Current.GetCurrentUser();
            var userId = user.Id;
            if (user.Id == -1)
                return;
            if (HasPermission(path, creatorId, lastModifierId, permissionTypes))
                return;
            throw GetAccessDeniedException(path, creatorId, lastModifierId, message, permissionTypes, user);
        }

        public void AssertSubtree(params PermissionType[] permissionTypes)
        {
            AssertSubtree(_node, permissionTypes);
        }
        public void AssertSubtree(string message, params PermissionType[] permissionTypes)
        {
            AssertSubtree(_node, message, permissionTypes);
        }
        public static void AssertSubtree(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            AssertSubtree(node.Path, node.CreatedById, node.ModifiedById, null, permissionTypes);
        }
        public static void AssertSubtree(Node node, string message, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            AssertSubtree(node.Path, node.CreatedById, node.ModifiedById, message, permissionTypes);
        }
        public static void AssertSubtree(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            AssertSubtree(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, null, permissionTypes);
        }
        public static void AssertSubtree(NodeHead nodeHead, string message, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            AssertSubtree(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, message, permissionTypes);
        }
        private static void AssertSubtree(string path, int creatorId, int lastModifierId, string message, params PermissionType[] permissionTypes)
        {
            //if (path == RepositoryConfiguration.VisitorUserId)
            //    return;
            IUser user = AccessProvider.Current.GetCurrentUser();
            var userId = user.Id;
            if (user.Id == -1)
                return;
            if (HasSubTreePermission(path, creatorId, lastModifierId, permissionTypes))
                return;
            throw GetAccessDeniedException(path, creatorId, lastModifierId, message, permissionTypes, user);
        }

        public bool HasPermission(params PermissionType[] permissionTypes)
        {
            return HasPermission(_node, permissionTypes);
        }
        public static bool HasPermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasPermission(node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public static bool HasPermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return HasPermission(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        private static bool HasPermission(string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return true;
            var isCreator = user.Id == creatorId;
            var isLastModifier = user.Id == lastModifierId;
            return PermissionEvaluator.Instance.HasPermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public bool HasPermission(IUser user, params PermissionType[] permissionTypes)
        {
            return HasPermission(user, _node, permissionTypes);
        }
        public bool HasPermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasPermission(user, node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public bool HasPermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return HasPermission(user, nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        public static bool HasPermission(IUser user, string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            if (user.Id != AccessProvider.Current.GetCurrentUser().Id)
                Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            if (path == null)
                throw new ArgumentNullException("path");
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            if (user.Id == -1)
                return true;
            var isCreator = user.Id == creatorId;
            var isLastModifier = user.Id == lastModifierId;
            return PermissionEvaluator.Instance.HasPermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public bool HasSubTreePermission(params PermissionType[] permissionTypes)
        {
            return HasSubTreePermission(_node, permissionTypes);
        }
        public static bool HasSubTreePermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasSubTreePermission(node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public static bool HasSubTreePermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return HasSubTreePermission(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        private static bool HasSubTreePermission(string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return true;
            var isCreator = user.Id == creatorId;
            bool isLastModifier = user.Id == lastModifierId;
            return PermissionEvaluator.Instance.HasSubTreePermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public bool HasSubTreePermission(IUser user, params PermissionType[] permissionTypes)
        {
            return HasSubTreePermission(user, _node, permissionTypes);
        }
        public static bool HasSubTreePermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return HasSubTreePermission(user, node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public static bool HasSubTreePermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return HasSubTreePermission(user, nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        private static bool HasSubTreePermission(IUser user, string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            var userId = user.Id;
            if (userId != AccessProvider.Current.GetCurrentUser().Id)
                Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            if (path == null)
                throw new ArgumentNullException("path");
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return false;
            if (userId == -1)
                return true;
            var isCreator = userId == creatorId;
            var isLastModifier = userId == lastModifierId;
            return PermissionEvaluator.Instance.HasSubTreePermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public PermissionValue GetPermission(params PermissionType[] permissionTypes)
        {
            return GetPermission(_node, permissionTypes);
        }
        public static PermissionValue GetPermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetPermission(node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public static PermissionValue GetPermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetPermission(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        private static PermissionValue GetPermission(string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return PermissionValue.Deny;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return PermissionValue.Allow;
            var isCreator = user.Id == creatorId;
            var isLastModifier = user.Id == lastModifierId;
            return PermissionEvaluator.Instance.GetPermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public PermissionValue GetPermission(IUser user, params PermissionType[] permissionTypes)
        {
            return GetPermission(user, _node, permissionTypes);
        }
        public PermissionValue GetPermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetPermission(user, node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public PermissionValue GetPermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetPermission(user, nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        private static PermissionValue GetPermission(IUser user, string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            var userId = user.Id;
            if (userId != AccessProvider.Current.GetCurrentUser().Id)
                Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            if (path == null)
                throw new ArgumentNullException("path");
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return PermissionValue.Deny;
            if (userId == -1)
                return PermissionValue.Allow;
            var isCreator = userId == creatorId;
            var isLastModifier = userId == lastModifierId;
            return PermissionEvaluator.Instance.GetPermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public PermissionValue GetSubtreePermission(params PermissionType[] permissionTypes)
        {
            return GetSubtreePermission(_node, permissionTypes);
        }
        public static PermissionValue GetSubtreePermission(Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetSubtreePermission(node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public static PermissionValue GetSubtreePermission(NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetSubtreePermission(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        private static PermissionValue GetSubtreePermission(string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return PermissionValue.Deny;
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return PermissionValue.Allow;
            var isCreator = user.Id == creatorId;
            var isLastModifier = user.Id == lastModifierId; 
            return PermissionEvaluator.Instance.GetSubtreePermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public PermissionValue GetSubtreePermission(IUser user, params PermissionType[] permissionTypes)
        {
            return GetSubtreePermission(user, _node, permissionTypes);
        }
        public PermissionValue GetSubtreePermission(IUser user, Node node, params PermissionType[] permissionTypes)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetSubtreePermission(user, node.Path, node.CreatedById, node.ModifiedById, permissionTypes);
        }
        public PermissionValue GetSubtreePermission(IUser user, NodeHead nodeHead, params PermissionType[] permissionTypes)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetSubtreePermission(user, nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, permissionTypes);
        }
        private static PermissionValue GetSubtreePermission(IUser user, string path, int creatorId, int lastModifierId, params PermissionType[] permissionTypes)
        {
            var userId = user.Id;
            if (userId != AccessProvider.Current.GetCurrentUser().Id)
                Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            if (path == null)
                throw new ArgumentNullException("path");
            if (permissionTypes == null)
                throw new ArgumentNullException("permissionTypes");
            if (permissionTypes.Length == 0)
                return PermissionValue.Deny;
            if (userId == -1)
                return PermissionValue.Allow;
            var isCreator = userId == creatorId;
            var isLastModifier = userId == lastModifierId;
            return PermissionEvaluator.Instance.GetSubtreePermission(path.ToLower(), user, isCreator, isLastModifier, permissionTypes);
        }

        public PermissionValue[] GetAllPermissions()
        {
            return GetAllPermissions(_node);
        }
        public static PermissionValue[] GetAllPermissions(Node node)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetAllPermissions(node.Path, node.CreatedById, node.ModifiedById);
        }
        public static PermissionValue[] GetAllPermissions(NodeHead nodeHead)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetAllPermissions(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId);
        }
        private static PermissionValue[] GetAllPermissions(string path, int creatorId, int lastModifierId)
        {
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
            {
                var result = new PermissionValue[PermissionType.NumberOfPermissionTypes];
                for (int i = 0; i < PermissionType.NumberOfPermissionTypes; i++)
                    result[i] = PermissionValue.Allow;
                return result;
            }
            var isCreator = user.Id == creatorId;
            var isLastModifier = user.Id == lastModifierId;
            return PermissionEvaluator.Instance.GetAllPermissions(path.ToLower(), user, isCreator, isLastModifier);
        }

        public PermissionValue[] GetAllPermissions(IUser user)
        {
            return GetAllPermissions(user, _node);
        }
        public PermissionValue[] GetAllPermissions(IUser user, Node node)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            return GetAllPermissions(user, node.Path, node.CreatedById, node.ModifiedById);
        }
        public PermissionValue[] GetAllPermissions(IUser user, NodeHead nodeHead)
        {
            if (nodeHead == null)
                throw new ArgumentNullException("nodeHead");
            return GetAllPermissions(user, nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId);
        }
        private static PermissionValue[] GetAllPermissions(IUser user, string path, int creatorId, int lastModifierId)
        {
            var userId = user.Id;
            if (userId != AccessProvider.Current.GetCurrentUser().Id)
                Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            if (userId == -1)
            {
                var result = new PermissionValue[PermissionType.NumberOfPermissionTypes];
                for (int i = 0; i < PermissionType.NumberOfPermissionTypes; i++)
                    result[i] = PermissionValue.Allow;
                return result;
            }
            var isCreator = userId == creatorId;
            var isLastModifier = userId == lastModifierId;
            return PermissionEvaluator.Instance.GetAllPermissions(path.ToLower(), user, isCreator, isLastModifier);
        }

        public static PermittedLevel GetPermittedLevel(NodeHead nodeHead)
        {
            return GetPermittedLevel(nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId);
        }
        public static PermittedLevel GetPermittedLevel(string path, int creatorId, int lastModifierId)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            var user = AccessProvider.Current.GetCurrentUser();
            if (user.Id == -1)
                return PermittedLevel.All;
            bool isCreator = user.Id == creatorId;
            bool isLastModifier = user.Id == lastModifierId;
            return PermissionEvaluator.Instance.GetPermittedLevel(path.ToLower(), user, isCreator, isLastModifier);
        }
        public static PermittedLevel GetPermittedLevel(string path, int creatorId, int lastModifierId, IUser user)
        {
            var userId = user.Id;
            if (path == null)
                throw new ArgumentNullException("path");
            if (userId != AccessProvider.Current.GetCurrentUser().Id)
                Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            if (userId == -1)
                return PermittedLevel.All;
            bool isCreator = userId == creatorId;
            bool isLastModifier = user.Id == lastModifierId;
            return PermissionEvaluator.Instance.GetPermittedLevel(path.ToLower(), user, isCreator, isLastModifier);
        }

		public SecurityEntry[] GetAllEntries()
		{
            Assert(PermissionType.SeePermissions);
            return PermissionEvaluator.Instance.GetAllEntries(_node.Path.ToLower());
		}
        public SecurityEntry[] GetExplicitEntries()
        {
            Assert(PermissionType.SeePermissions);
            return PermissionEvaluator.Instance.GetExplicitEntries(_node.Path.ToLower());
        }
        public static SecurityEntry[] GetExplicitEntries(string path, int creatorId, int lastModifierId)
        {
            Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            return PermissionEvaluator.Instance.GetExplicitEntries(path.ToLower());
        }

        public SecurityEntry[] GetEffectiveEntries()
        {
            return GetEffectiveEntries(_node.Path.ToLower());
        }
        public static SecurityEntry[] GetEffectiveEntries(string path)
        {
            return GetEffectiveEntries(path.ToLower(), null);
        }
        public static SecurityEntry[] GetEffectiveEntries(string path, IEnumerable<int> relatedIdentities)
        {
            var head = NodeHead.Get(path);
            if (head == null)
                //throw new ContentNotFoundException(path);
                return new SecurityEntry[0];
            Assert(head, PermissionType.SeePermissions);
            return PermissionEvaluator.Instance.GetEffectiveEntries(path.ToLower(), relatedIdentities);
        }
        public static SecurityEntry[] GetEffectiveEntries(string path, int creatorId, int lastModifierId)
        {
            Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            var user = AccessProvider.Current.GetOriginalUser();
            var identities = PermissionEvaluator.Instance.GetPrincipals(user, user.Id == creatorId, user.Id == lastModifierId);
            return PermissionEvaluator.Instance.GetEffectiveEntries(path.ToLower(), identities);
        }

        //======================================================================================================== Permission queries

        public static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind)
        {
            return PermissionEvaluator.GetRelatedIdentities(contentPath, level, identityKind);
        }
        public static IDictionary<PermissionType, int> GetRelatedPermissions(string contentPath, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<string> includedTypes)
        {
            return PermissionEvaluator.GetRelatedPermissions(contentPath, level, explicitOnly, member.Id, includedTypes);
        }
        public static IEnumerable<Node> GetRelatedNodes(string contentPath, PermissionLevel level, bool explicitOnly, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            return PermissionEvaluator.GetRelatedNodes(contentPath, level, explicitOnly, member.Id, permissions);
        }

        public static IEnumerable<Node> GetRelatedIdentities(string contentPath, PermissionLevel level, IdentityKind identityKind, IEnumerable<PermissionType> permissions)
        {
            return PermissionEvaluator.GetRelatedIdentities(contentPath, level, identityKind, permissions);
        }
        public static IDictionary<Node, int> GetRelatedNodesOneLevel(string contentPath, PermissionLevel level, ISecurityMember member, IEnumerable<PermissionType> permissions)
        {
            return PermissionEvaluator.GetRelatedNodesOneLevel(contentPath, level, member.Id, permissions);
        }

        //========================================================================================================

        public bool IsInGroup(int groupId)
        {
            if (this._node is IUser)
                return PermissionEvaluator.Instance.IsInGroup(_node.Id, groupId);
            return DataProvider.IsInGroup(_node.Id, groupId);
        }
        public List<int> GetPrincipals()
        {
            return GetPrincipals(false, false);
        }
        public List<int> GetPrincipals(bool isCreator, bool isLastModifier)
        {
            var iUser = this._node as IUser;
            if (iUser != null)
                return PermissionEvaluator.Instance.GetPrincipals(iUser, isCreator, isLastModifier);

            return null;
        }

        //========================================================================================================

        public AclEditor GetAclEditor()
        {
            Assert(PermissionType.SeePermissions);
            return new AclEditor(_node);
        }

        public SnAccessControlList GetAcl()
        {
            return GetAcl(_node.Id, _node.Path, _node.CreatedById, _node.ModifiedById);
        }
        public static SnAccessControlList GetAcl(int nodeId, string path, int creatorId, int lastModifierId)
        {
            Assert(path, creatorId, lastModifierId, null, PermissionType.SeePermissions);
            return PermissionEvaluator.Instance.GetAcl(nodeId, path.ToLower(), creatorId, lastModifierId);
        }
        public void SetAcl(SnAccessControlList acl)
        {
            SetAcl(acl, true);
        }
        public void SetAcl(SnAccessControlList acl, bool reset)
        {
            SetAcl(this._node, acl, reset);
        }
        public static void SetAcl(SnAccessControlList acl, int nodeId)
        {
            SetAcl(acl, nodeId, true);
        }
        public static void SetAcl(SnAccessControlList acl, int nodeId, bool reset)
        {
            var node = Node.LoadNode(nodeId);
            if (node == null)
                throw new ContentNotFoundException(nodeId.ToString());
            SetAcl(node, acl, reset);
        }
        public static void SetAcl(SnAccessControlList acl, string path)
        {
            SetAcl(acl, path, true);
        }
        public static void SetAcl(SnAccessControlList acl, string path, bool reset)
        {
            var node = Node.LoadNode(path);
            if (node == null)
                throw new ContentNotFoundException(path);
            SetAcl(node, acl, reset);
        }
        private static void SetAcl(Node node, SnAccessControlList acl, bool reset)
        {
            Assert(node, PermissionType.SetPermissions);

            var oldAcl = node.Security.GetAcl().ToString();
            var newAcl = acl.ToString();
            var changedData = new[] { new ChangedData { Name = "Acl", Original = oldAcl, Value = newAcl } };

            var args = new CancellableNodeEventArgs(node, CancellableNodeEvent.PermissionChanging, changedData);
            node.FireOnPermissionChanging(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, node);
            var customData = args.CustomData;

            var entriesToSet = GetEntriesFromAcl(new AclEditor(node), new AclEditor(node).Acl, acl);
            WriteEntries(entriesToSet);

            if(reset)
                Reset();

            node.FireOnPermissionChanged(changedData, customData);
        }
        private static IEnumerable<SecurityEntry> GetEntriesFromAcl(AclEditor ed, SnAccessControlList origAcl, SnAccessControlList acl)
        {
            var newEntries = new List<SecurityEntry>();

            foreach (var entry in acl.Entries)
            {
                if (entry.Identity.NodeId == RepositoryConfiguration.SomebodyUserId)
                    continue;

                var origEntry = origAcl.Entries.Where(x => x.Identity.NodeId == entry.Identity.NodeId && x.Propagates == entry.Propagates).FirstOrDefault();
                if (origEntry == null)
                {
                    ed.AddEntry(entry);
                }
                else
                {
                    //---- play modifications
                    var ident = entry.Identity.NodeId;
                    var propagates = entry.Propagates;
                    var perms = entry.Permissions.ToArray();
                    var origPerms = origEntry.Permissions.ToArray();

                    //---- reset readonly bits
                    for (int i = ActiveSchema.PermissionTypes.Count - 1; i >= 0; i--)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (!perm.DenyEnabled && origPerm.Deny)
                            ed.SetPermission(ident, propagates, ActiveSchema.PermissionTypes[perm.Name], PermissionValue.NonDefined);
                        if (!perm.AllowEnabled && origPerm.Allow)
                            ed.SetPermission(ident, propagates, ActiveSchema.PermissionTypes[perm.Name], PermissionValue.NonDefined);
                    }


                    //---- reset deny bits
                    for (int i = ActiveSchema.PermissionTypes.Count - 1; i >= 0; i--)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (perm.DenyEnabled)
                            if (origPerm.Deny && !perm.Deny) // reset
                                ed.SetPermission(ident, propagates, ActiveSchema.PermissionTypes[perm.Name], PermissionValue.NonDefined);
                    }

                    //---- reset allow bits
                    for (int i = 0; i < ActiveSchema.PermissionTypes.Count; i++)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (perm.AllowEnabled)
                            if (origPerm.Allow && !perm.Allow) // reset
                                ed.SetPermission(ident, propagates, ActiveSchema.PermissionTypes[perm.Name], PermissionValue.NonDefined);
                    }
                    //---- set allow bits
                    for (int i = 0; i < ActiveSchema.PermissionTypes.Count; i++)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (perm.AllowEnabled)
                            if (!origPerm.Allow && perm.Allow) // set
                                ed.SetPermission(ident, propagates, ActiveSchema.PermissionTypes[perm.Name], PermissionValue.Allow);
                    }
                    //---- set deny bits
                    for (int i = ActiveSchema.PermissionTypes.Count - 1; i >= 0; i--)
                    {
                        var perm = perms[i];
                        var origPerm = origPerms[i];
                        if (perm.DenyEnabled)
                            if (!origPerm.Deny && perm.Deny) // set
                                ed.SetPermission(ident, propagates, ActiveSchema.PermissionTypes[perm.Name], PermissionValue.Deny);
                    }

                    //---- reset entry if it is subset of the original (entry will be removed)
                    var newEntry = ed.GetEntry(entry.Identity.NodeId, entry.Propagates);
                    var newPerms = newEntry.Permissions.ToArray();
                    var deletable = true;
                    for (int i = 0; i < newPerms.Length; i++)
                    {
                        var newPerm = newPerms[i];
                        var origPerm = origPerms[i];
                        if (newPerm.AllowEnabled && newPerm.Allow)
                        {
                            deletable = false;
                            break;
                        }
                        if (newPerm.DenyEnabled && newPerm.Deny)
                        {
                            deletable = false;
                            break;
                        }
                    }
                    if (deletable)
                        newEntry.SetPermissionsBits(0, 0);
                }
            }
            var entries = PermissionEvaluator.Instance.SetAcl(ed.Acl);
            return entries;
        }

        //========================================================================================================

        private static string BitsToString(int allowBits, int denyBits)
        {
            var chars = new char[ActiveSchema.PermissionTypes.Count];
            var max = chars.Length - 1;
            for (int i = 0; i < chars.Length; i++)
            {
                if ((allowBits & (1 << i)) != 0) chars[max - i] = '+';
                else if ((denyBits & (1 << i)) != 0) chars[max - i] = '-';
                else chars[max - i] = '_';
            }
            return new String(chars);
        }

        //========================================================================================================

        /*!!!*/
        private static Exception GetAccessDeniedException(string path, int creatorId, int lastModifierId, string message, PermissionType[] permissionTypes, IUser user)
        {
            //TODO: #### az exception-ben legyen informacio, hogy a see pattant-e el!

            PermissionType deniedPermission = null;
            foreach (var permType in permissionTypes)
            {
                if (!HasSubTreePermission(path, creatorId, lastModifierId, permType))
                {
                    deniedPermission = permType;
                    break;
                }
            }

            if (deniedPermission == null)
                throw new SenseNetSecurityException(path, null, user);
            if (message != null)
                throw new SenseNetSecurityException(path, deniedPermission, user, message);
            else
                throw new SenseNetSecurityException(path, deniedPermission, user);
        }
        
        private static void WriteEntries(IEnumerable<SecurityEntry> entries)
        {
            foreach (var entry in entries)
                DataProvider.Current.SetPermission(entry);
        }

        /// <summary>
        /// Copies effective permissions from the source content to the target as explicite entries.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="mode">Whether a break or a permission clean is needed.</param>
        /// <param name="reset">Whether a permission system reset is needed after the copy.</param>
        public void CopyPermissionsFrom(string sourcePath, CopyPermissionMode mode, bool reset = true)
        {
            bool @break, @clear;
            switch (mode)
            {
                case CopyPermissionMode.NoBreak: @break = false; @clear = false; break;
                case CopyPermissionMode.BreakWithoutClear: @break = true; @clear = false; break;
                case CopyPermissionMode.BreakAndClear: @break = true; @clear = true; break;
                default: throw new NotImplementedException("Unknown mode: " + mode);
            }

            var targetEntries = @clear ? new List<SecurityEntry>() : PermissionEvaluator.Instance.GetEffectiveEntries(_node.Path.ToLower()).ToList();
            var sourceEntries = PermissionEvaluator.Instance.GetEffectiveEntries(sourcePath.ToLower());

            foreach (var sourceEntry in sourceEntries)
            {
                var targetEntry = targetEntries.Where(x => x.PrincipalId == sourceEntry.PrincipalId).FirstOrDefault();
                if (targetEntry == null)
                    targetEntry = new SecurityEntry(NodeId, sourceEntry.PrincipalId, sourceEntry.Propagates, sourceEntry.PermissionValues);
                else
                    targetEntry.Combine(sourceEntry);
                DataProvider.Current.SetPermission(targetEntry);
            }

            if (@break && _node.IsInherited)
                BreakInheritanceWithoutReset(false);

            if (reset)
                Reset();
        }
    }
}
