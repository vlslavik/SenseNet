using System;
using System.Collections.Generic;
using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using System.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Services.WebDav
{
    public class Move : IHttpMethod
    {
        private WebDavHandler _handler;
        public Move(WebDavHandler handler)
        {
            _handler = handler;
        }

        #region IHttpMethod Members

        public void HandleMethod()
        {
            bool overwrite = false;
            string origPath = _handler.GlobalPath;
            var destPathHeaderValue = _handler.Context.Server.UrlDecode(_handler.Context.Request.Headers["Destination"]);
            var destUrl = new Uri(destPathHeaderValue);
            var destPath = _handler.GetGlobalPath(HttpUtility.UrlDecode(destUrl.AbsolutePath));
            
            if (_handler.Context.Request.Headers["Overwrite"] != null && _handler.Context.Request.Headers["Overwrite"] == "T")
                overwrite = true;

            try
            {
                var destNode = Node.LoadNode(destPath);
                if (overwrite || destNode == null)
                {
                    var origName = RepositoryPath.GetFileName(origPath);
                    var destName = RepositoryPath.GetFileName(destPath);
                    var origNode = Node.LoadNode(_handler.GlobalPath);

                    // check if moving
                    if (RepositoryPath.GetParentPath(destPath) != RepositoryPath.GetParentPath(origPath))
                    {
                        // move node to destination directory
                        string parentPath = RepositoryPath.GetParentPath(destPath);
                        origNode.MoveTo(Node.LoadNode(parentPath));
                    }
                    // renaming
                    if (origName != destName)
                    {
                        origNode.Name = RepositoryPath.GetFileName(destPath);
                        origNode.DisplayName = origNode.Name;   // also set displayname
                        origNode.Save();
                    }

                    _handler.Context.Response.StatusCode = 201;
                }
                else
                {
                    _handler.Context.Response.StatusCode = 409;
                }
            }
            catch (SecurityException e) //logged
            {
                Logger.WriteException(e);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (SenseNetSecurityException ee) //logged
            {
                Logger.WriteException(ee);
                _handler.Context.Response.StatusCode = 403;
            }
            catch (Exception eee) //logged
            {
                Logger.WriteError(SenseNet.Portal.EventId.WebDav.FolderError, "Error during webdav move operation. " + eee, properties: new Dictionary<string, object> {
                    {"Original path", origPath}, 
                    {"Destination path header", destPathHeaderValue},
                    {"Destination path", destPath},
                    {"Destination absolute path", destUrl.AbsolutePath}
                });

                _handler.Context.Response.StatusCode = 409;
            }
        }
        
        #endregion
    }
}
