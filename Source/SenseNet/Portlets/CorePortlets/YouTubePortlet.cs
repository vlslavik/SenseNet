﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls.WebParts;
using System.Xml;
using SenseNet.Diagnostics;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Portal.Portlets
{
    public class YouTubePortlet : CacheablePortlet
    {
        [LocalizedWebDisplayName("YouTubePortlet", "LayoutStyleTitle"), LocalizedWebDescription("YouTubePortlet", "LayoutStyleDescription")]
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory("YouTubePortlet", "CategoryTitle", 5), WebOrder(10)]
        [Editor(typeof(TextEditorPartField), typeof(IEditorPartField))]
        [TextEditorPartOptions(TextEditorCommonType.MultiLine)]
        public string Html
        {
            get;
            set;
        }

        public YouTubePortlet()
        {
            this.Name = "$YouTubePortlet:PortletTitle";
            this.Description = "$YouTubePortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Application);

            this.HiddenProperties.Add("Renderer");
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            if (String.IsNullOrEmpty(Html))
            {
                writer.Write(SenseNetResourceManager.Current.GetString("YouTubePortlet", "CodeForHere"));
                return;
            }

            if (!Html.Contains("<iframe"))
            {
                if (Html.Contains("youtu.be"))
                {
                    Html = string.Format(@"<iframe width=""560"" height=""315"" src=""//www.youtube.com/embed/{0}"" frameborder=""0"" allowfullscreen></iframe>", Html.Replace("http://youtu.be/", ""));
                }
                else
                {
                    Html = Html.Replace("<embed", "<param name=\"wmode\" value=\"transparent\" ></param><embed wmode=\"transparent\"");
                    try
                    {
                        var xml = new XmlDocument();
                        xml.LoadXml(Html);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteException(ex);
                        writer.Write(SenseNetResourceManager.Current.GetString("YouTubePortlet", "WrongCode"));
                        return;
                    }

                }
            }
            writer.Write(Html);
        }
    }
}
