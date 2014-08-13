﻿using System;
using System.Collections.Generic;
using System.Security;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Portal;

namespace SenseNet.Services.WebDav
{
    public class MkCol : IHttpMethod
    {
        private WebDavHandler _handler;

        public MkCol(WebDavHandler handler)
        {
            _handler = handler;
        }

        #region IHttpMethod Members

        public void HandleMethod()
        {
            var parentPath = RepositoryPath.GetParentPath(_handler.GlobalPath);
            var folderName = RepositoryPath.GetFileName(_handler.GlobalPath);

            try
            {
                var f = new Folder(Node.LoadNode(parentPath)) { Name = folderName };
                f.Save();

                _handler.Context.Response.StatusCode = 201;
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
                Logger.WriteError(SenseNet.Portal.EventId.WebDav.FolderError, "Could not save folder. Error: " + eee.Message, properties: new Dictionary<string, object> { { "Parent path", parentPath}, { "Folder name", folderName}});
                _handler.Context.Response.StatusCode = 405;
            }
        }

        #endregion
    }
}
