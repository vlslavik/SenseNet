using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;
using SenseNet.Diagnostics;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.Portal.UI;


namespace SenseNet.Portal.Portlets
{
    /// <summary>
    /// Portlet for administrating tags.
    /// </summary>
    public class TagAdminPortlet : PortletBase
    {
        private const string TagAdminPortletClass = "TagAdminPortlet";

        /// <summary>
        /// Path of tags stored in Content Repository.
        /// </summary>
        private string tagPath;
        [WebBrowsable(true)]
        [Personalizable(true)]
        [WebCategory(EditorCategory.TagAdmin, EditorCategory.TagAdmin_Order)]
        [LocalizedWebDisplayName(TagAdminPortletClass, "Prop_Tags_DisplayName")]
        [LocalizedWebDescription(TagAdminPortletClass, "Prop_Tags_Description")]
        [WebOrder(10)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions()]
        public string Tags
        {
            get
            {
                return tagPath;
            }

            set
            {
                tagPath = value.TrimEnd('/');
            }
        }

        /// <summary>
        /// Path of content view with default value.
        /// </summary>
        private string contentViewPath = "/Root/System/SystemPlugins/Portlets/TagAdmin/TagAdminControl.ascx";

        /// <summary>
        /// Property for path of content view.
        /// </summary>
        /// Gets or sets path of content view.
        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(PORTLETFRAMEWORK_CLASSNAME, RENDERER_DISPLAYNAME)]
        [LocalizedWebDescription(PORTLETFRAMEWORK_CLASSNAME, RENDERER_DESCRIPTION)]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [Editor(typeof(ViewPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(ContentPickerCommonType.Ascx)]
        [WebOrder(100)]
        public string ContentViewPath
        {
            get { return contentViewPath; }
            set { contentViewPath = value; }
        }

        private string searchPaths = string.Empty;

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(TagAdminPortletClass, "Prop_SearchPaths_DisplayName")]
        [LocalizedWebDescription(TagAdminPortletClass, "Prop_SearchPaths_Description")]
        [WebCategory(EditorCategory.Search, EditorCategory.Search_Order)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(AllowedContentTypes = "Folder;SystemFolder")]
        [WebOrder(100)]
        public string SearchPaths
        {
            get { return searchPaths; }
            set { searchPaths = value; }
        }

        // portlet uses custom ascx, hide renderer property
        [WebBrowsable(false), Personalizable(true)]
        public override string Renderer { get; set; }

        /// <summary>
        /// Overridden method for creating controls.
        /// </summary>
        protected override void CreateChildControls()
        {
            Controls.Clear();
            CreateControls();
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TagAdminPortlet()
        {
            Name = "$TagAdminPortlet:PortletDisplayName";
            Description = "$TagAdminPortlet:PortletDescription";
            Category = new PortletCategory(PortletCategoryType.Application);
            Tags = "/Root/System/Tags";
            UITools.AddStyleSheetToHeader(UITools.GetHeader(), "$skin/styles/SN.Tagging.css");
        }

        /// <summary>
        /// Creates custom controls using the given content view.
        /// </summary>
        private void CreateControls()
        {
            try
            {
                var viewControl = Page.LoadControl(ContentViewPath) as Controls.TagAdminControl;
                if (viewControl != null)
                {
                    viewControl.TagPath = Tags;
                    viewControl.SearchPaths = SearchPaths.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    Controls.Add(viewControl);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
                Controls.Clear();
                Controls.Add(new LiteralControl("ContentView error: " + ex.Message));
            }
        }
    }
}
