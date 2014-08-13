using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls.WebParts;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Diagnostics;
using SenseNet.Portal.UI.Bundling;
using SenseNet.Portal.UI.Controls;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.Portal.Virtualization;

namespace SenseNet.Portal.UI
{
    public static class UITools
    {
        /// <summary>
        /// Generates script block to run commands at client-side.
        /// </summary>
        /// <param name="scriptName">Script unique name in request.</param>
        /// <param name="script">Script will be run. For example: SN.PortalExplorer.addFileUploadCallback('Grid');</param>
        public static void RegisterStartupScript(string scriptName, string script, System.Web.UI.Page page)
        {
            var sb = new StringBuilder();
            string generatedScriptName = "msajax{0}";
            sb.Append("Sys.Application.add_load(");
            sb.Append(String.Format(generatedScriptName, scriptName));
            sb.Append("); ");
            sb.Append(Environment.NewLine);
            sb.Append("function ");
            sb.Append(String.Format(generatedScriptName, scriptName));
            sb.Append("() { ");
            sb.Append(Environment.NewLine);
            sb.Append(script);
            sb.Append(Environment.NewLine);
            sb.Append("Sys.Application.remove_load(");
            sb.Append(String.Format(generatedScriptName, scriptName));
            sb.Append(");");
            sb.Append(Environment.NewLine);
            sb.Append("};");

            if (page == null)
                return;

            ScriptManager currScriptManager = ScriptManager.GetCurrent(page);
            if (currScriptManager == null)
                return;

            ScriptManager.RegisterStartupScript(
                page,
                typeof(System.Web.UI.Page),
                String.Concat(String.Format(generatedScriptName, scriptName), "_callback"),
                sb.ToString(),
                true
                );
        }

        public static T FindFirstContainerOfType<T>(Control source) where T : Control
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var control = source as T;
            if (control != null)
                return control;

            return FindFirstContainerOfType<T>(source.Parent);
        }
        public static ContextInfo FindContextInfo(Control source, string controlId)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (string.IsNullOrEmpty(controlId))
                return null;

            Control nc = source;
            Control control = null;

            while (control == null && nc != null)
            {
                nc = nc.NamingContainer;

                if (nc != null)
                    control = nc.FindControl(controlId);
            }

