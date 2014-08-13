using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SenseNet.ApplicationModel.AspectActions
{
    public abstract class AspectActionBase : ActionBase, IHttpHandler
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
        public override bool CausesStateChange { get { return true; } }
    }
}
