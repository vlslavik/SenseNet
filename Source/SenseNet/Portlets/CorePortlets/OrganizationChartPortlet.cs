using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.UI.PortletFramework;
using System.Web.UI.WebControls.WebParts;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.Search;
using Content = SenseNet.ContentRepository.Content;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Portal.Portlets
{
    public class OrganizationChartPortlet : CacheablePortlet
    {
        private string _firstManager = User.Administrator.Path;

        /// <summary>
        /// Gets or sets the first manager.
        /// </summary>
        /// <value>The first manager.</value>
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory("OrganizationChart", "CategoryTitle", 5), WebOrder(20)]
        [LocalizedWebDisplayName("OrganizationChart", "FirstManagerTitle"), LocalizedWebDescription("OrganizationChart", "FirstManagerDescription")]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(ContentPickerCommonType.FirstManager)]
        public string FirstManager
        {
            get { return _firstManager; }
            set { _firstManager = value; }
        }

        private int _depthLimit = 2;

        /// <summary>
        /// Gets or sets the depth limit.
        /// </summary>
        /// <value>The depth limit.</value>
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory("OrganizationChart", "CategoryTitle", 5), WebOrder(30)]
        [LocalizedWebDisplayName("OrganizationChart", "DepthLimitTitle"), LocalizedWebDescription("OrganizationChart", "DepthLimitDescription")]
        public int DepthLimit
        {
            get { return _depthLimit; }
            set { _depthLimit = value; }
        }

        [WebBrowsable(true), Personalizable(true)]
        [LocalizedWebDisplayName(PORTLETFRAMEWORK_CLASSNAME, RENDERER_DISPLAYNAME)]
        [LocalizedWebDescription(PORTLETFRAMEWORK_CLASSNAME, RENDERER_DESCRIPTION)]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(1000)]
        [Editor(typeof(ViewPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(PortletViewType.All)]
        public override string Renderer
        {
            get
            {
                return base.Renderer;
            }
            set
            {
                base.Renderer = value;
            }
        }

        private List<int> _usedNodeId;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationChartPortlet"/> class.
        /// </summary>
        public OrganizationChartPortlet()
        {
            this.Name = "$OrganizationChart:PortletDisplayName";
            this.Description = "$OrganizationChart:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Collection);
            this.Renderer = "/Root/System/SystemPlugins/Portlets/OrganizationChart/OrgChartView.xslt";

            //remove the xml/xslt fields from the hidden collection
            this.HiddenProperties.RemoveAll(s => XmlFields.Contains(s));
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <returns></returns>
        protected override object GetModel()
        {
            var managerHead = NodeHead.Get(FirstManager);
            if (managerHead == null)
                throw new NotSupportedException(SenseNetResourceManager.Current.GetString("OrganizationChart", "NoSuchManagerError"));

            var resultXml = new XmlDocument();

            if (!SecurityHandler.HasPermission(managerHead, PermissionType.Open))
                return resultXml;

            var manager = Content.Load(FirstManager);
            if (manager == null)
                throw new NotSupportedException(SenseNetResourceManager.Current.GetString("OrganizationChart", "NoSuchManagerError"));

            var managerStream = manager.GetXml(true);
            resultXml.Load(managerStream);

            _usedNodeId = new List<int> { manager.Id };

            try
            {
                GetEmployees(manager, resultXml.SelectSingleNode("/Content"), 1);
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
                throw new NotSupportedException(ex.Message);
            }

            return resultXml; 
        }

        /// <summary>
        /// Gets the employees.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="container">The container.</param>
        /// <param name="depth">The depth.</param>
        private void GetEmployees(Content manager, XmlNode container, int depth)
        {
            if (depth > DepthLimit)
                return;

            var employeesNode = container.OwnerDocument.CreateElement("Employees");
            container.AppendChild(employeesNode);

            foreach (var employee in ContentQuery.Query("+Manager:@0 +Type:User", null, manager.Id).Nodes.Select(Content.Create))
            {
                if (!_usedNodeId.Contains(employee.Id))
                    _usedNodeId.Add(employee.Id);
                else
                    throw new NotSupportedException(SenseNetResourceManager.Current.GetString("OrganizationChart", "CircularReferenceError"));

                var employeeStream = employee.GetXml();
                var employeeXml = new XmlDocument();
                employeeXml.Load(employeeStream);

                if (employeeXml.DocumentElement == null)
                    continue;

                var employeeXmlNode = employeesNode.OwnerDocument.ImportNode(employeeXml.DocumentElement,true);
                employeesNode.AppendChild(employeeXmlNode);

                // reload the xml node
                employeeXmlNode = employeesNode.SelectSingleNode(string.Format("Content[SelfLink='{0}']", employee.Path));

                GetEmployees(employee, employeeXmlNode, depth + 1);
            }
        }

    }
}
