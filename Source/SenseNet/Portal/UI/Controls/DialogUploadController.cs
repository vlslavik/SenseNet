using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Search;
using SenseNet.Services.ContentStore;

namespace SenseNet.Portal.UI.Controls
{
    [HandleError]
    public class DialogUploadController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetUserUploads(string startUploadDate, string path, string rnd)
        {
            if (!HasPermission())
                return Json(null, JsonRequestBehavior.AllowGet);

            var query = ContentQuery.CreateQuery("+CreatedById:" + ContentRepository.User.Current.Id);
            if (!string.IsNullOrEmpty(startUploadDate))
                query.AddClause("ModificationDate:>='" + startUploadDate + "'");
            if (!string.IsNullOrEmpty(path) && path.StartsWith("/Root/"))
                query.AddClause("InFolder:'" + path + "'");

            return Json((from n in query.Execute().Nodes
                         where n != null
                         select new Content(n, true, false, false, false, 0, 0)).ToArray(), JsonRequestBehavior.AllowGet);
        }


        //===================================================================== Helper methods
        private static readonly string PlaceholderPath = "/Root/System/PermissionPlaceholders/DialogUpload-mvc";
        private bool HasPermission()
        {
            var permissionContent = Node.LoadNode(PlaceholderPath);
            return !(permissionContent == null || !permissionContent.Security.HasPermission(PermissionType.RunApplication));
        }
        private void AssertPermission()
        {
            if (!HasPermission())
                throw new SenseNetSecurityException("Access denied for " + PlaceholderPath);
        }
    }
}
