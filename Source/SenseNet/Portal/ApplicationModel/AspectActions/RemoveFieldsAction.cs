﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel.AspectActions
{
    public class RemoveFieldsAction : AspectActionBase
    {
        private ActionParameter[] _actionParameters = new[] { new ActionParameter("fields", typeof(string[]), true) };
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }
        public override object Execute(Content content, params object[] parameters)
        {
            var fieldNames = (string[])parameters[0];
            var aspect = content.ContentHandler as Aspect;
            if (aspect == null)
                throw new InvalidOperationException("Cannot remove Fields from a content that is not an Aspect.");

            aspect.RemoveFields(fieldNames);
            return null;
        }
    }
}
