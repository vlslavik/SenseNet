using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal;
using sn = SenseNet.ContentRepository;
using System.Diagnostics;
using System.ComponentModel;
using SenseNet.Workflow.Activities.Design;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.Workflow.Activities
{
    [Designer(typeof(CreateContentDesigner))]
    public class CreateContent : NativeActivity<WfContent>
    {
        public InArgument<string> ParentPath { get; set; }
        public InArgument<string> ContentTypeName { get; set; }
        public InArgument<string> Name { get; set; }
        public InArgument<string> ContentDisplayName { get; set; }
        public InArgument<Dictionary<string,object>> FieldValues { get; set; }

        protected virtual string GetContentTypeName(NativeActivityContext context)
        {
            return ContentTypeName.Get(context);
        }
        protected virtual void SetContentFields(Content content, NativeActivityContext context)
        {
        }

        protected override void Execute(NativeActivityContext context)
        {
            var ext = context.GetExtension<ContentWorkflowExtension>();

            var parent = Node.LoadNode(ParentPath.Get(context));
            if (parent == null)
                throw new ApplicationException("Cannot create content because parent does not exist. Path: " + ParentPath.Get(context));

            var name = Name.Get(context);
            var displayName = ContentDisplayName.Get(context);
            if (string.IsNullOrEmpty(name))
                name = ContentNamingHelper.GetNameFromDisplayName(displayName);

            var content = ContentManager.CreateContentFromRequest(GetContentTypeName(context), name, ParentPath.Get(context), true);
            if (!string.IsNullOrEmpty(displayName))
                content.DisplayName = displayName;

            var fieldValues = FieldValues.Get(context);
            if (fieldValues != null)
            {
                foreach (var key in fieldValues.Keys)
                {
                    content[key] = fieldValues[key];
                }
            }

            SetContentFields(content, context);

            content.ContentHandler.DisableObserver(typeof(WorkflowNotificationObserver));

            try
            {
                content.Save();
            }
            catch (Exception e)
            {
                throw new ApplicationException(String.Concat("Cannot create content. See inner exception. Expected path: "
                    , ParentPath.Get<string>(context), "/", Name.Get(context)), e);
            }

            Result.Set(context, new WfContent(content.ContentHandler));
        }

        protected override void Cancel(NativeActivityContext context)
        {
            Debug.WriteLine("##WF> CreateContent.Cancel: " + Name.Get(context));
            base.Cancel(context);
        }
    }
}
