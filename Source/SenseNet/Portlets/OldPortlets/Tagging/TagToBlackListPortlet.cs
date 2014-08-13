﻿using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.Portal.UI.PortletFramework;

namespace SenseNet.Portal.Portlets
{
    public class TagToBlackListPortlet : ContentEditorPortlet
    {
        public TagToBlackListPortlet()
        {
            Name = "$TagToBlackListPortlet:PortletDisplayName";
            Description = "$TagToBlackListPortlet:PortletDescription";
            Category = new PortletCategory(PortletCategoryType.Application);
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var contextNode = GetContextNode();
            if (contextNode == null)
            {
                CallDone();
                return;
            }

            var tmp = Content.Load(contextNode.Id);
            var pathListParam = Page.Request.Params["Paths"];

            if (string.IsNullOrEmpty(pathListParam)) 
                return;

            var pathList = pathListParam.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();

            switch (Page.Request.QueryString["Do"])
            {
                case "Add":
                    //tmp["IsBlacklisted"] = true;
                    TagManager.ManageBlacklist(true, contextNode.Id, pathList);
                    TagManager.ReplaceTag(tmp.DisplayName, String.Empty, pathList);
                    break;
                case "Remove":
                    //tmp["IsBlacklisted"] = false;
                    TagManager.ManageBlacklist(false, contextNode.Id, pathList);
                    break;
            }

            tmp.Save();

            CallDone();
        }

        protected override void CreateChildControls()
        {
            Controls.Clear();
        }
    }
}

