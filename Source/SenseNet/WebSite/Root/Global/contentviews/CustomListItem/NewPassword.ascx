<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.UI.Controls" TagPrefix="sn" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Security" %>
<%@ Import Namespace="SenseNet.Search" %>

<sn:ScriptRequest ID="ScriptRequest1" runat="server" Path="/Root/Global/scripts/jquery/plugins/password_strength_plugin.js" />

  <% if (GetUser() == null)
   { %>
   <span><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "EmailIsNotValid")%></span>
<% }
   else if ((DateTime)this.Content["ValidTill"] > DateTime.UtcNow)
   {
       %>
<div id="InlineViewContent" runat="server" class="sn-content sn-content-inlineview">
    <% var forgottenUser = GetUser(); %>
    <div>
        <span><%= string.Format((string)HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "WelcomeUser"), User.Current.Id == User.Visitor.Id ? forgottenUser.FullName : User.Current.FullName)%></span> <br />
        <span><%= string.Format((string)HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "UserToChange"), forgottenUser.Username)%></span>
    </div>
    <br/>
    <sn:ErrorView ID="ErrorView1" runat="server" />   

    <div class="sn-inputunit ui-helper-clearfix" runat="server" id="InputUnitPanel">
        <div class="sn-iu-label">
            <span class="sn-iu-title"><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "NewPasswordTitle")%></span> <br />
            <span class="sn-iu-desc"><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "NewPasswordDescription")%></span>
        </div>
        <div class="sn-iu-control">
            <asp:TextBox CssClass="sn-ctrl sn-ctrl-text sn-ctrl-password" ID="InnerPassword1" runat="server" TextMode="Password" /><br />
            <asp:TextBox CssClass="sn-ctrl sn-ctrl-text sn-ctrl-password2" ID="InnerPassword2" runat="server" TextMode="Password" />
        </div>
    </div>
    
</div>

<div class="sn-panel sn-buttons">
  <asp:Button class="sn-submit" ID="SetPassword" runat="server" Text="Set new password" OnClick="OnSetPassword" />
</div>

<% }
   else
   { %>
   <span><%= HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "ItemHasExpired")%></span>
<% } %>

<script runat="server">
    
    protected void OnSetPassword(object sender, EventArgs e)
    {
        if ((DateTime)this.Content["ValidTill"] < DateTime.UtcNow)
        {
            this.ContentException = new InvalidOperationException(HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "ItemHasExpired") as string);
            return;
        }
        
        var email = this.Content["Description"] as string;
        if (string.IsNullOrEmpty(email))
        {
            this.ContentException = new InvalidOperationException(HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "EmailIsNotValid") as string);
            return;
        }
        
        var p1 = InnerPassword1.Text;
        var p2 = InnerPassword2.Text;

        if (string.IsNullOrEmpty(p1) || string.IsNullOrEmpty(p2) || p1.CompareTo(p2) != 0)
        {
            this.ContentException = new InvalidOperationException(HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "PasswordIsNotValid") as string);
            return;
        }

        using (new SystemAccount())
        {
            var user = GetUser();

            if (user != null)
            {
                user.Password = p1;
                user.PasswordHash = SenseNet.ContentRepository.Fields.PasswordField.EncodePassword(p1, user);
                user.Save();
                
                FormsAuthentication.SetAuthCookie(user.Username, false);

                //this.Content.DeletePhysical();
                this.Content["Description"] = string.Empty;
                this.Content.Save();

                HttpContext.Current.Response.Redirect("/");
            }
            else
            {
                this.ContentException = new InvalidOperationException(HttpContext.GetGlobalResourceObject("ForgottenWorkflow", "EmailIsNotValid") as string);
                return;
            }
        }
    }
    
    protected User GetUser()
    {
        using (new SystemAccount())
        {
            var email = this.Content["Description"] as string;
            if (string.IsNullOrEmpty(email))
                return null;

            return SenseNet.ContentRepository.Content.All.OfType<User>().Where(u => u.Email == email).FirstOrDefault();
        }
    }
   

</script>

<sn:InlineScript ID="InlineScript" runat="server">
<script type="text/javascript">
    $.fn.shortPass = 'Too short';
    $.fn.badPass = 'Weak';
    $.fn.goodPass = 'Good';
    $.fn.strongPass = 'Strong';
    $.fn.samePassword = 'Username and Password identical.';

    $(function () {
        $("input.sn-ctrl-password, input.sn-ctrl-password2").passStrength({ userid: ".sn-ctrl-username" });
    });
</script>
</sn:InlineScript>

