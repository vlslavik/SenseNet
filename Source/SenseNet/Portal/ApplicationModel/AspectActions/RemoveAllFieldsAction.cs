using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class RemoveAllFieldsAction : AspectActionBase
    {
        public override ActionParameter[] ActionParameters { get { return ActionParameter.EmptyParameters; } }
        public override object Execute(Content content, params object[] parameters)
        {
            var aspect = content.ContentHandler as Aspect;
            if (aspect == null)
                throw new InvalidOperationException("Cannot remove Fields from a content that is not an Aspect.");

            aspect.RemoveAllfields();
            return null;
        }
    }
}
