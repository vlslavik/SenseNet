using System;
using System.Collections.Generic;
using System.Linq;

namespace SenseNet.ContentRepository.Storage.Schema
{
    [System.Diagnostics.DebuggerDisplay("{Name} (Id={Id.ToString()}, Index={Index.ToString()}, system={IsSystemPermission})")]
    public class PermissionType : SchemaItem
    {
        private static List<string> systemPermissionNames = new List<string>{"See", "Open", "OpenMinor", "Save", "Publish", "ForceCheckin", "AddNew", "Approve", "Delete", "RecallOldVersion",
			"DeleteOldVersion", "SeePermissions", "SetPermissions", "RunApplication", "ManageListsAndWorkspaces", "Preview", "PreviewWithoutWatermark", "PreviewWithoutRedaction"};
        public static string[] SystemPermissionNames { get { return systemPermissionNames.ToArray(); } }

		private bool _isSystemPermission;
        public bool IsSystemPermission { get { return _isSystemPermission; } }
        public int Index { get; private set; }
        public uint Mask { get { return 1u << (Index - 1); } }

        public static readonly int NumberOfSystemPermissionTypes = systemPermissionNames.Count;
        public static readonly int NumberOfPermissionTypes = 32;

        internal static uint CustomMask = Convert.ToUInt32((0x00000000FFFFFFFFL << NumberOfSystemPermissionTypes) & 0x00000000FFFFFFFFL);
        internal static uint SystemMask = ~CustomMask;

		//=====================================================================================

		internal PermissionType(int id, int index, string name, ISchemaRoot schemaRoot) : base(schemaRoot, name, id)
		{
            Index = index;

            switch (name)
            {
                case "See":                      _see = this;                      _isSystemPermission = true; break;
                case "Open":                     _open = this;                     _isSystemPermission = true; break;
                case "OpenMinor":                _openMinor = this;                _isSystemPermission = true; break;
                case "Save":                     _save = this;                     _isSystemPermission = true; break;
                case "Publish":                  _publish = this;                  _isSystemPermission = true; break;
                case "ForceCheckin":             _forceCheckin = this;             _isSystemPermission = true; break;
                case "AddNew":                   _addNew = this;                   _isSystemPermission = true; break;
                case "Approve":                  _approve = this;                  _isSystemPermission = true; break;
                case "Delete":                   _delete = this;                   _isSystemPermission = true; break;
                case "RecallOldVersion":         _recallOldVersion = this;         _isSystemPermission = true; break;
                case "DeleteOldVersion":         _deleteOldVersion = this;         _isSystemPermission = true; break;
                case "SeePermissions":           _seePermissions = this;           _isSystemPermission = true; break;
                case "SetPermissions":           _setPermissions = this;           _isSystemPermission = true; break;
                case "RunApplication":           _runApplication = this;           _isSystemPermission = true; break;
                case "ManageListsAndWorkspaces": _manageListsAndWorkspaces = this; _isSystemPermission = true; break;

                case "Preview":                  _preview = this;                  _isSystemPermission = true; break;
                case "PreviewWithoutWatermark":  _previewWithoutWatermark = this;  _isSystemPermission = true; break;
                case "PreviewWithoutRedaction":  _previewWithoutRedaction = this;  _isSystemPermission = true; break;

                case "Custom01":                 _custom01 = this; break;
                case "Custom02":                 _custom02 = this; break;
                case "Custom03":                 _custom03 = this; break;
                case "Custom04":                 _custom04 = this; break;
                case "Custom05":                 _custom05 = this; break;
                case "Custom06":                 _custom06 = this; break;
                case "Custom07":                 _custom07 = this; break;
                case "Custom08":                 _custom08 = this; break;
                case "Custom09":                 _custom09 = this; break;
                case "Custom10":                 _custom10 = this; break;
                case "Custom11":                 _custom11 = this; break;
                case "Custom12":                 _custom12 = this; break;
                case "Custom13":                 _custom13 = this; break;
                case "Custom14":                 _custom14 = this; break;
                default:
                    break;
            }
		}

		//=====================================================================================

        public static PermissionType GetById(int id)
        {
            return NodeTypeManager.Current.PermissionTypes.GetItemById(id);
        }
		public static PermissionType GetByName(string permissionTypeName)
		{
			return NodeTypeManager.Current.PermissionTypes[permissionTypeName];
		}
        public static PermissionType[] GetSystemPermissions()
        {
            return NodeTypeManager.Current.PermissionTypes.Where(t => t.IsSystemPermission).ToArray();
        }
        public static PermissionType[] GetCustomPermissions()
        {
            return NodeTypeManager.Current.PermissionTypes.Where(t => !t.IsSystemPermission).ToArray();
        }

