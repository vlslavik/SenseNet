using System;
using System.Collections.Generic;
using System.Web;

namespace SenseNet.ContentRepository.Preview
{
    public class WatermarkTemplateReplacer : TemplateReplacerBase
    {
        public override IEnumerable<string> TemplateNames
        {
            get { return new[] { "CurrentDate", "CurrentTime", "CurrentUser", "FullName", "Email", "IpAddress" }; }
        }

        public override string EvaluateTemplate(string templateName, string propertyName, object parameters)
        {
            switch (templateName)
            {
                case "CurrentDate":
                    return DateTime.Today.ToShortDateString();
                case "CurrentTime":
                    return DateTime.UtcNow.ToString();
                case "CurrentUser":
                    return TemplateManager.GetProperty(User.Current as GenericContent, propertyName);
                case "FullName":
                    return TemplateManager.GetProperty(User.Current as GenericContent, "FullName");
                case "Email":
                    return TemplateManager.GetProperty(User.Current as GenericContent, "Email");
                case "IpAddress":
                    return GetIPAddress();
            }

            return string.Empty;
        }

        protected string GetIPAddress()
        {
            if (HttpContext.Current == null)
                return string.Empty;

            var visitorIPAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            
            if (string.IsNullOrEmpty(visitorIPAddress))
                visitorIPAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

            if (string.IsNullOrEmpty(visitorIPAddress))
                visitorIPAddress = HttpContext.Current.Request.UserHostAddress;

            return visitorIPAddress;
        }
    }
}
