using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI.WebControls.WebParts;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.UI.PortletFramework;

namespace SenseNet.Portal.Portlets
{
    public class WorkspaceAggregatedKPIPortlet : ContentCollectionPortlet
    {
        private const string WorkspaceAggregatedKPIPortletClass = "WorkspaceAggregatedKPIPortlet";

        private const string KPIViewPath = "/Root/Global/renderers/KPI/WorkspaceAggregatedKPI";

        //====================================================================== Constructor

        public WorkspaceAggregatedKPIPortlet()
        {
            Name = "$WorkspaceAggregatedKPIPortlet:PortletDisplayName";
            Description = "$WorkspaceAggregatedKPIPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.KPI);

            this.HiddenPropertyCategories = new List<string>() { EditorCategory.Collection, EditorCategory.ContextBinding };
            this.HiddenProperties.AddRange(new[] { "SkinPreFix", "Renderer" });
        }

        //====================================================================== Properties


        private string _viewName;

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(WorkspaceAggregatedKPIPortletClass, "Prop_ViewName_DisplayName")]
        [LocalizedWebDescription(WorkspaceAggregatedKPIPortletClass, "Prop_ViewName_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(100)]
        [Editor(typeof(DropDownPartField), typeof(IEditorPartField))]
        [DropDownPartOptions("InFolder:\"" + KPIViewPath + "\"")]
        public string ViewName
        {
            get { return _viewName; }
            set
            {
                _viewName = value;

                this.Renderer = RepositoryPath.Combine(KPIViewPath, _viewName ?? string.Empty);
            }
        }

        //====================================================================== Model

        protected override object GetModel()
        {
            var contextNode = GetContextNode();
            if (contextNode == null)
                return null;

            var smartFolder = SmartFolder.GetRuntimeQueryFolder();
            smartFolder.Query = string.Format("+InTree:\"{0}\" +TypeIs:Workspace -TypeIs:(Blog Wiki Site)", contextNode.Path);

            var content = Content.Create(smartFolder);

            //Get base model as Content and use some of its children definition properties.
            //Do not override the whole ChildrenDefinition object here because SmartFolder 
            //has its own special children definition override.
            var oldc = base.GetModel() as Content;
            if (oldc != null)
            {
                content.ChildrenDefinition.EnableAutofilters = oldc.ChildrenDefinition.EnableAutofilters;
                content.ChildrenDefinition.EnableLifespanFilter = oldc.ChildrenDefinition.EnableLifespanFilter;
                content.ChildrenDefinition.Skip = oldc.ChildrenDefinition.Skip;
                content.ChildrenDefinition.Sort = oldc.ChildrenDefinition.Sort;
                content.ChildrenDefinition.Top = oldc.ChildrenDefinition.Top;
            }

            return content;
        }
    }
}
