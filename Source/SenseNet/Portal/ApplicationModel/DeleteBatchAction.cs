using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;
using SenseNet.Search;
using SenseNet.ContentRepository.i18n;
using System;
using System.Linq;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage;
using System.Globalization;
using SenseNet.Diagnostics;

namespace SenseNet.ApplicationModel
{
    public class DeleteBatchAction : ClientAction
    {
        public override string Callback
        {
            get
            {
                return this.Forbidden ? string.Empty : string.Format("{0};", GetCallBackScript());
            }
            set
            {
                base.Callback = value;
            }
        }

        private string _portletClientId;
        public string PortletClientId
        {
            get 
            { 
                return _portletClientId ?? (_portletClientId = GetPortletClientId());
            }
        }
        protected string GetPortletClientId()
        {
            var parameters = GetParameteres();
            return parameters.ContainsKey("PortletClientID") ? parameters["PortletClientID"].ToString() : string.Empty;
        }

        protected virtual string GetCallBackScript()
        {
            return string.Format(
@"if ($(this).hasClass('sn-disabled')) 
    return false; 
var paths = SN.ListGrid.getSelectedPaths('{0}'); 
var ids = SN.ListGrid.getSelectedIdsList('{0}'); 
var contextpath = '{2}';
var redirectPath = '{3}';
SN.Util.CreateServerDialog('/Root/System/WebRoot/DeleteAction.aspx','{1}', {{paths:paths,ids:ids,contextpath:contextpath,batch:true,redirectPath:redirectPath}});",
                PortletClientId, 
                SenseNetResourceManager.Current.GetString("ContentDelete", "DeleteStatusDialogTitle"),
                this.Content.Path,
                PortalContext.Current.ActionName == "Explore"
                    ? this.Content.Path + "?action=Explore"
                    : string.Empty
                );
        }

        //=========================================================================== OData

        public override bool IsODataOperation { get { return true; } }
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("paths", typeof(string[]), true), new ActionParameter("permanent", typeof(bool)) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }

        public override object Execute(Content content, params object[] parameters)
        {
            var paths = (string[])parameters[0];
            var permanent = parameters.Length > 1 && parameters[1] != null && (bool)parameters[1];

            var exceptions = new List<Exception>();
            foreach (var path in paths)
            {
                try
                {
                    var node = Node.LoadNode(path);
                    if (node == null)
                        throw new InvalidOperationException(string.Format(SNSR.GetString(SNSR.Exceptions.Operations.ContentDoesNotExistWithPath_1), path));

                    var gc = node as GenericContent;
                    if (gc != null)
                    {
                        gc.Delete(permanent);
                    }
                    else
                    {
                        var ct = node as ContentType;
                        if (ct != null)
                            ct.Delete();
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);

                    //TODO: we should log only relevant exceptions here and skip
                    //business logic-related errors, e.g. lack of permissions or
                    //existing target content path.
                    Logger.WriteException(e);
                }
            }
            if (exceptions.Count > 0)
                throw new Exception(String.Join(Environment.NewLine, exceptions.Select(e => e.Message)));

            return null;
        }
    }
}
