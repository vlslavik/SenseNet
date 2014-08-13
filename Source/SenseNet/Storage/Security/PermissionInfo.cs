using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    [DebuggerDisplay("Id:{Id}; Path:{Path}; Inherits:{Inherits};")]
    internal class PermissionInfo
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public int Creator { get; set; }
        public int LastModifier { get; set; }
        public bool Inherits { get; set; }
        public PermissionInfo Parent { get; set; }
        public List<PermissionInfo> Children { get; set; }
        public List<PermissionSet> PermissionSets { get; set; }

        public PermissionInfo()
        {
            Children = new List<PermissionInfo>();
            PermissionSets = new List<PermissionSet>();
        }

        internal IEnumerable<SecurityEntry> GetAllEntries()
        {
            IEnumerable<SecurityEntry> aggregatedEntries = null;
            var info = this;
            while (info != null)
            {
                var entriesOnLevel = info.GetExplicitEntries();
                if (aggregatedEntries == null)
                    aggregatedEntries = entriesOnLevel;
                else
                    aggregatedEntries = aggregatedEntries.Union(entriesOnLevel);
                if (!info.Inherits)
                    break;
                info = info.Parent;
            }
            if (aggregatedEntries == null)
                return new SecurityEntry[0];
            return aggregatedEntries;
        }
        internal IEnumerable<SecurityEntry> GetExplicitEntries()
        {
            var x = (from set in PermissionSets select set.ToEntry(this.Id));
            return x;
        }
        internal SecurityEntry GetExplicitEntry(int identity)
        {
            var permSet = PermissionSets.Where(x => x.PrincipalId == identity).FirstOrDefault();
            if (permSet == null)
                return null;
            return permSet.ToEntry(this.Id);
        }
        internal IEnumerable<SecurityEntry> GetEffectiveEntries(bool withLevelOnly, IEnumerable<int> identities = null)
        {
            if (identities == null)
                identities = GetEffectedPrincipals();
            var entryIndex = new List<int>();
            var entries = new List<SecurityEntry>();
            foreach (var principal in identities)
            {
                uint allow = 0;
                uint deny = 0;
                for (var permInfo = this; permInfo != null; permInfo = permInfo.Inherits ? permInfo.Parent : null)
                    permInfo.AggregateEffectiveValues(new List<int>(new int[] { principal }), ref allow, ref deny);
                if (allow + deny > 0)
                {
                    entries.Add(new PermissionSet(principal, true, allow, deny).ToEntry(this.Id));
                    entryIndex.Add(principal);
                }
            }
            if (!withLevelOnly)
                return entries;
            foreach (var principal in identities)
            {
                uint allow = 0;
                uint deny = 0;
                AggregateLevelOnlyValues(new List<int>(new int[] { principal }), ref allow, ref deny);
                if (allow + deny > 0)
                {
                    var index = entryIndex.IndexOf(principal);
                    if (index < 0)
                        entries.Add(new PermissionSet(principal, false, allow, deny).ToEntry(this.Id));
                    else
                        entries[index].Combine(new PermissionSet(principal, false, allow, deny));
                }
            }
            return entries;

        }
        internal List<int> GetEffectedPrincipals()
        {
            var principals = new List<int>();
            var info = this;
            while (info != null)
            {
                foreach (var set in info.PermissionSets)
                    if (!principals.Contains(set.PrincipalId))
                        principals.Add(set.PrincipalId);
                if (!info.Inherits)
                    break;
                info = info.Parent;
            }
            return principals;
        }

        internal SnAccessControlList BuildAcl(SnAccessControlList acl)
        {
            //var principals = GetEffectedPrincipals();
            var aces = new Dictionary<int, SnAccessControlEntry>();
            var localOnlyAces = new List<SnAccessControlEntry>();

            if (this.Path == acl.Path)
            {
                foreach (var permSet in this.PermissionSets)
                {
                    if (permSet.Propagates)
                        continue;

                    var princ = permSet.PrincipalId;
                    SnAccessControlEntry ace;

                    ace = SnAccessControlEntry.CreateEmpty(princ, permSet.Propagates);
                    localOnlyAces.Add(ace);

                    // get permissions and paths
                    int mask = 1;
                    for (int i = 0; i < ActiveSchema.PermissionTypes.Count; i++)
                    {
                        var permission = ace.Permissions.ElementAt(i);
                        if ((permSet.DenyBits & mask) != 0)
                        {
                            permission.Deny = true;
                            permission.DenyFrom = null;
                        }
                        var allow = (permSet.AllowBits & mask) != 0;
                        if ((permSet.AllowBits & mask) != 0)
                        {
                            permission.Allow = true;
                            permission.AllowFrom = null;
                        }
                        mask = mask << 1;
                    }
                }
            }
            for (var permInfo = this; permInfo != null; permInfo = permInfo.Inherits ? permInfo.Parent : null)
            {
                foreach (var permSet in permInfo.PermissionSets)
                {
                    if (!permSet.Propagates)
                        continue;

                    var localEntry = acl.Path == permInfo.Path;
                    // get ace by princ
                    var princ = permSet.PrincipalId;
                    SnAccessControlEntry ace;
                    if (!aces.TryGetValue(princ, out ace))
                    {
                        ace = SnAccessControlEntry.CreateEmpty(princ, permSet.Propagates);
                        aces.Add(princ, ace);
                    }

                    // get permissions and paths
                    int mask = 1;
                    for (int i = 0; i < ActiveSchema.PermissionTypes.Count; i++)
                    {
                        var permission = ace.Permissions.ElementAt(i);
                        if (!permission.Deny)
                        {
                            if ((permSet.DenyBits & mask) != 0)
                            {
                                permission.Deny = true;
                                permission.DenyFrom = SearchFirstPath(acl.Path, permInfo, permSet, mask, true);
                            }
                        }
                        if (!permission.Allow)
                        {
                            var allow = (permSet.AllowBits & mask) != 0;
                            if ((permSet.AllowBits & mask) != 0)
                            {
                                permission.Allow = true;
                                permission.AllowFrom = SearchFirstPath(acl.Path, permInfo, permSet, mask, false);
                            }
                        }
                        mask = mask << 1;
                    }
                }
            }

            acl.Inherits = acl.Path == this.Path ? this.Inherits : true;
            localOnlyAces.AddRange(aces.Values);
            acl.Entries = localOnlyAces.ToArray();
            return acl;
        }
        private string SearchFirstPath(string aclPath, PermissionInfo basePermInfo, PermissionSet permSet, int mask, bool deny)
        {
            string lastPath = basePermInfo.Path;
            for (var permInfo = basePermInfo; permInfo != null; permInfo = permInfo.Inherits ? permInfo.Parent : null)
            {
                var entry = permInfo.GetExplicitEntry(permSet.PrincipalId);
                if (entry != null)
                {
                    var bit = mask & (deny ? entry.DenyBits : entry.AllowBits);
                    if (bit == 0)
                        break;
                    if (bit != 0 && !entry.Propagates) // not propagate equals bit == 0
                        break;
                    lastPath = permInfo.Path;
                }
            }
            return aclPath == lastPath ? null : lastPath;
        }

        public void AggregateEffectiveValues(List<int> principals, ref uint allow, ref uint deny)
        {
            foreach (var permSet in this.PermissionSets)
            {
                if (!permSet.Propagates)
                    continue;
                if (!principals.Contains(permSet.PrincipalId))
                    continue;
                allow |= permSet.AllowBits;
                deny |= permSet.DenyBits;
            }
        }
        public void AggregateLevelOnlyValues(List<int> principals, ref uint allow, ref uint deny)
        {
            foreach (var permSet in this.PermissionSets)
            {
                if (permSet.Propagates)
                    continue;
                if (!principals.Contains(permSet.PrincipalId))
                    continue;
                allow |= permSet.AllowBits;
                deny |= permSet.DenyBits;
            }
        }

        /// <summary>
        /// Format: [inheritedbit] path space* (| permSet)*
        /// inheritedbit: '+' (inherited) or '-' (breaked).
        /// path: lowercase string
        /// permSet: see PermissionSet.Parse
        /// Head info and PermissionSets are separated by '|'
        /// For example: "+/root/folder|+1345__+__|+0450__+__"
        /// </summary>
        /// <param name="src">Source string with the defined format.</param>
        /// <returns>Parsed instance of Entry.</returns>
        internal static PermissionInfo Parse(string src)
        {
            var sa = src.Trim().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var s = sa[0];
            var pmax = s.Length;
            var p = 0;

            var inherited = !(s[p] == '-');
            if (s[p] == '+' || s[p] == '-')
                p++;

            var id = 0;
            var path = s.Substring(p).Trim();
            p = 0;
            while (Char.IsDigit(path[p]))
                p++;
            if (p > 0)
            {
                id = Int32.Parse(path.Substring(0, p));
                path = path.Substring(p);
            }

            var permInfo = new PermissionInfo { Inherits = inherited, Path = path, Id = id };
            for (var i = 1; i < sa.Length; i++)
                permInfo.PermissionSets.Add(PermissionSet.Parse(sa[i]));

            return permInfo;
        }
    }
}
