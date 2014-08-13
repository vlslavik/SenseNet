using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
//using SenseNet.Portal;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using sn = SenseNet.ContentRepository.Schema;
using SNCR = SenseNet.ContentRepository;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Schema;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using System.Web.Script.Serialization;
using System.Globalization;

// MVC controller to host the central REST api of the Repository.
// It is going to obsolete and replace the current, emulated webservice approach.
namespace SenseNet.Services.ContentStore
{
    [HandleError]
    public class ContentStoreController : Controller
    {
        //[AcceptVerbs(HttpVerbs.Get)]
        //public ActionResult GetAvailableTypes(string node)
        //{
        //}

        //[AcceptVerbs(HttpVerbs.Get)]
        //public ActionResult GetAvailableTemplates(string contentType)
        //{
        //}

        //hack this was done around 20h in the evening, with a slight fever
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult CreateContent(string node, string contentName, string contentType, string templateName, string back)
        {
            AssertPermission();

            node = HttpUtility.UrlDecode(node);
            contentName = HttpUtility.UrlDecode(contentName);
            contentType = HttpUtility.UrlDecode(contentType);
            templateName = HttpUtility.UrlDecode(templateName);
            back = HttpUtility.UrlDecode(back);

            if (string.IsNullOrEmpty(contentName))
                contentName = !string.IsNullOrEmpty(templateName) ? templateName : contentType;

            var parent = Node.LoadNode(node);
            if (parent == null)
                return this.Redirect(back);

            //var template = SNCR.ContentTemplateResolver.Instance.GetNamedTemplate(contentType, templateName);
            var template = ContentTemplate.GetNamedTemplate(contentType, templateName);
            SNCR.Content newContent = null;

            if (template != null)
                newContent = ContentTemplate.CreateTemplated(parent, template, contentName);
            //else
            //    SNCR.Content.CreateNew(contentType, parent, contentName, null);

            try
            {
                if (newContent != null)
                    newContent.Save();
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult CreateContentByTemplate(string node, string contentName, string templatePath, string back)
        {
            AssertPermission();

            node = HttpUtility.UrlDecode(node);
            contentName = HttpUtility.UrlDecode(contentName);
            templatePath = HttpUtility.UrlDecode(templatePath);
            back = HttpUtility.UrlDecode(back);

            var parent = string.IsNullOrEmpty(node) ? null : Node.LoadNode(node);
            if (parent == null)
                return this.Redirect(back);

            var template = string.IsNullOrEmpty(templatePath) ? null : Node.LoadNode(templatePath);
            if (template == null)
                return this.Redirect(back);

            if (string.IsNullOrEmpty(contentName))
                contentName = template.Name;

            try
            {
                var newContent = ContentTemplate.CreateTemplated(parent, template, contentName);
                newContent.Save();
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult CreateContentByType(int parentId, string contentName, string contentType)
        {
            AssertPermission();

            contentName = HttpUtility.UrlDecode(contentName);
            contentType = HttpUtility.UrlDecode(contentType);

            if (string.IsNullOrEmpty(contentName))
                contentName = contentType;

            try
            {
                var parent = Node.LoadNode(parentId);
                if (parent == null)
                    return Json("Parent is missing", JsonRequestBehavior.AllowGet);

                var newContent = SNCR.Content.CreateNew(contentType, parent, contentName);

                if (newContent != null)
                    newContent.Save();
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);

                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }

            return Json("OK", JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetFields(string node, string scenario)
        {
            AssertPermission();

            node = HttpUtility.UrlDecode(node);
            scenario = HttpUtility.UrlDecode(scenario);

            IEnumerable<sn.FieldSetting> fieldSettings =  null;

            var clist = Node.Load<ContentRepository.ContentList>(node);

            if (clist != null)
                fieldSettings = clist.GetAvailableFields();

            var outArray = fieldSettings.Select<sn.FieldSetting, FieldSetting>(fs => new FieldSetting(fs)).ToArray();

            return this.Json(outArray, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetItem(string node)
        {
            AssertPermission();

            node = HttpUtility.UrlDecode(node);

            var a = new ContentStoreService().GetItem(node);
            return this.Json(a, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult CopyAppLocal(string nodepath, string apppath, string back)
        {
            AssertPermission();

            nodepath = HttpUtility.UrlDecode(nodepath);
            apppath = HttpUtility.UrlDecode(apppath);
            back = HttpUtility.UrlDecode(back);

            var targetAppPath = RepositoryPath.Combine(nodepath, "(apps)");
            var targetThisPath = RepositoryPath.Combine(targetAppPath, "This");

            //we don't use the system account here, the user must have create rights here
            if (!Node.Exists(targetAppPath))
            {
                var apps = new SystemFolder(Node.LoadNode(nodepath)) {Name = "(apps)"};
                apps.Save();
            }
            if (!Node.Exists(targetThisPath))
            {
                var thisFolder = new Folder(Node.LoadNode(targetAppPath)) { Name = "This" };
                thisFolder.Save();
            }

            try
            {
                Node.Copy(apppath, targetThisPath);
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        /* ===================================================================== Picker */
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetChildren(string parentPath, string contentTypes, string rnd, bool simpleContent = false)
        {
            AssertPermission();

            return GetChildrenInternal(parentPath, true, contentTypes, simpleContent);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetTreeNodeChildren(string path, string rootonly, string rnd, bool simpleContent = false)
        {
            AssertPermission();

            if (path == null)
                throw new ArgumentNullException("id");

            if (rootonly == "1")
            {
                // return requested node
                return GetNodeInternal(path, simpleContent);
            }
            else
            {
                return GetChildrenInternal(path, false, null, simpleContent);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetTreeNodeAllChildren(string path, string rootonly, string rnd, bool simpleContent = false)
        {
            AssertPermission();

            if (path == null)
                throw new ArgumentNullException("id");

            if (rootonly == "1")
            {
                // return requested node
                return GetNodeInternal(path, simpleContent);
            }
            else
            {
                return GetChildrenInternal(path, true, null, simpleContent);
            }
        }

        private ActionResult GetNodeInternal(string parentPath, bool simpleContent = false)
        {
            Node node = null;
            if (parentPath == Repository.RootPath || parentPath == Repository.ImsFolderPath)
            {
                // Elevation: Root and IMS should be accessible through this service 
                // (the user already passed the feature permission check).
                using (new SystemAccount())
                {
                    node = Node.LoadNode(parentPath);
                }
            }
            else
            {
                var head = NodeHead.Get(parentPath);
                if (head != null && SecurityHandler.HasPermission(head, PermissionType.Open))
                    node = Node.LoadNode(head);
            }
                
            if (node == null)
                throw new ArgumentNullException("parentPath");

            if (simpleContent)
            {
                return Json(new List<SimpleServiceContent> { new SimpleServiceContent(node) }.ToArray(), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new List<Content> { new Content(node, true, false, false, false, 0, 0) }.ToArray(), JsonRequestBehavior.AllowGet);
            }
        }

        private ActionResult GetChildrenInternal(string parentPath, bool includeLeafNodes, string contentTypes, bool simpleContent = false)
        {
            if (String.IsNullOrEmpty(parentPath))
                return null;

            Node parent = null;
            if (parentPath == Repository.RootPath || parentPath == Repository.ImsFolderPath)
            {
                // Elevation: Root and IMS should be accessible through this service 
                // (the user already passed the feature permission check).
                using (new SystemAccount())
                {
                    parent = Node.LoadNode(parentPath);
                }
            }
            else
            {
                var head = NodeHead.Get(parentPath);
                if (head != null && SecurityHandler.HasPermission(head, PermissionType.Open))
                    parent = Node.LoadNode(head);
            }

            return GetChildrenByNodeInternal(parent, includeLeafNodes, contentTypes, simpleContent);
        }

        private ActionResult GetChildrenByNodeInternal(Node node, bool includeLeafNodes, string contentTypes, bool simpleContent = false)
        {
            var folderParent = node as IFolder;
            if (folderParent == null)
                return null;

            var content = SenseNet.ContentRepository.Content.Create(node);
            var filter = GetContentTypesFilter(contentTypes);

            //add content type filter if needed
            if (!string.IsNullOrEmpty(filter))
                content.ChildrenDefinition.ContentQuery = ContentQuery.AddClause(content.ChildrenDefinition.ContentQuery, filter, ChainOperator.And);

            //in case of SmartFolder: do not override the settings given on the content
            if (!(folderParent is SmartFolder))
                content.ChildrenDefinition.EnableAutofilters = FilterStatus.Disabled;
            
            var children = content.Children.AsEnumerable().Where(c => c != null).Select(c => c.ContentHandler);
            if (!includeLeafNodes)
                children = children.Where(c => c is IFolder).ToList();

            //return simpleContent
            //    ? Json(children.Where(c => c != null).Select(child => new SimpleServiceContent(child)).ToArray(), JsonRequestBehavior.AllowGet)
            //    : Json(children.Where(c => c != null).Select(child => new Content(child, true, false, false, false, 0, 0)).ToArray(), JsonRequestBehavior.AllowGet);
            return Json(children.Where(c => c != null).Select(child => new SimpleServiceContent(child)).ToArray(), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult IsLuceneQuery(string rnd)
        {
            AssertPermission();

            return Json(IsLuceneQueryInternal(), JsonRequestBehavior.AllowGet);
        }

        private bool IsLuceneQueryInternal()
        {
            return StorageContext.Search.SearchEngine.GetType() == typeof(LuceneSearchEngine);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Search(string searchStr, string searchRoot, string contentTypes, string rnd, bool simpleContent = false)
        {
            AssertPermission();

            if (IsLuceneQueryInternal())
            {
                return SearchLucene(searchStr, searchRoot, contentTypes, simpleContent);
            }
            else
            {
                return SearchNodeQuery(searchStr, searchRoot, contentTypes, simpleContent);
            }
        }

        private ActionResult SearchLucene(string searchStr, string searchRoot, string contentTypes, bool simpleContent = false)
        {
            var queryStr = CreateLuceneQueryString(searchStr, searchRoot, contentTypes);
            var query = ContentQuery.CreateQuery(queryStr, new QuerySettings
                                                               {
                                                                   Sort = new List<SortInfo> { new SortInfo{ FieldName = "DisplayName" }},
                                                                   EnableAutofilters = FilterStatus.Disabled,
                                                                   EnableLifespanFilter = FilterStatus.Disabled
                                                               });

            if (simpleContent)
            {
                return Json((from n in query.Execute().Nodes
                             where n != null
                             select new SimpleServiceContent(n)).ToArray(),
                            JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json((from n in query.Execute().Nodes
                             where n != null
                             select new Content(n, true, false, false, false, 0, 0)).ToArray(),
                            JsonRequestBehavior.AllowGet);
            }
        }

        private ActionResult SearchNodeQuery(string searchStr, string searchRoot, string contentTypes, bool simpleContent = false)
        {
            if (!string.IsNullOrEmpty(searchStr))
            {
                // simple nodequery
                var query = new NodeQuery();
                query.Add(new SearchExpression(searchStr));
                var nodes = query.Execute().Nodes;

                // filter with path
                if (!string.IsNullOrEmpty(searchRoot))
                    nodes = nodes.Where(n => n.Path.StartsWith(searchRoot));

                // filter with contenttypes
                if (!string.IsNullOrEmpty(contentTypes))
                {
                    var contentTypesArr = GetContentTypes(contentTypes);
                    nodes = nodes.Where(n => contentTypesArr.Contains(n.NodeType.Name));
                }

                if (simpleContent)
                {
                    var contents = nodes.Where(n => n != null).Select(n => new SimpleServiceContent(n));
                    return Json(contents.ToArray(), JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var contents = nodes.Where(n => n != null).Select(n => new Content(n, true, false, false, false, 0, 0));
                    return Json(contents.ToArray(), JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(searchRoot) && string.IsNullOrEmpty(contentTypes))
                    return Json(null, JsonRequestBehavior.AllowGet);

                var query = new NodeQuery();
                var andExpression = new ExpressionList(ChainOperator.And);
                query.Add(andExpression);

                // filter with path
                if (!string.IsNullOrEmpty(searchRoot))
                    andExpression.Add(new StringExpression(StringAttribute.Path, StringOperator.StartsWith, searchRoot));

                // filter with contenttypes
                if (!string.IsNullOrEmpty(contentTypes))
                {
                    var contentTypesArr = GetContentTypes(contentTypes);
                    var orExpression = new ExpressionList(ChainOperator.Or);
                    foreach (var contentType in contentTypesArr)
                    {
                        orExpression.Add(new TypeExpression(NodeType.GetByName(contentType), true));
                    }
                    andExpression.Add(orExpression);
                }

                var nodes = query.Execute().Nodes;

                if (simpleContent)
                {
                    var contents = nodes.Select(n => new SimpleServiceContent(n));
                    return Json(contents.ToArray(), JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var contents = nodes.Select(n => new Content(n, true, false, false, false, 0, 0));
                    return Json(contents.ToArray(), JsonRequestBehavior.AllowGet);
                }
            }
        }

        private static bool IsLuceneSyntax(string s)
        {
            return s.Contains(":") || s.Contains("+") || s.Contains("*");
        }

        private static string CreateLuceneQueryString(string searchStr, string searchRoot, string contentTypesStr)
        {
            var queryStr = string.Empty;

            if (!string.IsNullOrEmpty(searchStr))
            {
                if (!IsLuceneSyntax(searchStr))
                {
                    // ha több szó van: _Text:<kifejezés>
                    // ha egy szó van de nem idézőjelben: _Text:<kifejezés>*
                    // ha egy szó van de idézőjelben: _Text:<kifejezés>

                    var words = searchStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1 && !searchStr.Contains('"'))
                    {
                        searchStr = string.Concat("_Text:", searchStr.TrimEnd('*'), "*");
                    }
                    else
                    {
                        searchStr = string.Concat("_Text:", searchStr);
                    }
                }

                // the given query will be AND-ed to all other query terms
                // ie.: _Text:user1 _Text:user2 
                // -> +(_Text:user1 _Text:user2) +(<ancestor and contentype queries>)
                // ie.: +_Text:user1 +_Text:user2 
                // -> +(+_Text:user1 +_Text:user2) +(<ancestor and contentype queries>)
                searchStr = string.Format("+({0})", searchStr);

                queryStr = searchStr;
            }

            if (!string.IsNullOrEmpty(searchRoot))
            {
                var pathQuery = string.Format("+InTree:\"{0}\"", searchRoot.ToLower());
                queryStr = string.Concat(queryStr, " ", pathQuery);
            }

            if (!string.IsNullOrEmpty(contentTypesStr))
            {
                queryStr = string.Format("{0} +({1})", queryStr, GetContentTypesFilter(contentTypesStr));
            }

            return queryStr;
        }
        private static IEnumerable<string> GetContentTypes(string contentTypesStr)
        {
            return contentTypesStr.Split(',');
        }

        private static string GetContentTypesFilter(string contentTypeNames)
        {
            if (string.IsNullOrEmpty(contentTypeNames))
                return string.Empty;

            var filter = string.Empty;
            
            return contentTypeNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Aggregate(filter, (current, ctName) => current + ("TypeIs:" + ctName + " ")).Trim();
        }

        //===================================================================== Overrides

        protected override RedirectResult Redirect(string url)
        {
            return string.IsNullOrEmpty(url) ? null : base.Redirect(url);
        }

        //===================================================================== Versioning

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult CheckOut(string path, string back)
        {
            AssertPermission();

            path = HttpUtility.UrlDecode(path);
            back = HttpUtility.UrlDecode(back);

            var content = ContentRepository.Content.Load(path);
            if (content == null)
                return this.Redirect(back);

            try
            {
                content.CheckOut();
            }
            catch (InvalidContentActionException ex)
            {
                WriteInvalidActionError(path, ex.Reason);
            }
            catch (SenseNetSecurityException ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult CheckIn(string path, string back)
        {
            AssertPermission();

            path = HttpUtility.UrlDecode(path);
            back = HttpUtility.UrlDecode(back);

            var content = ContentRepository.Content.Load(path);
            if (content == null)
                return this.Redirect(back);

            try
            {
                content.CheckIn();
            }
            catch (InvalidContentActionException ex)
            {
                WriteInvalidActionError(path, ex.Reason);
            }
            catch (SenseNetSecurityException ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Publish(string path, string back)
        {
            AssertPermission();

            path = HttpUtility.UrlDecode(path);
            back = HttpUtility.UrlDecode(back);

            var content = ContentRepository.Content.Load(path);
            if (content == null)
                return this.Redirect(back);

            try
            {
                content.Publish();
            }
            catch (InvalidContentActionException ex)
            {
                WriteInvalidActionError(path, ex.Reason);
            }
            catch (SenseNetSecurityException ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult UndoCheckOut(string path, string back)
        {
            AssertPermission();

            path = HttpUtility.UrlDecode(path);
            back = HttpUtility.UrlDecode(back);

            var content = ContentRepository.Content.Load(path);
            if (content == null)
                return this.Redirect(back);

            try
            {
                content.UndoCheckOut();
            }
            catch (InvalidContentActionException ex)
            {
                WriteInvalidActionError(path, ex.Reason);
            }
            catch(SenseNetSecurityException ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ForceUndoCheckOut(string path, string back)
        {
            AssertPermission();

            path = HttpUtility.UrlDecode(path);
            back = HttpUtility.UrlDecode(back);

            var content = ContentRepository.Content.Load(path);
            if (content == null)
                return this.Redirect(back);

            try
            {
                content.ForceUndoCheckOut();
            }
            catch (InvalidContentActionException ex)
            {
                WriteInvalidActionError(path, ex.Reason);
            }
            catch (SenseNetSecurityException ex)
            {
                Logger.WriteException(ex);
            }

            return this.Redirect(back);
        }

        //===================================================================== Helper methods

        private static readonly string PlaceholderPath = "/Root/System/PermissionPlaceholders/ContentStore-mvc";

        private void AssertPermission()
        {
            var permissionContent = Node.LoadNode(PlaceholderPath);
            if (permissionContent == null || !permissionContent.Security.HasPermission(PermissionType.RunApplication))
                throw new SenseNetSecurityException("Access denied for " + PlaceholderPath);
        }

        private static void WriteInvalidActionError(string path, InvalidContentActionReason reason)
        {
            Logger.WriteWarning(EventId.ContentStore.InvalidContentAction, "Invalid content action.",
                                    properties:
                                        new Dictionary<string, object>
                                            {
                                                {"Path", path},
                                                {"Reason", Enum.GetName(typeof (InvalidContentActionException), reason)}
                                            });
        }
    }
}
