using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.ContentRepository;
using SenseNet.Portal.Workspaces;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.PortletFramework;
using System.Web.UI.WebControls.WebParts;
using SenseNet.Search;

namespace SenseNet.Portal.Portlets
{
    public class JournalPortlet : ContextBoundPortlet
    {
        private const string JournalPortletClass = "JournalPortlet";

        [WebBrowsable(true)]
        [Personalizable(true)]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [LocalizedWebDisplayName(JournalPortletClass, "Prop_MaxItems_DisplayName")]
        [LocalizedWebDescription(JournalPortletClass, "Prop_MaxItems_Description")]
        [WebOrder(100)]
        public int MaxItems { get; set; }

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(JournalPortletClass, "Prop_ShowSystemContent_DisplayName")]
        [LocalizedWebDescription(JournalPortletClass, "Prop_ShowSystemContent_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(110)]
        public bool ShowSystemContent { get; set; }

        public JournalPortlet()
        {
            this.Name = "$JournalPortlet:PortletDisplayName";
            this.Description = "$JournalPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.System);
        }

        protected override void CreateChildControls()
        {
            if (Cacheable && CanCache && IsInCache)
                return;

            Controls.Clear();

            ChildControlsCreated = true;
        }

        protected override object GetModel()
        {
            var node = GetContextNode();
            if (node == null)
                return null;

            var top = this.MaxItems < 1 ? 10 : this.MaxItems;
            var nodeList = new List<Node>();
            IEnumerable<JournalItem> items;

            if (ShowSystemContent)
            {
                items = Journals.Get(node.Path, top);

                nodeList.AddRange(items.Select(item => new JournalNode(node, item)).Cast<Node>());
            }
            else
            {
                var tempTop = top;
                var tempSkip = 0;

                while (true)
                {
                    items = Journals.Get(node.Path, tempTop, tempSkip, true);
                    var query = new StringBuilder("+(");
                    var pathAdded = false;

                    foreach (var item in items)
                    {
                        query.AppendFormat("Path:\"{0}\" ", item.Wherewith);
                        pathAdded = true;
                    }

                    //not found any journal items, finish the search
                    if (!pathAdded)
                        break;

                    query.Append(") +IsSystemContent:yes");

                    var queryResults = ContentQuery.Query(query.ToString(), new QuerySettings() { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled });
                    var pathList = queryResults.Nodes.Select(n => n.Path).ToArray();

                    var maxToAdd = Math.Max(0, top - nodeList.Count);

                    nodeList.AddRange(items.Where(item => !pathList.Contains(item.Wherewith)).Take(maxToAdd).Select(ji => new JournalNode(node, ji)));

                    if (nodeList.Count >= top || items.Count() == 0)
                        break;

                    tempSkip += tempTop;
                    tempTop = Math.Max(tempTop*2, 50);
                }
            }

            var folder = SearchFolder.Create(nodeList);
            return folder.GetXml(true);
        }

    }
}
