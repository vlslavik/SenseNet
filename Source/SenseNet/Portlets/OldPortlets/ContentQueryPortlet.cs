using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.UI.Controls;
using SenseNet.Portal.UI.PortletFramework;
using System.Linq;
using SenseNet.Diagnostics;
using SenseNet.Search;

namespace SenseNet.Portal.Portlets
{
    public class ContentQueryPortlet : CacheablePortlet
    {
        private const string ContentQueryPortletClass = "ContentQueryPortlet";

        //-- Variables ----------------------------------------------------

        string _queryString = string.Empty;
        string _queryString2 = string.Empty;
        string _queryString3 = string.Empty;
        string _cvPath = string.Empty;
        int _pageSize;
        int _currentPage = 1;

        //-- Properties ---------------------------------------------------

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_QueryString_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_QueryString_Description")]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(10)]
        [Editor(typeof(TextEditorPartField), typeof(IEditorPartField))]
        [TextEditorPartOptions(TextEditorCommonType.MultiLine)]
        public string QueryString
        {
            get { return _queryString; }
            set { _queryString = value; }
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_QueryString2_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_QueryString2_Description")]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(20)]
        [Editor(typeof(TextEditorPartField), typeof(IEditorPartField))]
        [TextEditorPartOptions(TextEditorCommonType.MultiLine)]
        public string QueryString2
        {
            get { return _queryString2; }
            set { _queryString2 = value; }
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_QueryString3_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_QueryString3_Description")]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(30)]
        [Editor(typeof(TextEditorPartField), typeof(IEditorPartField))]
        [TextEditorPartOptions(TextEditorCommonType.MultiLine)]
        public string QueryString3
        {
            get { return _queryString3; }
            set { _queryString3 = value; }
        }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_UrlParamPreFix_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_UrlParamPreFix_Description")]
        [WebBrowsable(true)]
        [Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(40)]
        public string UrlParamPreFix { get; set; }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_IsSystemAccount_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_IsSystemAccount_Description")]
        [WebBrowsable(true)]
        [Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(50)]
        public bool IsSystemAccount { get; set; }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_EnablePaging_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_EnablePaging_Description")]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(60)]
        public bool EnablePaging
        {
            get;
            set;
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_EnableSettingPageSize_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_EnableSettingPageSize_Description")]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(70)]
        public bool EnableSettingPageSize
        {
            get;
            set;
        }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_DefaultPageSize_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_DefaultPageSize_Description")]
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(80)]
        public int DefaultPageSize
        {
            get;
            set;
        }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_Top_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_Top_Description")]
        [WebBrowsable(true)]
        [Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(90)]
        public int Top { get; set; }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_SkipFirst_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_SkipFirst_Description")]
        [WebBrowsable(true)]
        [Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(100)]
        public int SkipFirst { get; set; }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_EnableAutofilters_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_EnableAutofilters_Description")]
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(180)]
        public FilterStatus EnableAutofilters
        {
            get;
            set;
        }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_EnableLifespanFilter_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_EnableLifespanFilter_Description")]
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory(EditorCategory.Query, EditorCategory.Query_Order)]
        [WebOrder(190)]
        public FilterStatus EnableLifespanFilter
        {
            get;
            set;
        }

        [LocalizedWebDisplayName(ContentQueryPortletClass, "Prop_CvPath_DisplayName")]
        [LocalizedWebDescription(ContentQueryPortletClass, "Prop_CvPath_Description")]
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(100)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(ContentPickerCommonType.Ascx)]
        public string CvPath
        {
            get { return _cvPath; }
            set { _cvPath = value; }
        }

        private int CurrentPage
        {
            get { return Math.Max(1, _currentPage); }
            set { _currentPage = Math.Max(1, value); }
        }

        private int PageSize
        {
            get
            {
                return _pageSize == 0 || !this.EnableSettingPageSize ? this.DefaultPageSize : _pageSize;
            }
            set
            {
                _pageSize = value;
            }
        }

        private string CQPQueryNumber
        {
            get
            {
                var number = HttpContext.Current.Request.Params[string.Concat(UrlParamPreFix, "CQPQueryNumber")];
                return string.IsNullOrEmpty(number) ? "1" : number;
            }
        }

        private int CQPStartIndex
        {
            get
            {
                var startIndex = 0;
                int.TryParse(HttpContext.Current.Request.Params[string.Concat(UrlParamPreFix, "CQPStartIndex")], out startIndex);
                return startIndex;
            }
        }

        private int CQPPageSize
        {
            get
            {
                var pageSize = 0;
                int.TryParse(HttpContext.Current.Request.Params[string.Concat(UrlParamPreFix, "CQPPageSize")], out pageSize);
                return pageSize;
            }
        }

        private bool CQPParamsExist
        {
            get { return this.CQPStartIndex > 0 && this.CQPPageSize > 0; }
        }

        //-- Constructor --------------------------------------------------

        public ContentQueryPortlet()
        {
            this.Name = "$ContentQueryPortlet:PortletDisplayName";
            this.Description = "$ContentQueryPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Search);
        }

