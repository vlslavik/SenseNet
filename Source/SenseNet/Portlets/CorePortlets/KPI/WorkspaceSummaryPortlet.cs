using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls.WebParts;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.UI.PortletFramework;

namespace SenseNet.Portal.Portlets
{
    public class WorkspaceSummaryPortlet : ContentCollectionPortlet
    {
        private const string WorkspaceSummaryPortletClass = "WorkspaceSummaryPortlet";

        private const string KPIViewPath = "/Root/Global/renderers/KPI/WorkspaceSummary";

        //====================================================================== Constructor

        public WorkspaceSummaryPortlet()
        {
            Name = "$WorkspaceSummaryPortlet:PortletDisplayName";
            Description = "$WorkspaceSummaryPortlet:PortletDescription";
            Category = new PortletCategory(PortletCategoryType.KPI);

            this.HiddenPropertyCategories = new List<string>() { EditorCategory.Collection, EditorCategory.ContextBinding };
            this.HiddenProperties.AddRange(new[] { "SkinPreFix", "Renderer" });
        }

        //====================================================================== Properties

        private int _daysMediumWarning = 5;

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(WorkspaceSummaryPortletClass, "Prop_DaysMediumWarning_DisplayName")]
        [LocalizedWebDescription(WorkspaceSummaryPortletClass, "Prop_DaysMediumWarning_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(130)]
        public int DaysMediumWarning
        {
            get { return _daysMediumWarning; }
            set { _daysMediumWarning = value; }
        }

        private int _daysHighWarning = 20;

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(WorkspaceSummaryPortletClass, "Prop_DaysHighWarning_DisplayName")]
        [LocalizedWebDescription(WorkspaceSummaryPortletClass, "Prop_DaysHighWarning_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(140)]
        public int DaysHighWarning
        {
            get { return _daysHighWarning; }
            set { _daysHighWarning = value; }
        }

        private string _viewName;

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(WorkspaceSummaryPortletClass, "Prop_ViewName_DisplayName")]
        [LocalizedWebDescription(WorkspaceSummaryPortletClass, "Prop_ViewName_Description")]
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
            smartFolder.Query = string.Format("+InTree:\"{0}\" +TypeIs:Workspace -TypeIs:(Blog Wiki)", contextNode.Path);

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
