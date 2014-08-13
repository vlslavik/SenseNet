using System;
using System.Web;
using System.Web.UI;
using SenseNet.Portal.Virtualization;
using SenseNet.Services.Instrumentation;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.UI.Controls
{
    public class BaseTag : Control
    {
        /// <summary>
        /// Whether the url should end with a slash (e.g. 'http://example.com/mycontent/'). Override it to implement custom logic based on the current environment.
        /// </summary>
        public virtual bool AppendTrailingSlash { get; set; }

        protected override void Render(HtmlTextWriter writer)
        {
            var headControl = Page.Header;
            if (headControl == null)
            {
                base.Render(writer);
                return;
            }
            try
            {
                var hrefServerPart = VirtualPathUtility.AppendTrailingSlash(PortalContext.Current.SiteUrl);
                var hrefPathPart = PortalContext.Current.RequestedUri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
                var hrefString = string.Concat(hrefServerPart, hrefPathPart);

                //note that if the original URI already contains a trailing slash, we do not remove it
                if (AppendTrailingSlash)
                    hrefString = VirtualPathUtility.AppendTrailingSlash(hrefString);

                var baseTag = new LiteralControl
                {
                    ID = "baseTag",
                    Text = String.Format("<base href=\"//{0}\" />", hrefString)
                };

                baseTag.RenderControl(writer);

            }
            catch (Exception exc) //logged
            {
                Logger.WriteException(exc);
            }

        }

    }
}
