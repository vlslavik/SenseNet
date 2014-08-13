using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that performs a reject operation on a content.
    /// </summary>
    public sealed class RejectAction : ActionBase
    {
        public sealed override string Uri { get { return null; } }
        public sealed override bool IsHtmlOperation { get { return false; } }
        public sealed override bool IsODataOperation { get { return true; } }
        public sealed override bool CausesStateChange { get { return true; } }
        public sealed override ActionParameter[] ActionParameters { get { return _actionParameters; } }

        private ActionParameter[] _actionParameters = new ActionParameter[] { new ActionParameter("rejectReason", typeof(string), false), };

        public sealed override object Execute(Content content, params object[] parameters)
        {
            // Perform checks
            if (!(content.ContentHandler is GenericContent))
                throw new Exception(string.Format("Can't reject content '{0}' because its content handler is not a GenericContent. It needs to inherit from GenericContent for collaboration feature support.", content.Path));

            // Get parameters
            string rejectReason = parameters.FirstOrDefault() as string ?? "";

            // Do the action
            content["RejectReason"] = rejectReason;
            content.Reject();

            // Return actual state of content
            return content;
        }
    }
}
