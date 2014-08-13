using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Portal.ApplicationModel
{
    public class SetPermissionsRequest
    {
        public SetPermissionRequest[] r;
        public string inheritance;
    }
    public class SetPermissionRequest
    {
        // {r:[{identity:"/Root/IMS/BuiltIn/Portal/Visitor", OpenMinor:"allow", Save:"deny"},{identity:"/Root/IMS/BuiltIn/Portal/Creators", OpenMinor:"A", Save:"1"}]}

        public string identity;  // Id or Path
        public bool? propagates;
        public string See;       // Insensitive. Available values: "u", "a", "d", "undefined", "allow", "deny", "0", "1", "2" 
        public string Open;
        public string OpenMinor;
        public string Save;
        public string Publish;
        public string ForceCheckin;
        public string AddNew;
        public string Approve;
        public string Delete;
        public string RecallOldVersion;
        public string DeleteOldVersion;
        public string SeePermissions;
        public string SetPermissions;
        public string RunApplication;
        public string ManageListsAndWorkspaces;

        public string Preview;
        public string PreviewWithoutWatermark;
        public string PreviewWithoutRedaction;

        public string Custom01;
        public string Custom02;
        public string Custom03;
        public string Custom04;
        public string Custom05;
        public string Custom06;
        public string Custom07;
        public string Custom08;
        public string Custom09;
        public string Custom10;
        public string Custom11;
        public string Custom12;
        public string Custom13;
        public string Custom14;
        public string Custom15;
        public string Custom16;
        public string Custom17;
    }

    public class SetPermissionsAction : UrlAction, IHttpHandler
    {
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

        public override bool IsHtmlOperation { get { return true; } }
        public override bool IsODataOperation { get { return true; } }
        public override bool CausesStateChange { get { return true; } }
        private ActionParameter[] _actionParameters = new[] { new ActionParameter(null, typeof(SetPermissionsRequest), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }
        public override object Execute(Content content, params object[] parameters)
        {
            var request = (SetPermissionsRequest)parameters[0];
            if (request.inheritance != null)
                SetInheritance(content, request);
            else
                SetPermissions(content, request);
            return null;
        }
        private void SetInheritance(Content content, SetPermissionsRequest request)
        {
            if (request.r != null)
                throw new InvalidOperationException("Cannot use 'r' and 'inheritance' parameters at the same time.");
            switch (request.inheritance.ToLower())
            {
                default:
                    throw new ArgumentException("The value of the 'inheritance' must be 'break' or 'unbreak'.");
                case "break":
                    content.ContentHandler.Security.BreakInheritance();
                    break;
                case "unbreak":
                    content.ContentHandler.Security.RemoveBreakInheritance();
                    break;
            }
        }
        private void SetPermissions(Content content, SetPermissionsRequest request)
        {
            var editor = content.ContentHandler.Security.GetAclEditor();
            foreach (var permReq in request.r)
            {
                var member = LoadMember(permReq.identity);
                var propagates = permReq.propagates.HasValue ? permReq.propagates.Value : true;

                if (permReq.See != null) editor.SetPermission(member, propagates, PermissionType.See, GetPermissionValue(permReq.See));
                if (permReq.Open != null) editor.SetPermission(member, propagates, PermissionType.Open, GetPermissionValue(permReq.Open));
                if (permReq.OpenMinor != null) editor.SetPermission(member, propagates, PermissionType.OpenMinor, GetPermissionValue(permReq.OpenMinor));
                if (permReq.Save != null) editor.SetPermission(member, propagates, PermissionType.Save, GetPermissionValue(permReq.Save));
                if (permReq.Publish != null) editor.SetPermission(member, propagates, PermissionType.Publish, GetPermissionValue(permReq.Publish));
                if (permReq.ForceCheckin != null) editor.SetPermission(member, propagates, PermissionType.ForceCheckin, GetPermissionValue(permReq.ForceCheckin));
                if (permReq.AddNew != null) editor.SetPermission(member, propagates, PermissionType.AddNew, GetPermissionValue(permReq.AddNew));
                if (permReq.Approve != null) editor.SetPermission(member, propagates, PermissionType.Approve, GetPermissionValue(permReq.Approve));
                if (permReq.Delete != null) editor.SetPermission(member, propagates, PermissionType.Delete, GetPermissionValue(permReq.Delete));
                if (permReq.RecallOldVersion != null) editor.SetPermission(member, propagates, PermissionType.RecallOldVersion, GetPermissionValue(permReq.RecallOldVersion));
                if (permReq.DeleteOldVersion != null) editor.SetPermission(member, propagates, PermissionType.DeleteOldVersion, GetPermissionValue(permReq.DeleteOldVersion));
                if (permReq.SeePermissions != null) editor.SetPermission(member, propagates, PermissionType.SeePermissions, GetPermissionValue(permReq.SeePermissions));
                if (permReq.SetPermissions != null) editor.SetPermission(member, propagates, PermissionType.SetPermissions, GetPermissionValue(permReq.SetPermissions));
                if (permReq.RunApplication != null) editor.SetPermission(member, propagates, PermissionType.RunApplication, GetPermissionValue(permReq.RunApplication));
                if (permReq.ManageListsAndWorkspaces != null) editor.SetPermission(member, propagates, PermissionType.ManageListsAndWorkspaces, GetPermissionValue(permReq.ManageListsAndWorkspaces));

                if (permReq.Preview != null) editor.SetPermission(member, propagates, PermissionType.Preview, GetPermissionValue(permReq.Preview));
                if (permReq.PreviewWithoutWatermark != null) editor.SetPermission(member, propagates, PermissionType.PreviewWithoutWatermark, GetPermissionValue(permReq.PreviewWithoutWatermark));
                if (permReq.PreviewWithoutRedaction != null) editor.SetPermission(member, propagates, PermissionType.PreviewWithoutRedaction, GetPermissionValue(permReq.PreviewWithoutRedaction));

                if (permReq.Custom01 != null) editor.SetPermission(member, propagates, PermissionType.Custom01, GetPermissionValue(permReq.Custom01));
                if (permReq.Custom02 != null) editor.SetPermission(member, propagates, PermissionType.Custom02, GetPermissionValue(permReq.Custom02));
                if (permReq.Custom03 != null) editor.SetPermission(member, propagates, PermissionType.Custom03, GetPermissionValue(permReq.Custom03));
                if (permReq.Custom04 != null) editor.SetPermission(member, propagates, PermissionType.Custom04, GetPermissionValue(permReq.Custom04));
                if (permReq.Custom05 != null) editor.SetPermission(member, propagates, PermissionType.Custom05, GetPermissionValue(permReq.Custom05));
                if (permReq.Custom06 != null) editor.SetPermission(member, propagates, PermissionType.Custom06, GetPermissionValue(permReq.Custom06));
                if (permReq.Custom07 != null) editor.SetPermission(member, propagates, PermissionType.Custom07, GetPermissionValue(permReq.Custom07));
                if (permReq.Custom08 != null) editor.SetPermission(member, propagates, PermissionType.Custom08, GetPermissionValue(permReq.Custom08));
                if (permReq.Custom09 != null) editor.SetPermission(member, propagates, PermissionType.Custom09, GetPermissionValue(permReq.Custom09));
                if (permReq.Custom10 != null) editor.SetPermission(member, propagates, PermissionType.Custom10, GetPermissionValue(permReq.Custom10));
                if (permReq.Custom11 != null) editor.SetPermission(member, propagates, PermissionType.Custom11, GetPermissionValue(permReq.Custom11));
                if (permReq.Custom12 != null) editor.SetPermission(member, propagates, PermissionType.Custom12, GetPermissionValue(permReq.Custom12));
                if (permReq.Custom13 != null) editor.SetPermission(member, propagates, PermissionType.Custom13, GetPermissionValue(permReq.Custom13));
                if (permReq.Custom14 != null) editor.SetPermission(member, propagates, PermissionType.Custom14, GetPermissionValue(permReq.Custom14));
            }
            editor.Apply();
        }

        private ISecurityMember LoadMember(string idstr)
        {
            int id;
            ISecurityMember ident;
            if ((ident = (int.TryParse(idstr, out id) ? Node.LoadNode(id) : Node.LoadNode(idstr)) as ISecurityMember) != null)
                return ident;
            throw new ContentNotFoundException("Identity not found: " + idstr);
        }
        private PermissionType GetPermissionTypeByName(string name)
        {
            var permissionType = PermissionType.GetByName(name);
            if (permissionType != null)
                return permissionType;
            throw new ArgumentException("Unknown permission: " + name);
        }
        private PermissionValue GetPermissionValue(string request)
        {
            switch (request.ToLower())
            {
                case "0": case "u": case "undefined":
                    return PermissionValue.NonDefined;
                case "1": case "a": case "allow":
                    return PermissionValue.Allow;
                case "2": case "d": case "deny":
                    return PermissionValue.Deny;
                default:
                    throw new ArgumentException("Invalid permission value: " + request);
            }
        }
    }
}
