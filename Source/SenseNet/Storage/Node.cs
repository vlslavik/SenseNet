using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{

    public delegate void CancellableNodeEventHandler(object sender, CancellableNodeEventArgs e);
    public delegate void CancellableNodeOperationEventHandler(object sender, CancellableNodeOperationEventArgs e);

    internal enum AccessLevel { Header, Major, Minor }
    public enum VersionRaising { None, NextMinor, NextMajor }
    public enum ContentSavingState { Finalized, Creating, Modifying, ModifyingLocked }

    /// <summary>
    /// <para>Represents a structured set of data that can be stored in the Sense/Net Content Repository.</para>
    /// <para>A node can be loaded from the Sense/Net Content Repository, the represented data can be modified via the properties of the node, and the changes can be persisted back.</para>
    /// </summary>
    /// <remarks>Remark</remarks>
    /// <example>Here is an example from the test project that creates a new Node with two integer Properties TestInt1 and TestInt2
    /// <code>
    /// [ContentHandler]
    /// public class TestNode : Node
    /// {
    /// public TestNode(Node parent) : base(parent) { }
    /// public TestNode(NodeToken token) : base(token) { }
    /// [RepositoryProperty("TestInt1")]
    /// public int TestInt
    /// {
    /// get { return (int)this["TestInt1"]; }
    /// set { this["TestInt1"] = value; }
    /// }
    /// [RepositoryProperty("TestInt2", DataType.Int)]
    /// public string TestInt2
    /// {
    /// get { return this["TestInt2"].ToString(); }
    /// set { this["TestInt2"] = Convert.ToInt32(value); }
    /// }
    /// }
    /// </code></example>
    [DebuggerDisplay("Id={Id}, Name={Name}, Version={Version}, Path={Path}")]
    public abstract class Node : IPasswordSaltProvider
    {
        private NodeData _data;
        internal NodeData Data
        {
            get
            {
                return _data;
            }
        }

        private bool _copying;
        public bool CopyInProgress
        {
            get { return _copying; }
            protected set { _copying = value; }
        }

        private bool IsDirty { get; set; }

        public abstract bool IsContentType { get; }

        /// <summary>
        /// Set this to override AllowIncrementalNaming setting of ContentType programatically
        /// </summary>
        public bool? AllowIncrementalNaming;

        public string NodeOperation { get; set; }

        private static IIndexPopulator Populator
        {
            get { return StorageContext.Search.SearchEngine.GetPopulator(); }
        }

        //public bool AllowDeferredIndexingOnSave { get; set; }

        private SecurityHandler _security;
        private LockHandler _lockHandler;
        public bool IsHeadOnly { get; private set; }
        public bool IsPreviewOnly { get; private set; }

        private static readonly List<string> SeeEnabledProperties = new List<string> { "Name", "Path", "Id", "Index", "NodeType", "ContentListId", "ContentListType", "Parent", "IsModified", "IsDeleted", "CreationDate", "ModificationDate", "CreatedBy", "ModifiedBy", "VersionCreationDate", "VersionModificationDate", "VersionCreatedById", "VersionModifiedById", "Aspects", "Icon", "StoredIcon" };
        public bool IsAllowedProperty(string name)
        {
            if (!HasProperty(name))
                return false;
            if (IsPreviewOnly)
            {
                var propertyType = this.PropertyTypes[name];
                if (propertyType != null && propertyType.DataType == DataType.Binary)
                    return false;
                return true;
            }
            if (IsHeadOnly)
                return SeeEnabledProperties.Contains(name);
            return true;
        }
        public static string[] GetHeadOnlyProperties()
        {
            return SeeEnabledProperties.ToArray();
        }

        public static readonly List<string> EXCLUDED_COPY_PROPERTIES = new List<string> { "ApprovingMode", "InheritableApprovingMode", "InheritableVersioningMode", "VersioningMode", "AvailableContentTypeFields", "FieldSettingContents" };

        public IEnumerable<Node> PhysicalChildArray
        {
            get { return this.GetChildren(); }
        }

        protected virtual IEnumerable<Node> GetChildren()
        {
            var nodeHeads = DataBackingStore.GetNodeHeads(QueryChildren().Identifiers);
            var user = AccessProvider.Current.GetCurrentUser();

            //use loop here instead of LoadNodes to check permissions
            return new NodeList<Node>((from nodeHead in nodeHeads
                                       where nodeHead != null && Security.HasPermission(user, nodeHead, PermissionType.See)
                                       select nodeHead.Id));
        }
        protected int GetChildCount()
        {
            return QueryChildren().Count;
        }
        private NodeQueryResult QueryChildren()
        {
            if (this.Id == 0)
                return new NodeQueryResult(new NodeList<Node>());

            if (StorageContext.Search.IsOuterEngineEnabled && StorageContext.Search.SearchEngine != InternalSearchEngine.Instance)
            {
                var query = new NodeQuery();
                query.Add(new IntExpression(IntAttribute.ParentId, ValueOperator.Equal, this.Id));
                return query.Execute();
            }
            else
            {
                //fallback to the direct db query
                return NodeQuery.QueryChildren(this.Id);
            }
        }

        public virtual int NodesInTree
        {
            get
            {
                var childQuery = new NodeQuery();
                childQuery.Add(new StringExpression(StringAttribute.Path, StringOperator.StartsWith, this.Path));
                var childResult = childQuery.Execute();
                var childNum = childResult.Count;

                return childNum;
            }
        }

        internal void MakePrivateData()
        {
            if (!_data.IsShared)
                return;
            _data = NodeData.CreatePrivateData(_data);
        }

        #region //================================================================================================= General Properties

        /// <summary>
        /// The unique identifier of the node.
        /// </summary>
        /// <remarks>Notice if you develop a data provider for the system you have to convert your Id to integer.</remarks>
        public int Id
        {
            get
            {
                if (_data == null)
                    return 0;
                return _data.Id;
            }
            internal set
            {
                MakePrivateData();
                _data.Id = value;
            }
        }
        public virtual int NodeTypeId
        {
            get { return _data.NodeTypeId; }
        }
        public virtual int ContentListTypeId
        {
            get { return _data.ContentListTypeId; }
        }
        /// <summary>
        /// Gets the <see cref="SenseNet.ContentRepository.Storage.Schema.NodeType">NodeType</see> of the instance.
        /// </summary>
        public virtual NodeType NodeType
        {
            get { return NodeTypeManager.Current.NodeTypes.GetItemById(_data.NodeTypeId); }
        }
        public virtual ContentListType ContentListType
        {
            get
            {
                if (_data.ContentListTypeId == 0)
                    return null;
                return NodeTypeManager.Current.ContentListTypes.GetItemById(_data.ContentListTypeId);
            }
            internal set
            {
                MakePrivateData();
                _data.ContentListTypeId = value.Id;
            }
        }
        public virtual int ContentListId
        {
            get { return _data.ContentListId; }
            internal set
            {
                MakePrivateData();
                _data.ContentListId = value;
            }
        }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public VersionNumber Version
        {
            get
            {
                return _data.Version;
            }
            set
            {
                MakePrivateData();
                _data.Version = value;
            }
        }
        /// <summary>
        /// Gets the version id.
        /// </summary>
        /// <value>The version id.</value>
        public int VersionId
        {
            get
            {
                return _data.VersionId;
            }
        }

        public bool CreatingInProgress
        {
            get { return _data.CreatingInProgress; }
            internal set
            {
                MakePrivateData();
                _data.CreatingInProgress = value;
            }
        }

        public bool IsLastPublicVersion { get; private set; }
        public bool IsLatestVersion { get; private set; }

        /// <summary>
        /// Gets the parent node.
        /// Use this.ParentId, this.ParentPath, this.ParentName instead of Parent.Id, Parent.Path, Parent.Name
        /// </summary>
        /// <value>The parent.</value>
        public Node Parent
        {
            get
            {
                if (_data.ParentId == 0)
                    return null;
                try
                {
                    return Node.LoadNode(_data.ParentId);
                }
                catch (Exception e) //rethrow
                {
                    throw Exception_ReferencedNodeCouldNotBeLoadedException("Parent", _data.ParentId, e);
                }
            }
        }
        /// <summary>
        /// Gets the Id of parent.
        /// </summary>
        /// <value>The parent.</value>
        public int ParentId
        {
            get { return Data.ParentId; }
        }
        public string ParentPath
        {
            get { return RepositoryPath.GetParentPath(this.Path); }
        }
        public string ParentName
        {
            get { return RepositoryPath.GetFileName(this.ParentPath); }
        }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// A node is uniquely identified by the Name within a leaf in the Sense/Net Content Repository tree.
        /// This guarantees that there can be no two nodes with the same path in the Repository.
        /// </remarks>
        /// <value>The name.</value>
        public virtual string Name
        {
            get { return _data.Name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                MakePrivateData();
                _data.Name = value;
            }

        }
        public virtual string DisplayName
        {
            get { return _data.DisplayName; }
            set
            {
                MakePrivateData();
                _data.DisplayName = value;
            }
        }
        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public virtual string Path
        {
            get { return _data.Path; }
        }
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public virtual int Index
        {
            get { return _data.Index; }
            set
            {
                MakePrivateData();
                _data.Index = value;
            }
        }
        /// <summary>
        /// Gets a value indicating whether this instance is modified.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is modified; otherwise, <c>false</c>.
        /// </value>
        public bool IsModified
        {
            get { return this._data.AnyDataModified; }
        }
        /// <summary>
        /// Gets a value indicating whether this instance is deleted.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is deleted; otherwise, <c>false</c>.
        /// </value>
        public bool IsDeleted
        {
            get { return _data.IsDeleted; }
        }
        /// <summary>
        /// Gets a value indicating whether the default permissions of this instance are inherited from its parent.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the default permissions of this instance are inherited from its parent; otherwise <c>false</c>.
        /// </value>
        public bool IsInherited
        {
            get { return _data.IsInherited; }
        }
        /// <summary>
        /// Gets the security.
        /// </summary>
        /// <value>The security.</value>
        public SecurityHandler Security
        {
            get
            {
                if (_security == null)
                    _security = new SecurityHandler(this);
                return _security;
            }
        }
        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <value>The lock.</value>
        public LockHandler Lock
        {
            get
            {
                if (_lockHandler == null)
                    _lockHandler = new LockHandler(this);
                return _lockHandler;
            }
        }

        /// <summary>
        /// Indicates the state of the multi step saving. 
        /// </summary>
        public ContentSavingState SavingState
        {
            get { return _data.SavingState; }
            private set
            {
                MakePrivateData();
                _data.SavingState = value;
            }
        }
        internal IEnumerable<ChangedData> ChangedData
        {
            get { return _data.ChangedData == null ? Storage.ChangedData.EmptyArray : _data.ChangedData; }
            private set
            {
                MakePrivateData();
                _data.ChangedData = value;
            }
        }

        public bool IsSystem
        {
            get { return _data.IsSystem; }
            set
            {
                MakePrivateData();
                _data.IsSystem = value;
            }
        }

        public long NodeTimestamp
        {
            get
            {
                return _data.NodeTimestamp;
            }
        }
        public long VersionTimestamp
        {
            get
            {
                return _data.VersionTimestamp;
            }
        }

        /// <summary>
        /// Means: node is new if its Id is 0
        /// </summary>
        public virtual bool IsNew
        {
            get
            {
                return this.Id == 0;
            }
        }

        #endregion
        #region //--------------------------------------------------------- Creation, Modification
        //--------------------------------------------------------- Node-level Creation, Modification

        /// <summary>
        /// Gets or sets the creation date of the first version. Writing is not allowed except in import working mode.
        /// </summary>
        public virtual DateTime CreationDate
        {
            get { return _data.CreationDate; }
            set
            {
                AssertUserIsOperator("CreationDate");
                SetCreationDate(value);
            }
        }
        protected void AssertUserIsOperator(string propertyName)
        {
            var user = AccessProvider.Current.GetCurrentUser();

            // there is no need for group check in elevated mode
            if (user is SystemUser)
                return;

            using (new SystemAccount())
            {
                if (!user.IsInGroup((IGroup)Node.LoadNode(RepositoryConfiguration.OperatorsGroupPath)))
                    throw new NotSupportedException(String.Format(SR.Exceptions.General.Msg_CannotWriteReadOnlyProperty_1, propertyName));
            }
        }
        protected void SetCreationDate(DateTime creation)
        {
            if (creation < DataProvider.Current.DateTimeMinValue)
                throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
            if (creation > DataProvider.Current.DateTimeMaxValue)
                throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();
            MakePrivateData();
            _data.CreationDate = creation;
        }

        /// <summary>
        /// Gets or sets the user who created the last version.
        /// </summary>
        public virtual Node CreatedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(CreatedById, "CreatedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'CreatedBy' node must be saved.");
                if (value.Id == RepositoryConfiguration.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the CreatedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'CreatedBy' must be IUser.");

                MakePrivateData();
                _data.CreatedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who created the first version.
        /// </summary>
        public virtual int CreatedById
        {
            get { return _data.CreatedById; }
        }
        /// <summary>
        /// Gets or sets the modification date of the last version.
        /// </summary>
        public virtual DateTime ModificationDate
        {
            get { return _data.ModificationDate; }
            set
            {
                if (value < DataProvider.Current.DateTimeMinValue)
                    throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
                if (value > DataProvider.Current.DateTimeMaxValue)
                    throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();

                MakePrivateData();
                _data.ModificationDate = value;
            }
        }
        /// <summary>
        /// Gets or sets the user who modified the last version.
        /// </summary>
        public virtual Node ModifiedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(ModifiedById, "ModifiedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'ModifiedBy' node must be saved.");
                if (value.Id == RepositoryConfiguration.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the ModifiedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'ModifiedBy' must be IUser.");

                MakePrivateData();
                _data.ModifiedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who modified the last version.
        /// </summary>
        public virtual int ModifiedById
        {
            get { return _data.ModifiedById; }
        }

        //--------------------------------------------------------- Version-level Creation, Modification

        /// <summary>
        /// Gets or sets the creation date of this version. Writing is not allowed except in import working mode.
        /// </summary>
        public virtual DateTime VersionCreationDate
        {
            get { return _data.VersionCreationDate; }
            set
            {
                AssertUserIsOperator("VersionCreationDate");
                SetVersionCreationDate(value);
            }
        }
        protected void SetVersionCreationDate(DateTime creation)
        {
            if (creation < DataProvider.Current.DateTimeMinValue)
                throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
            if (creation > DataProvider.Current.DateTimeMaxValue)
                throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();
            MakePrivateData();
            _data.VersionCreationDate = creation;
        }

        /// <summary>
        /// Gets or sets the user who created this version.
        /// </summary>
        public virtual Node VersionCreatedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(VersionCreatedById, "VersionCreatedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'CreatedBy' node must be saved.");
                if (value.Id == RepositoryConfiguration.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the CreatedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'CreatedBy' must be IUser.");

                MakePrivateData();
                _data.VersionCreatedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who created this version.
        /// </summary>
        public virtual int VersionCreatedById
        {
            get { return _data.VersionCreatedById; }
        }
        /// <summary>
        /// Gets or sets the modification date of this version.
        /// </summary>
        public virtual DateTime VersionModificationDate
        {
            get { return _data.VersionModificationDate; }
            set
            {
                if (value < DataProvider.Current.DateTimeMinValue)
                    throw SR.Exceptions.General.Exc_LessThanDateTimeMinValue();
                if (value > DataProvider.Current.DateTimeMaxValue)
                    throw SR.Exceptions.General.Exc_BiggerThanDateTimeMaxValue();

                MakePrivateData();
                _data.VersionModificationDate = value;
            }
        }
        /// <summary>
        /// Gets or sets the user who modified this version.
        /// </summary>
        public virtual Node VersionModifiedBy
        {
            get
            {
                return (Node)LoadRefUserOrSomebody(VersionModifiedById, "VersionModifiedBy");
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Id == 0)
                    throw new ArgumentOutOfRangeException("value", "Referenced 'ModifiedBy' node must be saved.");
                if (value.Id == RepositoryConfiguration.SomebodyUserId)
                    throw new SenseNetSecurityException("Cannot set the ModifiedBy property with the Somebody user");
                if (!(value is IUser))
                    throw new ArgumentOutOfRangeException("value", "'ModifiedBy' must be IUser.");

                MakePrivateData();
                _data.VersionModifiedById = value.Id;
            }
        }
        /// <summary>
        /// Gets the user id who modified this version.
        /// </summary>
        public virtual int VersionModifiedById
        {
            get { return _data.VersionModifiedById; }
        }

        #endregion
        #region //--------------------------------------------------------- Properties for locking

        public bool Locked
        {
            get { return LockedById != 0; }
            //internal set { _data.Locked = value; }
        }
        public int LockedById
        {
            get { return _data == null ? 0 : _data.LockedById; }
            internal set
            {
                MakePrivateData();
                _data.LockedById = value;
                _data.Locked = value != 0;
            }
        }
        public IUser LockedBy
        {
            get
            {
                if (!this.Locked)
                    return null;
                return LoadRefUserOrSomebody(LockedById, "LockedBy");
            }
        }
        public string ETag
        {
            get { return _data.ETag; }
            set
            {
                MakePrivateData();
                _data.ETag = value;
            }
        }
        public int LockType
        {
            get { return _data.LockType; }
            set
            {
                MakePrivateData();
                _data.LockType = value;
            }
        }
        public int LockTimeout
        {
            get { return _data.LockTimeout; }
            internal set
            {
                MakePrivateData();
                _data.LockTimeout = value;
            }
        }
        public DateTime LockDate
        {
            get { return _data.LockDate; }
            set
            {
                MakePrivateData();
                _data.LockDate = value;
            }
        }
        public string LockToken
        {
            get { return _data.LockToken; }
            internal set
            {
                MakePrivateData();
                _data.LockToken = value;
            }
        }
        public DateTime LastLockUpdate
        {
            get { return _data.LastLockUpdate; }
            internal set
            {
                MakePrivateData();
                _data.LastLockUpdate = value;
            }
        }

        #endregion

        public object GetStoredValue(string name)
        {
            switch (name)
            {
                case "Id": return Data.Id;
                case "NodeTypeId": return Data.NodeTypeId;
                case "ContentListId": return Data.ContentListId;
                case "ContentListTypeId": return Data.ContentListTypeId;
                case "ParentId": return Data.ParentId;
                case "CreatingInProgress": return Data.CreatingInProgress;
                case "Name": return Data.Name;
                case "DisplayName": return Data.DisplayName;
                case "Path": return Data.Path;
                case "Index": return Data.Index;
                case "IsInherited": return Data.IsInherited;
                case "VersionCreationDate": return Data.VersionCreationDate;
                case "VersionModificationDate": return Data.VersionModificationDate;
                case "VersionCreatedById": return Data.VersionCreatedById;
                case "VersionModifiedById": return Data.VersionModifiedById;
                case "Version": return Data.Version;
                case "VersionId": return Data.VersionId;
                case "CreationDate": return Data.CreationDate;
                case "ModificationDate": return Data.ModificationDate;
                case "CreatedById": return Data.CreatedById;
                case "ModifiedById": return Data.ModifiedById;
                case "Locked": return Data.Locked;
                case "LockedById": return Data.LockedById;
                case "ETag": return Data.ETag;
                case "LockType": return Data.LockType;
                case "LockTimeout": return Data.LockTimeout;
                case "LockDate": return Data.LockDate;
                case "LockToken": return Data.LockToken;
                case "LastLockUpdate": return Data.LastLockUpdate;
                case "NodeTimestamp": return Data.NodeTimestamp;
                case "VersionTimestamp": return Data.VersionTimestamp;
                default: return Data.GetDynamicRawData(GetPropertyTypeByName(name)); // this[GetPropertyTypeByName(name)];
            }
        }

        #region //================================================================================================= Dynamic property accessors

        public object this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case "Id": return this.Id;
                    case "NodeType": return this.NodeType;
                    case "ContentListId": return this.ContentListId;
                    case "ContentListType": return this.ContentListType;
                    case "Parent": return this.Parent;
                    case "ParentId": return this._data.ParentId;
                    case "Name": return this.Name;
                    case "DisplayName": return this.DisplayName;
                    case "Path": return this.Path;
                    case "Index": return this.Index;
                    case "IsModified": return this.IsModified;
                    case "IsDeleted": return this.IsDeleted;
                    case "IsInherited": return this.IsInherited;
                    case "IsSystem": return this.IsSystem;
                    case "CreationDate": return this.CreationDate;
                    case "CreatedBy": return this.CreatedBy;
                    case "CreatedById": return this.CreatedById;
                    case "ModificationDate": return this.ModificationDate;
                    case "ModifiedBy": return this.ModifiedBy;
                    case "ModifiedById": return this.ModifiedById;
                    case "Version": return this.Version;
                    case "VersionId": return this.VersionId;
                    case "VersionCreationDate": return this.VersionCreationDate;
                    case "VersionCreatedBy": return this.VersionCreatedBy;
                    case "VersionCreatedById": return this.VersionCreatedById;
                    case "VersionModificationDate": return this.VersionModificationDate;
                    case "VersionModifiedBy": return this.VersionModifiedBy;
                    case "VersionModifiedById": return this.VersionModifiedById;
                    case "Locked": return this.Locked;
                    case "Lock": return this.Lock;
                    case "LockedById": return this.LockedById;
                    case "LockedBy": return this.LockedBy;
                    case "ETag": return this.ETag;
                    case "LockType": return this.LockType;
                    case "LockTimeout": return this.LockTimeout;
                    case "LockDate": return this.LockDate;
                    case "LockToken": return this.LockToken;
                    case "LastLockUpdate": return this.LastLockUpdate;
                    case "Security": return this.Security;
                    default: return this[GetPropertyTypeByName(propertyName)];
                }
            }
            set
            {
                switch (propertyName)
                {
                    case "Id":
                    case "IsModified":
                    case "NodeType":
                    case "Parent":
                    case "Path":
                    case "Security":
                        throw new InvalidOperationException(String.Concat("Property is read only: ", propertyName));
                    case "CreationDate": this.CreationDate = (DateTime)value; break;
                    case "VersionCreationDate": this.VersionCreationDate = (DateTime)value; break;
                    case "CreatedBy": this.CreatedBy = (Node)value; break;
                    case "VersionCreatedBy": this.VersionCreatedBy = (Node)value; break;
                    case "Index": this.Index = value == null ? 0 : (int)value; break;
                    case "ModificationDate": this.ModificationDate = (DateTime)value; break;
                    case "ModifiedBy": this.ModifiedBy = (Node)value; break;
                    case "VersionModificationDate": this.VersionModificationDate = (DateTime)value; break;
                    case "VersionModifiedBy": this.VersionModifiedBy = (Node)value; break;
                    case "Name": this.Name = (string)value; break;
                    case "DisplayName": this.DisplayName = (string)value; break;
                    case "Version": this.Version = (VersionNumber)value; break;
                    default: this[GetPropertyTypeByName(propertyName)] = value; break;
                }
            }
        }
        internal object this[int propertyId]
        {
            get { return this[GetPropertyTypeById(propertyId)]; }
            set { this[GetPropertyTypeById(propertyId)] = value; }
        }
        public object this[PropertyType propertyType]
        {
            get
            {
                AssertSeeOnly(propertyType);

                switch (propertyType.DataType)
                {
                    case DataType.Binary:
                        if (this.SavingState != ContentSavingState.Finalized)
                            throw new InvalidOperationException(SR.GetString(SR.Exceptions.General.Error_AccessToNotFinalizedBinary_2, this.Path, propertyType.Name));
                        return GetAccessor(propertyType);
                    case DataType.Reference:
                        return GetAccessor(propertyType);
                    default:
                        return _data.GetDynamicRawData(propertyType) ?? propertyType.DefaultValue;
                }
            }
            set
            {
                MakePrivateData();
                switch (propertyType.DataType)
                {
                    case DataType.Binary:
                        ChangeAccessor((BinaryData)value, propertyType);
                        break;
                    case DataType.Reference:
                        var nodeList = value as NodeList<Node>;
                        if (nodeList == null)
                            nodeList = value == null ? new NodeList<Node>() : new NodeList<Node>((IEnumerable<Node>)value);
                        ChangeAccessor(nodeList, propertyType);
                        break;
                    default:
                        _data.SetDynamicRawData(propertyType, value);
                        break;
                }

            }
        }

        private static readonly List<string> _propertyNames = new List<string>(new[] {
            "Id","NodeType","ContentListId","ContentListType","Parent","ParentId","Name","DisplayName","Path",
            "Index","IsModified","IsDeleted","IsInherited","IsSystem","CreationDate","CreatedBy","CreatedById","ModificationDate","ModifiedBy","ModifiedById",
            "Version","VersionId","VersionCreationDate","VersionCreatedBy","VersionCreatedById","VersionModificationDate","VersionModifiedBy","VersionModifiedById",
            "Locked","Lock","LockedById","LockedBy","ETag","LockType","LockTimeout","LockDate","LockToken","LastLockUpdate","Security",
            "SavingState", "ChangedData"});

        public TypeCollection<PropertyType> PropertyTypes { get { return _data.PropertyTypes; } }
        public virtual bool HasProperty(string name)
        {
            if (PropertyTypes[name] != null)
                return true;
            return _propertyNames.Contains(name);
        }
        public bool HasProperty(PropertyType propType)
        {
            return HasProperty(propType.Id);
        }
        public bool HasProperty(int propertyTypeId)
        {
            return PropertyTypes.GetItemById(propertyTypeId) != null;
        }

        private Dictionary<string, IDynamicDataAccessor> __accessors;
        private Dictionary<string, IDynamicDataAccessor> Accessors
        {
            get
            {
                if (__accessors == null)
                {
                    var accDict = new Dictionary<string, IDynamicDataAccessor>();

                    foreach (var propType in this.PropertyTypes)
                    {
                        IDynamicDataAccessor acc = null;
                        if (propType.DataType == DataType.Binary)
                            acc = new BinaryData();
                        if (propType.DataType == DataType.Reference)
                            acc = new NodeList<Node>();
                        if (acc == null)
                            continue;
                        acc.OwnerNode = this;
                        acc.PropertyType = propType;
                        accDict[propType.Name] = acc;
                    }

                    __accessors = accDict;
                }
                return __accessors;
            }
        }

        //---------------- General axis

        public T GetProperty<T>(string propertyName)
        {
            return (T)this[propertyName];
        }
        internal T GetProperty<T>(int propertyId)
        {
            return (T)this[propertyId];
        }
        public T GetProperty<T>(PropertyType propertyType)
        {
            return (T)this[propertyType];
        }
        public virtual object GetPropertySafely(string propertyName)
        {
            if (this.HasProperty(propertyName))
            {
                var result = this[propertyName];
                return result;
            }
            return null;
        }

        private IDynamicDataAccessor GetAccessor(PropertyType propType)
        {
            IDynamicDataAccessor value;
            if (!Accessors.TryGetValue(propType.Name, out value))
                throw NodeData.Exception_PropertyNotFound(propType.Name, this.NodeType.Name);
            //if (_data.GetDynamicRawData(propType) == null)
            //    if(propType.DataType == DataType.Binary)
            //        return null;
            return value;
        }
        private PropertyType GetPropertyTypeByName(string name)
        {
            var propType = PropertyTypes[name];
            if (propType == null)
                throw NodeData.Exception_PropertyNotFound(name, this.NodeType.Name);
            return propType;
        }
        private PropertyType GetPropertyTypeById(int id)
        {
            var propType = PropertyTypes.GetItemById(id);
            if (propType == null)
                throw NodeData.Exception_PropertyNotFound(id);
            return propType;
        }
        internal void ChangeAccessor(IDynamicDataAccessor newAcc, PropertyType propType)
        {
            var value = _data.GetDynamicRawData(propType);
            if (value != null)
            {
                var oldAcc = Accessors[propType.Name];
                if (oldAcc == null)
                    throw new NullReferenceException("Accessor not found: " + propType.Name);
                oldAcc.RawData = value;
                oldAcc.OwnerNode = null;
                oldAcc.PropertyType = null;
            }

            MakePrivateData();
            if (newAcc == null)
            {
                _data.SetDynamicRawData(propType, null);
            }
            else
            {
                _data.SetDynamicRawData(propType, newAcc.RawData);
                newAcc.OwnerNode = this;
                newAcc.PropertyType = propType;
                if (Accessors.ContainsKey(propType.Name))
                    Accessors[propType.Name] = newAcc;
                else
                    Accessors.Add(propType.Name, newAcc);
            }
        }

        //---------------- Binary axis

        public BinaryData GetBinary(string propertyName)
        {
            return (BinaryData)this[propertyName];
        }
        internal BinaryData GetBinary(int propertyId)
        {
            return (BinaryData)this[propertyId];
        }
        public BinaryData GetBinary(PropertyType property)
        {
            return (BinaryData)this[property];
        }
        public void SetBinary(string propertyName, BinaryData data)
        {
            if (data == null)
                GetBinary(propertyName).Reset();
            else
                GetBinary(propertyName).CopyFrom(data);
        }

        //---------------- Reference axes

        public IEnumerable<Node> GetReferences(string propertyName)
        {
            return (IEnumerable<Node>)this[propertyName];
        }
        internal IEnumerable<Node> GetReferences(int propertyId)
        {
            return (IEnumerable<Node>)this[propertyId];
        }
        public IEnumerable<Node> GetReferences(PropertyType property)
        {
            return (IEnumerable<Node>)this[property];
        }

        public void SetReferences<T>(string propertyName, IEnumerable<T> nodes) where T : Node
        {
            ClearReference(propertyName);
            AddReferences(propertyName, nodes);
        }
        internal void SetReferences<T>(int propertyId, IEnumerable<T> nodes) where T : Node
        {
            ClearReference(propertyId);
            AddReferences(propertyId, nodes);
        }
        public void SetReferences<T>(PropertyType property, IEnumerable<T> nodes) where T : Node
        {
            ClearReference(property);
            AddReferences(property, nodes);
        }

        public void ClearReference(string propertyName)
        {
            GetNodeList(propertyName).Clear();
        }
        internal void ClearReference(int propertyId)
        {
            GetNodeList(propertyId).Clear();
        }
        public void ClearReference(PropertyType property)
        {
            GetNodeList(property).Clear();
        }

        public void AddReference(string propertyName, Node refNode)
        {
            GetNodeList(propertyName).Add(refNode);
        }
        internal void AddReference(int propertyId, Node refNode)
        {
            GetNodeList(propertyId).Add(refNode);
        }
        public void AddReference(PropertyType property, Node refNode)
        {
            GetNodeList(property).Add(refNode);
        }

        public void AddReferences<T>(string propertyName, IEnumerable<T> refNodes) where T : Node
        {
            AddReferences<T>(propertyName, refNodes, false);
        }
        internal void AddReferences<T>(int propertyId, IEnumerable<T> refNodes) where T : Node
        {
            AddReferences<T>(propertyId, refNodes, false);
        }
        public void AddReferences<T>(PropertyType property, IEnumerable<T> refNodes) where T : Node
        {
            AddReferences<T>(property, refNodes, false);
        }
        public void AddReferences<T>(string propertyName, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            AddReferences<T>(GetNodeList(propertyName), refNodes, distinct);
        }
        internal void AddReferences<T>(int propertyId, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            AddReferences<T>(GetNodeList(propertyId), refNodes, distinct);
        }
        public void AddReferences<T>(PropertyType property, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            AddReferences<T>(GetNodeList(property), refNodes, distinct);
        }

        public bool HasReference(string propertyName, Node refNode)
        {
            return GetNodeList(propertyName).Contains(refNode);
        }
        internal bool HasReference(int propertyId, Node refNode)
        {
            return GetNodeList(propertyId).Contains(refNode);
        }
        public bool HasReference(PropertyType property, Node refNode)
        {
            return GetNodeList(property).Contains(refNode);
        }

        public void RemoveReference(string propertyName, Node refNode)
        {
            GetNodeList(propertyName).Remove(refNode);
        }
        internal void RemoveReference(int propertyId, Node refNode)
        {
            GetNodeList(propertyId).Remove(refNode);
        }
        public void RemoveReference(PropertyType property, Node refNode)
        {
            GetNodeList(property).Remove(refNode);
        }

        public int GetReferenceCount(string propertyName)
        {
            return GetNodeList(propertyName).Count;
        }
        internal int GetReferenceCount(int propertyId)
        {
            return GetNodeList(propertyId).Count;
        }
        public int GetReferenceCount(PropertyType property)
        {
            return GetNodeList(property).Count;
        }

        //---------------- Single reference interface

        public T GetReference<T>(string propertyName) where T : Node
        {
            return GetNodeList(propertyName).GetSingleValue<T>();
        }
        internal T GetReference<T>(int propertyId) where T : Node
        {
            return GetNodeList(propertyId).GetSingleValue<T>();
        }
        public T GetReference<T>(PropertyType property) where T : Node
        {
            return GetNodeList(property).GetSingleValue<T>();
        }
        public void SetReference(string propertyName, Node node)
        {
            GetNodeList(propertyName).SetSingleValue<Node>(node);
        }
        internal void SetReference(int propertyId, Node node)
        {
            GetNodeList(propertyId).SetSingleValue<Node>(node);
        }
        public void SetReference(PropertyType property, Node node)
        {
            GetNodeList(property).SetSingleValue<Node>(node);
        }

        //---------------- reference tools

        private NodeList<Node> GetNodeList(string propertyName)
        {
            return (NodeList<Node>)this[propertyName];
        }
        private NodeList<Node> GetNodeList(int propertyId)
        {
            return (NodeList<Node>)this[propertyId];
        }
        private NodeList<Node> GetNodeList(PropertyType property)
        {
            return (NodeList<Node>)this[property];
        }

        private static void AddReferences<T>(NodeList<Node> nodeList, IEnumerable<T> refNodes, bool distinct) where T : Node
        {
            foreach (var node in refNodes)
                if (!distinct || !nodeList.Contains(node))
                    nodeList.Add(node);
        }

        #endregion


        #region //================================================================================================= Construction

        protected Node() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        protected Node(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        protected Node(Node parent, string nodeTypeName)
        {
            if (nodeTypeName == null)
                nodeTypeName = this.GetType().Name;

            var nodeType = NodeTypeManager.Current.NodeTypes[nodeTypeName];
            if (nodeType == null)
            {
                nodeTypeName = this.GetType().FullName;
                nodeType = NodeTypeManager.Current.NodeTypes[nodeTypeName];

                if (nodeType == null)
                    throw new RegistrationException(String.Concat(SR.Exceptions.Schema.Msg_UnknownNodeType, ": ", nodeTypeName));
            }

            int listId = 0;
            ContentListType listType = null;
            if (parent != null && !nodeType.IsInstaceOfOrDerivedFrom("SystemFolder"))
            {
                listId = (parent.ContentListType != null && parent.ContentListId == 0) ? parent.Id : parent.ContentListId;
                listType = parent.ContentListType;
            }
            if (listType != null && this is IContentList)
            {
                throw new ApplicationException("Cannot create a ContentList under another ContentList");
            }

            var data = DataBackingStore.CreateNewNodeData(parent, nodeType, listType, listId);
            //data.InitializeDynamicData();
            this._data = data;

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class in the loading procedure. Do not use this constructor directly from your code.
        /// </summary>
        /// <param name="token">The token.</param>
        protected Node(NodeToken token)
        {
            //-- caller: CreateTargetClass()
            if (token == null)
                throw new ArgumentNullException("token");
            string typeName = this.GetType().FullName;
            if (token.NodeType.ClassName != typeName)
            {
                var message = String.Concat("Cannot create a ", typeName, " instance because type name is different in the passed token: ", token.NodeType.ClassName);
                throw new RegistrationException(message);
            }
            FillData(this, token);
            SetVersionInfo(token.NodeHead);
        }

        #endregion

        public void RefreshVersionInfo()
        {
            SetVersionInfo(NodeHead.Get(this.Id));
        }
        internal void RefreshVersionInfo(NodeHead nodeHead)
        {
            SetVersionInfo(nodeHead);
        }
        private void SetVersionInfo(NodeHead nodeHead)
        {
            var versionId = this.VersionId;
            this.IsLastPublicVersion = nodeHead.LastMajorVersionId == versionId && this.Version.Status == VersionStatus.Approved;
            this.IsLatestVersion = nodeHead.LastMinorVersionId == versionId;
        }

        #region //================================================================================================= Loader methods

        private static VersionNumber DefaultAbstractVersion { get { return VersionNumber.LastAccessible; } }



        //----------------------------------------------------------------------------- Static batch loaders

        public static List<Node> LoadNodes(IEnumerable<int> idArray)
        {
            return LoadNodes(DataBackingStore.GetNodeHeads(idArray), VersionNumber.LastAccessible);
        }
        private static List<Node> LoadNodes(IEnumerable<NodeHead> heads, VersionNumber version)
        {
            var headList = new List<NodeHead>();
            var versionIdList = new List<int>();
            var headonlyList = new List<int>();

            //-- resolving versionid array
            foreach (var head in heads)
            {
                if (head == null)
                    continue;

                AccessLevel userAccessLevel;

                try
                {
                    userAccessLevel = GetUserAccessLevel(head);
                }
                catch (SenseNetSecurityException)
                {
                    //the user does not have permission to see/open this node
                    continue;
                }

                var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                if (acceptedLevel == AccessLevel.Header)
                    headonlyList.Add(head.Id);

                var versionId = GetVersionId(head, acceptedLevel, version);

                //if user has not enough permissions, skip the node
                if (versionId <= 0)
                    continue;

                headList.Add(head);
                versionIdList.Add(versionId);
            }

            //-- loading data
            var result = new List<Node>();
            var tokenArray = DataBackingStore.GetNodeData(headList.ToArray(), versionIdList.ToArray());
            for (int i = 0; i < tokenArray.Length; i++)
            {
                var token = tokenArray[i];
                var retry = 0;
                var isHeadOnly = headonlyList.Contains(token.NodeId);
                while (true)
                {
                    if (token.NodeData != null)
                    {
                        var node = CreateTargetClass(token);
                        if (isHeadOnly)
                        {
                            //if the user has Preview permissions, that means a broader access than headonly
                            if (PreviewProvider.HasPreviewPermission(headList.FirstOrDefault(h => h.Id == token.NodeId)))
                                node.IsPreviewOnly = true;
                            else
                                node.IsHeadOnly = true;
                        }

                        result.Add(node);
                        break;
                    }
                    else
                    {
                        //==== retrying with reload nodehead
                        if (++retry > 1) //-- one time
                            break;

                        Logger.WriteVerbose("Version is lost.", new Dictionary<string, object> { { "nodeId", token.NodeHead.Path }, { "versionId", token.VersionId }, });
                        var head = NodeHead.Get(token.NodeHead.Id);
                        if (head == null) //-- deleted
                            break;

                        var userAccessLevel = GetUserAccessLevel(head);
                        var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                        if (acceptedLevel == AccessLevel.Header)
                            isHeadOnly = true;
                        var versionId = GetVersionId(head, acceptedLevel, version);
                        token = DataBackingStore.GetNodeData(head, versionId);
                    }
                }
            }
            return result;
        }

        //----------------------------------------------------------------------------- Static single loaders

        public static T Load<T>(int nodeId) where T : Node
        {
            return (T)LoadNode(nodeId);
        }
        public static T Load<T>(int nodeId, VersionNumber version) where T : Node
        {
            return (T)LoadNode(nodeId, version);
        }
        public static T Load<T>(string path) where T : Node
        {
            return (T)LoadNode(path);
        }
        public static T Load<T>(string path, VersionNumber version) where T : Node
        {
            return (T)LoadNode(path, version);
        }

        /// <summary>
        /// Loads the appropiate node by the given path.
        /// </summary>
        /// <example>How to load a Node by passing the Sense/Net Content Repository path.
        /// In this case you will get a Node named node filled with the data of the latest version of Node /Root/MyFavoriteNode.
        /// <code>
        /// Node node = Node.LoadNode("/Root/MyFavoriteNode");
        /// </code>
        /// </example>
        /// <returns>The latest version of the Node has the given path.</returns>
        public static Node LoadNode(string path)
        {
            return LoadNode(path, DefaultAbstractVersion);
        }
        /// <summary>
        /// Loads the appropiate node by the given path and version.
        /// </summary>
        /// <example>How to load the version 2.0 of a Node by passing the Sense/Net Content Repository path.
        /// In this case you will get a Node named node filled with the data of the latest version of Node /Root/MyFavoriteNode.
        /// <code>
        /// VersionNumber versionNumber = new VersionNumber(2, 0);
        /// Node node = Node.LoadNode("/Root/MyFavoriteNode", versionNumber);
        /// </code>
        /// </example>
        /// <returns>The a node holds the data of the given version of the Node that has the given path.</returns>
        public static Node LoadNode(string path, VersionNumber version)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            return LoadNode(DataBackingStore.GetNodeHead(path), version);
        }
        /// <summary>
        /// Loads the appropiate node by the given ID.
        /// </summary>
        /// <example>How to load a the latest version of Node identified with ID 132. 
        /// In this case you will get a Node named node filled with the data of the latest version of Node 132.
        /// <code>
        /// Node node = Node.LoadNode(132);
        /// </code>
        /// </example>
        /// <returns>The latest version of the Node that has the given ID.</returns> 
        public static Node LoadNode(int nodeId)
        {
            return LoadNode(nodeId, DefaultAbstractVersion);
        }
        /// <summary>
        /// Loads the appropiate node by the given ID and version number.
        /// </summary>
        /// <example>How to load a the version 2.0 of Node identified with ID 132. In this case you will get a Node named node filled with the data of the given version of Node 132.
        /// <code>
        /// VersionNumber versionNumber = new VersionNumber(2, 0);
        /// Node node = Node.LoadNode(132, versionNumber);
        /// </code>
        /// </example>
        /// <returns>The given version of the Node that has the given ID.</returns>
        public static Node LoadNode(int nodeId, VersionNumber version)
        {
            return LoadNode(DataBackingStore.GetNodeHead(nodeId), version);
        }
        public static Node LoadNode(NodeHead head)
        {
            return LoadNode(head, null);
        }
        public static Node LoadNode(NodeHead head, VersionNumber version)
        {
            if (version == null)
                version = DefaultAbstractVersion;

            if (version == VersionNumber.LastFinalized)
                return LoadLastFinalizedVersion(head);

            var retry = 0;
            while (true)
            {
                if (head == null)
                    return null;

                var userAccessLevel = GetUserAccessLevel(head);
                var acceptedLevel = GetAcceptedLevel(userAccessLevel, version);
                var versionId = GetVersionId(head, acceptedLevel != AccessLevel.Header ? acceptedLevel : AccessLevel.Major, version);

                // if the requested version does not exist, return immediately
                if (versionId == 0)
                    return null;

                //-- <L2Cache>
                var l2cacheKey = GetL2CacheKey(versionId, acceptedLevel);
                var cachedNode = StorageContext.L2Cache.Get(l2cacheKey);
                if (cachedNode != null)
                    return (Node)cachedNode;
                //-- </L2Cache>

                var token = DataBackingStore.GetNodeData(head, versionId);
                if (token.NodeData != null)
                {
                    var node = CreateTargetClass(token);
                    if (acceptedLevel == AccessLevel.Header)
                    {
                        //if the user has Preview permissions, that means a broader access than headonly
                        if (PreviewProvider.HasPreviewPermission(head))
                            node.IsPreviewOnly = true;
                        else
                            node.IsHeadOnly = true;
                    }

                    //-- <L2Cache>
                    StorageContext.L2Cache.Set(l2cacheKey, node);
                    //-- </L2Cache>

                    return node;
                }
                //-- lost version
                if (++retry > 1)
                    return null;
                //-- retry
                Logger.WriteVerbose("Version is lost.", new Dictionary<string, object> { { "nodeId", head.Path }, { "versionId", versionId }, });
                head = NodeHead.Get(head.Id);
            }
        }
        private static Node LoadLastFinalizedVersion(NodeHead head)
        {
            var node = Node.LoadNode(head, VersionNumber.LastAccessible);
            if (node.SavingState == ContentSavingState.Finalized)
                return node;
            if (node.Security.HasPermission(PermissionType.RecallOldVersion))
                return LoadNodeByLastBeforeVersionId(head);
            else
                return Node.LoadNode(head, VersionNumber.LastMajor);
        }
        private static Node LoadNodeByLastBeforeVersionId(NodeHead head)
        {
            var versions = head.Versions;
            if (versions.Length < 2)
                return null;
            return Node.LoadNodeByVersionId(versions[versions.Length - 2].VersionId);
        }

        //-- <L2Cache>
        private static string GetL2CacheKey(int versionId, AccessLevel accessLevel)
        {
            // access level is part of the key because otherwise in elevated mode we would cache 'full' nodes
            // even if the user has only see-only or preview-only permissions for this content
            return String.Concat("node|", versionId, "|", AccessProvider.Current.GetCurrentUser().Id, "|", accessLevel.ToString());
        }
        //-- </L2Cache>

        //----------------------------------------------------------------------------- Load algorithm steps

        private static AccessLevel GetUserAccessLevel(NodeHead head)
        {
            //int userId = AccessProvider.Current.GetCurrentUser().Id;
            //if (SecurityHandler.GetPermission(nodeId, userId, PermissionType.OpenMinor) == PermissionValue.Allow)
            //    return AccessLevel.Minor;
            //if (SecurityHandler.GetPermission(nodeId, userId, PermissionType.Open) == PermissionValue.Allow)
            //    return AccessLevel.Major;
            //if (SecurityHandler.GetPermission(nodeId, userId, PermissionType.See) == PermissionValue.Allow)
            //    return AccessLevel.Header;
            //throw new SenseNetSecurityException("Access denied.");

            var userId = AccessProvider.Current.GetCurrentUser().Id;
            var isOwner = head.CreatorId == userId;
            switch (SecurityHandler.GetPermittedLevel(head))
            {
                case PermittedLevel.None:
                    throw new SenseNetSecurityException(head.Path, PermissionType.See, AccessProvider.Current.GetCurrentUser(), "Access denied.");
                case PermittedLevel.HeadOnly:
                    return AccessLevel.Header;
                case PermittedLevel.PublicOnly:
                    return AccessLevel.Major;
                case PermittedLevel.All:
                    return AccessLevel.Minor;
                default:
                    throw new NotImplementedException();
            }
        }
        private static AccessLevel GetAcceptedLevel(AccessLevel userAccessLevel, VersionNumber requestedVersion)
        {
            //			HO	Ma	Mi
            //	-------------------		
            //	LA		HO	Ma	Mi
            //	-------------------		
            //	HO		HO	HO	HO
            //	LMa		X	Ma	Ma
            //	VMa		X	Ma	Ma
            //	LMi		X	X	Mi
            //	VMi		X	X	Mi

            var definedIsMajor = false;
            var definedIsMinor = false;
            if (!requestedVersion.IsAbstractVersion)
            {
                definedIsMajor = requestedVersion.IsMajor;
                definedIsMinor = !requestedVersion.IsMajor;
            }

            if (requestedVersion == VersionNumber.LastAccessible || requestedVersion == VersionNumber.LastFinalized)
                return userAccessLevel;
            if (requestedVersion == VersionNumber.Header)
                return AccessLevel.Header;
            if (requestedVersion == VersionNumber.LastMajor || definedIsMajor)
            {
                // In case the user has only head access, we should still enable loading 
                // major versions: it will be a head-only or preview-only node.
                if (userAccessLevel < AccessLevel.Major)
                    return userAccessLevel;

                return AccessLevel.Major;
            }
            if (requestedVersion == VersionNumber.LastMinor || definedIsMinor)
            {
                if (userAccessLevel < AccessLevel.Minor)
                    throw new SenseNetSecurityException("");
                return AccessLevel.Minor;
            }
            throw new NotImplementedException("####");
        }
        private static int GetVersionId(NodeHead nodeHead, AccessLevel acceptedLevel, VersionNumber version)
        {
            if (version.IsAbstractVersion)
            {
                switch (acceptedLevel)
                {
                    //-- get from last major/minor slot of nodeHead
                    case AccessLevel.Header:
                    //return 0; //UNDONE: Storage2: Mi van a versionId-val, ha az acceptedLevel == Header
                    case AccessLevel.Major:
                        return nodeHead.LastMajorVersionId;
                    case AccessLevel.Minor:
                        return nodeHead.LastMinorVersionId;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                //-- lookup versionlist of node from nodeHead or read from DB
                return nodeHead.GetVersionId(version);
            }
        }
        private static Node CreateTargetClass(NodeToken token)
        {
            if (token == null)
                return null;

            var node = token.NodeType.CreateInstance(token);

            node.FireOnLoaded();
            return node;
        }

        public object GetCachedData(string name)
        {
            return this.Data.GetExtendedSharedData(name);
        }
        public void SetCachedData(string name, object value)
        {
            this.Data.SetExtendedSharedData(name, value);
        }
        public void ResetCachedData(string name)
        {
            this.Data.ResetExtendedSharedData(name);
        }

        private static void FillData(Node node, NodeToken token)
        {
            string typeName = node.GetType().FullName;
            string typeNameInHead = NodeTypeManager.Current.NodeTypes.GetItemById(token.NodeData.NodeTypeId).ClassName;
            if (typeNameInHead != typeName)
            {
                var message = String.Concat("Cannot create a ", typeName, " instance because type name is different in the passed head: ", typeNameInHead);
                throw new RegistrationException(message);
            }

            //node._data = NodeData.CreatePrivateData(token.NodeData);
            node._data = token.NodeData;
            node._data.IsShared = true;
        }

        /// <summary>
        /// Gets the list of avaliable versions of the Node identified by Id.
        /// </summary>
        /// <returns>A list of version numbers.</returns>
        public static List<VersionNumber> GetVersionNumbers(int nodeId)
        {
            return new List<VersionNumber>(DataProvider.Current.GetVersionNumbers(nodeId));
        }
        /// <summary>
        /// Gets the list of avaliable versions of the Node identified by path.
        /// </summary>
        /// <returns>A list of version numbers.</returns>
        public static List<VersionNumber> GetVersionNumbers(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            return new List<VersionNumber>(DataProvider.Current.GetVersionNumbers(path));
        }

        public IEnumerable<Node> LoadVersions()
        {
            Security.Assert(PermissionType.RecallOldVersion, PermissionType.Open);
            if (Security.HasPermission(PermissionType.OpenMinor))
                return LoadAllVersions();
            return LoadPublicVersions();
        }
        private IEnumerable<Node> LoadPublicVersions()
        {
            var head = NodeHead.Get(this.Id);
            var x = head.Versions.Where(v => v.VersionNumber.Status == VersionStatus.Approved).Select(v => Node.LoadNode(this.Id, v.VersionNumber)).Where(n => n != null).ToArray();
            return x;
        }
        private IEnumerable<Node> LoadAllVersions()
        {
            var head = NodeHead.Get(this.Id);
            var x = head.Versions.Select(v => Node.LoadNode(this.Id, v.VersionNumber)).Where(n => n != null).ToArray();
            return x;
        }

        public static Node LoadNodeByVersionId(int versionId)
        {

            NodeHead head = NodeHead.GetByVersionId(versionId);
            if (head == null)
                return null;

            SecurityHandler.Assert(head, PermissionType.RecallOldVersion, PermissionType.Open);

            var token = DataBackingStore.GetNodeData(head, versionId);

            Node node = null;
            if (token.NodeData != null)
                node = CreateTargetClass(token);
            return node;
        }

        internal void Refresh()
        {
            if (!this.IsDirty)
                return;

            var nodeHead = NodeHead.Get(Id);

            if (nodeHead != null)
            {
                //  Reload by nodeHead.LastMinorVersionId
                var token = DataBackingStore.GetNodeData(nodeHead, nodeHead.LastMinorVersionId);
                var sharedData = token.NodeData;
                sharedData.IsShared = true;
                _data = sharedData;
                __accessors = null;
            }

            this.IsDirty = false;
        }
        internal void Reload()
        {
            var nodeHead = NodeHead.GetByVersionId(this.VersionId);
            if (nodeHead == null)
                throw new ContentNotFoundException(String.Format("Version of a content was not found. VersionId: {0}, old Path: {1}", this.VersionId, this.Path));

            var token = DataBackingStore.GetNodeData(nodeHead, this.VersionId);
            var sharedData = token.NodeData;
            sharedData.IsShared = true;
            _data = sharedData;
            __accessors = null;
            this.IsDirty = false;
        }
        #endregion

        #region //================================================================================================= Save methods

        public virtual void Save()
        {
            var settings = new NodeSaveSettings
            {
                Node = this,
                HasApproving = false,
                VersioningMode = VersioningMode.None,
            };
            settings.ExpectedVersionId = settings.CurrentVersionId;
            settings.Validate();
            this.Save(settings);
        }
        private static IDictionary<string, object> CollectAllProperties(NodeData data)
        {
            return data.GetAllValues();
        }
        private static IDictionary<string, object> CollectChangedProperties(object[] args)
        {
            var sb = new StringBuilder();
            //sb.Append("<ChangedData>");
            foreach (var changedValue in (IEnumerable<ChangedData>)args[2])
            {
                var name = changedValue.Name ?? string.Empty;
                if (name.StartsWith("#"))
                    name = name.Replace("#", "ContentListField-");

                //sb.Append("<").Append(name).Append(">");
                //if (changedValue.Original.ToString().StartsWith("<![CDATA["))
                //    sb.Append("<OldValue>").Append(changedValue.Original).Append("</OldValue>");
                //else
                //    sb.Append("<OldValue><![CDATA[").Append(changedValue.Original).Append("]]></OldValue>");
                //if (changedValue.Value.ToString().StartsWith("<![CDATA["))
                //    sb.Append("<NewValue>").Append(changedValue.Value).Append("</NewValue>");
                //else
                //    sb.Append("<NewValue><![CDATA[").Append(changedValue.Value).Append("]]></NewValue>");
                //sb.Append("</").Append(name).Append(">");

                var old = changedValue.Original.ToString();
                if (old.Contains("\""))
                    old = old.Replace("\"", "\\\"");

                var @new = changedValue.Value.ToString();
                if (@new.Contains("\""))
                    @new = @new.Replace("\"", "\\\"");

                sb.Append("{").AppendFormat(" name: \"{0}\", oldValue: \"{1}\", newValue: \"{2}\"", name, old, @new).AppendLine("}");
            }
            //sb.Append("</ChangedData>");
            return new Dictionary<string, object>
            {
                {"Id", args[0]},
                {"Path", args[1]},
                {"ChangedData", sb.ToString()}
            };
        }
        //private void ExtendAccessorsWithListProperties()
        //{
        //    //TODO: Handle remove list property?
        //    foreach (var propType in this.PropertyTypes)
        //    {
        //        if (_accessors.ContainsKey(propType.Name))
        //            continue;
        //        IDynamicDataAccessor acc = null;
        //        if (propType.DataType == DataType.Binary)
        //            acc = new BinaryData();
        //        if (propType.DataType == DataType.Reference)
        //            acc = new NodeList<Node>();
        //        if (acc == null)
        //            continue;
        //        acc.OwnerNode = this;
        //        acc.PropertyType = propType;
        //        _accessors[propType.Name] = acc;
        //    }

        //}
        /**/
        private void SaveCopied(NodeSaveSettings settings)
        {
            using (var traceOperation = Logger.TraceOperation("Node.SaveCopied"))
            {
                var currentUser = AccessProvider.Current.GetOriginalUser();
                var currentUserId = currentUser.Id;

                var currentUserNode = currentUser as Node;
                if (currentUserNode == null)
                    throw new InvalidOperationException("Cannot save the content because the current user account representation is not a Node.");

                var thisList = this as IContentList;
                if (thisList != null)
                {
                    var newListType = thisList.GetContentListType();
                    if (this.ContentListType != null || newListType != null)
                    {
                        if (this.ContentListType == null)
                        {
                            //-- AssignNewContentListType
                            this.ContentListType = newListType;
                        }
                        else if (newListType == null)
                        {
                            //-- AssignNullContentListType
                            throw new NotSupportedException();
                        }
                        else if (this.ContentListType.Id != newListType.Id)
                        {
                            //-- Change ContentListType
                            throw new NotSupportedException();
                        }
                    }
                }

                if (this.Id != 0)
                    throw new InvalidOperationException("Id of copied node must be 0.");

                if (IsDeleted)
                    throw new InvalidOperationException("Cannot save deleted node.");

                // Check permissions: got to have AddNew permission on the parent
                //SecurityHandler.AssertPermission(this.Parent.Path, PermissionType.AddNew);
                this.Parent.Security.Assert(PermissionType.AddNew);

                RepositoryPath.CheckValidName(this.Name);

                // Validate
                if (this.ParentId == 0)
                    throw new InvalidPathException(SR.Exceptions.General.Msg_ParentNodeDoesNotExists); // parent Node does not exists
                if (this.Name.Trim().Length == 0)
                    throw new InvalidPathException(SR.Exceptions.General.Msg_NameCannotBeEmpty);
                if (this.IsModified)
                    this.Name = this.Name.Trim();

                //==== Update the modification
                //-- save modification

                //-- update to current
                DateTime now = DataProvider.Current.RoundDateTime(DateTime.UtcNow);
                this.ModificationDate = now;
                this.Data.ModifiedById = currentUserId;
                this.VersionModificationDate = now;
                this.Data.VersionModifiedById = currentUserId;

                //-- collect data for populator
                var parentPath = RepositoryPath.GetParentPath(this.Path);
                var thisPath = RepositoryPath.Combine(parentPath, this.Name);

                //-- save
                DataBackingStore.SaveNodeData(this, settings, Populator, thisPath, thisPath);

                //-- <L2Cache>
                StorageContext.L2Cache.Clear();
                //-- </L2Cache>

                if (this is IGroup)
                    //DataProvider.Current.ExplicateGroupMemberships();
                    SecurityHandler.ExplicateGroupMembership();

                IUser thisAsUser = this as IUser;
                if (thisAsUser != null)
                    //DataProvider.Current.ExplicateOrganizationUnitMemberships(thisAsUser);
                    SecurityHandler.ExplicateOrganizationUnitMemberships(thisAsUser);

                //-- refresh data (e.g. in case of undo checkout)
                if (settings.ForceRefresh)
                {
                    this.IsDirty = true;
                    Refresh();
                }

                //-- make it as shared (flatten the data)
                NodeData.MakeSharedData(this._data);

                //-- insert node data into cache after save
                CacheNodeAfterSave();

                //-- events
                FireOnCreated(null);

                traceOperation.IsSuccessful = true;
            }

        }
        /**/

        ///// <summary>
        ///// Delete the current node version.
        ///// </summary>
        //public virtual void DeleteVersion()
        //{
        //    var deletedVersionNumber = this.Version.Clone();

        //    DeleteVersion(this);
        //    var nodeHead = NodeHead.Get(Id);
        //    var sharedData = DataBackingStore.GetNodeData(nodeHead, nodeHead.LastMinorVersionId);
        //    var privateData = NodeData.CreatePrivateData(sharedData.NodeData);

        //    _data = privateData;

        //    Logger.WriteVerbose("Version deleted.", GetLoggerPropertiesAfterDeleteVersion, new object[] { this, deletedVersionNumber });
        //}

        public void Save(VersionRaising versionRaising, VersionStatus versionStatus)
        {
            var settings = new NodeSaveSettings { Node = this, HasApproving = false };
            var curVer = settings.CurrentVersion;
            var history = NodeHead.Get(this.Id).Versions;
            var biggest = history.OrderBy(v => v.VersionNumber.VersionString).LastOrDefault();
            var biggestVer = biggest == null ? curVer : biggest.VersionNumber;

            switch (versionRaising)
            {
                case VersionRaising.None:
                    settings.VersioningMode = VersioningMode.None;
                    settings.ExpectedVersion = curVer.ChangeStatus(versionStatus);
                    settings.ExpectedVersionId = settings.CurrentVersionId;
                    break;
                case VersionRaising.NextMinor:
                    settings.VersioningMode = VersioningMode.Full;
                    settings.ExpectedVersion = new VersionNumber(biggestVer.Major, biggestVer.Minor + 1, versionStatus);
                    settings.ExpectedVersionId = 0;
                    break;
                case VersionRaising.NextMajor:
                    settings.VersioningMode = VersioningMode.Full;
                    settings.ExpectedVersion = new VersionNumber(biggestVer.Major + 1, 0, versionStatus);
                    settings.ExpectedVersionId = 0;
                    break;
                default:
                    break;
            }
            Save(settings);
        }
        #endregion

        public virtual void Save(NodeSaveSettings settings)
        {
            var isNew = this.IsNew;
            var previousSavingState = this.SavingState;

            //BenchmarkCounter.Reset();

            if (_data != null)
                _data.SavingTimer.Restart();

            StorageContext.Search.SearchEngine.WaitIfIndexingPaused();

            var lockBefore = this.Version == null ? false : this.Version.Status == VersionStatus.Locked;
            if (isNew)
                CreatingInProgress = true;
            var creating = CreatingInProgress;
            if (settings.ExpectedVersion.Status != VersionStatus.Locked)
                CreatingInProgress = false;

            if (_copying)
            {
                SaveCopied(settings);
                if (_data != null)
                    _data.SavingTimer.Stop();
                return;
            }

            // If this is a regular save (like in most cases), saving state will be Finalized. Otherwise 
            // it can be Creating if it is new or Modifying if it already exists. ModifyingLocked state
            // was created to let the finalizing code know that it should not check in the content
            // at the end of the multistep saving process.
            this.SavingState = settings.MultistepSaving
                                   ? (isNew
                                        ? ContentSavingState.Creating
                                        : (this.Locked
                                            ? ContentSavingState.ModifyingLocked
                                            : ContentSavingState.Modifying))
                                   : ContentSavingState.Finalized;

            settings.Validate();
            ApplySettings(settings);

            using (var traceOperation = Logger.TraceOperation(String.Format("Node.Save(NodeSavingSettings) Path: {0}/{1}", this.ParentPath, this.Name)))
            {
                ChecksBeforeSave();

                var currentUser = AccessProvider.Current.GetOriginalUser();
                var currentUserId = currentUser.Id;

                bool isNewNode = (this.Id == 0);

                // No changes -> return
                if (!settings.NodeChanged())
                {
                    Logger.WriteVerbose("Node is not saved because it has no changes", Logger.GetDefaultProperties, this);
                    traceOperation.IsSuccessful = true;
                    if (_data != null)
                        _data.SavingTimer.Stop();
                    return;
                }


                //-- Rename?
                string thisName = this.Name;
                var originalName = (this.Data.SharedData == null) ? thisName : this.Data.SharedData.Name;
                var renamed = originalName.ToLower() != thisName.ToLower();
                var parentPath = RepositoryPath.GetParentPath(this.Path);
                var newPath = RepositoryPath.Combine(parentPath, thisName);
                var originalPath = renamed ? RepositoryPath.Combine(parentPath, originalName) : newPath;

                //==== Update the modification
                DateTime now = DataProvider.Current.RoundDateTime(DateTime.UtcNow);
                if (!_data.VersionModificationDateChanged)
                    this.VersionModificationDate = now;
                if (!_data.VersionModifiedByIdChanged)
                    this.Data.VersionModifiedById = currentUserId;
                if (!_data.ModificationDateChanged)
                    this.ModificationDate = now;
                if (!_data.ModifiedByIdChanged)
                    this.Data.ModifiedById = currentUserId;

                //==== Update the creator if the Visitor is creating this content
                if (isNewNode && currentUserId == RepositoryConfiguration.VisitorUserId)
                {
                    using (new SystemAccount())
                    {
                        var list = this.LoadContentList();
                        if (list != null)
                        {
                            var ownerWhenVsitor = list.GetReference<Node>("OwnerWhenVisitor");
                            var adminId = ownerWhenVsitor != null
                                            ? ownerWhenVsitor.Id
                                            : RepositoryConfiguration.AdministratorUserId;

                            this.Data.VersionCreatedById = adminId;
                            this.Data.VersionModifiedById = adminId;
                            this.Data.CreatedById = adminId;
                            this.Data.ModifiedById = adminId;
                        }
                    }
                }

                //-- collect changed field values for logging and info for nodeobservers
                IEnumerable<ChangedData> changedData = null;
                if (!isNewNode)
                    changedData = this.Data.GetChangedValues();

                if (settings.MultistepSaving)
                {
                    this.ChangedData = changedData;
                }
                else
                {
                    if (previousSavingState == ContentSavingState.Modifying || previousSavingState == ContentSavingState.ModifyingLocked)
                    {
                        changedData = changedData == null
                            ? this.ChangedData
                            : this.ChangedData.Union(changedData).ToList();
                    }

                    this.ChangedData = null;
                }

                object customData = null;
                if (!lockBefore)
                {
                    CancellableNodeEventArgs args = null;
                    if (creating)
                    {
                        args = new CancellableNodeEventArgs(this, CancellableNodeEvent.Creating);
                        FireOnCreating(args);
                    }
                    else
                    {
                        args = new CancellableNodeEventArgs(this, CancellableNodeEvent.Modifying, changedData);
                        FireOnModifying(args);
                    }
                    if (args.Cancel)
                    {
                        throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);
                    }
                    customData = args.CustomData;
                }

                BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.BeforeSaveToDb, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
                if (_data != null)
                    _data.SavingTimer.Restart();

                //-- save
                DataBackingStore.SaveNodeData(this, settings, Populator, originalPath, newPath);

                //-- <L2Cache>
                StorageContext.L2Cache.Clear();
                //-- </L2Cache>

                BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.CommitPopulateNode, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
                if (_data != null)
                    _data.SavingTimer.Restart();

                //-- security
                if (renamed)
                    SecurityHandler.Rename(originalPath, newPath);

                //-- log
                if (!settings.MultistepSaving)
                {
                    if (Logger.AuditEnabled)
                    {
                        if (isNewNode || previousSavingState == ContentSavingState.Creating)
                            Logger.WriteAudit(AuditEvent.ContentCreated, CollectAllProperties, this.Data);
                        else
                            Logger.WriteAudit(AuditEvent.ContentUpdated, CollectChangedProperties,
                                new object[] { this.Id, this.Path, changedData });
                    }
                }

                BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.Audit, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
                if (_data != null)
                    _data.SavingTimer.Restart();

                //-- memberships
                if (this is IGroup)
                    SecurityHandler.ExplicateGroupMembership();

                IUser thisAsUser = this as IUser;
                if (thisAsUser != null)
                    SecurityHandler.ExplicateOrganizationUnitMemberships(thisAsUser);

                //-- refresh data (e.g. in case of undo checkout)
                if (settings.ForceRefresh)
                {
                    this.IsDirty = true;
                    Refresh();
                }

                //-- make it as shared (flatten the data)
                NodeData.MakeSharedData(this._data);

                //-- remove too big items
                this._data.RemoveStreamsAndLongTexts();

                //-- insert node data into cache after save
                CacheNodeAfterSave();

                //-- events
                if (!settings.MultistepSaving)
                {
                    if (this.Version.Status != VersionStatus.Locked)
                    {
                        if (creating)
                            FireOnCreated(customData);
                        else
                            FireOnModified(originalPath, customData, changedData);
                    }
                }

                traceOperation.IsSuccessful = true;

                BenchmarkCounter.IncrementBy(BenchmarkCounter.CounterName.FinalizingSave, _data != null ? _data.SavingTimer.ElapsedTicks : 0);
                if (_data != null)
                    _data.SavingTimer.Restart();
            }
            if (_data != null)
                _data.SavingTimer.Stop();
        }

        public virtual void FinalizeContent()
        {
            if (SavingState == ContentSavingState.Finalized)
                throw new InvalidOperationException("Cannot finalize the content " + this.Path);

            using (var traceOperation = Logger.TraceOperation(String.Format("Node.Save(NodeSavingSettings) Path: {0}/{1}", this.ParentPath, this.Name)))
            {
                //-- log
                if (Logger.AuditEnabled)
                {
                    if (SavingState == ContentSavingState.Creating)
                        Logger.WriteAudit(AuditEvent.ContentCreated, CollectAllProperties, this.Data);
                    else
                        Logger.WriteAudit(AuditEvent.ContentUpdated, CollectChangedProperties,
                            new object[] { this.Id, this.Path, this.ChangedData });
                }

                this.SavingState = ContentSavingState.Finalized;
                var changedData = this.ChangedData;
                this.ChangedData = null;

                //UNDONE Finalize in database, indexing with binaries etc.
                var settings = new NodeSaveSettings
                {
                    Node = this,
                    ExpectedVersion = this.Version,
                    ExpectedVersionId = this.VersionId,
                    MultistepSaving = false
                };
                DataBackingStore.SaveNodeData(this, settings, Populator, Path, Path);

                //-- events
                if (this.Version.Status != VersionStatus.Locked)
                {
                    if (SavingState == ContentSavingState.Creating)
                        FireOnCreated(null);
                    else
                        FireOnModified(this.Path, null, changedData);
                }

                traceOperation.IsSuccessful = true;
            }
        }

        private void ApplySettings(NodeSaveSettings settings)
        {
            if (settings.ExpectedVersion != null && settings.ExpectedVersion.ToString() != this.Version.ToString())
            {
                this.Version = settings.ExpectedVersion;

                MakePrivateData();
                _data.VersionCreationDate = DataProvider.Current.RoundDateTime(DateTime.UtcNow);
            }

            if (settings.LockerUserId != null)
            {
                if (settings.LockerUserId != 0)
                {
                    if (!this.Locked)
                    {
                        //-- Lock
                        LockToken = Guid.NewGuid().ToString();
                        LockedById = AccessProvider.Current.GetCurrentUser().Id;
                        LockDate = DateTime.UtcNow;
                        LastLockUpdate = DateTime.UtcNow;
                        LockTimeout = LockHandler.DefaultLockTimeOut;
                    }
                    else
                    {
                        //-- RefreshLock
                        if (this.LockedById != AccessProvider.Current.GetCurrentUser().Id)
                            throw new SenseNetSecurityException(this.Id, "Node is locked by another user");
                        LastLockUpdate = DateTime.UtcNow;
                    }
                }
                else
                {
                    //-- Unlock
                    if (Locked)
                    {
                        this.LockedById = 0;
                        this.LockToken = string.Empty;
                        this.LockTimeout = 0;
                        this.LockDate = new DateTime(1800, 1, 1);
                        this.LastLockUpdate = new DateTime(1800, 1, 1);
                        this.LockType = 0;
                    }
                }
            }
        }

        private void ChecksBeforeSave()
        {
            var currentUser = AccessProvider.Current.GetOriginalUser();

            var currentUserNode = currentUser as Node;
            if (currentUserNode == null)
                throw new InvalidOperationException("Cannot save the content because the current user account representation is not a Node.");

            var thisList = this as IContentList;
            if (thisList != null)
            {
                var newListType = thisList.GetContentListType();
                if (this.ContentListType != null || newListType != null)
                {
                    if (this.ContentListType == null)
                    {
                        //-- AssignNewContentListType
                        this.ContentListType = newListType;
                    }
                    else if (newListType == null)
                    {
                        //-- AssignNullContentListType
                        throw new NotSupportedException();
                    }
                    else if (this.ContentListType.Id != newListType.Id)
                    {
                        //-- Change ContentListType
                        throw new NotSupportedException();
                    }
                }
            }

            if (IsDeleted)
                throw new InvalidOperationException("Cannot save deleted node.");

            //-- Check permissions
            if (this.Id == 0)
                this.Parent.Security.Assert(PermissionType.AddNew);
            else
                this.Security.Assert(PermissionType.Save);


            AssertLock();
            RepositoryPath.CheckValidName(this.Name);

            // Validate
            if (this.ParentId == 0)
                throw new InvalidPathException(SR.Exceptions.General.Msg_ParentNodeDoesNotExists);
            if (this.Name.Trim().Length == 0)
                throw new InvalidPathException(SR.Exceptions.General.Msg_NameCannotBeEmpty);
            if (this.IsModified)
                this.Name = this.Name.Trim();

            AssertMimeTypes();
        }
        private void AssertMimeTypes()
        {
            foreach (var item in this.Accessors)
            {
                var binProp = item.Value as BinaryData;
                if (binProp != null)
                    if (binProp.Size > 0 && string.IsNullOrEmpty(binProp.ContentType))
                        binProp.ContentType = MimeTable.GetMimeType(String.Empty);
            }
        }

        private void CacheNodeAfterSave()
        {
            // don't insert into cache if node is a content type
            if (this.IsContentType)
                return;

            switch (RepositoryConfiguration.CacheContentAfterSaveMode)
            {
                case RepositoryConfiguration.CacheContentAfterSaveOption.None:
                    // do nothing
                    break;
                case RepositoryConfiguration.CacheContentAfterSaveOption.Containers:
                    // cache IFolders only
                    if (this is IFolder)
                        DataBackingStore.CacheNodeData(this._data);
                    break;
                case RepositoryConfiguration.CacheContentAfterSaveOption.All:
                    // cache every node
                    DataBackingStore.CacheNodeData(this._data);
                    break;
            }
        }


        #region //================================================================================================= Move methods

        public static IEnumerable<NodeType> GetChildTypesToAllow(int nodeId)
        {
            return DataProvider.Current.LoadChildTypesToAllow(nodeId);
        }
        public IEnumerable<NodeType> GetChildTypesToAllow()
        {
            return DataProvider.Current.LoadChildTypesToAllow(this.Id);
        }

        /// <summary>
        /// Moves the Node indentified by its path to another location. The destination node is also identified by path. 
        /// </summary>
        /// <remarks>Use this method if you do not want to instantiate the nodes.</remarks>
        public static void Move(string sourcePath, string targetPath)
        {
            Node sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.MoveFailed_SouceDoesNotExistWithPath_1, sourcePath));
            var sourceTimestamp = sourceNode.NodeTimestamp;
            Node targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.MoveFailed_TargetDoesNotExistWithPath_1, targetPath));
            var targetTimestamp = targetNode.NodeTimestamp;
            targetNode.AssertLock();
            sourceNode.MoveTo(targetNode);
        }
        /// <summary>
        /// Modes the Node instance to another loacation. The new location is a Node instance which will be parent node.
        /// </summary>
        public virtual void MoveTo(Node target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            MoveTo(target, this.NodeTimestamp, target.NodeTimestamp);
        }
        private void MoveTo(Node target, long sourceTimestamp, long targetTimestamp)
        {
            StorageContext.Search.SearchEngine.WaitIfIndexingPaused();

            this.AssertLock();

            if (target == null)
                throw new ArgumentNullException("target");

            //check permissions
            this.Security.AssertSubtree(PermissionType.Delete);
            target.Security.Assert(PermissionType.Open);
            target.Security.Assert(PermissionType.AddNew);

            var originalPath = this.Path;
            var correctTargetPath = RepositoryPath.Combine(target.Path, RepositoryPath.PathSeparator);
            var correctCurrentPath = RepositoryPath.Combine(this.Path, RepositoryPath.PathSeparator);

            if (correctTargetPath.IndexOf(correctCurrentPath, StringComparison.Ordinal) != -1)
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Node cannot be moved under itself."));

            if (target.Id == this.ParentId)
                throw new InvalidOperationException("Node cannot be moved to its parent.");

            var targetPath = RepositoryPath.Combine(target.Path, this.Name);

            Node.AssertPath(targetPath);

            if (Node.Exists(targetPath))
                throw new ApplicationException(String.Concat("Target folder already contains a content named '", this.Name, "'."));

            var args = new CancellableNodeOperationEventArgs(this, target, CancellableNodeEvent.Moving);
            FireOnMoving(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);
            var customData = args.CustomData;

            var pathToInvalidate = String.Concat(this.Path, "/");

            try
            {
                DataProvider.Current.MoveNode(this.Id, target.Id, sourceTimestamp, targetTimestamp);
            }
            catch (DataOperationException e) //rethrow
            {
                if (e.Result == DataOperationResult.DataTooLong)
                    throw new RepositoryPathTooLongException(targetPath);

                throw new ApplicationException("Cannot move", e);
            }

            SecurityHandler.Move(this.Path, target.Path);
            PathDependency.FireChanged(pathToInvalidate);
            PathDependency.FireChanged(this.Path);

            Populator.DeleteTree(this.Path, true);

            //-- <L2Cache>
            StorageContext.L2Cache.Clear();
            //-- </L2Cache>

            var nodeHead = NodeHead.Get(Id);
            var userAccessLevel = GetUserAccessLevel(nodeHead);
            var acceptedLevel = GetAcceptedLevel(userAccessLevel, VersionNumber.LastAccessible);
            var versionId = GetVersionId(nodeHead, acceptedLevel != AccessLevel.Header ? acceptedLevel : AccessLevel.Major, VersionNumber.LastAccessible);

            var sharedData = DataBackingStore.GetNodeData(nodeHead, versionId);
            var privateData = NodeData.CreatePrivateData(sharedData.NodeData);
            _data = privateData;

            Populator.PopulateTree(this.Path);

            Logger.WriteVerbose("Node moved", GetLoggerPropertiesAfterMove, new object[] { this, originalPath, targetPath });
            FireOnMoved(target, customData, originalPath);
        }


        public static void MoveMore(List<Int32> nodeList, string targetPath, ref List<Exception> errors)
        {
            MoveMoreInternal2(new NodeList<Node>(nodeList), Node.LoadNode(targetPath), ref errors);
            return;

            //if (nodeList == null) throw new ArgumentNullException("nodeList");
            //if (nodeList.Count == 0) return;
            //if (string.IsNullOrEmpty(targetPath)) return;

            //MoveMoreInternal(nodeList, targetPath, ref errors);
        }

        private static void MoveMoreInternal2(NodeList<Node> sourceNodes, Node target, ref  List<Exception> errors)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            foreach (var sourceNode in sourceNodes)
            {
                try
                {
                    sourceNode.MoveTo(target);
                }
                catch (Exception e) //not logged, not thrown
                {
                    errors.Add(e);
                }
            }
        }

        private static IDictionary<string, object> GetLoggerPropertiesAfterMove(object[] args)
        {
            var props = Logger.GetDefaultProperties(args[0]);
            props.Add("OriginalPath", args[1]);
            props.Add("NewPath", args[2]);
            return props;
        }

        #endregion

        #region //================================================================================================= Copy methods

        /// <summary>
        /// Copy the Node indentified by its path to another location. The destination node is also identified by path. 
        /// </summary>
        /// <remarks>Use this method if you do not want to instantiate the nodes.</remarks>
        public static void Copy(string sourcePath, string targetPath)
        {
            Node sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.CopyFailed_SouceDoesNotExistWithPath_1, sourcePath));
            Node targetNode = Node.LoadNode(targetPath);
            if (targetNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.CopyFailed_TargetDoesNotExistWithPath_1, targetPath));
            sourceNode.CopyTo(targetNode);
        }

        public static void Copy(List<int> nodeList, string targetPath, ref List<Exception> errors)
        {
            if (nodeList == null)
                throw new ArgumentNullException("nodeList");
            if (nodeList.Count == 0)
                return;

            CopyMoreInternal(nodeList, targetPath, ref errors);
        }
        private static void CopyMoreInternal(IEnumerable<int> nodeList, string targetNodePath, ref List<Exception> errors)
        {
            StorageContext.Search.SearchEngine.WaitIfIndexingPaused();

            var col2 = new List<Node>();

            var targetNode = LoadNode(targetNodePath);
            if (targetNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.CopyFailed_TargetDoesNotExistWithPath_1, targetNodePath));

            using (var traceOperation = Logger.TraceOperation("Node.CopyMoreInternal"))
            {
                //
                // #1 check copy conditions
                //
                foreach (var nodeId in nodeList)
                {
                    var n = LoadNode(nodeId);
                    if (n == null)    // node has already become unavailable
                        continue;
                    var msg = n.CheckListAndItemCopyingConditions(targetNode);
                    if (msg == null)
                    {
                        col2.Add(n);
                        continue;
                    }
                    errors.Add(new InvalidOperationException(msg));
                }
                var nodesToRemove = new List<int>();
                string correctTargetPath;
                foreach (var node in col2)
                {
                    correctTargetPath = RepositoryPath.Combine(targetNode.Path, RepositoryPath.PathSeparator);
                    var correctCurrentPath = RepositoryPath.Combine(node.Path, RepositoryPath.PathSeparator);

                    if (correctTargetPath.IndexOf(correctCurrentPath) == -1)
                        continue;
                    errors.Add(new InvalidOperationException(String.Format("Node cannot be copied under itself: {0}.", correctCurrentPath)));
                    //col2.Remove(node);
                    nodesToRemove.Add(node.Id);
                }
                col2.RemoveAll(n => nodesToRemove.Contains(n.Id));
                nodesToRemove.Clear();

                targetNode.AssertLock();

                //
                // fire copying and cancel events
                //
                var customDataDictionary = new Dictionary<int, object>();
                foreach (var node in col2)
                {
                    var args = new CancellableNodeOperationEventArgs(node, targetNode, CancellableNodeEvent.Copying);
                    node.FireOnCopying(args);
                    if (!args.Cancel)
                    {
                        customDataDictionary.Add(node.Id, args.CustomData);
                        continue;
                    }
                    errors.Add(new CancelNodeEventException(args.CancelMessage, args.EventType, node));
                    //col2.Remove(node);
                    nodesToRemove.Add(node.Id);
                }
                col2.RemoveAll(n => nodesToRemove.Contains(n.Id));
                nodesToRemove.Clear();

                //
                //  copying
                //
                var targetChildren = targetNode.GetChildren();
                var targetNodeId = targetNode.Id;
                foreach (var node in col2)
                {
                    var originalPath = node.Path;
                    var newName = node.Name;
                    var targetName = newName;

                    int i = 0;

                    try
                    {
                        while (NameExists(targetChildren, targetName))
                        {
                            if (targetNodeId != node.ParentId)
                                throw new ApplicationException(String.Concat("This folder already contains a content named '", newName, "'."));
                            targetName = node.GenerateCopyName(i++);
                        }
                        correctTargetPath = RepositoryPath.Combine(targetNode.Path, RepositoryPath.PathSeparator);
                        var newPath = correctTargetPath + targetName;

                        node.DoCopy(newPath, targetName);

                        Logger.WriteVerbose("Node copied", GetLoggerPropertiesAfterCopy, new object[] { node, originalPath, newPath });
                        node.FireOnCopied(targetNode, customDataDictionary[node.Id]);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                        //col2.Remove(node);
                    }
                }

                traceOperation.IsSuccessful = true;

            }


        }

        /// <summary>
        /// Copies the Node instance to another loacation. The new location is a Node instance which will be parent node.
        /// </summary>
        public virtual void CopyTo(Node target)
        {
            CopyTo(target, this.Name);
        }
        /// <summary>
        /// Copies the Node instance to another loacation. The new location is a Node instance which will be parent node.
        /// </summary>
        public virtual void CopyTo(Node target, string newName)
        {
            StorageContext.Search.SearchEngine.WaitIfIndexingPaused();

            using (var traceOperation = Logger.TraceOperation("Node.SaveCopied"))
            {
                if (target == null)
                    throw new ArgumentNullException("target");

                string msg = CheckListAndItemCopyingConditions(target);
                if (msg != null)
                    throw new InvalidOperationException(msg);

                var originalPath = this.Path;
                string newPath;
                var correctTargetPath = RepositoryPath.Combine(target.Path, RepositoryPath.PathSeparator);
                var correctCurrentPath = RepositoryPath.Combine(this.Path, RepositoryPath.PathSeparator);

                if (correctTargetPath.IndexOf(correctCurrentPath) != -1)
                    throw new InvalidOperationException("Node cannot be copied under itself.");

                target.AssertLock();

                var args = new CancellableNodeOperationEventArgs(this, target, CancellableNodeEvent.Copying);
                FireOnCopying(args);

                if (args.Cancel)
                    throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);

                var customData = args.CustomData;

                var targetName = newName;

                int i = 0;
                var nodeList = target.GetChildren();
                while (NameExists(nodeList, targetName))
                {
                    if (target.Id != this.ParentId)
                        throw new ApplicationException(String.Concat("This folder already contains a content named '", newName, "'."));
                    targetName = GenerateCopyName(i++);
                }

                newPath = correctTargetPath + targetName;
                DoCopy(newPath, targetName);

                Logger.WriteVerbose("Node copied", GetLoggerPropertiesAfterCopy, new object[] { this, originalPath, newPath });
                FireOnCopied(target, customData);

                traceOperation.IsSuccessful = true;
            }
        }
        private string CheckListAndItemCopyingConditions(Node target)
        {
            string msg = null;
            bool sourceIsOuter = this.ContentListType == null;
            bool sourceIsList = !sourceIsOuter && this.ContentListId == 0;
            bool sourceIsItem = !sourceIsOuter && this.ContentListId != 0;

            //hack
            bool sourceIsSystemFolder = this.NodeType.IsInstaceOfOrDerivedFrom("SystemFolder");

            bool targetIsOuter = target.ContentListType == null;
            bool targetIsList = !targetIsOuter && target.ContentListId == 0;
            bool targetIsItem = !targetIsOuter && target.ContentListId != 0;
            if (sourceIsOuter && !targetIsOuter && !sourceIsSystemFolder)
            {
                msg = "Cannot copy outer item into a list. ";
            }
            else if (sourceIsList && !targetIsOuter)
            {
                msg = "Cannot copy a list into an another list. ";
            }
            else if (sourceIsItem)
            {
                //change: we don't mind if somebody copies an item out from the list
                //(it will lose the list fields though...)
                if (targetIsOuter)
                    msg = null; //"Cannot copy a list item out from the list. ";
                else if (targetIsList && this.ContentListType != target.ContentListType)
                    msg = "Cannot copy a list item into an another list. ";
                else if (targetIsItem && this.ContentListId != target.ContentListId)
                    msg = "Cannot copy a list item into an another list. ";
            }
            return msg;
        }

        private string GenerateCopyName(int index)
        {
            if (index == 0)
                return String.Concat("Copy of ", this.Name);
            return String.Concat("Copy (", index, ") of ", this.Name);
        }
        private void DoCopy(string targetPath, string newName)
        {
            bool first = true;
            var sourcePath = this.Path;
            if (!Node.Exists(sourcePath))
                throw new ContentNotFoundException(sourcePath);
            foreach (var sourceNode in NodeEnumerator.GetNodes(sourcePath, ExecutionHint.ForceRelationalEngine))
            {
                var targetNodePath = targetPath + sourceNode.Path.Substring(sourcePath.Length);
                targetNodePath = RepositoryPath.GetParentPath(targetNodePath);
                var targetNode = Node.LoadNode(targetNodePath);
                var copy = sourceNode.MakeCopy(targetNode, newName);
                copy.Save();
                CopyExplicitPermissionsTo(sourceNode, copy);
                if (first)
                {
                    newName = null;
                    first = false;
                }
            }
        }
        private void CopyExplicitPermissionsTo(Node sourceNode, Node targetNode)
        {
            AccessProvider.ChangeToSystemAccount();
            try
            {
                var entriesToCopy = sourceNode.Security.GetExplicitEntries();
                if (entriesToCopy.Count() == 0)
                    return;

                var ed = targetNode.Security.GetAclEditor();
                foreach (var entry in entriesToCopy)
                    foreach (var permType in ActiveSchema.PermissionTypes)
                        ed.SetPermission(entry.PrincipalId, entry.Propagates, permType, entry.PermissionValues[permType.Index - 1]);
                ed.Apply();
                if (!targetNode.IsInherited)
                    targetNode.Security.RemoveBreakInheritance();
            }
            finally
            {
                AccessProvider.RestoreOriginalUser();
            }
        }

        public Node MakeTemplatedCopy(Node target, string newName)
        {
            var copy = MakeCopy(target, newName);
            copy._copying = false;

            return copy;
        }

        public virtual Node MakeCopy(Node target, string newName)
        {
            var copy = this.NodeType.CreateInstance(target);
            copy._copying = true;
            var name = newName ?? this.Name;
            var path = RepositoryPath.Combine(target.Path, name);

            Node.AssertPath(path);

            copy.Data.Name = name;
            copy.Data.Path = path;
            this.Data.CopyGeneralPropertiesTo(copy.Data);
            CopyDynamicProperties(copy);

            // These properties must be copied this way. 
            copy["VersioningMode"] = this["VersioningMode"];
            copy["InheritableVersioningMode"] = this["InheritableVersioningMode"];
            copy["ApprovingMode"] = this["ApprovingMode"];
            copy["InheritableApprovingMode"] = this["InheritableApprovingMode"];

            var now = DateTime.UtcNow;
            copy.SetCreationDate(now);
            copy.ModificationDate = now;
            copy.SetVersionCreationDate(now);
            copy.VersionModificationDate = now;

            return copy;
        }
        protected virtual void CopyDynamicProperties(Node target)
        {
            this.Data.CopyDynamicPropertiesTo(target.Data);
        }
        private static IDictionary<string, object> GetLoggerPropertiesAfterCopy(object[] args)
        {
            var props = Logger.GetDefaultProperties(args[0]);
            props.Add("OriginalPath", args[1]);
            props.Add("NewPath", args[2]);
            return props;
        }
        #endregion

        #region //==========================================================================Templated creation

        private Node _template;
        public Node Template
        {
            get { return _template; }
            set { _template = value; }
        }

        #endregion

        #region //================================================================================================= Delete methods

        /// <summary>
        /// Deletes a Node and all of its contents from the database. This operation removes all child nodes too.
        /// </summary>
        /// <param name="sourcePath">The path of the Node that will be deleted.</param>
        [Obsolete("DeletePhysical is obsolete. Use ForceDelete to delete Node permanently.")]
        public static void DeletePhysical(string sourcePath)
        {
            var sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithPath_1, sourcePath));
            sourceNode.Delete();
        }
        /// <summary>
        /// Deletes a Node and all of its contents from the database. This operation removes all child nodes too.
        /// </summary>
        /// <param name="nodeId">Identifier of the Node that will be deleted.</param>
        [Obsolete("DeletePhysical is obsolete. Use ForceDelete to delete Node permanently.")]
        public static void DeletePhysical(int nodeId)
        {
            var sourceNode = Node.LoadNode(nodeId);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithId_1, nodeId));
            sourceNode.Delete();
        }
        /// <summary>
        /// Deletes the Node instance and all of its contents. This operation removes the appropriate nodes from the database.
        /// </summary>
        [Obsolete("The DeletePhysical is obsolete. Use ForceDelete to delete Node permanently.")]
        public virtual void DeletePhysical()
        {
            Delete();
        }

        public static void ForceDelete(string sourcePath)
        {
            var sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithPath_1, sourcePath));
            sourceNode.ForceDelete();
        }

        public static void ForceDelete(int nodeId)
        {
            var sourceNode = Node.LoadNode(nodeId);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithId_1, nodeId));
            sourceNode.ForceDelete();
        }

        /// <summary>
        /// This method deletes the node permanently
        /// </summary>
        public virtual void ForceDelete()
        {
            ForceDelete(this.NodeTimestamp);
        }
        private int _deletingAttempt;
        public virtual void ForceDelete(long timestamp)
        {
            StorageContext.Search.SearchEngine.WaitIfIndexingPaused();

            this.Security.AssertSubtree(PermissionType.Delete);

            this.AssertLock();

            var myPath = Path;

            var args = new CancellableNodeEventArgs(this, CancellableNodeEvent.DeletingPhysically);
            FireOnDeletingPhysically(args);
            if (args.Cancel)
                throw new CancelNodeEventException(args.CancelMessage, args.EventType, this);
            var customData = args.CustomData;

            var contentListTypesInTree = (this is IContentList) ?
                new List<ContentListType>(new[] { this.ContentListType }) :
                DataProvider.Current.GetContentListTypesInTree(this.Path);

            var logProps = CollectAllProperties(this.Data);
            try
            {
                DataProvider.Current.DeleteNodePsychical(this.Id, timestamp);
            }
            catch (NodeIsOutOfDateException) //rethrown
            {
                if (_deletingAttempt++ > 1) //max three times
                    throw;
                var oldPath = this.Path;
                Reload();
                if (this.Path != oldPath)
                    throw new ContentNotFoundException(oldPath); //content was moved
                ForceDelete(this.NodeTimestamp);
                return;
            }
            catch (Exception e) //rethrow
            {
                if (e.Message.Contains("DELETE statement conflicted with the REFERENCE constraint"))
                {
                    int totalCountOfReferrers;
                    var referrers = GetReferrers(5, out totalCountOfReferrers);
                    throw new CannotDeleteReferredContentException(referrers, totalCountOfReferrers);
                }
                throw new ApplicationException("You cannot delete this content", e);
            }

            MakePrivateData();
            this._data.IsDeleted = true;

            var hadContentList = RemoveContentListTypesInTree(contentListTypesInTree) > 0;

            SecurityHandler.Delete(myPath);
            Populator.DeleteTree(myPath, false);

            if (hadContentList)
                FireAnyContentListDeleted();

            PathDependency.FireChanged(myPath);

            Logger.WriteAudit(AuditEvent.ContentDeleted, logProps);
            FireOnDeletedPhysically(customData);


            //-- memberships
            if (this is IGroup)
                SecurityHandler.ExplicateGroupMembership();

            IUser thisAsUser = this as IUser;
            if (thisAsUser != null)
                SecurityHandler.ExplicateOrganizationUnitMemberships(thisAsUser);
        }
        protected virtual IEnumerable<Node> GetReferrers(int top, out int totalCount)
        {
            totalCount = 0;
            return null;
        }

        private int RemoveContentListTypesInTree(List<ContentListType> contentListTypesInTree)
        {
            var count = 0;
            if (contentListTypesInTree.Count > 0)
            {
                var editor = new SchemaEditor();
                editor.Load();
                foreach (var t in contentListTypesInTree)
                {
                    if (t != null)
                    {
                        editor.DeleteContentListType(editor.ContentListTypes[t.Name]);
                        count++;
                    }
                }
                editor.Register();
            }
            return count;
        }

        /// <summary>
        /// Delete the node
        /// </summary>
        public static void Delete(string sourcePath)
        {
            var sourceNode = Node.LoadNode(sourcePath);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithPath_1, sourcePath));
            sourceNode.Delete();
        }

        /// <summary>
        /// Delete the node
        /// </summary>
        public static void Delete(int nodeId)
        {
            var sourceNode = Node.LoadNode(nodeId);
            if (sourceNode == null)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.Operations.DeleteFailed_ContentDoesNotExistWithId_1, nodeId));
            sourceNode.Delete();
        }

        /// <summary>
        /// Batch delete.
        /// </summary>
        /// <param name="nodeList">Represents an Id collection which holds the identifiers of the nodes will be deleted.</param>
        /// <param name="errors">If any error occures, it is added to the errors collection passed by errors parameter.</param>
        /// <exception cref="ArgumentNullException">You must specify a list collection instance.</exception>
        public static void Delete(List<int> nodeList, ref List<Exception> errors)
        {
            if (nodeList == null)
                throw new ArgumentNullException("nodeList");
            if (nodeList.Count == 0)
                return;
            Node.DeleteMoreInternal(nodeList, ref errors);
        }

        //
        // TODO: need to consider> method based upon the original DeleteInternal, this contains duplicated source code
        //
        private static void DeleteMoreInternal(ICollection<Int32> nodeList, ref List<Exception> errors)
        {
            StorageContext.Search.SearchEngine.WaitIfIndexingPaused();

            if (nodeList == null)
                throw new ArgumentNullException("nodeList");
            if (nodeList.Count == 0)
                return;

            var col2 = new List<Node>();
            var col3 = new List<Node>();

            foreach (Int32 n in nodeList)
            {
                var node = LoadNode(n);
                try
                {
                    node.Security.AssertSubtree(PermissionType.Delete);
                    node.AssertLock();

                }
                catch (Exception e)
                {
                    errors.Add(e);
                    continue;
                }
                col2.Add(node);
            }

            var customDataDictionary = new Dictionary<int, object>();
            foreach (var nodeRef in col2)
            {
                var internalError = false;
                var myPath = nodeRef.Path;

                var args = new CancellableNodeEventArgs(nodeRef, CancellableNodeEvent.DeletingPhysically);
                nodeRef.FireOnDeletingPhysically(args);
                if (args.Cancel)
                    throw new CancelNodeEventException(args.CancelMessage, args.EventType, nodeRef);
                customDataDictionary.Add(nodeRef.Id, args.CustomData);

                //var logProps = CollectAllProperties(nodeRef.Data);

                try
                {
                    DataProvider.Current.DeleteNodePsychical(nodeRef.Id, nodeRef.NodeTimestamp);
                }
                catch (Exception e) //rethrow
                {
                    var msg = new StringBuilder("You cannot delete this content ");
                    if (e.Message.Contains("DELETE statement conflicted with the REFERENCE constraint"))
                        msg.Append("because it is referenced by another content.");
                    //throw new ApplicationException(msg.ToString(), e);
                    internalError = true;
                    errors.Add(new ApplicationException(msg.ToString(), e));
                }
                if (internalError)
                    continue;

                col3.Add(nodeRef);

                nodeRef.MakePrivateData();
                nodeRef._data.IsDeleted = true;

                if (nodeRef is IContentList)
                {
                    if (nodeRef.ContentListType != null)
                    {
                        var editor = new SchemaEditor();
                        editor.Load();
                        editor.DeleteContentListType(editor.ContentListTypes[nodeRef.ContentListType.Name]);
                        editor.Register();
                    }
                }
                SecurityHandler.Delete(myPath);
            }

            var ids = new List<Int32>();
            for (int index = 0; index < col3.Count; index++)
            {
                var n = col3[index];
                ids.Add(n.Id);
            }
            try
            {
                Populator.DeleteForest(ids, false);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }


            for (int index = 0; index < col3.Count; index++)
            {
                var n = col3[index];
                PathDependency.FireChanged(n.Path);
                //Logger.WriteAudit(AuditEvent.ContentDeleted, n.Path);
                n.FireOnDeletedPhysically(customDataDictionary[n.Id]);
            }
        }




        /// <summary>
        /// Delete current node
        /// </summary>
        public virtual void Delete()
        {
            ForceDelete();
        }

        ///// <summary>
        ///// Delete the current node version.
        ///// </summary>
        //public virtual void DeleteVersion()
        //{
        //    var deletedVersionNumber = this.Version.Clone();

        //    DeleteVersion(this);
        //    var nodeHead = NodeHead.Get(Id);
        //    var sharedData = DataBackingStore.GetNodeData(nodeHead, nodeHead.LastMinorVersionId);
        //    var privateData = NodeData.CreatePrivateData(sharedData.NodeData);

        //    _data = privateData;

        //    Logger.WriteVerbose("Version deleted.", GetLoggerPropertiesAfterDeleteVersion, new object[] { this, deletedVersionNumber });
        //}
        //private static IDictionary<string, object> GetLoggerPropertiesAfterDeleteVersion(object[] args)
        //{
        //    var props = Logger.GetDefaultProperties(args[0]);
        //    props.Add("DeletedVersion", args[1]);
        //    return props;
        //}

        ///// <summary>
        ///// Delete the specified node version.
        ///// </summary>
        //public static void DeleteVersion(int majorNumber, int minorNumber, int nodeId)
        //{
        //    var oldVersion = Node.LoadNode(nodeId, new VersionNumber(majorNumber, minorNumber));
        //    if (oldVersion != null)
        //        DeleteVersion(oldVersion);
        //}
        //private static void DeleteVersion(Node oldVersion)
        //{
        //    int majorNumber = oldVersion.Version.Major;
        //    int minorNumber = oldVersion.Version.Minor;
        //    int nodeId=oldVersion.Id;
        //    int versionId = oldVersion.VersionId;

        //    var populatorData = Populator.BeginDeleteVersion(oldVersion);
        //    DataProvider.Current.DeleteVersion(majorNumber, minorNumber, nodeId);
        //    NodeIdDependency.FireChanged(nodeId);
        //    Populator.CommitDeleteVersion(populatorData);
        //}

        #endregion

        public virtual string GetPasswordSalt()
        {
            return this.CreationDate.ToString("yyyyMMddHHmmssff");
        }

        #region //================================================================================================= Events

        private List<Type> _disabledObservers;
        public IEnumerable<Type> DisabledObservers { get { return _disabledObservers; } }
        public void DisableObserver(Type observerType)
        {
            if (observerType == null || !observerType.IsSubclassOf(typeof(NodeObserver)))
                throw new InvalidOperationException("You can only disable types that are derived from NodeObserver.");

            if (_disabledObservers == null)
                _disabledObservers = new List<Type>();
            if (!_disabledObservers.Contains(observerType))
                _disabledObservers.Add(observerType);
        }

        public event CancellableNodeEventHandler Creating;
        public event EventHandler<NodeEventArgs> Created;
        public event CancellableNodeEventHandler Modifying;
        public event EventHandler<NodeEventArgs> Modified;
        public event CancellableNodeEventHandler Deleting;
        public event EventHandler<NodeEventArgs> Deleted;
        public event CancellableNodeEventHandler DeletingPhysically;
        public event EventHandler<NodeEventArgs> DeletedPhysically;
        public event CancellableNodeOperationEventHandler Moving;
        public event EventHandler<NodeOperationEventArgs> Moved;
        public event CancellableNodeOperationEventHandler Copying;
        public event EventHandler<NodeOperationEventArgs> Copied;
        public event CancellableNodeEventHandler PermissionChanging;
        public event EventHandler<NodeEventArgs> PermissionChanged;

        //TODO: public event EventHandler Undeleted;
        //TODO: public event EventHandler Locked;
        //TODO: public event EventHandler Unlocked;
        //TODO: public event EventHandler LockRemoved;

        private void FireOnCreating(CancellableNodeEventArgs e)
        {
            OnCreating(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeCreating(Creating, this, e, _disabledObservers);
        }
        private void FireOnCreated(object customData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.Created, customData);
            OnCreated(this, e);
            NodeObserver.FireOnNodeCreated(Created, this, e, _disabledObservers);
        }
        private void FireOnModifying(CancellableNodeEventArgs e)
        {
            OnModifying(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeModifying(Modifying, this, e, _disabledObservers);
        }
        private void FireOnModified(string originalSourcePath, object customData, IEnumerable<ChangedData> changedData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.Modified, customData, originalSourcePath, changedData);
            OnModified(this, e);
            NodeObserver.FireOnNodeModified(Modified, this, e, _disabledObservers);
        }
        private void FireOnDeleting(CancellableNodeEventArgs e)
        {
            OnDeleting(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeDeleting(Deleting, this, e, _disabledObservers);
        }
        private void FireOnDeleted(object customData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.Deleted, customData);
            OnDeleted(this, e);
            NodeObserver.FireOnNodeDeleted(Deleted, this, e, _disabledObservers);
        }
        private void FireOnDeletingPhysically(CancellableNodeEventArgs e)
        {
            OnDeletingPhysically(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeDeletingPhysically(DeletingPhysically, this, e, _disabledObservers);
        }
        private void FireOnDeletedPhysically(object customData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.DeletedPhysically, customData);
            OnDeletedPhysically(this, e);
            NodeObserver.FireOnNodeDeletedPhysically(DeletedPhysically, this, e, _disabledObservers);
        }
        private void FireOnMoving(CancellableNodeOperationEventArgs e)
        {
            OnMoving(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeMoving(Moving, this, e, _disabledObservers);
        }
        private void FireOnMoved(Node targetNode, object customData, string originalSourcePath)
        {
            NodeOperationEventArgs e = new NodeOperationEventArgs(this, targetNode, NodeEvent.Moved, customData, originalSourcePath);
            OnMoved(this, e);
            NodeObserver.FireOnNodeMoved(Moved, this, e, _disabledObservers);
        }
        private void FireOnCopying(CancellableNodeOperationEventArgs e)
        {
            OnCopying(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnNodeCopying(Copying, this, e, _disabledObservers);
        }
        private void FireOnCopied(Node targetNode, object customData)
        {
            NodeOperationEventArgs e = new NodeOperationEventArgs(this, targetNode, NodeEvent.Copied, customData);
            OnCopied(this, e);
            NodeObserver.FireOnNodeCopied(Copied, this, e, _disabledObservers);
        }
        private void FireOnLoaded()
        {
            var e = new NodeEventArgs(this, NodeEvent.Loaded, null);
            OnLoaded(this, e);
            //NodeObserver.FireOnNodeLoaded(Created, this, e, _disabledObservers);
        }
        internal void FireOnPermissionChanging(CancellableNodeEventArgs e)
        {
            OnPermissionChanging(this, e);
            if (e.Cancel)
                return;
            NodeObserver.FireOnPermissionChanging(PermissionChanging, this, e, _disabledObservers);
        }
        internal void FireOnPermissionChanged(IEnumerable<ChangedData> changedData, object customData)
        {
            NodeEventArgs e = new NodeEventArgs(this, NodeEvent.PermissionChanged, customData, this.Path, changedData);
            OnPermissionChanged(this, e);
            NodeObserver.FireOnPermissionChanged(PermissionChanged, this, e, _disabledObservers);
        }

        protected virtual void OnCreating(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnCreated(object sender, NodeEventArgs e) { }
        protected virtual void OnModifying(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnModified(object sender, NodeEventArgs e) { }
        protected virtual void OnDeleting(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnDeleted(object sender, NodeEventArgs e) { }
        protected virtual void OnDeletingPhysically(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnDeletedPhysically(object sender, NodeEventArgs e) { }
        protected virtual void OnMoving(object sender, CancellableNodeOperationEventArgs e) { }
        protected virtual void OnMoved(object sender, NodeOperationEventArgs e) { }
        protected virtual void OnCopying(object sender, CancellableNodeOperationEventArgs e) { }
        protected virtual void OnCopied(object sender, NodeOperationEventArgs e) { }
        protected virtual void OnLoaded(object sender, NodeEventArgs e) { }
        protected virtual void OnPermissionChanging(object sender, CancellableNodeEventArgs e) { }
        protected virtual void OnPermissionChanged(object sender, NodeEventArgs e) { }

        #endregion

        public static event EventHandler AnyContentListDeleted;
        private void FireAnyContentListDeleted()
        {
            if (AnyContentListDeleted != null)
                AnyContentListDeleted(this, EventArgs.Empty);
        }


        #region //================================================================================================= Public Tools

        public bool IsPropertyChanged(string propertyName)
        {
            return Data.IsPropertyChanged(propertyName);
        }

        public static bool Exists(string path)
        {
            return DataBackingStore.NodeExists(path);
        }

        public Node LoadContentList()
        {
            if (this.ContentListId == 0)
                return null;
            return Node.LoadNode(this.ContentListId);
        }

        public static Node GetAncestorOfNodeType(Node child, string typeName)
        {
            while ((child != null) && (!child.NodeType.IsInstaceOfOrDerivedFrom(typeName)))
                child = child.Parent;

            return child;
        }

        public static T GetAncestorOfType<T>(Node child) where T : Node
        {
            T ancestor = null;

            while ((child != null) && ((ancestor = child as T) == null))
            {
                var head = NodeHead.Get(child.ParentPath);
                if (head == null)
                    return null;
                if (!SecurityHandler.HasPermission(head, PermissionType.See))
                    return null;
                child = child.Parent;
            }

            return ancestor;
        }

        public Node GetAncestor(int ancestorIndex)
        {
            if (ancestorIndex == 0) return this;
            if (ancestorIndex < 0)
                throw new NotSupportedException("AncestorIndex < 0");

            //TODO: implement unsafe str* handling to get number of slashes
            string[] path = this.Path.Split('/');
            if (ancestorIndex >= path.Length)
                throw new ApplicationException("ancestorIndex overflow");

            //TODO: implement unsafe str* handling
            string ancestorPath = string.Join("/", path, 0, path.Length - ancestorIndex);

            Node ancestor = Node.LoadNode(ancestorPath);

            return ancestor;
        }
        public bool IsDescendantOf(Node ancestor)
        {
            return (this.Path.StartsWith(ancestor.Path + "/"));
        }
        public bool IsDescendantOf(Node ancestor, out int distance)
        {
            distance = -1;
            if (!IsDescendantOf(ancestor))
                return false;
            distance = this.NodeLevel() - ancestor.NodeLevel();
            return true;
        }

        ///// <summary>
        ///// Check if there is a Node with the same name with the same parent Node.
        ///// </summary>
        ///// <returns>True if path is available.</returns>
        //public bool IsPathAvailable
        //{
        //    get
        //    {
        //        string parentPath = RepositoryPath.GetParentPath(this.Path);
        //        string newPath = string.Concat(parentPath, RepositoryPath.PathSeparator, this.Name);
        //        return !DataProvider.NodeExists(newPath, this.Id);
        //    }
        //}

        /// <summary>
        /// Returns the level of hierachy the node is located at. The virtual Root node has always
        /// a level of 0.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int NodeLevel()
        {
            return this.Path.Split('/').Length - 2;
        }

        public long GetFullSize()
        {
            return Node.GetTreeSize(this.Path, false);
        }

        public long GetTreeSize()
        {
            return Node.GetTreeSize(this.Path, true);
        }

        public static long GetTreeSize(string path)
        {
            return GetTreeSize(path, true);
        }

        private static long GetTreeSize(string path, bool includeChildren)
        {
            return DataProvider.Current.GetTreeSize(path, includeChildren);
        }

        #endregion

        #region //================================================================================================= Private Tools

        private void AssertSeeOnly(PropertyType propertyType)
        {
            if (IsPreviewOnly && propertyType.DataType == DataType.Binary)
            {
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.General.Error_Preview_BinaryAccess_2, this.Path, propertyType.Name));
            }
            else if (IsHeadOnly && !SeeEnabledProperties.Contains(propertyType.Name)) // && AccessProvider.Current.GetCurrentUser().Id != -1)
            {
                throw new InvalidOperationException(String.Concat("Invalid property access attempt on a See-only node. The accessible properties are: ", String.Join(", ", SeeEnabledProperties), "."));
            }
        }
        private void AssertLock()
        {
            if ((Lock.LockedBy != null && Lock.LockedBy.Id != AccessProvider.Current.GetCurrentUser().Id) && Lock.Locked)
                throw new LockedNodeException(Lock);
        }
        private static bool NameExists(IEnumerable<Node> nodeList, string name)
        {
            foreach (Node node in nodeList)
                if (node.Name == name)
                    return true;
            return false;
        }
        private static bool IsValidName(string name)
        {
            return !RepositoryPath.NameContainsInvalidChar(name);
        }
        private Exception Exception_ReferencedNodeCouldNotBeLoadedException(string referenceCategory, int referenceId, Exception innerException)
        {
            return new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "The '{0}' could not be loaded because it has been deleted, or the actual user hasn't got sufficient rights to see that node.\nThe NodeId of this node is {1}, the reference id is {2}.", referenceCategory, this.Id, referenceId), innerException);
        }

        private IUser LoadRefUserOrSomebody(int nodeId, string propertyName)
        {
            Node node = null;
            IUser user = null;
            if (nodeId == 0)
                return null;
            var head = NodeHead.Get(nodeId);
            if (head == null)
                return null;
            if (!SecurityHandler.HasPermission(head, PermissionType.See))
                return LoadSomebody();

            try
            {
                node = Node.LoadNode(nodeId);
                user = node as IUser;
            }
            catch (Exception e) //rethrow
            {
                throw Exception_ReferencedNodeCouldNotBeLoadedException(propertyName, nodeId, e);
            }
            if (user != null)
                return user;

            if (node == null)
                throw new ApplicationException("User not found. Content: " + this.Path + ", " + propertyName);
            else
                throw new ApplicationException("'" + propertyName + "' should refer to an IUser.");
        }
        private static IUser LoadSomebody()
        {
            using (new SystemAccount())
            {
                return (IUser)LoadNode(RepositoryConfiguration.SomebodyUserId);
            }
        }

        internal static void AssertPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (path.Length > RepositoryConfiguration.MaximumPathLength)
                throw new RepositoryPathTooLongException(path);
        }

        #endregion

        /*================================================================================================= Linq Tools */

        public bool InFolder(string path) { return path.Equals(this.ParentPath, StringComparison.OrdinalIgnoreCase); }
        public bool InFolder(Node node) { return InFolder(node.Path); }
        public bool InTree(string path) { return Path.StartsWith(path, StringComparison.OrdinalIgnoreCase); }
        public bool InTree(Node node) { return InTree(node.Path); }
        public bool Type(string contentTypeName) { return NodeType.Name == contentTypeName; }
        public bool TypeIs(string contentTypeName) { return NodeType.IsInstaceOfOrDerivedFrom(contentTypeName); }

    }
    public class BenchmarkCounter
    {
        public enum CounterName
        {
            GetParentPath,
            LoadParent,
            ContentCreate,
            BinarySet,
            FullSave,
            BeforeSaveToDb,
            SaveNodeBaseData,
            SaveNodeBinaries,
            SaveNodeFlatProperties,
            SaveNodeTextProperties,
            SaveNodeReferenceProperties,
            CommitPopulateNode,
            Audit,
            FinalizingSave
        }

        public static readonly string TraceCounterDataSlotName = "XCounterDataSlot";
        private static readonly int EnumLength;
        public static void IncrementBy(CounterName counterName, long value)
        {
            var slot = System.Threading.Thread.GetNamedDataSlot(TraceCounterDataSlotName);
            var xcounter = (BenchmarkCounter)System.Threading.Thread.GetData(slot);
            if (xcounter == null)
            {
                xcounter = new BenchmarkCounter();
                System.Threading.Thread.SetData(slot, xcounter);
            }
            xcounter[counterName] += value;
        }

        public static void Reset()
        {
            var slot = System.Threading.Thread.GetNamedDataSlot(TraceCounterDataSlotName);
            System.Threading.Thread.SetData(slot, new BenchmarkCounter());
        }
        public static long[] GetAll()
        {
            var slot = System.Threading.Thread.GetNamedDataSlot(TraceCounterDataSlotName);
            var xcounter = (BenchmarkCounter)System.Threading.Thread.GetData(slot);
            if (xcounter == null)
                return new long[EnumLength];
            return xcounter._counters;
        }

        // ==========================================================================================

        static BenchmarkCounter()
        {
            EnumLength = Enum.GetNames(typeof(CounterName)).Length;
        }

        private long[] _counters = new long[EnumLength];
        private long this[CounterName counterName]
        {
            get { return _counters[(int)counterName]; }
            set { _counters[(int)counterName] = value; }
        }

    }
}
