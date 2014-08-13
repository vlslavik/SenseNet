using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Portal.UI;

namespace SenseNet.ApplicationModel
{
    public class OpenPickerAction : ClientAction
    {
        protected virtual string DefaultPath
        {
            get
            {
                if (this.Content == null)
                    return null;

                var parentPath = this.Content.ContentHandler.ParentPath;
                return string.IsNullOrEmpty(parentPath) ? null : parentPath;
            }
        }

        protected virtual string MultiSelectMode
        {
            get { return "none"; }
        }

        protected virtual string TargetActionName
        {
            get { throw new NotImplementedException(); }
        }

        protected virtual string TargetParameterName
        {
            get { return "sourceids"; }
        }

        protected virtual string GetOpenContentPickerScript()
        {
            if (Content == null || this.Forbidden)
                return string.Empty;

            var rootPathString = UITools.GetGetContentPickerRootPathString(Content.Path);

            string script;
            if (this.DefaultPath != null)
                script = string.Format("if ($(this).hasClass('sn-disabled')) return false; SN.PickerApplication.open({{ MultiSelectMode: '{0}', callBack: {1}, TreeRoots: {2}, DefaultPath: '{3}' }});",
                    MultiSelectMode, GetCallBackScript(), rootPathString, DefaultPath);
            else
                script = string.Format("if ($(this).hasClass('sn-disabled')) return false; SN.PickerApplication.open({{ MultiSelectMode: '{0}', callBack: {1}, TreeRoots: {2}}});",
                    MultiSelectMode, GetCallBackScript(), rootPathString);

            return script;
        }

        protected virtual string GetCallBackScript()
        {
            return string.Format(@"function(resultData) {{if (!resultData) return; var targetPath = resultData[0].Path; var idlist = {0}; var requestPath = targetPath + '?action={1}&{2}=' + idlist + '&back=' + escape(window.location.href); window.location = requestPath;}}", GetIdList(), TargetActionName, TargetParameterName);
        }

        protected string GetServiceCallBackScript(string url, string scriptBeforeServiceCall, string postData, string inprogressTitle, string successContent, string successTitle, string successCallback, string errorCallback, string successCallbackAfterDialog, string errorCallbackAfterDialog)
        {
            var callback = String.Concat(
@"function(resultData) {
    if (!resultData) return; 
    var waitdlg = SN.Util.CreateWaitDialog('", inprogressTitle, @"');
    var targetPath = resultData[0].Path;", scriptBeforeServiceCall, @"; 
    $.ajax({
        url: '", url, @"', 
        type:'POST',
        cache:false, 
        data: ", postData, @", 
        success: function(data) {
            waitdlg.close(); 
            ", successCallback, @"
            SN.Util.CreateStatusDialog('", successContent, @"', '", successTitle, @"', function() {", successCallbackAfterDialog, @"}); 
        },
        error: function(response) { 
            waitdlg.close(); 
            ", errorCallback, @"
            var respObj = JSON.parse(response.responseText); 
            SN.Util.CreateErrorDialog(respObj.error.message.value, '", SenseNetResourceManager.Current.GetString("Action", "ErrorDialogTitle"), @"', function() {", errorCallbackAfterDialog, @"});
        }
    }); 
}");

            return callback;
        }

        protected virtual string GetIdList()
        {
            return (Content == null ? string.Empty : Content.Id.ToString());
        }

        protected string GetPathListMethod()
        {
            var parameters = GetParameteres();
            var portletId = parameters.ContainsKey("PortletClientID") ? parameters["PortletClientID"] : string.Empty;
            return string.Format("SN.ListGrid.getSelectedPaths('{0}')", portletId);
        }

        protected string GetIdListMethod()
        {
            var parameters = GetParameteres();
            var portletId = parameters.ContainsKey("PortletClientID") ? parameters["PortletClientID"] : string.Empty;

            return string.Format("SN.ListGrid.getSelectedIds('{0}')", portletId);
        }

        public override string Callback
        {
            get
            {
                return this.Forbidden ? string.Empty : GetOpenContentPickerScript();
            }
            set
            {
                base.Callback = value;
            }
        } 
    }
}
