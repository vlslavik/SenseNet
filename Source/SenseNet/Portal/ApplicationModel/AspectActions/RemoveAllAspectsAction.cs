﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class RemoveAllAspectsAction : AspectActionBase
    {
        public override ActionParameter[] ActionParameters { get { return ActionParameter.EmptyParameters; } }
        public override object Execute(Content content, params object[] parameters)
        {
            var gc = content.ContentHandler as GenericContent;
            if (gc == null)
                throw new InvalidOperationException("Cannot remove Aspects from a content that is not a GenericContent.");
            content.RemoveAllAspects();
            content.Save();
            return null;
        }
    }
}
