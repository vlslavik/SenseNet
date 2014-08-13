using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.ApplicationModel
{
    public class SetNotificationAction : UrlAction
    {
        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            var enabled = SenseNet.Messaging.NotificationConfig.NotificationEnabled;
            //this.Visible = enabled;
            this.Forbidden = !enabled;
        }
    }
}
