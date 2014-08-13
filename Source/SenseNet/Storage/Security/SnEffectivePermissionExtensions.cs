using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Security
{
    [DebuggerDisplay("{ToString()}")]
    public partial class SnPermission
    {
        public bool AllowEnabled
        {
            get
            {
                return string.IsNullOrEmpty(this.AllowFrom);
            }
        }

        public bool DenyEnabled
        {
            get
            {
                return string.IsNullOrEmpty(this.DenyFrom);
            }
        }

        public PermissionValue ToPermissionValue()
        {
            if (Deny)
                return PermissionValue.Deny;
            if (Allow)
                return PermissionValue.Allow;
            return PermissionValue.NonDefined;
        }

        public override string ToString()
        {
            return String.Format("{0} Allow: {1}, Deny: {2}, AllowFrom: {3}, DenyFrom: {4}", Name, Allow, Deny, AllowFrom, DenyFrom);
        }
    }
}
