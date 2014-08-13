using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace SenseNet.Portal.Routing
{
    public sealed class ProxyingRouteHandler : IRouteHandler
    {
        private Func<RequestContext, IHttpHandler> _getHttpHandler = null;

        public ProxyingRouteHandler(Func<RequestContext, IHttpHandler> getHttpHandlerAction)
        {
            _getHttpHandler = getHttpHandlerAction;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            if (_getHttpHandler != null)
                return _getHttpHandler(requestContext);

            return null;
        }
    }
}
