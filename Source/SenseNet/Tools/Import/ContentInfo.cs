using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Handlers;
using SNC = SenseNet.ContentRepository;
using System.IO;
using System.Reflection;
using SenseNet.ContentRepository;
using SenseNet.Portal.UI.Controls;
using SenseNet.Portal.UI.PortletFramework;
using System.Xml.Xsl;

namespace SenseNet.Tools.ContentImporter
{
    internal class TransformerContext
    {
        public XslCompiledTransform XsltTransformer { get; set; }
        public XmlWriterSettings XsltOutputSettings { get; set; }
        public Encoding XsltEncoding { get; set; }
        public XsltArgumentList XsltArgList { get; set; }
        public string[] NamespaceExtensions { get; set; }
        public bool IsValid
        {
            get { return this.XsltTransformer != null; }
        }

        public TransformerContext(string transformerPath)
        {
            if (transformerPath != null)
            {
                XslCompiledTransform transform = null;
                if (transformerPath.StartsWith("/Root/") && Node.Exists(transformerPath))
                {
                    transform = new XslCompiledTransform();
                    SenseNet.Portal.UI.PortletFramework.Xslt.RepositoryPathResolver resolver = new SenseNet.Portal.UI.PortletFramework.Xslt.RepositoryPathResolver();
                    transform.Load(transformerPath, XsltSettings.Default, resolver);
                    NamespaceExtensions = resolver.ImportNamespaceCollection.Distinct().ToArray();
                }
                else if (System.IO.File.Exists(transformerPath))
                {
                    transform = new XslCompiledTransform();
                    SenseNet.Portal.UI.PortletFramework.Xslt.FilePathResolver resolver = new Xslt.FilePathResolver();
                    transform.Load(transformerPath, XsltSettings.Default, resolver);
                    NamespaceExtensions = resolver.ImportNamespaceCollection.Distinct().ToArray();
                }

                if (transform != null)
                {
                    XsltOutputSettings = transform.OutputSettings;
                    this.XsltEncoding = Encoding.UTF8;
                    if (XsltOutputSettings != null && XsltOutputSettings.Encoding != null)
                    {
                        this.XsltEncoding = XsltOutputSettings.Encoding;
                    }
                    this.XsltTransformer = transform;
                }

                if (XsltArgList == null && NamespaceExtensions.Length > 0)
                {
                    XsltArgList = new XsltArgumentList();
                }

                foreach (var namespaceExtension in NamespaceExtensions)
                {
                    var typename = namespaceExtension.Substring(5);
                    var o = XsltArgList.GetExtensionObject(namespaceExtension);
                    if (o == null)
                    {
                        var instance = TypeHandler.CreateInstance(typename);
                        XsltArgList.AddExtensionObject(namespaceExtension, instance);
                    }
                }

            }
        }

    }

    [DebuggerDisplay("ContentInfo: Name={Name}; ContentType={ContentTypeName}; IsFolder={IsFolder} ({Attachments.Count} Attachments)")]
    internal class ContentInfo
    {
        private string _metaDataPath;
        private int _contentId;
        private bool _isFolder;
        private string _name;
        private List<string> _attachments;
        private string _contentTypeName;
        private XmlDocument _xmlDoc;
        private string _childrenFolder;
        private ImportContext _transferringContext;

        public string MetaDataPath
        {
            get { return _metaDataPath; }
        }
        public int ContentId
        {
            get { return _contentId; }
        }
        public bool IsFolder
        {
            get { return _isFolder; }
        }
        public string Name
        {
            get { return _name; }
        }
        public List<string> Attachments
        {
            get { return _attachments; }
        }
        public string ContentTypeName
        {
            get { return _contentTypeName; }
        }
        public string ChildrenFolder
        {
            get { return _childrenFolder; }
            // Written by ImporterClass in case of existing .Children folder.
            set { _childrenFolder = value; }
        }
        public bool HasReference
        {
            get
            {
                if (_transferringContext == null)
                    return false;
                return _transferringContext.HasReference;
            }
        }
        public bool HasPermissions { get; private set; }
        public bool HasBreakPermissions { get; private set; }
        public bool HasAspect { get; private set; }
        public bool ClearPermissions { get; private set; }
        public bool FileIsHidden { get; private set; }
        public bool ContentTypeIsInferredFolder { get; private set; }
        public bool ContentTypeIsInferredFile { get; private set; }
        public bool Delete { get; private set; }

