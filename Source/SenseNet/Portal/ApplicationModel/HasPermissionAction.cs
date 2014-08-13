using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.Portal.ApplicationModel
{
    public class HasPermissionAction : ActionBase, IHttpHandler
    {
        public override string Uri
        {
            get { return null; }
        }

        public bool IsReusable
        {
            get { return true; }
        }
        public void ProcessRequest(HttpContext context)
        {
            context.Response.StatusCode = 204;
            context.Response.Clear();
        }

        //=========================================================================== OData

        public override bool IsHtmlOperation { get { return false; } }
        public override bool IsODataOperation { get { return true; } }
        public override bool CausesStateChange { get { return false; } }
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("user", typeof(string)), new ActionParameter("permissions", typeof(string[]), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }
        public override object Execute(Content content, params object[] parameters)
        {
            var userParamValue = (string)parameters[0];
            var permissionsParamValue = (string[])parameters[1];
            IUser user = null;
            if (!string.IsNullOrEmpty(userParamValue))
            {
                user = Node.Load<User>(userParamValue);
                if (user == null)
                    throw new ContentNotFoundException("Identity not found: " + userParamValue);
            }

            if (permissionsParamValue == null)
                throw new ArgumentNullException("permissions");
            var permissionNames = permissionsParamValue;//.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var permissions = permissionNames.Select(n => GetPermissionTypeByName(n)).ToArray();

            if (user == null)
                return content.Security.HasPermission(permissions);
            return content.Security.HasPermission(user, permissions);
        }
        private PermissionType GetPermissionTypeByName(string name)
        {
            var permissionType = PermissionType.GetByName(name);
            if (permissionType != null)
                return permissionType;
            throw new ArgumentException("Unknown permission: " + name);
        }
    }
}
