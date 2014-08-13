using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Portal.UI.PortletFramework;
using System.Web.UI.WebControls.WebParts;

namespace SenseNet.Portal.Portlets
{
    public class DialogFileUploadPortlet : ContextBoundPortlet
    {
        private const string DialogFileUploadPortletClass = "DialogFileUploadPortlet";

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(DialogFileUploadPortletClass, "Prop_AllowedContentTypes_DisplayName")]
        [LocalizedWebDescription(DialogFileUploadPortletClass, "Prop_AllowedContentTypes_Description")]
        [WebCategory("Dialog File Upload", 100)]
        [WebOrder(100)]
        public string AllowedContentTypes { get; set; }

        public DialogFileUploadPortlet()
        {
            this.Name = "$DialogFileUploadPortlet:PortletDisplayName";
            this.Description = "$DialogFileUploadPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Application);
        }

        protected override void OnInit(EventArgs e)
        {
            var control = this.Page.LoadControl("/Root/System/SystemPlugins/Controls/DialogFileUpload.ascx") as SenseNet.Portal.UI.Controls.DialogFileUpload;
            control.AllowedContentTypes = this.AllowedContentTypes;
            this.Controls.Add(control);
            base.OnInit(e);
        }
    }
}
