using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    [DebuggerDisplay("Principal:{PrincipalId}; Propagates:{Propagates}; Values:{ValuesString}")]
    public class PermissionSet : PermissionBits
    {
        public int PrincipalId { get; private set; }
        public bool Propagates { get; private set; }

        public PermissionSet(int principalId, bool propagates, uint allowBits, uint denyBits)
            : base(allowBits, denyBits)
        {
            PrincipalId = principalId;
            Propagates = propagates;
        }
        public PermissionSet(int principalId, bool propagates, PermissionValue[] values)
            : base(values)
        {
            PrincipalId = principalId;
            Propagates = propagates;
        }

        /// <summary>
        /// Format: [inheritbit] principalid permissionflags
        /// inheritbit: '+' (inherit) or '-' (not inherit).
        /// principalid: max 10 number chars (e.g. 0000000647)
        /// permissionflags: '_' (not defined), '+' (allow) or '-' (deny)
        /// The permissionflags will be aligned right.
        /// For example: "_-_+" == "_____________________________-_+" and it means: 
        /// OpenMinor deny, See allow, other permissions are not defined.
        /// </summary>
        /// <param name="src">Source string with the defined format.</param>
        /// <returns>Parsed instance of PermissionSet.</returns>
        public static PermissionSet Parse(string src)
        {
            var s = src.Trim();
            var pmax = s.Length;
            var p = 0;

            var isInheritable = !(s[p] == '-');
            if (s[p] == '+' || s[p] == '-')
                p++;

            var p0 = p;
            while (p < pmax && Char.IsDigit(s[p]))
                p++;

            var s1 = s.Substring(p0, p - 1);
            if (s1.Length == 0)
                s1 = "0";
            var principal = Int32.Parse(s1);

            uint allow = 0;
            uint deny = 0;
            while (p < pmax)
            {
                var c = s[p];
                allow = allow << 1;
                deny = deny << 1;
                if (c == '+')
                    allow++;
                else if (c == '-')
                    deny++;
                p++;
            }

            return new PermissionSet(principal, isInheritable, allow, deny);
        }

        internal SecurityEntry ToEntry(int nodeId)
        {
            return new SecurityEntry(nodeId, PrincipalId, Propagates, this.PermissionValues);
        }

        internal void Combine(PermissionSet entry)
        {
            if (this.PrincipalId != entry.PrincipalId)
                throw new InvalidOperationException("Cannot combine permission sets with different principal");
            if (this.Propagates != entry.Propagates)
                throw new InvalidOperationException("Cannot combine permission sets with different propagation");
            AllowBits |= entry.AllowBits;
            DenyBits |= entry.DenyBits;
        }
    }
}