        //-- Methods ------------------------------------------------------

        protected override void CreateChildControls()
        {

            if (CanCache && Cacheable && IsInCache)
                return;

            using (var traceOperation = SenseNet.Diagnostics.Logger.TraceOperation("ContentQueryPortlet.CreateChildControls"))
            {
                Controls.Clear();

                try
                {
                    CreateControls();
                }
                catch (Exception ex) //logged
                {
                    Logger.WriteException(ex);
                    Controls.Add(new LiteralControl(string.Concat(ex.Message, "<br />", ex.StackTrace)));
                }

                ChildControlsCreated = true;
                traceOperation.IsSuccessful = true;
            }
        }

        private void CreateControls()
        {
            this.Controls.Clear();

            if (string.IsNullOrEmpty(CvPath))
                return;

            try
            {
                var qv = Page.LoadControl(CvPath) as QueryView;
                if (qv == null)
                    return;

                var contentQuery = GetQuery();
                if (contentQuery == null)
                    return;

                if (IsSystemAccount)
                    AccessProvider.ChangeToSystemAccount();

                //get full result list, without loading the nodes
                var result = contentQuery.Execute();
                var fullCount = result.Count;

                if (EnablePaging)
                {
                    //Get results for current page only.
                    //Merge two mechanisms: url params and paging
                    if (this.CQPParamsExist)
                    {
                        contentQuery.Settings.Skip = (this.CurrentPage - 1) * this.PageSize + this.SkipFirst +
                            this.CQPStartIndex - 1;

                        contentQuery.Settings.Top = this.CQPPageSize;
                    }
                    else
                    {
                        contentQuery.Settings.Skip = (this.CurrentPage - 1) * this.PageSize + this.SkipFirst;
                        contentQuery.Settings.Top = this.PageSize > 0 ? this.PageSize : NodeQuery.UnlimitedPageSize - 1;
                    }

                    result = contentQuery.Execute();
                }

                //refresh pager controls
                foreach (var pc in qv.PagerControls)
                {
                    if (EnablePaging)
                    {
                        pc.ResultCount = fullCount;
                        pc.PageSize = contentQuery.Settings.Top;
                        pc.CurrentPage = this.CurrentPage;
                        pc.EnableSettingPageSize = this.EnableSettingPageSize;
                        pc.OnPageSelected += PagerControl_OnPageSelected;
                        pc.OnPageSizeChanged += PagerControl_OnPageSizeChanged;
                    }
                    else
                    {
                        pc.Visible = false;
                    }
                }

                if (IsSystemAccount)
                    AccessProvider.RestoreOriginalUser();

                qv.ID = "QueryView";
                //qv.NodeItemList = result.CurrentPage.ToList();
                qv.NodeItemList = result.Nodes.ToList();

                this.Controls.Add(qv);
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);

                this.Controls.Add(new LiteralControl(ex.ToString()));
            }
        }

        protected void PagerControl_OnPageSizeChanged(object sender, EventArgs e)
        {
            var pager = sender as PagerControl;

            if (pager == null ||
                //this.PageSize == pager.PageSize || 
                !this.EnablePaging)
                return;

            //need to get _all_ of the dinamic properties to
            //refresh portlet property values!
            this.PageSize = pager.PageSize;
            this.CurrentPage = pager.CurrentPage;

            CreateControls();
        }

        protected void PagerControl_OnPageSelected(object sender, EventArgs e)
        {
            var pager = sender as PagerControl;

            if (pager == null ||
                //this.CurrentPage == pager.CurrentPage || 
                !this.EnablePaging)
                return;

            //need to get _all_ of the dinamic properties to
            //refresh portlet property values!
            this.PageSize = pager.PageSize;
            this.CurrentPage = pager.CurrentPage;

            CreateControls();
        }

        private ContentQuery GetQuery()
        {
            var queryString = GetQueryString();

            if (string.IsNullOrEmpty(queryString))
                return null;

            try
            {
                var query = ContentQuery.CreateQuery(queryString);

                if (EnableAutofilters != FilterStatus.Default)
                    query.Settings.EnableAutofilters = EnableAutofilters;
                if (EnableLifespanFilter != FilterStatus.Default)
                    query.Settings.EnableLifespanFilter = EnableLifespanFilter;

                if (this.CQPParamsExist)
                {
                    query.Settings.Skip = CQPStartIndex + this.SkipFirst;
                    query.Settings.Top = NodeQuery.UnlimitedPageSize - 1;
                    //CQPPageSize;
                }
                else if (this.SkipFirst > 0)
                {
                    query.Settings.Skip = this.SkipFirst;
                    query.Settings.Top = NodeQuery.UnlimitedPageSize - 1;
                }

                if (Top > 0)
                {
                    query.Settings.Top = Top;
                }

                return query;
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
                return null;
            }
        }

        private string GetQueryString()
        {
            switch (CQPQueryNumber)
            {
                case "1": return QueryString;
                case "2": return QueryString2;
                case "3": return QueryString3;
            }
            return string.Empty;
        }
    }
}