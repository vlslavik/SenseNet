using System;
using System.Web;
using System.IO;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.Handlers
{
    /// <summary>
    /// Serves binary properties of Sense/Net content items according to the supplied query string.
    /// </summary>
    public class BinaryHandler : BinaryHandlerBase
    {
        /* ============================================================================= Public Properties */

        public static NodeHead RequestedNodeHead
        {
            get
            {
                var nodeid = RequestedNodeId;
                if (nodeid.HasValue)
                    return NodeHead.Get(nodeid.Value);

                var nodepath = RequestedNodePath;
                if (!string.IsNullOrEmpty(nodepath))
                    return NodeHead.Get(nodepath);

                return null;
            }
        }
        
        /* ============================================================================= Properties */

        protected override string PropertyName
        {
            get
            {
                var propertyName = HttpContext.Current.Request.QueryString["propertyname"];
                if (string.IsNullOrEmpty(propertyName))
                    return null;

                return propertyName.Replace("$", "#");
            }
        }

        private static int? RequestedNodeId
        {
            get
            {
                var nodeidStr = HttpContext.Current.Request.QueryString["nodeid"];
                if (!string.IsNullOrEmpty(nodeidStr))
                {
                    int nodeid;
                    var success = Int32.TryParse(nodeidStr, out nodeid);
                    if (success)
                        return nodeid;
                }
                return null;
            }
        }

        private static string RequestedNodePath
        {
            get
            {
                var nodePathStr = HttpContext.Current.Request.QueryString["nodepath"];
                return nodePathStr;
            }
        }
        
        protected override Node RequestedNode
        {
            get
            {
                if (!SecurityHandler.HasPermission(PortalContext.Current.BinaryHandlerRequestedNodeHead, PermissionType.Open))
                    return null;

                if (string.IsNullOrEmpty(PortalContext.Current.VersionRequest))
                {
                    return Node.LoadNode(PortalContext.Current.BinaryHandlerRequestedNodeHead, VersionNumber.LastFinalized);
                }
                else
                {
                    VersionNumber version;
                    if (VersionNumber.TryParse(PortalContext.Current.VersionRequest, out version))
                    {
                        var node = Node.LoadNode(PortalContext.Current.BinaryHandlerRequestedNodeHead, version);
                        if (node != null && node.SavingState == ContentSavingState.Finalized)
                            return node;
                    }
                }

                return null;
            }
        }

        protected override int? Width
        {
            get
            {
                var widthStr = HttpContext.Current.Request.QueryString["width"];
                if (!string.IsNullOrEmpty(widthStr))
                {
                    int width;
                    var success = Int32.TryParse(widthStr, out width);
                    if (success)
                        return width;
                }
                return null;
            }
        }

        protected override int? Height
        {
            get
            {
                var heightStr = HttpContext.Current.Request.QueryString["height"];
                if (!string.IsNullOrEmpty(heightStr))
                {
                    int height;
                    var success = Int32.TryParse(heightStr, out height);
                    if (success)
                        return height;
                }
                return null;
            }
        }

        protected override TimeSpan? MaxAge
        {
            get
            {
                var maxAgeInDaysStr = HttpContext.Current.Request.QueryString["maxAge"] as string;
                int maxAgeInDays;

                if (!string.IsNullOrEmpty(maxAgeInDaysStr) && int.TryParse(maxAgeInDaysStr, out maxAgeInDays))
                {
                    return TimeSpan.FromDays(maxAgeInDays);
                }

                return null;
            }
        }
    }
}
