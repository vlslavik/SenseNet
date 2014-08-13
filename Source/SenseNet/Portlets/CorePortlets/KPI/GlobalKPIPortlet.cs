using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.UI.PortletFramework;
using System.Web.UI.WebControls.WebParts;
using System.ComponentModel;
using System.Collections.Generic;
using System.Web.UI;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Workspaces;
using System.Web.UI.WebControls;
using SenseNet.Portal.UI;
using System;
using SenseNet.Search;
using System.Text;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.Portlets
{
    public class GlobalKPIPortlet : ContextBoundPortlet
    {
        private const string GlobalKPIPortletClass = "GlobalKPIPortlet";

        /* ====================================================================================================== Constants */
        private const string kpiSourcePath = "/Root/KPI";
        private const string masterDropdownCss = "sn-kpiViewMaster";
        private const string slaveDropdownCss = "sn-kpiViewSlave";


        /* ====================================================================================================== Properties */
        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(GlobalKPIPortletClass, "Prop_KPIDataSource_DisplayName")]
        [LocalizedWebDescription(GlobalKPIPortletClass, "Prop_KPIDataSource_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(100)]
        [Editor(typeof(DropDownPartField), typeof(IEditorPartField))]
        [DropDownPartOptions("InFolder:\"" + kpiSourcePath + "\"", masterDropdownCss)]
        public string KPIDataSource { get; set; }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(GlobalKPIPortletClass, "Prop_KPIViewName_DisplayName")]
        [LocalizedWebDescription(GlobalKPIPortletClass, "Prop_KPIViewName_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(110)]
        //[Editor(typeof(DropDownPartField), typeof(IEditorPartField))]
        [Editor(typeof(KPIViewDropDownPartField), typeof(IEditorPartField))]
        [DropDownPartOptions(null, slaveDropdownCss)]
        public string KPIViewName { get; set; }



        /* ====================================================================================================== Constructor */
        public GlobalKPIPortlet()
        {
            this.Name = "$GlobalKPIPortlet:PortletDisplayName";
            this.Description = "$GlobalKPIPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.KPI);

            this.HiddenProperties.AddRange(new [] { "SkinPreFix", "Renderer" });
            this.HiddenPropertyCategories = new List<string>() {"Context binding"};
        }


        /* ====================================================================================================== Methods */
        protected override void OnInit(EventArgs e)
        {
            if (ShowExecutionTime)
                Timer.Start();

            UITools.AddScript("$skin/scripts/sn/SN.KPIViewDropDown.js");

            // setup views list
            // the source list is built up from a query
            var sortinfo = new List<SortInfo>() { new SortInfo() { FieldName = "Name", Reverse = false } };
            var settings = new QuerySettings() { EnableAutofilters = FilterStatus.Disabled, Sort = sortinfo };
            var query = ContentQuery.CreateQuery("InFolder:@0", settings, kpiSourcePath);
            var result = query.Execute();
            var viewList = new StringBuilder();

            // collect kpi views from under sources
            foreach (Node node in result.Nodes)
            {
                var c = SenseNet.ContentRepository.Content.Create(node);
                c.ChildrenDefinition.EnableAutofilters = FilterStatus.Disabled;
                c.ChildrenDefinition.Sort = sortinfo;
                foreach (var child in c.Children)
                {
                    viewList.Append(string.Concat("{ sourceName: '",node.Name,"', viewName: '",child.Name,"'},"));
                }
            }

            var viewListStr = string.Concat('[',viewList.ToString().TrimEnd(','),']');


            string script = string.Format("SN.KPIViewDropDown.init('{0}','{1}',{2});", masterDropdownCss, slaveDropdownCss, viewListStr);
            UITools.RegisterStartupScript("KPIViewDropDownScript", script, this.Page);

            if (ShowExecutionTime)
                Timer.Stop();

            base.OnInit(e);
        }
        protected override void CreateChildControls()
        {
            if (ShowExecutionTime)
                Timer.Start();

            // load view
            UserControl view = null;
            if (!string.IsNullOrEmpty(this.KPIViewName))
            {
                var sourcePath = RepositoryPath.Combine(kpiSourcePath, this.KPIDataSource);
                try
                {
                    view = Page.LoadControl(RepositoryPath.Combine(sourcePath, this.KPIViewName)) as UserControl;
                } 
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    this.Controls.Add(new Label() { Text = "An error occurred while trying to load KPI view" });
                }
            }

            if (view != null)
                this.Controls.Add(view);
            else
                this.Controls.Add(new Label() { Text = "No KPI view is loaded" });


            this.ChildControlsCreated = true;

            if (ShowExecutionTime)
                Timer.Stop();
        }
        protected override Node GetContextNode()
        {
            if (!string.IsNullOrEmpty(this.KPIDataSource))
            {
                var sourcePath = RepositoryPath.Combine(kpiSourcePath, this.KPIDataSource);
                var sourceHead = NodeHead.Get(sourcePath);
                
                return !SecurityHandler.HasPermission(sourceHead, PermissionType.See) 
                    ? null
                    : Node.LoadNode(sourceHead);
            }
            return null;
        }
    }
}
