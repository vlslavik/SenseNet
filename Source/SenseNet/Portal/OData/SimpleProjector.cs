﻿using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;

namespace SenseNet.Portal.OData
{
    internal class SimpleProjector : Projector
    {
        Dictionary<string, IEnumerable<string>> _fieldNamesForPaths = new Dictionary<string, IEnumerable<string>>();

        internal override void Initialize(Content container)
        {
            // do nothing
        }
        internal override Dictionary<string, object> Project(Content content)
        {
            var fields = new Dictionary<string, object>();
            var selfurl = GetSelfUrl(content);
            if (this.Request.EntityMetadata != MetadataFormat.None)
                fields.Add("__metadata", GetMetadata(content, selfurl, this.Request.EntityMetadata));

            IEnumerable<string> fieldNames = null;
            if (Request.HasSelect)
            {
                fieldNames = Request.Select;
            }
            else
            {
                if (IsCollectionItem)
                {
                    if (_fieldNamesForPaths.ContainsKey(content.ContentHandler.ParentPath))
                        fieldNames = _fieldNamesForPaths[content.ContentHandler.ParentPath];
                    else
                        _fieldNamesForPaths[content.ContentHandler.ParentPath] = fieldNames = content.GetFieldNamesInParentTable();

                    if (content.AspectFields != null && content.AspectFields.Count > 0)
                        fieldNames = fieldNames.Concat(content.AspectFields.Keys);
                }
                else
                {
                    fieldNames = content.Fields.Keys;
                }
            }

            if (Request.HasSelect)
            {
                foreach (var selectItem in Request.Select)
                    if (selectItem.Contains("/"))
                        throw new ODataException("Bad item in $select: " + selectItem, ODataExceptionCode.InvalidSelectParameter);
            }

            if (!Request.HasSelect)
                fieldNames = fieldNames.Concat(new[] { ACTIONSPROPERTY, ISFILEPROPERTY });

            foreach (var fieldName in fieldNames)
            {
                if (fields.ContainsKey(fieldName))
                    continue;

                if (ODataHandler.DisabledFieldNames.Contains(fieldName))
                {
                    fields.Add(fieldName, null);
                    continue;
                }

                if (base.IsAllowedField(content, fieldName))
                {
                    Field field;
                    if (content.Fields.TryGetValue(fieldName, out field))
                        fields.Add(fieldName, ODataFormatter.GetJsonObject(field, selfurl));
                    else if (fieldName == ACTIONSPROPERTY)
                        fields.Add(ACTIONSPROPERTY, ODataReference.Create(String.Concat(selfurl, "/", ODataHandler.PROPERTY_ACTIONS)));
                    else if (fieldName == ISFILEPROPERTY)
                        fields.Add(ISFILEPROPERTY, content.Fields.ContainsKey(ODataHandler.PROPERTY_BINARY));
                    else if (fieldName == ICONPROPERTY)
                        fields.Add(fieldName, content.Icon ?? content.ContentType.Icon);
                    else
                        fields.Add(fieldName, null);
                }
                else
                {
                    fields.Add(fieldName, null);
                }
            }
            return fields;
        }
    }
}