            return control as ContextInfo;
        }

        [Obsolete("Use UITools.AddScript instead")]
        public static void AddScriptWithHttpContext(string scriptPath)
        {
            AddScript(scriptPath);
        }

        /// <summary>
        /// Adds a script reference to ScriptManager that will be rendered to the html.
        /// </summary>
        /// <param name="scriptPath">Path of the script. Can be a skin-relative path.</param>
        /// <param name="control">Source control, optional. It will be used to find the current CacheablePortlet and the script reference to it too.</param>
        public static void AddScript(string scriptPath, Control control = null)
        {
            // Take care of folders
            if (Tools.RecurseFilesInVirtualPath(SkinManager.Resolve(scriptPath), true, p => AddScript(p, control)))
                return;

            var currScriptManager = GetScriptManager();

            if (currScriptManager == null)
                throw new Exception("The current page does not contain a script manager.");

            if (currScriptManager is SNScriptManager)
            {
                // use SNScriptManager's SmartLoader if present
                var smartLoader = ((SNScriptManager)currScriptManager).SmartLoader;
                smartLoader.AddScript(scriptPath);
            }
            else
            {
                // fallback to ASP.NET ScriptManager
                var scriptReference = new ScriptReference { Path = scriptPath, NotifyScriptLoaded = true };
                currScriptManager.Scripts.Add(scriptReference);
            }

            // Add script reference to cacheable portlet if possible, to 
            // be able to add references even if the portlet html is cached.
            var cb = GetCacheablePortlet(control);
            if (cb != null)
                cb.AddScript(scriptPath);
        }

        /// <summary>
        /// Adds a CSS link to the given header
        /// </summary>
        /// <param name="header">Page header</param>
        /// <param name="cssPath">Path of CSS file</param>
        /// <param name="control">Source control, optional. It will be used to find the current CacheablePortlet and the script reference to it too.</param>
        public static void AddStyleSheetToHeader(Control header, string cssPath, Control control = null)
        {
            AddStyleSheetToHeader(header, cssPath, 0, control);
        }

        /// <summary>
        /// Adds a CSS link to the given header using the given order. If a link with the given order already exists new link is added right after.
        /// </summary>
        /// <param name="header">Page header</param>
        /// <param name="cssPath">Path of CSS file</param>
        /// <param name="order">Desired order of CSS link</param>
        /// <param name="control">Source control, optional. It will be used to find the current CacheablePortlet and the script reference to it too.</param>
        public static void AddStyleSheetToHeader(Control header, string cssPath, int order, Control control = null)
        {
            AddStyleSheetToHeader(header, cssPath, order, "stylesheet", "text/css", "all", string.Empty, control: control);
        }

        /// <summary>
        /// Adds a CSS link to the given header using the given order and parameters. If a link with the given order already exists new link is added right after.
        /// </summary>
        /// <param name="header">Page header</param>
        /// <param name="cssPath">Path of CSS file</param>
        /// <param name="order">Desired order of CSS link</param>
        /// <param name="allowBundlingIfEnabled"></param>
        /// <param name="control">Source control, optional. It will be used to find the current CacheablePortlet and the script reference to it too.</param>
        public static void AddStyleSheetToHeader(Control header, string cssPath, int order, string rel, string type, string media, string title, bool allowBundlingIfEnabled = true, Control control = null)
        {
            if (header == null)
                return;

            if (string.IsNullOrEmpty(cssPath))
                return;

            // Take care of folders
            if (Tools.RecurseFilesInVirtualPath(SkinManager.Resolve(cssPath), true, p => AddStyleSheetToHeader(header, p, order, rel, type, media, title, allowBundlingIfEnabled, control)))
                return;

            var resolvedPath = SkinManager.Resolve(cssPath);

            if (allowBundlingIfEnabled && rel == "stylesheet" && type == "text/css" && PortalContext.Current.BundleOptions.AllowCssBundling)
            {
                if (!string.IsNullOrEmpty(title))
                    throw new Exception("The title attribute on link tags is not supported when CSS bundling is enabled.");

                PortalContext.Current.BundleOptions.EnableCssBundling(header);

                // If this is CSS stylesheet and bundling is enabled, add it to the bundle

                // Find the bundle object for the current media
                var bundle = PortalContext.Current.BundleOptions.CssBundles.SingleOrDefault(x => x.Media == media);

                if (bundle == null)
                {
                    bundle = new CssBundle()
                    {
                        Media = media,
                    };
                    PortalContext.Current.BundleOptions.CssBundles.Add(bundle);
                }

                // Add the current resolved path to the bundle
                if (PortalBundleOptions.CssIsBlacklisted(resolvedPath))
                    bundle.AddPostponedPath(resolvedPath);
                else
                    bundle.AddPath(resolvedPath, order);
            }
            else
            {
                // If bundling is disabled, fallback to the old behaviour

                var cssLink = new HtmlLink();
                cssLink.ID = "cssLink_" + resolvedPath.GetHashCode().ToString();

                // link already added to header
                if (header.FindControl(cssLink.ID) != null)
                    return;

                cssLink.Href = resolvedPath;
                cssLink.Attributes["rel"] = rel;
                cssLink.Attributes["type"] = type;
                cssLink.Attributes["media"] = media;
                cssLink.Attributes["title"] = title;
                cssLink.Attributes["cssorder"] = order.ToString();

                // find next control with higher order
                var index = -1;
                bool found = false;
                foreach (Control headerControl in header.Controls)
                {
                    index++;

                    var link = headerControl as HtmlLink;
                    if (link == null)
                        continue;

                    var orderStr = link.Attributes["cssorder"];
                    if (string.IsNullOrEmpty(orderStr))
                        continue;

                    int linkOrder = Int32.MinValue;
                    if (Int32.TryParse(orderStr, out linkOrder) && linkOrder > order)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    // add link right before higher order link
                    header.Controls.AddAt(index, cssLink);
                }
                else
                {
                    // add link at end of header's controlcollection
                    header.Controls.Add(cssLink);
                }
            }

            // Add stylesheet reference to cacheable portlet if possible, to 
            // be able to add references even if the portlet html is cached.
            var cb = GetCacheablePortlet(control);
            if (cb != null)
            {
                cb.AddStyleSheet(new StyleSheetReference
                {
                    CssPath = cssPath,
                    Media = media,
                    Order = order,
                    Rel = rel,
                    Title = title,
                    Type = type,
                    AllowBundlingIfEnabled = allowBundlingIfEnabled
                });
            }
        }

        public static System.Web.UI.Page GetPage()
        {
            HttpContext currHttpCtx = HttpContext.Current;
            if (currHttpCtx == null) return null;
            IHttpHandler currentHandler = currHttpCtx.CurrentHandler;
            return currentHandler as System.Web.UI.Page;
        }

        public static string GetPageModeClass()
        {
            return GetPageModeClass(null);
        }

        public static string GetPageModeClass(string prefix)
        {
            var page = GetPage();
            if (page != null)
            {
                try
                {
                    var wpm = WebPartManager.GetCurrentWebPartManager(page);
                    if (wpm != null)
                        return (string.IsNullOrEmpty(prefix) ? "sn-viewmode-" : prefix) + wpm.DisplayMode.Name.ToLower();
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }
            }

            return string.Empty;
        }

        public static Control GetHeader()
        {
            System.Web.UI.Page currentPage = GetPage();
            return currentPage == null ? null : currentPage.Header;
        }

        public static ScriptManager GetScriptManager()
        {
            var currentPage = GetPage();
            return currentPage == null ? null : ScriptManager.GetCurrent(currentPage);
        }

        public static SNScriptManager GetSNScriptManager()
        {
            var currentPage = GetPage();
            return SNScriptManager.GetCurrent(currentPage) as SNScriptManager;
        }

        public static void AddPickerCss()
        {
            var header = GetHeader();
            AddStyleSheetToHeader(header, ClientScriptConfigurations.jQueryCustomUICssPath);
            AddStyleSheetToHeader(header, ClientScriptConfigurations.jQueryGridCSSPath);
            AddStyleSheetToHeader(header, ClientScriptConfigurations.IconsCssPath);
            AddStyleSheetToHeader(header, ClientScriptConfigurations.jQueryTreeThemePath);
            AddStyleSheetToHeader(header, ClientScriptConfigurations.jQueryUIWidgetCSSPath);
            AddStyleSheetToHeader(header, ClientScriptConfigurations.SNWidgetsCss, 100);
        }

        public static List<string> GetGetContentPickerRootPathList(string path)
        {
            var pathList = new List<string>();
            var contentHead = NodeHead.Get(path);
            var parentPath = string.Empty;

            // add the highest reachable parent
            while (contentHead != null)
            {
                var parent = NodeHead.Get(contentHead.ParentId);

                if (parent == null || !SecurityHandler.HasPermission(parent, PermissionType.See))
                {
                    parentPath = contentHead.Path;
                    break;
                }

                contentHead = parent;
            }

            if (!string.IsNullOrEmpty(parentPath) && !pathList.Contains(parentPath))
                pathList.Add(parentPath);

            // add site path
            //var site = PortalContext.GetSiteByNodePath(path);
            //if (site != null && !pathList.Contains(site.Path))
            //    pathList.Add(site.Path);

            // add root
            if (!pathList.Contains(Repository.RootPath))
                pathList.Add(Repository.RootPath);

            return pathList;
        }

        public static string GetGetContentPickerRootPathString(string path)
        {
            var rootPaths = GetGetContentPickerRootPathList(path);
            
            // in case the only path is the /Root, return null
            return (rootPaths.Count > 1 || (rootPaths.Count == 1 && rootPaths.First() != Repository.RootPath))
                ? "[" + string.Join(", ", rootPaths.Select(rp => "'" + rp + "'")) + "]"
                : "null";
        }

        public static string GetUrlWithParameters(string baseUri, string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
                return baseUri;

            if (parameters.StartsWith("?") || parameters.StartsWith("&"))
                parameters = parameters.Remove(0, 1);

            if (string.IsNullOrEmpty(baseUri))
                return string.Concat("?", parameters);

            if (baseUri.Contains("?"))
                baseUri += "&";
            else
                baseUri += "?";

            return string.Concat(baseUri, parameters);
        }

        public static string GetAvatarUrl(int? width = null, int? height = null)
        {
            return GetAvatarUrl(User.Current as User, width, height);
        }

        public static string GetAvatarUrl(Node node, int? width = null, int? height = null)
        {
            var group = node as Group;
            var user = node as User;

            if (group != null)
            {
                var url = SkinManager.Resolve("$skin/images/default_groupavatar.png");
                url = AddWidthHeightParam(url, width, height);
                return url;
            }
            if (user != null)
            {
                var avatarUrl = user.AvatarUrl;
                var url = string.IsNullOrEmpty(avatarUrl) ? SkinManager.Resolve("$skin/images/default_avatar.png") : avatarUrl;
                url = AddWidthHeightParam(url, width, height);
                return url;
            }
            return string.Empty;
        }

        public static Node GetReferenceElevated(Content content, string fieldName)
        {
            if (content == null)
                return null;

            using (new SystemAccount())
            {
                return content[fieldName] as Node;
            }
        }

        public static IEnumerable<Node> GetReferencesElevated(Content content, string fieldName)
        {
            if (content == null)
                return null;

            using (new SystemAccount())
            {
                return content[fieldName] as IEnumerable<Node>;
            }
        }

        private static string AddWidthHeightParam(string url, int? width, int? height)
        {
            if (width.HasValue)
            {
                url += url.Contains("?") ? "&" : "?";
                url += "width=" + width.Value.ToString();
            }
            if (height.HasValue)
            {
                url += url.Contains("?") ? "&" : "?";
                url += "height=" + height.Value.ToString();
            }
            return url;
        }

        private static CacheablePortlet GetCacheablePortlet(Control control)
        {
            var cc = control;
            while (cc != null)
            {
                var cb = cc as CacheablePortlet;
                if (cb != null)
                    return cb;

                cc = cc.Parent;
            }

            return null;
        }

        #region Nested type: ClientScriptConfigurations

        public static class ClientScriptConfigurations
        {
            public static string MSAjaxPath = GetScriptSetting("MSAjaxPath");
            public static string SNWebdavPath = GetScriptSetting("SNWebdavPath");
            public static string SNReferenceGridPath = GetScriptSetting("SNReferenceGridPath");
            public static string SNBinaryFieldControlPath = GetScriptSetting("SNBinaryFieldControlPath");
            public static string SNUtilsPath = GetScriptSetting("SNUtilsPath");
            public static string SNPickerPath = GetScriptSetting("SNPickerPath");
            public static string SNWallPath = "$skin/scripts/sn/SN.Wall.js";
            public static string SNQueryBuilderJSPath = "$skin/scripts/sn/SN.QueryBuilder.js";
            public static string SNQueryBuilderCSSPath = "$skin/styles/SN.QueryBuilder.css";
            public static string SNPortalRemoteControlPath = GetScriptSetting("SNPortalRemoteControlPath");
            public static string SNListGridPath = GetScriptSetting("SNListGridPath");
            public static string TinyMCEPath = GetScriptSetting("TinyMCEPath");
            public static string jQueryPath = GetScriptSetting("jQueryPath");
            public static string JQueryUIPath = GetScriptSetting("jQueryUIPath");
            public static string JQueryUIFolderPath = RepositoryPath.GetParentPath(JQueryUIPath);
            public static string jQueryTreePath = GetScriptSetting("jQueryTreePath");
            public static string jQueryGridPath = GetScriptSetting("jQueryGridPath");
            public static string jQueryTreeCheckboxPluginPath = GetScriptSetting("jQueryTreeCheckboxPluginPath");

            // themes
            public static string IconsCssPath = GetScriptSetting("IconsCssPath");
            public static string jQueryCustomUICssPath = GetScriptSetting("jQueryCustomUICssPath");
            public static string jQueryTreeThemePath = GetScriptSetting("jQueryTreeThemePath");
            public static string jQueryGridCSSPath = GetScriptSetting("jQueryGridCSSPath");
            public static string SNWidgetsCss = GetScriptSetting("SNWidgetsCss");
            public static string jQueryUIWidgetCSSPath = GetScriptSetting("jQueryUIWidgetCSSPath");
        }

        #endregion

        #region configuration settings

        public static string ScriptMode
        {
            get
            {
                var configName = "ScriptMode";
                var result = GetScriptSetting(configName);
                if (result != "Debug" && result != "Release")
                    throw new ConfigurationErrorsException(
                        string.Format(
                            "The {1} property has been set to '{0}' in the appSettings section, which is invalid. The valid values are 'Release' and 'Debug'.",
                            result, configName));
                return result;
            }
        }

        private static string GetScriptSetting(string configName)
        {
            string result = ConfigurationManager.AppSettings[configName];
            if (result == null)
                throw new ConfigurationErrorsException(
                    string.Format(
                        "The {1} property is not given in the appSettings section.",
                        result, configName));
            return result;
        }

        #endregion

        public static class ControlChars
        {
            public const char Back = '\b';
            public const char Cr = '\r';
            public const string CrLf = "\r\n";
            public const char FormFeed = '\f';
            public const char Lf = '\n';
            public const string NewLine = "\r\n";
            public const char NullChar = '\0';
            public const char Quote = '"';
            public const char Tab = '\t';
            public const char VerticalTab = '\v';
        }

        public static string GetVersionText(GenericContent node)
        {
            if (node == null)
                return string.Empty;

            var result = string.Empty;

            switch (node.Version.Status)
            {
                case VersionStatus.Approved:
                    result = HttpContext.GetGlobalResourceObject("PortalRemoteControl", "Public") as string;
                    break;
                case VersionStatus.Draft:
                    result = HttpContext.GetGlobalResourceObject("PortalRemoteControl", "Draft") as string;
                    break;
                case VersionStatus.Locked:
                    // TODO: snippet comes from the old prc
                    result =
                        string.Format(
                            HttpContext.GetGlobalResourceObject("PortalRemoteControl", "CheckedOutBy") as string,
                            node.Lock.LockedBy.Name);
                    break;
                case VersionStatus.Pending:
                    result = HttpContext.GetGlobalResourceObject("PortalRemoteControl", "Approving") as string;
                    break;
                case VersionStatus.Rejected:
                    result = HttpContext.GetGlobalResourceObject("PortalRemoteControl", "Reject") as string;
                    break;
                default:
                    break;

            }
            return node.VersioningMode == VersioningType.None ? result : string.Concat(node.Version.VersionString, " ", result);

        }

        public static string GetVersioningModeText(GenericContent node)
        {
            if (node == null)
                return string.Empty;

            var modeString = HttpContext.GetGlobalResourceObject("Portal", node.VersioningMode.ToString()) as string;

            return string.IsNullOrEmpty(modeString) ? node.VersioningMode.ToString() : modeString;
        }

        /// <summary>
        /// Gets the user friendly string representation of a date relative to the current time
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetFriendlyDate(DateTime date)
        {
            //- 53 seconds ago
            //- 15 minutes ago
            //- 21 hours ago
            //- Yesterday at 3:43pm
            //- Sunday at 2:12pm
            //- May 25 at 1:23pm
            //- Friday, December 27, 2010 at 5:41pm

            var shortTime = date.ToShortTimeString();   // 5:41 PM

            string secondText = SenseNetResourceManager.Current.GetString("Portal", "SecondMessage");
            string minuteText = SenseNetResourceManager.Current.GetString("Portal", "MinutesMessage");
            string hoursext = SenseNetResourceManager.Current.GetString("Portal", "HoursMessage");
            string yesterdayAt = SenseNetResourceManager.Current.GetString("Portal", "YesterdayAt");
            string atString = SenseNetResourceManager.Current.GetString("Portal", "At");

            var ago = DateTime.UtcNow - date;
            if (ago < new TimeSpan(0, 1, 0))
                return ago.Seconds == 1 ?
                    "1 " + secondText :
                    string.Format("{0} " + secondText, ago.Seconds);
            if (ago < new TimeSpan(1, 0, 0))
                return ago.Minutes == 1 ?
                    "1 " + minuteText :
                    string.Format("{0} " + minuteText, ago.Minutes);
            if (ago < new TimeSpan(1, 0, 0, 0))
                return ago.Hours == 1 ?
                    "1 " + hoursext :
                    string.Format("{0} " + hoursext, ago.Hours);
            if (ago < new TimeSpan(2, 0, 0, 0))
                return string.Format(yesterdayAt + " {0}", shortTime);
            if (ago < new TimeSpan(7, 0, 0, 0))
                return string.Format("{0}" + atString + " {1}", date.ToString("dddd"), shortTime);
            if (date.Year == DateTime.UtcNow.Year)
                return string.Format("{0}" + atString + " {1}", date.ToString("m"), shortTime);

            return string.Format("{0}" + atString + " {1}", date.ToLongDateString(), shortTime);
        }

        public static string GetFriendlyDate(Content content, string fieldName)
        {
            if (content == null || string.IsNullOrEmpty(fieldName))
                return string.Empty;

            var dt = string.Empty;

            try
            {
                return UITools.GetFriendlyDate(Convert.ToDateTime(content[fieldName]));
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return dt;
        }

        public static string GetFieldNameClass(Control fieldControl)
        {
            var fieldCtrl = fieldControl as FieldControl;
            return fieldCtrl == null ? string.Empty : fieldCtrl.FieldName.Replace("#", "lf-");
        }

        public static string GetClassForField(object content, string fieldName)
        {
            var c = content as Content;
            if (c == null)
                return string.Empty;

            return fieldName == null 
                ? string.Empty 
                : c.Id + "-" + fieldName.Replace("#", "lf-").Replace('.', '-');
        }

        public static int GetNullableInteger(int? value, int defaultValue)
        {
            return value.HasValue ? value.Value : defaultValue;
        }

        public static decimal GetNullableDecimal(decimal? value, decimal defaultValue)
        {
            return value.HasValue ? value.Value : defaultValue;
        }

        public static bool GetNullableBool(bool? value, bool defaultValue)
        {
            return value.HasValue ? value.Value : defaultValue;
        }

        public static string GetNumericFormat(Control fieldControl)
        {
            var fs = fieldControl is FieldControl ? ((FieldControl)fieldControl).Field.FieldSetting : null;
            if (fs == null)
                return "n";

            var intFs = fs as IntegerFieldSetting;
            if (intFs != null)
            {
                return intFs.ShowAsPercentage.HasValue && intFs.ShowAsPercentage.Value ? "p0" : "n0";
            }

            var currFs = fs as CurrencyFieldSetting;
            if (currFs != null)
            {
                return "C" + Math.Min(!currFs.Digits.HasValue ? 0 : currFs.Digits.Value, 29);
            }

            var numberFs = fs as NumberFieldSetting;
            if (numberFs != null)
            {
                return numberFs.ShowAsPercentage.HasValue && numberFs.ShowAsPercentage.Value ? "p0" : "n" + Math.Min(!numberFs.Digits.HasValue ? 0 : numberFs.Digits.Value, 29);
            }

            return "n";
        }

        public static string GetNumericStep(Control fieldControl)
        {
            var fs = fieldControl is FieldControl ? ((FieldControl)fieldControl).Field.FieldSetting : null;
            if (fs == null)
                return "n";

            var intFs = fs as IntegerFieldSetting;
            if (intFs != null)
            {
                return intFs.ShowAsPercentage.HasValue && intFs.ShowAsPercentage.Value ? "0.01" : "1";
            }

            var currFs = fs as CurrencyFieldSetting;
            if (currFs != null)
            {
                return currFs.Digits.HasValue ? "0." + new string('0', currFs.Digits.Value - 1) + "1" : "1";
            }

            var numberFs = fs as NumberFieldSetting;
            if (numberFs != null)
            {
                var digCount = numberFs.Digits.HasValue ? numberFs.Digits.Value : 2;

                return numberFs.Digits.HasValue ? "0." + new string('0', Math.Max(0, digCount - 1)) + "1" : "1";
            }

            return "1";
        }

        public static string GetCurrencyCulture(Control fieldControl)
        {
            var fs = fieldControl is FieldControl ? ((FieldControl)fieldControl).Field.FieldSetting as CurrencyFieldSetting : null;
            if (fs == null || string.IsNullOrEmpty(fs.Format))
                return CultureInfo.CurrentCulture.Name;

            var cultForField = CultureInfo.GetCultureInfo(fs.Format);

            return cultForField.Name;
        }

        /// <summary>
        /// HTML encodes a text to make it safe (xss-free) for displaying in markup. The only exception is
        /// if the text is a recognisable Sense/Net resource editor markup.
        /// </summary>
        /// <param name="text">A text to make HTML-safe</param>
        /// <returns>An HTML encoded text.</returns>
        public static string GetSafeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // resource editor markup is always safe as it contains only our markup (a link tag) and a sanitized display text
            if (PortalContext.Current.IsResourceEditorAllowed && SenseNetResourceManager.IsEditorMarkup(text))
                return text;

            // encode the text to make it safe for displaying
            return HttpUtility.HtmlEncode(text);
        }
    }
}
