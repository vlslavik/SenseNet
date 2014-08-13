using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Portal;

namespace SenseNet.ApplicationModel
{
    public class GetPermissionsAction : ActionBase, IHttpHandler
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
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("identity", typeof(string)) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }

        public override object Execute(Content content, params object[] parameters)
        {
            var identityParamValue = (string)parameters[0];
            var canSeePermissions = content.Security.HasPermission(PermissionType.SeePermissions);

            // If the user doesn't have SeePermissions right, it can only query its own permissions
            if (!canSeePermissions && (string.IsNullOrEmpty(identityParamValue) || identityParamValue != User.Current.Path))
                throw new Exception("You are only authorized to query your own permissions for this content.");

            // Elevation is required if the user doesn't have SeePermissions right, but
            // in this case, it will only see its own permissions anyway.
            IDisposable sysacc = null;
            try
            {
                if (!canSeePermissions)
                    sysacc = new SystemAccount();

                if (String.IsNullOrEmpty(identityParamValue))
                    return GetAcl(content);

                return GetAce(content, identityParamValue);
            }
            finally
            {
                if (sysacc != null)
                    sysacc.Dispose();
            }
        }

        internal static object GetAcl(Content content)
        {
            var acl = content.ContentHandler.Security.GetAcl();

            var entries = new List<Dictionary<string, object>>();
            foreach (var entry in acl.Entries)
                entries.Add(CreateAce(entry));

            var aclout = new Dictionary<string, object>(){
                {"id", content.Id},
                {"path", content.Path},
                {"inherits", acl.Inherits},
                {"entries", entries}
            };
            return aclout;
        }
        internal static Dictionary<string, object>[] GetAce(Content content, string identityPath)
        {
            var acl = content.ContentHandler.Security.GetAcl();
            var entries = acl.Entries.Where(e => String.Compare(e.Identity.Path, identityPath, true) == 0).Select(e => CreateAce(e)).ToArray();
            if (entries.Length == 0)
                return new[] { GetEmptyEntry(identityPath) };
            return entries;
        }
        private static Dictionary<string, object> CreateAce(SnAccessControlEntry entry)
        {
            var perms = new Dictionary<string, object>();
            foreach (var perm in entry.Permissions)
            {
                if (perm.Allow || perm.Deny)
                {
                    perms.Add(perm.Name, new Dictionary<string, object>
                    {
                        {"value", perm.Allow ? "allow" : "deny"},
                        {"from", perm.AllowFrom ?? perm.DenyFrom},
                    });
                }
                else
                {
                    perms.Add(perm.Name, null);
                }
            }
            var ace = new Dictionary<string, object>
            {
                { "identity", GetIdentity(entry) },
                { "propagates", entry.Propagates },
                { "permissions", perms }
            };

            return ace;
        }
        private static Dictionary<string, object> GetEmptyEntry(string identityPath)
        {
            var perms = new Dictionary<string, object>();
            foreach (var pt in ActiveSchema.PermissionTypes)
                perms.Add(pt.Name, null);
            return new Dictionary<string, object>
            {
                {"identity", GetIdentity(Node.LoadNode(identityPath)) },
                {"propagates", true},
                {"permissions", perms}
            };
        }
        private static object GetIdentity(SnAccessControlEntry entry)
        {
            return GetIdentity(Node.LoadNode(entry.Identity.Path));
        }
        private static object GetIdentity(Node node)
        {
            if (node == null)
                throw new ArgumentException("Identity not found");

            string domain = null; //TODO: domain
            var kind = node is User ? SnIdentityKind.User : node is Group ? SnIdentityKind.Group : SnIdentityKind.OrganizationalUnit;

            return new Dictionary<string, object>
            {
                { "id", node.Id },
                { "path", node.Path },
                { "name", node.Name },
                { "displayName", SNSR.GetString(node.DisplayName) },
                { "domain", domain },
                { "kind", kind.ToString().ToLower() }
            };
        }
        private static object GetPermissions(Content content, string identityPath, string permissionsParamValue)
        {
            throw new NotImplementedException();
        }

    }
}
