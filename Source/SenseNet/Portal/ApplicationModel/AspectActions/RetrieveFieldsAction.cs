using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.Portal.ApplicationModel.AspectActions
{
    public sealed class RetrieveFieldsAction : ActionBase
    {
        public override string Uri { get { return null; } }
        public override bool IsHtmlOperation { get { return false; } }
        public override bool IsODataOperation { get { return true; } }
        public override bool CausesStateChange { get { return false; } }

        public override object Execute(ContentRepository.Content content, params object[] parameters)
        {
            var aspect = content.ContentHandler as Aspect;
            if (aspect == null)
                throw new InvalidOperationException("This action only works with Aspect content items.");

            var result = aspect.FieldSettings.Select(x => x.ToFieldInfo()).ToArray();
            return result;
        }
    }
}