        public ContentInfo(string path, Node parent, TransformerContext xsltOptions)
        {
            try
            {
                _metaDataPath = path;
                _attachments = new List<string>();

                string directoryName = Path.GetDirectoryName(path);
                _name = Path.GetFileName(path);
                string extension = Path.GetExtension(_name);
                if (extension.ToLower() == ".content")
                {
                    var fileInfo = new FileInfo(path);
                    FileIsHidden = (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

                    _xmlDoc = new XmlDocument();
                    _xmlDoc.Load(path);

                    // start of xml transformation
                    if (xsltOptions != null && xsltOptions.IsValid)
                    {
                        using (MemoryStream output = new MemoryStream())
                        {
                            using (var writer = XmlWriter.Create(output, xsltOptions.XsltOutputSettings))
                            {

                                xsltOptions.XsltTransformer.Transform(_xmlDoc, xsltOptions.XsltArgList, writer);
                                output.Position = 0;
                                _xmlDoc.Load(output);
                            }
                        }
                    }
                    // end of xml transformation

                    XmlNode nameNode = _xmlDoc.SelectSingleNode("/ContentMetaData/ContentName");
                    _name = nameNode == null ? Path.GetFileNameWithoutExtension(_name) : nameNode.InnerText;

                    var deleteAttr = _xmlDoc.DocumentElement.Attributes["delete"];
                    if (deleteAttr != null && deleteAttr.Value == "true")
                    {
                        this.Delete = true;
                    }
                    else
                    {
                        this.Delete = false;

                        _contentTypeName = _xmlDoc.SelectSingleNode("/ContentMetaData/ContentType").InnerText;

                        ClearPermissions = _xmlDoc.SelectSingleNode("/ContentMetaData/Permissions/Clear") != null;
                        HasBreakPermissions = _xmlDoc.SelectSingleNode("/ContentMetaData/Permissions/Break") != null;
                        HasPermissions = _xmlDoc.SelectNodes("/ContentMetaData/Permissions/Identity").Count > 0;
                        HasAspect = _xmlDoc.SelectNodes("ContentMetaData/Fields/Aspects").Count > 0;

                        // /ContentMetaData/Properties/*/@attachment
                        foreach (XmlAttribute attachmentAttr in _xmlDoc.SelectNodes("/ContentMetaData/Fields/*/@attachment"))
                        {
                            string attachment = attachmentAttr.Value;
                            _attachments.Add(attachment);
                            bool isFolder = Directory.Exists(Path.Combine(directoryName, attachment));
                            if (isFolder)
                            {
                                if (_isFolder)
                                    throw new ApplicationException(String.Concat("Two or more attachment folder is not enabled. ContentName: ", _name));
                                _isFolder = true;
                                _childrenFolder = Path.Combine(directoryName, attachment);
                            }
                        }
                        //-- default attachment
                        var defaultAttachmentPath = Path.Combine(directoryName, _name);
                        if (!_attachments.Contains(_name))
                        {
                            string[] paths;
                            if (Directory.Exists(defaultAttachmentPath))
                                paths = new string[] { defaultAttachmentPath };
                            else
                                paths = new string[0];

                            //string[] paths = Directory.GetDirectories(directoryName, _name);
                            if (paths.Length == 1)
                            {
                                if (_isFolder)
                                    throw new ApplicationException(String.Concat("Two or more attachment folder is not enabled. ContentName: ", _name));
                                _isFolder = true;
                                _childrenFolder = defaultAttachmentPath;
                                _attachments.Add(_name);
                            }
                            else
                            {
                                if (System.IO.File.Exists(defaultAttachmentPath))
                                    _attachments.Add(_name);
                            }
                        }
                    } //if (deleteAttr != null && deleteAttr.Value == "true")
                }
                else
                {
                    _isFolder = Directory.Exists(path);
                    if (_isFolder)
                    {
                        var dirInfo = new DirectoryInfo(path);
                        FileIsHidden = (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        ContentTypeIsInferredFolder = true;

                        _contentTypeName = GetParentAllowedContentTypeName(path, parent, "Folder");
                        _childrenFolder = path;

                        // start of xml transformation - for possible contentname conversion
                        if (xsltOptions != null && xsltOptions.IsValid)
                        {
                            using (MemoryStream output = new MemoryStream())
                            {
                                using (var writer = XmlWriter.Create(output, xsltOptions.XsltOutputSettings))
                                {
                                    var processXml = new XmlDocument();
                                    processXml.LoadXml(String.Format("<ContentMetaData><ContentName>{0}</ContentName></ContentMetaData>", _name));

                                    xsltOptions.XsltTransformer.Transform(processXml, xsltOptions.XsltArgList, writer);
                                    output.Position = 0;
                                    processXml.Load(output);

                                    XmlNode nameNode = processXml.SelectSingleNode("/ContentMetaData/ContentName");
                                    _name = nameNode == null ? Path.GetFileNameWithoutExtension(_name) : nameNode.InnerText;
                                }
                            }
                        }
                        // end of xml transformation
                    }
                    else
                    {
                        var fileInfo = new FileInfo(path);
                        FileIsHidden = (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        ContentTypeIsInferredFile = true;

                        _xmlDoc = new XmlDocument();
                        _contentTypeName = UploadHelper.GetContentType(path, parent.Path) ?? GetParentAllowedContentTypeName(path, parent, "File");

                        // modified for possible contentname conversion
                        var contentMetaData = String.Concat("<ContentMetaData><ContentType>{0}</ContentType><ContentName>{1}</ContentName><Fields><Binary attachment='", _name.Replace("'", "&apos;"), "' /></Fields></ContentMetaData>");
                        _xmlDoc.LoadXml(String.Format(contentMetaData, _contentTypeName, _name));

                        // start of xml transformation - for possible contentname conversion
                        if (xsltOptions != null && xsltOptions.IsValid)
                        {
                            using (MemoryStream output = new MemoryStream())
                            {
                                using (var writer = XmlWriter.Create(output, xsltOptions.XsltOutputSettings))
                                {
                                    xsltOptions.XsltTransformer.Transform(_xmlDoc, xsltOptions.XsltArgList, writer);
                                    output.Position = 0;
                                    _xmlDoc.Load(output);

                                    XmlNode nameNode = _xmlDoc.SelectSingleNode("/ContentMetaData/ContentName");
                                    _name = nameNode == null ? Path.GetFileNameWithoutExtension(_name) : nameNode.InnerText;
                                }
                            }
                        }
                        // end of xml transformation

                        _attachments.Add(_name);
                    }
                }

            }
            catch (Exception e)
            {
                throw new ApplicationException("Cannot create a ContentInfo. Path: " + path, e);
            }
        }

        public bool SetMetadata(SNC.Content content, string currentDirectory, bool isNewContent, bool needToValidate, bool updateReferences)
        {
            if (_xmlDoc == null)
                return true;
            _transferringContext = new ImportContext(
                _xmlDoc.SelectNodes("/ContentMetaData/Fields/*"), currentDirectory, isNewContent, needToValidate, updateReferences);
            bool result = content.ImportFieldData(_transferringContext);
            _contentId = content.ContentHandler.Id;
            return result;
        }

        internal bool UpdateReferences(SNC.Content content, bool needToValidate)
        {
            if (_transferringContext == null)
                _transferringContext = new ImportContext(_xmlDoc.SelectNodes("/ContentMetaData/Fields/*"), null, false, needToValidate, true);
            else
                _transferringContext.UpdateReferences = true;

            var node = content.ContentHandler;
            node.ModificationDate = node.ModificationDate;
            node.VersionModificationDate = node.VersionModificationDate;
            node.ModifiedBy = node.ModifiedBy;
            node.VersionModifiedBy = node.VersionModifiedBy;

            if (!content.ImportFieldData(_transferringContext))
                return false;
            if (!HasPermissions && !HasBreakPermissions)
                return true;
            var permissionsNode = _xmlDoc.SelectSingleNode("/ContentMetaData/Permissions");
            content.ContentHandler.Security.ImportPermissions(permissionsNode, this._metaDataPath);

            return true;
        }

        private static string GetParentAllowedContentTypeName(string fileName, Node parent, string defaultFileTypeName)
        {
            var node = (parent as GenericContent);
            if (node == null)
                return defaultFileTypeName;

            var allowedChildTypes = node.GetAllowedChildTypes().ToList();
            string typeName = null;
            foreach (var item in allowedChildTypes)
            {
                // skip any SystemFolder if it is not the only allowed type
                if (item.IsInstaceOfOrDerivedFrom("SystemFolder")
                    && allowedChildTypes.Count > 1)
                    continue;

                // choose the allowed type if this is the only suitable allowed type (eg the only type inheriting from File)
                // otherwise if more allowed types are suitable, choose the default type
                if (item.IsInstaceOfOrDerivedFrom(defaultFileTypeName))
                {
                    if (typeName != null)
                        typeName = defaultFileTypeName;
                    else
                        typeName = item.Name;
                }
            }

            return typeName ?? defaultFileTypeName;
        }
    }
}
