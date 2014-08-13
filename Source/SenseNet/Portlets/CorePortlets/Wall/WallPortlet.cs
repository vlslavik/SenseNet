﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.UI.WebControls;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Portal.UI;
using SenseNet.Portal.UI.Controls;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.Portal.Wall;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls.WebParts;
using SenseNet.Diagnostics;
using Content = SenseNet.ContentRepository.Content;
using System.ComponentModel;

namespace SenseNet.Portal.Portlets.Wall
{
    public class WallPortlet : ContextBoundPortlet
    {
        private const string WallPortletClass = "WallPortlet";

        [DataContract]
        private class ContentTypeItem
        {
            [DataMember]
            public string label { get; set; }
            [DataMember]
            public string value { get; set; }
        }

        // ================================================================================================ Properties

        public int _pageSize = 20;
        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(WallPortletClass, "Prop_PageSize_DisplayName")]
        [LocalizedWebDescription(WallPortletClass, "Prop_PageSize_Description")]
        [WebCategory("Wall", 60)]
        [WebOrder(100)]
        public int PageSize { get { return _pageSize; } set { _pageSize = value; } }

        private string _controlPath = "/Root/Global/renderers/Wall/Wall.ascx";
        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(WallPortletClass, "Prop_ControlPath_DisplayName")]
        [LocalizedWebDescription(WallPortletClass, "Prop_ControlPath_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(100)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(ContentPickerCommonType.Ascx)]
        public string ControlPath 
        { 
            get { return _controlPath; } 
            set { _controlPath = value; } 
        }


        // ================================================================================================ Members

        private PlaceHolder _workspaceIsWallContainer;

        // ================================================================================================ Constructor

        public WallPortlet()
        {
            this.Name = "$WallPortlet:PortletDisplayName";
            this.Description = "$WallPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Enterprise20);

            this.HiddenProperties.Add("Renderer");
        }

        // ================================================================================================ Methods

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            
            if (this.ContextNode == null)
                return;

            if (ShowExecutionTime)
                Timer.Start();

            UITools.AddScript(UITools.ClientScriptConfigurations.SNWallPath);
            
            UITools.AddPickerCss();
            UITools.AddScript(UITools.ClientScriptConfigurations.SNPickerPath);

            // get items for content types drowpdown in dropbox
            var gc = new GenericContent(this.ContextNode, "Folder");
            var newItems = GenericScenario.GetNewItemNodes(gc, ContentType.GetContentTypes());

            string jsonData;

            using (var s = new MemoryStream())
            {
                var workData = newItems.Select(n => new ContentTypeItem { value = n.Name, label = Content.Create(n).DisplayName }).OrderBy(n => n.label);
                var serializer = new DataContractJsonSerializer(typeof(ContentTypeItem[]));
                serializer.WriteObject(s, workData.ToArray());
                s.Flush();
                s.Position = 0;
                using (var sr = new StreamReader(s))
                {
                    jsonData = sr.ReadToEnd();
                }
            }

            UITools.RegisterStartupScript("initdropboxautocomplete", string.Format("SN.Wall.initDropBox({0})", jsonData), this.Page);

            if (ShowExecutionTime)
                Timer.Stop();
        }

        protected override void CreateChildControls()
        {
            if (this.ContextNode == null)
                return;

            if (ShowExecutionTime)
                Timer.Start();

            System.Web.UI.Control control = null;
            try
            {
                var viewHead = NodeHead.Get(ControlPath);
                if (viewHead != null && SecurityHandler.HasPermission(viewHead, PermissionType.RunApplication))
                {
                    control = Page.LoadControl(ControlPath);
                    this.Controls.Add(control);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
                this.Controls.Add(new System.Web.UI.LiteralControl(ex.Message));
                return;
            }

            if (control == null)
                return;

            _workspaceIsWallContainer = control.FindControlRecursive("WorkspaceIsWallContainer") as PlaceHolder;
            var portletContextNodeLink = control.FindControlRecursive("PortletContextNodeLink") as System.Web.UI.WebControls.HyperLink;
            var configureWorkspaceWall = control.FindControlRecursive("ConfigureWorkspaceWall") as Button;
            if (_workspaceIsWallContainer != null && configureWorkspaceWall != null)
            {
                _workspaceIsWallContainer.Visible = false;

                var ws = this.ContextNode as Workspace;
                if (ws != null && !ws.IsWallContainer && ws.Security.HasPermission(PermissionType.Save))
                {
                    _workspaceIsWallContainer.Visible = true;
                    if (portletContextNodeLink != null)
                    {
                        portletContextNodeLink.Text = System.Web.HttpUtility.HtmlEncode(Content.Create(ws).DisplayName);
                        portletContextNodeLink.NavigateUrl = ws.Path;
                    }

                    configureWorkspaceWall.Click += ConfigureWorkspaceWall_Click;
                }
            }

            var postsPlaceholder = control.FindControlRecursive("Posts");
            
            List<PostInfo> posts;
            using (new OperationTrace("Wall - Gather posts"))
            {
                var postInfos = GatherPosts();
                posts = postInfos == null ? new List<PostInfo>() : postInfos.Take(PageSize).ToList();
            }
            using (new OperationTrace("Wall - Posts markup"))
            {
                postsPlaceholder.Controls.Add(new Literal { Text = WallHelper.GetWallPostsMarkup(this.ContextNode.Path, posts) });
            }

            if (ShowExecutionTime)
                Timer.Stop();

            base.CreateChildControls();
            this.ChildControlsCreated = true;
        }

        protected virtual IEnumerable<PostInfo> GatherPosts()
        {
            return DataLayer.GetPostsForWorkspace(this.ContextNode.Path);
        }
        
        // ================================================================================================ Event handlers

        protected void ConfigureWorkspaceWall_Click(object sender, EventArgs e)
        {
            var ws = this.ContextNode as Workspace;

            if (ws != null)
            {
                ws.IsWallContainer = true;
                ws.Save();
            }

            if (_workspaceIsWallContainer != null)
                _workspaceIsWallContainer.Visible = false;
        }
    }
}
