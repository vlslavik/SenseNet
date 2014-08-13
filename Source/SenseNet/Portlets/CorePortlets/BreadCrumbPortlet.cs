using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.UI;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.Portal.Virtualization;
using SNP = SenseNet.Portal;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.Portal.Portlets
{
    public enum StartBindTarget { Root, CurrentSite, CurrentWorkspace, CurrentList }

    public class BreadCrumbPortlet : ContextBoundPortlet
    {
        private const string BreadCrumbPortletClass = "BreadCrumbPortlet";

        // Constructor ////////////////////////////////////////////////////////////

        public BreadCrumbPortlet()
        {
            this.Name = "$BreadCrumbPortlet:PortletDisplayName";
            this.Description = "$BreadCrumbPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Navigation);

            //maybe Renderer property will be resurrected later when the portlet html will be customizable
            this.HiddenProperties.Add("Renderer");
        }

        // Members and properties /////////////////////////////////////////////////
        private string _separator = " / ";
        private string _currentSiteUrl;
        private string _linkCssClass = string.Empty;
        private string _itemCssClass = string.Empty;
        private string _separatorCssClass = string.Empty;
        private string _activeItemCssClass = string.Empty;
        private bool _showSite = false;
        private string _siteDisplayName = string.Empty;
        private List<Node> _pathNodeList;
        private int _actualNodeLevel = 0;

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_ItemCssClass_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_ItemCssClass_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(100)]
        public string ItemCssClass
        {
            get { return _itemCssClass; }
            set { _itemCssClass = value; }
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_LinkCssClass_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_LinkCssClass_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(110)]
        public string LinkCssClass
        {
            get { return _linkCssClass; }
            set { _linkCssClass = value; }
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_SeparatorCssClass_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_SeparatorCssClass_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(120)]
        public string SeparatorCssClass
        {
            get { return _separatorCssClass; }
            set { _separatorCssClass = value; }
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_Separator_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_Separator_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(130)]
        public string Separator
        {
            get { return _separator; }
            set { _separator = value; }
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_ActiveItemCssClass_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_ActiveItemCssClass_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(140)]
        public string ActiveItemCssClass
        {
            get { return _activeItemCssClass; }
            set { _activeItemCssClass = value; }
        }

        private StartBindTarget _startBindTarget = StartBindTarget.CurrentSite;
        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_StartBindTarget_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_StartBindTarget_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(150)]
        public StartBindTarget StartBindTarget
        {
            get { return _startBindTarget; }
            set { _startBindTarget = value; }
        }

        [WebBrowsable(false)]
        [Personalizable(true)]
        [Obsolete("Only for backward compatibility. Use ShowFirstElement instead.")]
        public bool ShowSite
        {
            get { return _showSite; }
            set { _showSite = value; }
        }

        [WebBrowsable(false)]
        [Personalizable(true)]
        [Obsolete("Only for backward compatibility. Use FirstDisplayName instead.")]
        public string SiteDisplayName
        {
            get { return _siteDisplayName; }
            set { _siteDisplayName = value; }
        }

        private bool? _showFirstElement;
        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_ShowFirstElement_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_ShowFirstElement_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(160)]
        public bool ShowFirstElement
        {
            get { return _showFirstElement.HasValue ? _showFirstElement.Value : _showSite; }
            set { _showFirstElement = value; }
        }

        private string _firstDisplayName;
        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(BreadCrumbPortletClass, "Prop_FirstDisplayName_DisplayName")]
        [LocalizedWebDescription(BreadCrumbPortletClass, "Prop_FirstDisplayName_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(170)]
        public string FirstDisplayName
        {
            get { return _firstDisplayName == null ? _siteDisplayName : _firstDisplayName; }
            set { _firstDisplayName = value; }
        }


        public string CurrentSiteUrl
        {
            get
            {
                if (String.IsNullOrEmpty(_currentSiteUrl))
                {
                    foreach (string urlItem in PortalContext.Current.Site.UrlList.Keys)
                    {
                        string requestUrl = string.Concat(HttpContext.Current.Request.Url.ToString(), "/");
                        if (requestUrl.IndexOf(string.Concat(urlItem, "/")) != -1)
                        {
                            _currentSiteUrl = urlItem;
                            break;
                        }
                    }
                }
                return _currentSiteUrl;
            }
        }

        // Events /////////////////////////////////////////////////////////////////
        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (ShowExecutionTime)
                Timer.Start();

            RenderContentsInternal(writer);

            if (ShowExecutionTime)
                Timer.Stop();

            base.RenderContents(writer);
        }

        // Internals //////////////////////////////////////////////////////////////
        private void RenderContentsInternal(HtmlTextWriter writer)
        {
            var actualNode = PortalContext.Current.ContextNode;

            _pathNodeList = new List<Node>();

            if (actualNode != null)
            {
                _pathNodeList.Add(actualNode);
                SetActualParent(actualNode, 0);
                RenderBreadCrumbItems(writer, _actualNodeLevel);
            }
            else if (this.RenderException != null)
            {
                writer.Write(String.Concat(String.Concat("Portlet Error: ", this.RenderException.Message), this.RenderException.InnerException == null ? string.Empty : this.RenderException.InnerException.Message));
            }
        }
        private void SetActualParent(Node actualNode, int index)
        {
            _actualNodeLevel = index;

            switch (StartBindTarget)
            {
                case Portlets.StartBindTarget.CurrentSite:
                    if (actualNode.NodeType.IsInstaceOfOrDerivedFrom("Site"))
                        return;
                    break;
                case Portlets.StartBindTarget.CurrentList:
                    if (PortalContext.Current.ContentList != null && actualNode.Path == PortalContext.Current.ContentList.Path)
                        return;
                    break;
                case Portlets.StartBindTarget.CurrentWorkspace:
                    if (PortalContext.Current.ContextWorkspace != null && actualNode.Path == PortalContext.Current.ContextWorkspace.Path)
                        return;
                    break;
                default:
                    break;
            }

            var parentHead = NodeHead.Get(actualNode.ParentId);
            var parent = parentHead == null
                             ? null
                             : SecurityHandler.HasPermission(parentHead, PermissionType.See, PermissionType.Open)
                                   ? actualNode.Parent
                                   : null;
            if (parent != null)
            {
                index++;
                _pathNodeList.Add(parent);
                SetActualParent(parent, index);
            }
            else return;
        }
        private void RenderBreadCrumbItems(HtmlTextWriter writer, int index)
        {
            bool first = true;
            for (int i = _actualNodeLevel; i >= 0; i--)
            {
                var currentNode = _pathNodeList[i];
                var currentContent = Content.Create(currentNode);

                string displayName;

                if (first)
                {
                    first = false;
                    if (ShowFirstElement)
                    {
                        displayName = !string.IsNullOrEmpty(FirstDisplayName) ? FirstDisplayName : currentContent.DisplayName;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    displayName = currentNode is GenericContent ? currentContent.DisplayName : currentNode.Name;
                }

                var pageHref = currentContent.Id == PortalContext.Current.Site.Id ? "/" : ProcessUrl(ReplaceSitePath(currentNode.Path));

                RenderBreadCrumbItems(writer, pageHref, displayName, true);

                if (i == 0)
                    continue;

                writer.Write(string.Format("<span class='{0}'>", SeparatorCssClass));
                writer.WriteEncodedText(Separator);
                writer.Write("</span>");
            }
        }
        private string ReplaceSitePath(string path)
        {
            return path.Replace(PortalContext.Current.Site.Path, CurrentSiteUrl);
        }
        private static string ProcessUrl(string url)
        {
            return url.Contains("/") ? url.Substring(url.IndexOf('/')) : url;
        }

        private void RenderBreadCrumbItems(HtmlTextWriter writer, string href, string menuText, bool renderLink)
        {
            var isEditor = PortalContext.Current.IsResourceEditorAllowed && SenseNetResourceManager.IsEditorMarkup(menuText);
            var text = UITools.GetSafeText(menuText);

            if (renderLink && !isEditor)
                writer.Write(string.Format("<a class=\"{0} {1}\" href=\"{2}\"><span>{3}</span></a>", ItemCssClass,
                                           LinkCssClass, href, text));
            else
                writer.Write(string.Format("<span class=\"{0} {1}\"><span>{2}</span></span>", ItemCssClass,
                                           ActiveItemCssClass, text));
        }
    }
}