﻿using System.Web;
using SenseNet.Portal.Virtualization;

namespace SenseNet.ApplicationModel
{
    public class ServiceAction : PortalAction
    {
        public virtual string ServiceName { get; set; }

        public virtual string MethodName { get; set; }

        public override string Uri
        {
            get
            {
                if (this.Forbidden)
                    return string.Empty;

                var s = SerializeParameters(GetParameteres());
                var uri = string.Format("/{0}/{1}", ServiceName, MethodName);

                if (!string.IsNullOrEmpty(s))
                {
                    uri = ContinueUri(uri);
                    uri += s.Substring(1);
                }

                if (this.IncludeBackUrl && !string.IsNullOrEmpty(this.BackUri))
                {
                    uri = ContinueUri(uri);
                    uri += string.Format("{0}={1}", PortalContext.BackUrlParamName, System.Uri.EscapeDataString(this.BackUri));
                }

                return uri;
            }
        }
    }
}
