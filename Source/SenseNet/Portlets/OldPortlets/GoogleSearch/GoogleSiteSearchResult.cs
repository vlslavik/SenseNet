using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Portal.UI.PortletFramework;
using System.Xml;
using System.Web.UI.WebControls.WebParts;
using System.Web;
using System.Net;
using System.Xml.Xsl;

namespace SenseNet.Portal.Portlets.GoogleSearch
{
    /// <summary>
    /// This portlet requests a google site search service through http webrequest using
    /// the given portlet properties (http://www.google.com/sitesearch/)
    /// The portlet should be used with XSLT rendering mode, since the XML returned by the
    /// service is passed on by the portlet.
    /// </summary>
    public class GoogleSiteSearchResult : PortletBase
    {
        private const string GoogleSiteSearchResultClass = "GoogleSiteSearchResult";

        public GoogleSiteSearchResult()
        {
            this.Name = "$GoogleSiteSearchResult:PortletDisplayName";
            this.Description = "$GoogleSiteSearchResult:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Application);
        }
        string _params = "num=10&lr=lang_hu&cx=";
        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(GoogleSiteSearchResultClass, "Prop_Params_DisplayName")]
        [LocalizedWebDescription(GoogleSiteSearchResultClass, "Prop_Params_Description")]
        [WebCategory(EditorCategory.GoogleSearch, EditorCategory.GoogleSearch_Order)]
        public string Params
        {
            get { return _params; }
            set { _params = value; }
        }

        public string StartIdx
        {
            get
            {
                string startIdx = HttpContext.Current.Request.QueryString["start"];
                if (startIdx == null)
                    return "0";
                return startIdx;
            }
        }

        public string SearchText
        {
            get
            {
                string searchText = HttpContext.Current.Request.QueryString["search"];
                if (searchText == null)
                    return string.Empty;
                return searchText;
            }
        }

        protected override XsltArgumentList GetXsltArgumentList()
        {
            XsltArgumentList list = new XsltArgumentList();
            list.AddExtensionObject("sn://SenseNet.ContentRepository.i18n.ResourceXsltClient", new ResourceXsltClient());
            return list;
        }

        protected override object GetModel()
        {
            // setup request url
            string requestUrl = string.Format(
                "http://www.google.com/search?client=google-csbe&output=xml_no_dtd&q={0}&start={1}&{2}",
                SearchText, StartIdx, _params);

            // get googlesearch XML
            var request = WebRequest.Create(requestUrl);
            var response = request.GetResponse();

            var doc = new XmlDocument();
            doc.Load(response.GetResponseStream());

            response.Close();

            return doc;
        }
    }
}
