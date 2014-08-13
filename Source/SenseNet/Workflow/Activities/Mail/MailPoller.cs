using System;
using System.Activities;
using System.Net.Mail;
using SenseNet.ContentRepository.Mail;
using SenseNet.Diagnostics;

namespace SenseNet.Workflow.Activities
{
    public sealed class MailPoller : AsyncCodeActivity<MailMessage[]>
    {
        public InArgument<string> ContentListPath { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var contentListPath = ContentListPath.Get(context);

            if (string.IsNullOrEmpty(contentListPath))
            {
                Logger.WriteError(Logger.EventId.NotDefined, "MailPoller activity: ContentList path is empty.");
                return null;
            }

            var getMessagesDelegate = new Func<string, MailMessage[]>(GetMessages);
            context.UserState = getMessagesDelegate;
            return getMessagesDelegate.BeginInvoke(contentListPath, callback, state);  
        }

        protected override MailMessage[] EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var getMessagesDelegate = (Func<string, MailMessage[]>)context.UserState;
            return getMessagesDelegate.EndInvoke(result);     
        }

        private MailMessage[] GetMessages(string contentListPath)
        {
            try
            {
                return MailHelper.GetMailMessages(contentListPath);
            }
            catch (Exception ex)
            {
                Logger.WriteError(Logger.EventId.NotDefined, "Mail processor workflow error: " + ex);
                return new MailMessage[0];
            }
        }
    }
}
