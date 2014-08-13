using System;
using System.Linq;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;

namespace SenseNet.ApplicationModel
{
    public class WebdavOpenAction : ClientAction
    {
        public override string MethodName
        {
            get
            {
                return "SN.WebDav.OpenDocument";
            }
            set
            {
                base.MethodName = value;
            }
        }

        public override string ParameterList
        {
            get
            {
                return this.Content == null ? string.Empty : string.Format(@"'{0}'", this.Content.Path);
            }
            set
            {
                base.ParameterList = value;
            }
        }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            // this action should be accessible only if we are on NTLM (Windows) authentication, or using HTTPS
            if (!string.Equals(PortalContext.Current.AuthenticationMode, "Windows", StringComparison.OrdinalIgnoreCase))
            {
                var forwardedHttps = string.Equals(HttpContext.Current.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);
                var headerHost443 = HttpContext.Current.Request.Headers["Host"].EndsWith(":443");

                var referrerHttps = false;
                var referrer = HttpContext.Current.Request.UrlReferrer;
                if (referrer != null)
                    referrerHttps = referrer.Host == HttpContext.Current.Request.Url.Host && string.Equals(referrer.Scheme, "https", StringComparison.OrdinalIgnoreCase);

                var isSecureConnection = HttpContext.Current.Request.IsSecureConnection || forwardedHttps || headerHost443 || referrerHttps;
                if (!isSecureConnection)
                    this.Forbidden = true; 
            }

            if (!Repository.WebdavEditExtensions.Any(extension => context.Name.EndsWith(extension)))
                this.Visible = false;
        }
    }
}
