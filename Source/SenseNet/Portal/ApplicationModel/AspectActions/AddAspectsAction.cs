using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class AddAspectsAction : AspectActionBase
    {
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("aspects", typeof(string[]), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }
        public override object Execute(Content content, params object[] parameters)
        {
            var aspectPaths = (string[])parameters[0];
            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
                throw new InvalidOperationException("Cannot add an aspect to a content that is not a GenericContent.");
            var aspects = new Aspect[aspectPaths.Length];
            for (int i = 0; i < aspectPaths.Length; i++)
            {
                var pathOrName = aspectPaths[i];
                aspects[i] = Aspect.LoadAspectByPathOrName(pathOrName);
                if (aspects[i] == null)
                    throw new InvalidOperationException("Unknown aspect: " + aspectPaths[i]);
            }
            content.AddAspects(aspects);
            content.Save();
            return null;
        }
    }
}
