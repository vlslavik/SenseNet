using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.i18n;
using System.Globalization;
using System.Web.Script.Serialization;
using SenseNet.Search;
using System.Xml;
using SenseNet.Portal.UI;
using SenseNet.Diagnostics;
using Newtonsoft.Json;

namespace SenseNet.Portal
{
    public class ResourceEditorController : Controller
    {
        internal class ResourceKeyData
        {
            public string Name { get; set; }
            public List<ResourceData> Datas { get; set; }
        }

        internal class ResourceData
        {
            public string Lang { get; set; }
            public string Value { get; set; }
        }

        //===================================================================== Public interface

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetStringResources(string classname, string name, string rnd)
        {
            AssertPermission();

            var resources = GetResources(classname, name);

            return Json(resources.ToArray(), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetAllResourcesForClass(string classname, string rnd)
        {
            AssertPermission();

            var langs = GetCurrentSupportedLanguageNames().OrderBy(x => x);
            var resourceKeys = new Dictionary<string, KeyValuePair<string, string>[]>();

            foreach (var name in SenseNetResourceManager.Current.GetClassKeys(classname).OrderBy(x => x))
            {
                var resources = new Dictionary<string, string>();
                foreach (var lang in langs)
                {
                    try
                    {
                        var cultureInfo = new CultureInfo(lang);
                        var s = SenseNetResourceManager.Current.GetObjectOrNull(classname, name, cultureInfo) as string;
                        resources.Add(lang, s);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteException(ex);
                        resources.Add(lang, "ERROR: " + ex.Message);
                    }
                }
                resourceKeys.Add(name, resources.ToArray());
            }

            return Json(resourceKeys.ToArray(), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        [ValidateInput(false)]
        public ActionResult SaveResource(string classname, string name, string resources, string rnd)
        {
            AssertPermission();

            var ser = new JavaScriptSerializer();
            var resourcesData = ser.Deserialize<List<ResourceData>>(resources);

            SaveResource(classname, name, resourcesData);

            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult SaveResources(string classname, string resourceKeyDataStr)
        {
            AssertPermission();

            var ser = new JavaScriptSerializer();
            var resourceKeyDatas = ser.Deserialize<List<ResourceKeyData>>(resourceKeyDataStr);

            var res = GetResourceForClassName(classname);

            if (res != null)
            {
                var xml = new XmlDocument();
                using (var stream = res.Binary.GetStream())
                {
                    xml.Load(stream);
                }

                foreach (var resourceKeyData in resourceKeyDatas)
                {
                    AddOrModifyEntry(xml, classname, resourceKeyData.Name, resourceKeyData.Datas);
                }

                using (new SystemAccount())
                {
                    using (var stream = new System.IO.MemoryStream())
                    {
                        xml.Save(stream);
                        res.Binary.SetStream(stream);
                        res.Save();
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return null;
        }

        //===================================================================== Public helpers

        internal static Dictionary<string, string> GetResources(string classname, string name)
        {
            var langs = GetCurrentSupportedLanguageNames();
            var resources = new Dictionary<string, string>();
            foreach (var lang in langs)
            {
                try
                {
                    var cultureInfo = new CultureInfo(lang);
                    var s = SenseNetResourceManager.Current.GetObjectOrNull(classname, name, cultureInfo, false, false) as string;
                    resources.Add(lang, s);
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    resources.Add(lang, "ERROR: " + ex.Message);
                }
            }
            return resources;
        }

        internal static void SaveResource(string classname, string name, List<ResourceData> resourcesData)
        {
            var res = GetResourceForClassName(classname);

            if (res != null)
            {
                // parse xml
                var xml = new XmlDocument();
                using (var stream = res.Binary.GetStream())
                {
                    xml.Load(stream);
                }

                AddOrModifyEntry(xml, classname, name, resourcesData);

                using (new SystemAccount())
                {
                    using (var stream = new System.IO.MemoryStream())
                    {
                        xml.Save(stream);
                        res.Binary.SetStream(stream);
                        res.Save();
                    }
                }
            }
            else
            {
                // create new resource file for this classname
                CreateNewResourceFile(classname, name, resourcesData);
            }
        }

        public static IEnumerable<string> GetCurrentSupportedLanguageNames()
        {
            var languages = Portal.Site.GetAllLanguages()
                .Select(x => x.Value).ToList()
                .Concat(SenseNetResourceManager.Current.GetCultures().Select(x => x.Name))
                .Distinct()
                .ToList();

            if (PortalContext.Current.Site.EnableClientBasedCulture && !languages.Contains(CultureInfo.CurrentUICulture.Name))
            {
                //If the culture coming from the client is not in the list, but its
                //parent is already added, skip it. Only add the language if the
                //whole family is missing from the list.
                if (!languages.Contains(CultureInfo.CurrentUICulture.Parent.Name))
                    languages.Add(CultureInfo.CurrentUICulture.Name);
            }

            return languages;
        }

        private static string GetOptionLocalizedText(IEnumerable<ChoiceOption> options, string langKey)
        {
            //If there is no option for the specified language (this may happen if
            //the language key is missing from the option list of the Language field
            //in the Site CTD), use the language code itself.
            var choiceOption = options.FirstOrDefault(o => o.Value == langKey);
            var text = choiceOption == null ? langKey : choiceOption.StoredText;

            // choiceOption.Text contains the localized value, but since we are in resource editing mode, it will be a generated link to edit it
            // so let's manually resolve the localized text, because we don't want that link in the resource editor itself
            string className;
            string name;
            if (SenseNetResourceManager.ParseResourceKey(text, out className, out name))
            {
                var localizedText = SenseNetResourceManager.Current.GetObjectOrNull(className, name, CultureInfo.CurrentUICulture, false) as string;
                return String.IsNullOrEmpty(localizedText) ? text : localizedText;
            }
            return text;
        }

        public static void InitEditorScript(System.Web.UI.Page page)
        {
            // we init the resource editor with languages (languages keys like 'en' and language text values like 'English')
            // the values and string resources for languages are stored on the site CTD, 'Language' fieldsetting
            var languages = GetCurrentSupportedLanguageNames();
            var siteContent = SenseNet.ContentRepository.Content.Create(PortalContext.Current.Site);
            var languageFieldSetting = siteContent.Fields["Language"].FieldSetting as ChoiceFieldSetting;
            var options = languageFieldSetting.Options;
            var languageValues = languages.Select(langKey => new { Key = langKey, Value = GetOptionLocalizedText(options, langKey) });
            var languageValuesJson = JsonConvert.SerializeObject(languageValues);

            UITools.AddScript("$skin/scripts/sn/SN.ResourceEditor.js");
            UITools.RegisterStartupScript("resourceEditorInit", "SN.ResourceEditor.init(" + languageValuesJson + ");", page);
        }

        //===================================================================== Resource xml handling

        private static void AddOrModifyEntry(XmlDocument xml, string classname, string name, IEnumerable<ResourceData> resourcesData)
        {
            foreach (var resourceData in resourcesData)
            {
                // search resource in xml
                var reselement = xml.SelectSingleNode("/Resources/ResourceClass[@name='" + classname + "']/Languages/Language[@cultureName='" + resourceData.Lang + "']/data[@name='" + name + "']/value");
                if (reselement != null)
                {
                    reselement.InnerText = resourceData.Value;
                }
                else if (!string.IsNullOrEmpty(resourceData.Value))
                {
                    CreateResDataUnderClass(xml, classname, name, resourceData);
                }
            }
        }

        private static void CreateNewResourceFile(string classname, string name, List<ResourceData> resourcesData)
        {
            var parentNode = Node.LoadNode("/Root/Localization");
            if (parentNode == null)
                return;

            var res = new Resource(parentNode);
            res.Name = classname + "Resources.xml";

            var xml = new XmlDocument();
            var root = xml.CreateElement("Resources");
            xml.AppendChild(root);

            var classelement = xml.CreateElement("ResourceClass");
            var classnameattr = xml.CreateAttribute("name");
            classnameattr.Value = classname;
            classelement.Attributes.Append(classnameattr);
            root.AppendChild(classelement);

            var languageselement = xml.CreateElement("Languages");
            classelement.AppendChild(languageselement);

            foreach (var resourceData in resourcesData)
            {
                CreateResDataUnderClass(xml, classname, name, resourceData);
            }

            using (new SystemAccount())
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    xml.Save(stream);
                    res.Binary.SetStream(stream);
                    res.Save();
                }
            }
        }

        private static void CreateResDataUnderClass(XmlDocument xml, string classname, string name, ResourceData resourceData)
        {
            // create new element
            var langelement = xml.SelectSingleNode("/Resources/ResourceClass[@name='" + classname + "']/Languages/Language[@cultureName='" + resourceData.Lang + "']");
            if (langelement == null)
            {
                // create language element
                var langparent = xml.SelectSingleNode("/Resources/ResourceClass[@name='" + classname + "']/Languages");
                if (langparent == null)
                    return;

                langelement = xml.CreateElement("Language");
                var cultureNameAttr = xml.CreateAttribute("cultureName");
                cultureNameAttr.Value = resourceData.Lang;
                langelement.Attributes.Append(cultureNameAttr);
                langparent.AppendChild(langelement);
            }

            // create data element under language element
            var dataelement = xml.CreateElement("data");
            var nameAttr = xml.CreateAttribute("name");
            nameAttr.Value = name;
            dataelement.Attributes.Append(nameAttr);
            langelement.AppendChild(dataelement);

            // create value element under data element
            var valueelement = xml.CreateElement("value");
            valueelement.InnerText = resourceData.Value;
            dataelement.AppendChild(valueelement);
        }

        private static Resource GetResourceForClassName(string classname)
        {
            var res = Node.LoadNode("/Root/Localization/" + classname + "Resources.xml") as Resource;
            if (res != null)
                return res;

            res = Node.LoadNode("/Root/Localization/" + classname + ".xml") as Resource;
            if (res != null)
                return res;

            var resources = ContentQuery.Query("+Type:Resource", new QuerySettings { EnableAutofilters = FilterStatus.Disabled }).Nodes.OrderBy(i => i.Index);
            foreach (Resource resc in resources)
            {
                var xml = new XmlDocument();
                xml.Load(resc.Binary.GetStream());
                var classelement = xml.SelectSingleNode("/Resources/ResourceClass[@name='" + classname + "']");
                if (classelement != null)
                    return resc;
            }

            return null;
        }


        //===================================================================== Helper methods

        private static readonly string PlaceholderPath = "/Root/System/PermissionPlaceholders/ResourceEditor-mvc";

        private static void AssertPermission()
        {
            if (!HasPermission())
                throw new SenseNetSecurityException("Access denied for " + PlaceholderPath);
        }

        private static bool HasPermission()
        {
            var permissionContent = Node.LoadNode(PlaceholderPath);
            var nopermission = (permissionContent == null || !permissionContent.Security.HasPermission(PermissionType.RunApplication));
            return !nopermission;
        }
    }
}
