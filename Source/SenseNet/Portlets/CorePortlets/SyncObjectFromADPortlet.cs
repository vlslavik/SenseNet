using System;
using SenseNet.ContentRepository.i18n;
using SenseNet.Portal.UI.PortletFramework;
using System.Web.UI.WebControls;
using SenseNet.DirectoryServices;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository;
using System.Security;
using System.Security.Principal;
using System.Web.UI.WebControls.WebParts;

namespace SenseNet.Portal.Portlets
{
    public class SyncObjectFromADPortlet : PortletBase
    {
        private const string SyncObjectFromADPortletClass = "SyncObjectFromADPortlet";

        /* ==================================================================================== Members */
        private TextBox _tbLdapPath;
        private Button _btnSyncObject;
        private Button _btnCheck;


        /* ==================================================================================== Properties */
        private bool _useImpersonate = true;
        [LocalizedWebDisplayName(SyncObjectFromADPortletClass, "Prop_UseImpersonate_DisplayName")]
        [LocalizedWebDescription(SyncObjectFromADPortletClass, "Prop_UseImpersonate_Description")]
        [WebBrowsable(true), Personalizable(true)]
        [WebCategory(EditorCategory.ADSync, EditorCategory.ADSync_Order)]
        [WebOrder(100)]
        public bool UseImpersonate
        {
            get { return _useImpersonate; }
            set { _useImpersonate = value; }
        }


        /* ==================================================================================== Constructor */
        public SyncObjectFromADPortlet()
        {
            this.Name = "$SyncObjectFromADPortlet:PortletDisplayName";
            this.Description = "$SyncObjectFromADPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Portal);

            this.HiddenProperties.Add("Renderer");
        }


        /* ==================================================================================== Methods */
        protected override void CreateChildControls()
        {
            _tbLdapPath = new TextBox { Columns = 110 };
            _btnCheck = new Button { Text = SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "CheckButtonText"), CssClass = "sn-submit" };
            _btnCheck.Click += new EventHandler(_btnCheck_Click);
            _btnSyncObject = new Button { Text = SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "SyncButtonText"), CssClass = "sn-submit" };
            _btnSyncObject.Click += new EventHandler(_btnSyncObject_Click);

            this.Controls.Add(new Literal { Text = SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "LdapLabel") });
            this.Controls.Add(new Literal { Text = "<br/>" });
            this.Controls.Add(_tbLdapPath);
            this.Controls.Add(new Literal { Text = "&nbsp;" });
            this.Controls.Add(_btnCheck);
            this.Controls.Add(new Literal { Text = "&nbsp;" });
            this.Controls.Add(_btnSyncObject);

            this.ChildControlsCreated = true;
            base.CreateChildControls();
        }


        /* ==================================================================================== Event handlers */
        protected void _btnCheck_Click(object sender, EventArgs e)
        {
            var syncAD2Portal = new SyncAD2Portal();
            var syncInfo = syncAD2Portal.GetSyncInfo(_tbLdapPath.Text);
            
            string syncInfoStr;
            if (!syncInfo.SyncTreeFound) 
            {
                syncInfoStr = SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "SyncTreeNotFound");
            }
            else 
            {
                syncInfoStr = string.Format(SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "SyncTreeFound"),
                    syncInfo.SyncTreeADIPAddress,
                    syncInfo.SyncTreeADPath,
                    syncInfo.SyncTreePortalPath,
                    syncInfo.TargetPortalPath,
                    SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", syncInfo.PortalNodeExists ? "TargetPathExists" : "TargetPathDoesNotExist"),
                    SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", syncInfo.PortalParentExists ? "ParentPathExists" : "ParentPathDoesNotExist")
                    );
            }
            this.Controls.Add(new Literal { Text = string.Format("<hr/><strong>{0}:</strong><br/>", SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "Results")) });
            this.Controls.Add(new Literal { Text = syncInfoStr });
        }
        protected void _btnSyncObject_Click(object sender, EventArgs e)
        {
            this.Controls.Add(new Literal { Text = string.Format("<hr/><strong>{0}:</strong><br/>", SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "Results")) });
            
            try
            {
                var syncAD2Portal = new SyncAD2Portal();

                // impersonate to currently logged on windows user, to use its credentials to connect to AD
                WindowsImpersonationContext impersonationContext = null;
                if (this.UseImpersonate)
                {
                    var windowsIdentity = ((User)User.Current).WindowsIdentity;
                    if (windowsIdentity == null)
                    {
                        this.Controls.Add(new Literal { Text = SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "IdentityImpersonationFailed") });
                        return;
                    }
                    impersonationContext = windowsIdentity.Impersonate();
                }

                int? logid = null;
                bool noerrors = false;
                try
                {
                    logid = AdLog.SubscribeToLog();
                    syncAD2Portal.SyncObjectFromAD(_tbLdapPath.Text);
                    noerrors = true;
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    this.Controls.Add(new Literal { Text = string.Format(SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "SyncError"), ex.Message) });
                }
                finally
                {
                    if (impersonationContext != null)
                        impersonationContext.Undo();

                    if (logid.HasValue)
                    {
                        var logStr = AdLog.GetLogAndRemoveSubscription(logid.Value);
                        this.Controls.Add(new Literal { Text = logStr.Replace(Environment.NewLine, "<br/>") });
                    }

                    // add link to object to bottom
                    if (noerrors)
                    {
                        var syncInfo = syncAD2Portal.GetSyncInfo(_tbLdapPath.Text);
                        if (syncInfo.PortalNodeExists)
                            this.Controls.Add(new Literal { Text = string.Format(SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "CheckResults"), syncInfo.TargetPortalPath) });
                    }
                }
            }
            catch (SecurityException ex)
            {
                Logger.WriteException(ex);
                this.Controls.Add(new Literal { Text = string.Format(SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "SyncErrorSecurity"), ex.Message) });
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
                this.Controls.Add(new Literal { Text = string.Format(SenseNetResourceManager.Current.GetString("SyncObjectFromADPortlet", "SyncErrorException"), ex.Message) });
            }
        }
    }
}
