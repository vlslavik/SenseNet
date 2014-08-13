using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;

namespace SenseNet.ApplicationModel
{
    public class ExpenseClaimPublishAction : UrlAction
    {
        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            if (context == null)
                return;

            var ec = context.ContentHandler as ExpenseClaim;
            if (ec == null)
                return;

            if (ec.ChildCount == 0)
                this.Forbidden = true;
        }
    }
}