        private static PermissionType _see;
        private static PermissionType _open;
        private static PermissionType _openMinor;
        private static PermissionType _save;
        private static PermissionType _publish;
        private static PermissionType _forceCheckin;
        private static PermissionType _addNew;
        private static PermissionType _approve;
        private static PermissionType _delete;
        private static PermissionType _recallOldVersion;
        private static PermissionType _deleteOldVersion;
        private static PermissionType _seePermissions;
        private static PermissionType _setPermissions;
        private static PermissionType _runApplication;
        private static PermissionType _manageListsAndWorkspaces;
        private static PermissionType _preview;
        private static PermissionType _previewWithoutWatermark;
        private static PermissionType _previewWithoutRedaction;
        private static PermissionType _custom01;
        private static PermissionType _custom02;
        private static PermissionType _custom03;
        private static PermissionType _custom04;
        private static PermissionType _custom05;
        private static PermissionType _custom06;
        private static PermissionType _custom07;
        private static PermissionType _custom08;
        private static PermissionType _custom09;
        private static PermissionType _custom10;
        private static PermissionType _custom11;
        private static PermissionType _custom12;
        private static PermissionType _custom13;
        private static PermissionType _custom14;

        public static PermissionType See { get { return _see; } }
        public static PermissionType Open { get { return _open; } }
        public static PermissionType OpenMinor { get { return _openMinor; } }
        public static PermissionType Save { get { return _save; } }
        public static PermissionType Publish { get { return _publish; } }
        public static PermissionType ForceCheckin { get { return _forceCheckin; } }
        public static PermissionType AddNew { get { return _addNew; } }
        public static PermissionType Approve { get { return _approve; } }
        public static PermissionType Delete { get { return _delete; } }
        public static PermissionType RecallOldVersion { get { return _recallOldVersion; } }
        public static PermissionType DeleteOldVersion { get { return _deleteOldVersion; } }
        public static PermissionType SeePermissions { get { return _seePermissions; } }
        public static PermissionType SetPermissions { get { return _setPermissions; } }
        public static PermissionType RunApplication { get { return _runApplication; } }
        public static PermissionType ManageListsAndWorkspaces { get { return _manageListsAndWorkspaces; } }

        public static PermissionType Preview { get { return _preview; } }
        public static PermissionType PreviewWithoutWatermark { get { return _previewWithoutWatermark; } }
        public static PermissionType PreviewWithoutRedaction { get { return _previewWithoutRedaction; } }

        public static PermissionType Custom01 { get { return _custom01; } }
        public static PermissionType Custom02 { get { return _custom02; } }
        public static PermissionType Custom03 { get { return _custom03; } }
        public static PermissionType Custom04 { get { return _custom04; } }
        public static PermissionType Custom05 { get { return _custom05; } }
        public static PermissionType Custom06 { get { return _custom06; } }
        public static PermissionType Custom07 { get { return _custom07; } }
        public static PermissionType Custom08 { get { return _custom08; } }
        public static PermissionType Custom09 { get { return _custom09; } }
        public static PermissionType Custom10 { get { return _custom10; } }
        public static PermissionType Custom11 { get { return _custom11; } }
        public static PermissionType Custom12 { get { return _custom12; } }
        public static PermissionType Custom13 { get { return _custom13; } }
        public static PermissionType Custom14 { get { return _custom14; } }

        private static PermissionType[] _permTypesByIndex;
        private static PermissionType[] _permTypesById;
        internal static uint ConvertBitsIndexToId(uint bits)
        {
            if (_permTypesByIndex == null)
                _permTypesByIndex = ActiveSchema.PermissionTypes.ToArray();

            uint result = 0;
            uint mask = 1;
            for (int index = 0; index < PermissionType.NumberOfPermissionTypes; index++)
            {
                if ((bits & mask) != 0)
                    result = result | (1u << (_permTypesByIndex[index].Id - 1)); //@@ PermissionType.Id
                mask = mask << 1;
            }

            return result;
        }
        internal static uint ConvertBitsIdToIndex(uint bits)
        {
            if (_permTypesById == null)
                _permTypesById = ActiveSchema.PermissionTypes.OrderBy(x => x.Id).ToArray();

            uint result = 0;
            uint mask = 1;
            for (int id = 0; id < PermissionType.NumberOfPermissionTypes; id++)
            {
                if ((bits & mask) != 0)
                    result = result | (1u << (_permTypesById[id].Index - 1));
                mask = mask << 1;
            }

            return result;
        }

    }
}