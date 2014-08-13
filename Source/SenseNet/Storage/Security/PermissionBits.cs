using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Storage.Security
{
    public class PermissionBits
    {
        public uint AllowBits { get; protected set; }
        public uint DenyBits { get; protected set; }
        public PermissionValue[] PermissionValues { get { return GetPermissionValues(); } }

        public PermissionBits(uint allowBits, uint denyBits)
        {
            AllowBits = allowBits;
            DenyBits = denyBits;
        }
        public PermissionBits(PermissionValue[] values)
        {
            SetPermissionValues(values);
        }

        protected string AllowBitsString
        {
            get { return Convert.ToString(AllowBits, 2); }
        }
        protected string DenyBitsString
        {
            get { return Convert.ToString(DenyBits, 2); }
        }
        protected string ValuesString
        {
            get
            {
                var values = GetPermissionValues();
                var chars = new char[ActiveSchema.PermissionTypes.Count];
                for (int i = 0; i < values.Length; i++)
                {
                    switch (values[values.Length - i - 1])
                    {
                        case PermissionValue.NonDefined: chars[i] = '_'; break;
                        case PermissionValue.Allow: chars[i] = '+'; break;
                        case PermissionValue.Deny: chars[i] = '-'; break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                return new String(chars);
            }
        }

        private PermissionValue[] GetPermissionValues()
        {
            var result = new PermissionValue[ActiveSchema.PermissionTypes.Count];
            var allow = AllowBits;
            var deny = DenyBits;
            for (int i = 0; i < result.Length; i++)
            {
                if ((deny & 1) == 1)
                    result[i] = PermissionValue.Deny;
                else if ((allow & 1) == 1)
                    result[i] = PermissionValue.Allow;
                else
                    result[i] = PermissionValue.NonDefined;
                allow = allow >> 1;
                deny = deny >> 1;
            }
            return result;
        }
        private void SetPermissionValues(PermissionValue[] values)
        {
            uint allow = 0;
            uint deny = 0;
            //foreach (var value in values)
            //{
            //    allow = allow << 1;
            //    deny = deny << 1;
            //    if (value == PermissionValue.Allow)
            //        allow++;
            //    else if (value == PermissionValue.Deny)
            //        deny++;
            //}
            for (int i = values.Length - 1; i >= 0; i--)
            {
                allow = allow << 1;
                deny = deny << 1;
                if (values[i] == PermissionValue.Allow)
                    allow++;
                else if (values[i] == PermissionValue.Deny)
                    deny++;
            }
            AllowBits = allow;
            DenyBits = deny;
        }

        public static readonly uint SeeBit = 0x00001u;
        public static readonly uint PreviewBit = 0x00002u;
        public static readonly uint PWaterBit = 0x00004u;
        public static readonly uint PRedaBit = 0x00008u;
        public static readonly uint OpenBit = 0x00010u;
        public static readonly uint OpenMinorBit = 0x00020u;
        public static readonly uint SaveBit = 0x00040u;
        public static readonly uint PublishBit = 0x00080u;
        public static readonly uint ForceCheckinBit = 0x00100u;
        public static readonly uint AddNewBit = 0x00200u;
        public static readonly uint ApproveBit = 0x00400u;
        public static readonly uint DeleteBit = 0x00800u;
        public static readonly uint RecallOldversionBit = 0x01000u;
        public static readonly uint DeleteoldversionBit = 0x02000u;
        public static readonly uint SeePermissionsBit = 0x04000u;
        public static readonly uint SetPermissionsBit = 0x08000u;
        public static readonly uint RunApplicationBit = 0x10000u;
        public static readonly uint ManageListsAndWorkspacesBit = 0x20000u;

        public static readonly int[][] PermissionDependencyTable = new[] {
            //                         See
            //                         |  Prev
            //                         |  |  PWater
            //                         |  |  |  PReda
            //                         |  |  |  |  Open
            //                         |  |  |  |  |  OpenMinor 
            //                         |  |  |  |  |  |  Save      
            //                         |  |  |  |  |  |  |  Publish   
            //                         |  |  |  |  |  |  |  |  Checkin   
            //                         |  |  |  |  |  |  |  |  |  AddNew    
            //                         |  |  |  |  |  |  |  |  |  |  Approve   
            //                         |  |  |  |  |  |  |  |  |  |  |  Delete    
            //                         |  |  |  |  |  |  |  |  |  |  |  |  RecallVer 
            //                         |  |  |  |  |  |  |  |  |  |  |  |  |  DeleteVer 
            //                         |  |  |  |  |  |  |  |  |  |  |  |  |  |  SeePerm   
            //                         |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  SetPerm   
            //                         |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  Run       
            //                         |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  ManageLst 
            //                         |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  01 02 03 04 05 06 07 08 09 10 11 12 13 14
            //                         |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
            /*  1 See       */ new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  2 Prev      */ new[] { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  3 PWater    */ new[] { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  4 PReda     */ new[] { 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  5 Open      */ new[] { 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  6 OpenMinor */ new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  7 Save      */ new[] { 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  8 Publish   */ new[] { 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /*  9 Checkin   */ new[] { 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 10 AddNew    */ new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 11 Approve   */ new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 12 Delete    */ new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 13 RecallVer */ new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 14 DeleteVer */ new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 15 SeePerm   */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 16 SetPerm   */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 17 Run       */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 18 ManageLst */ new[] { 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 19 Custom01  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 20 Custom02  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 21 Custom03  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 22 Custom04  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 23 Custom05  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 24 Custom06  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
            /* 25 Custom07  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
            /* 26 Custom08  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 },
            /* 27 Custom09  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
            /* 28 Custom10  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
            /* 29 Custom11  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 },
            /* 30 Custom12  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
            /* 31 Custom13  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
            /* 32 Custom14  */ new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }
        };

        internal static void SetBits(ref uint allowBits, ref uint denyBits, PermissionType permissionType, PermissionValue permissionValue)
        {
            var permCount = ActiveSchema.PermissionTypes.Count;
            var y = permissionType.Index - 1;
            var thisbit = 1u << y;
            var allowedBefore = (allowBits & thisbit) != 0;
            var deniedBefore = (denyBits & thisbit) != 0;

            switch (permissionValue)
            {
                case PermissionValue.Allow:
                    for (var x = 0; x < permCount; x++)
                        if (PermissionDependencyTable[y][x] == 1)
                        {
                            allowBits |= 1u << x;
                            denyBits &= ~(1u << x);
                        }
                    break;
                case PermissionValue.Deny:
                    for (var x = 0; x < permCount; x++)
                        if (PermissionDependencyTable[x][y] == 1)
                        {
                            allowBits &= ~(1u << x);
                            denyBits |= 1u << x;
                        }
                    break;
                case PermissionValue.NonDefined:
                    if (allowedBefore)
                    {
                        for (var x = 0; x < permCount; x++)
                            if (PermissionDependencyTable[x][y] == 1)
                                allowBits &= ~(1u << x);
                    }
                    else if (deniedBefore)
                    {
                        for (var x = 0; x < permCount; x++)
                            if (PermissionDependencyTable[y][x] == 1)
                                denyBits &= ~(1u << x);
                    }
                    break;
                default:
                    throw new NotSupportedException("Unknown PermissionValue: " + permissionValue);
            }
        }
        internal static void SetBits(ref uint allowBits, ref uint denyBits)
        {
            var perms = ActiveSchema.PermissionTypes.ToArray();
            var values = new PermissionValue[perms.Length];
            foreach (var perm in perms)
                values[perm.Index - 1] = GetValue(allowBits, denyBits, perm);
            foreach (var perm in perms)
                if (values[perm.Index - 1] == PermissionValue.Allow)
                    SetBits(ref allowBits, ref denyBits, perm, PermissionValue.Allow);
            foreach (var perm in perms)
                if (values[perm.Index - 1] == PermissionValue.Deny)
                    SetBits(ref allowBits, ref denyBits, perm, PermissionValue.Deny);
        }
        private static PermissionValue GetValue(uint allowBits, uint denyBits, PermissionType perm)
        {
            var mask = 1u << (perm.Index - 1);
            if ((denyBits & mask) != 0)
                return PermissionValue.Deny;
            if ((allowBits & mask) != 0)
                return PermissionValue.Allow;
            return PermissionValue.NonDefined;
        }
    }
}
