using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that performs a check-in operation on a content, using the given check-in comments.
    /// </summary>
    public sealed class CheckInAction : UrlAction
    {
        public sealed override bool IsHtmlOperation { get { return true; } }
        public sealed override bool IsODataOperation { get { return true; } }
        public sealed override bool CausesStateChange { get { return true; } }
        public sealed override ActionParameter[] ActionParameters { get { return _actionParameters; } }

        private ActionParameter[] _actionParameters = new ActionParameter[] { new ActionParameter("checkInComments", typeof(string), false), };

        public sealed override object Execute(Content content, params object[] parameters)
        {
            // Get parameter
            var checkInComments = parameters.FirstOrDefault() as string ?? "";

            // Perform checks
            if (string.IsNullOrEmpty(checkInComments) && content.CheckInCommentsMode == CheckInCommentsMode.Compulsory)
                throw new Exception(string.Format("Can't check in content '{0}' without checkin comments because its CheckInCommentsMode is set to CheckInCommentsMode.Compulsory.", content.Path));
            if (!(content.ContentHandler is GenericContent))
                throw new Exception(string.Format("Can't check in content '{0}' because its content handler is not a GenericContent. It needs to inherit from GenericContent for collaboration feature support.", content.Path));

            // Do the action
            content["CheckInComments"] = checkInComments;
            content.CheckIn();

            // Return actual state of content
            return content;
        }
    }
}
