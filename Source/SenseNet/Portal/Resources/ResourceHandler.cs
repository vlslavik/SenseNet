using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SenseNet.ContentRepository;
using System.IO;
using SenseNet.ContentRepository.i18n;
using System.Globalization;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using System.Text.RegularExpressions;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.Resources
{
    /// <summary>
    /// When /Resource.ashx?class=xy is requested, a javascript variable is defined
    /// </summary>
    public class ResourceHandler : IHttpHandler
    {
        private static readonly string REGEX_RESOURCES = "(?<prev>[^/]*?)/" + UrlPart + "/(?<lang>[^/]+?)/(?<class>[^/]+)";

        public static string UrlPart
        {
            get { return "sn-resources"; }
        }

        //================================================================================= Helper methods

        private void DenyRequest(HttpContext context)
        {
            context.Response.Clear();
            context.Response.StatusCode = 404;
            context.Response.Flush();
            context.Response.End();
        }

        public static DateTime GetLastResourceModificationDate(DateTime? modifiedSince)
        {
            // Elevation: last resource modification date is independent from the current user.
            using (new SystemAccount())
            {
                var q = (modifiedSince == null)
                    ? ContentQuery.Query(SafeQueries.Resources,
                        new QuerySettings { EnableAutofilters = FilterStatus.Disabled })
                    : ContentQuery.Query(SafeQueries.ResourcesAfterADate,
                        new QuerySettings { EnableAutofilters = FilterStatus.Disabled },
                        modifiedSince.Value.ToString("yyyy-MM-dd HH:mm:ss"));

                if (q.Count == 0 || !q.Nodes.Any())
                    return modifiedSince ?? DateTime.MinValue;

                return q.Nodes.Max(x => x.ModificationDate);
            }
        }

        protected internal static Tuple<string, string> ParseUrl(string url)
        {
            var regex = new Regex(REGEX_RESOURCES);
            var match = regex.Match(url);

            return match.Success ? Tuple.Create(match.Groups["lang"].ToString(), match.Groups["class"].ToString()) : null;
        }

        //================================================================================= IHttpHandler

        public void ProcessRequest(HttpContext context)
        {
            // Handling If-Modified-Since

            var modifiedSinceHeader = HttpContext.Current.Request.Headers["If-Modified-Since"];

            if (modifiedSinceHeader != null)
            {
                DateTime ifModifiedSince = DateTime.MinValue;
                DateTime.TryParse(modifiedSinceHeader, out ifModifiedSince);

                if (ifModifiedSince != DateTime.MinValue)
                {
                    // TODO: Once you can query properly in text files, only take into account the
                    //       resource files which contain the resource class in question.
                    var lastModificationDate = GetLastResourceModificationDate(ifModifiedSince);

                    if (lastModificationDate != null && lastModificationDate <= ifModifiedSince)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = 304;
                        context.Response.Flush();
                        context.Response.End();
                    }
                }
            }

            // Handling the rest of the request

            var shouldDeny = true;

            try
            {
                var parsedUrl = ParseUrl(context.Request.RawUrl);

                if (parsedUrl != null)
                {
                    var cultureName = parsedUrl.Item1;
                    var className = parsedUrl.Item2;
                    CultureInfo culture = null;

                    if (!string.IsNullOrEmpty(cultureName))
                        culture = CultureInfo.GetCultureInfo(cultureName);

                    if (culture != null && !string.IsNullOrEmpty(className))
                    {
                        var script = ResourceScripter.RenderResourceScript(className, culture);
                        var lastModificationDate = GetLastResourceModificationDate(null);

                        HttpHeaderTools.SetCacheControlHeaders(lastModified: lastModificationDate);

                        // TODO: add an expires header when appropriate, but without clashing with the resource editor

                        context.Response.ContentType = "text/javascript";
                        context.Response.Write(script);
                        context.Response.Flush();

                        shouldDeny = false;
                    }
                }
            }
            catch
            {
                shouldDeny = true;
            }

            // If it failed for some reason, deny it

            if (shouldDeny)
                DenyRequest(context);

        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}
