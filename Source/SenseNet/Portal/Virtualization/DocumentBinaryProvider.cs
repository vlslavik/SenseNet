using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.Portal.Virtualization
{
    public abstract class DocumentBinaryProvider
    {
        //============================================================ Provider 

        private static DocumentBinaryProvider _current;
        public static DocumentBinaryProvider Current
        {
            get
            {
                if (_current == null)
                {
                    var baseType = typeof(DocumentBinaryProvider);
                    var defType = typeof(DefaultDocumentBinaryProvider);
                    var dbpType = TypeHandler.GetTypesByBaseType(baseType).FirstOrDefault(t => t.FullName != baseType.FullName && t.FullName != defType.FullName) ?? defType;

                    _current = Activator.CreateInstance(dbpType) as DocumentBinaryProvider;
                }

                return _current;
            }
        }

        //============================================================ Instance API

        public abstract Stream GetStream(Node node, string propertyName, out string contentType, out BinaryFileName fileName);
    }

    public class DefaultDocumentBinaryProvider : DocumentBinaryProvider
    {
        //============================================================ Overrides

        public override Stream GetStream(Node node, string propertyName, out string contentType, out BinaryFileName fileName)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            BinaryData binaryData = null;
            var content = Content.Create(node);
            
            //try to find a field with this name
            if (content.Fields.ContainsKey(propertyName) && content.Fields[propertyName] is BinaryField)
                binaryData = content[propertyName] as BinaryData;

            //no field found, try a property
            if (binaryData == null)
            {
                var property = node.PropertyTypes[propertyName];
                if (property != null && property.DataType == DataType.Binary)
                    binaryData = node.GetBinary(property);
            }

            if (binaryData != null)
            {
                contentType = binaryData.ContentType;
                fileName = binaryData.FileName;

                return binaryData.GetStream();
            }

            contentType = string.Empty;
            fileName = string.Empty;

            return null;
        }
    }
}
