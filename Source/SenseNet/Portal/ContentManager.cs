﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage;
using System.Web;
using SenseNet.ContentRepository.Storage.AppModel;

namespace SenseNet.Portal
{
    public static class ContentManager
    {
        internal static Content CreateContentFromRequest()
        {
            return CreateContentFromRequest(null, null, null, false);
        }


        public static Content CreateContentFromRequest(string contentTypeName, string contentName, string parentPath, bool templated)
        {
            // TODO: templated parameter has become unused.

            Node parentNode;

            if (String.IsNullOrEmpty(contentTypeName ?? (contentTypeName = GetRequestParameter("ContentTypeName"))))
                return null;

            if (String.IsNullOrEmpty(parentPath ?? (parentPath = GetRequestParameter("ParentPath"))))
                parentNode = PortalContext.Current.ContextNode;
            else
                parentNode = Node.LoadNode(parentPath);

            if (parentNode == null)
                throw new ApplicationException("Cannot create a new Content: invalid parent");

            if (String.IsNullOrEmpty(contentName ?? (contentName = GetRequestParameter("ContentName"))))
                contentName = contentTypeName;

            var fieldData = RecognizeFieldParameters(contentTypeName);

            var contentType = ContentType.GetByName(contentTypeName);
            if (contentType == null) 
            {
                //full template path is given as content type name
                if (fieldData.Count == 0)
                {
                    return ContentTemplate.CreateTemplated(parentNode.Path, contentTypeName);
                }
                else
                {
                    var template = Node.LoadNode(contentTypeName);
                    if (template != null)
                        return ContentTemplate.CreateTemplatedAndParse(parentNode, template, template.Name, fieldData);
                }
            }

            return Content.CreateNewAndParse(contentTypeName, parentNode, contentName, fieldData);
        }

        public static string GetDisplayNameFromContentTypeOrTemplate(string templatePathOrContentTypeName)
        {
            if (string.IsNullOrEmpty(templatePathOrContentTypeName))
                return string.Empty;

            if (templatePathOrContentTypeName.StartsWith("/Root"))
            {
                //full content template path
                var template = Content.Load(templatePathOrContentTypeName);
                if (template != null)
                    return template.DisplayName;
            }
            else
            {
                //simple content type name
                var ct = ContentType.GetByName(templatePathOrContentTypeName);
                if (ct != null)
                    return SNSR.GetString(ct.DisplayName);
            }

            return string.Empty;
        }

        private static Dictionary<string, string> RecognizeFieldParameters(string contentTypeName)
        {
            var fieldData = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(contentTypeName))
                return fieldData;

            ContentType contentType = null;

            if (contentTypeName.StartsWith("/Root/"))
            {
                //templated creation
                var template = Node.LoadNode(contentTypeName);
                if (template != null)
                    contentType = ContentType.GetByName(template.NodeType.Name);
            }
            else
            {
                contentType = ContentType.GetByName(contentTypeName);
            }

            if (contentType == null)
                return fieldData;

            foreach (var fieldSetting in contentType.FieldSettings)
            {
                var value = GetRequestParameter(fieldSetting.Name);
                if (value != null)
                    fieldData.Add(fieldSetting.Name, value);
            }
            return fieldData;
        }
        private static string GetRequestParameter(string paramName)
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null)
                return null;

            return HttpUtility.UrlDecode(HttpContext.Current.Request.Params[paramName]);
        }

        internal static void ModifyContentFromRequest(Content content)
        {
            var contentTypeName = content.ContentType.Name;
            var fieldData = RecognizeFieldParameters(contentTypeName);
            Content.Modify(content, fieldData);
        }
    }
}
