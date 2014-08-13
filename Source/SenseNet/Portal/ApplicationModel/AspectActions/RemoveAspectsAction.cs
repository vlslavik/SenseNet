using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class RemoveAspectsAction : AspectActionBase
    {
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("aspects", typeof(string[]), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }
        public override object Execute(Content content, params object[] parameters)
        {
            var aspectPaths = (string[])parameters[0];
            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
                throw new InvalidOperationException("Cannot remove Aspects from a content that is not a GenericContent.");
            content.RemoveAspects(aspectPaths);
            content.Save();
            return null;
        }
    }
}
