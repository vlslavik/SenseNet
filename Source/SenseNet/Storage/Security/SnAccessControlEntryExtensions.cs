using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Security
{
    public partial class SnAccessControlEntry
    {
        public static SnAccessControlEntry CreateEmpty(int principalId, bool propagates)
        {
            var perms = new List<SnPermission>();
            foreach (var permType in ActiveSchema.PermissionTypes) // .OrderBy(x => x.Id)
                perms.Add(new SnPermission { Name = permType.Name });
            return new SnAccessControlEntry { Identity = SnIdentity.Create(principalId), Permissions = perms, Propagates = propagates };
        }
        public void GetPermissionBits(out uint allowBits, out uint denyBits)
        {
            allowBits = 0;
            denyBits = 0;
            var index = 0;
            foreach (var perm in this.Permissions)
            {
                index = ActiveSchema.PermissionTypes[perm.Name].Index - 1;
                if (perm.Deny)
                    denyBits |= 1u << index;
                else if (perm.Allow)
                    allowBits |= 1u << index;
                //index++;
            }
        }
        public void SetPermissionsBits(uint allowBits, uint denyBits)
        {
            var index = 0;
            foreach (var perm in this.Permissions)
            {
                index = ActiveSchema.PermissionTypes[perm.Name].Index - 1;
                var mask = 1u << index;
                perm.Deny = (denyBits & mask) != 0;
                perm.Allow = (allowBits & mask) != 0;
                //index++;
            }
        }
    }
}
