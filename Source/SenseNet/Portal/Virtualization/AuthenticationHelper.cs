using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SenseNet.Portal.Virtualization
{
    public static class AuthenticationHelper
    {
        private static readonly string authHeaderName = "WWW-Authenticate";

        public static void DenyAccess(HttpApplication application)
        {
            application.Context.Response.Clear();
            application.Context.Response.StatusCode = 401;
            application.Context.Response.Status = "401 Unauthorized";
            application.Context.Response.End();
        }

        public static void ForceBasicAuthentication(HttpContext context)
        {
            context.Response.Clear();
            context.Response.Buffer = true;
            context.Response.StatusCode = 401;
            context.Response.Status = "401 Unauthorized";

            // make sure that the auth header appears only once in the response
            if (context.Response.Headers.AllKeys.Contains(authHeaderName))
                context.Response.Headers.Remove(authHeaderName);

            context.Response.AddHeader(authHeaderName, "Basic");
            context.Response.End();
        }

        public static void ThrowForbidden(string contentNameOrPath = null)
        {
            throw new HttpException(403, SNSR.GetString(SNSR.Exceptions.HttpAction.Forbidden_1, contentNameOrPath ?? string.Empty));
        }

        public static void ThrowNotFound(string contentNameOrPath = null)
        {
            throw new HttpException(404, SNSR.GetString(SNSR.Exceptions.HttpAction.NotFound_1, contentNameOrPath ?? string.Empty));
        }
    }
}
