<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.UI.Controls" TagPrefix="sn" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.UI.PortletFramework" TagPrefix="cbp" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.Portlets" TagPrefix="nep" %>

<%
var smtp = System.Configuration.ConfigurationManager.AppSettings["SMTP"];
if(String.IsNullOrEmpty(smtp)){%>
  <div class="sn-infobox">
    <h1 class="sn-content-title" style="color: Red"><img src="/Root/Global/images/icons/32/error.png" class="sn-icon sn-icon_32" alt=""><%=GetGlobalResourceObject("Content", "SMTPSettings")%></h1>
  </div>
<%}
else{
    if(smtp.ToLower().Equals("mail.sn.hu")){
%>
 <div class="sn-infobox" style="margin-bottom: 10px">
   <h1 class="sn-content-title" style="color: Black"><img src="/Root/Global/images/icons/32/warning.png" class="sn-icon sn-icon_32" alt=""><%= SenseNet.ContentRepository.i18n.SenseNetResourceManager.Current.GetString("Notification", "AttentionDefaultSettings")%></h1>
   <%= SenseNet.ContentRepository.i18n.SenseNetResourceManager.Current.GetString("Notification", "AttentionDetails")%>
 </div>
<%}%>
<%}%>

<% var targetContent = SenseNet.ContentRepository.Content.Load(this.Content["ContentPath"] as string); %>

<div class="sn-content-inlineview-header ui-helper-clearfix">
    <%= targetContent == null ? string.Empty : SenseNet.Portal.UI.IconHelper.RenderIconTag(targetContent.Icon, null, 32)%>
    <div class="sn-content-info sn-notification-content">
        <h2 class="sn-view-title"><%= targetContent == null ? (HttpContext.GetGlobalResourceObject("Notifications", "UnknownContent") as string) : HttpUtility.HtmlEncode(targetContent.DisplayName) %>
        <% if ( !(bool)(ContextBoundPortlet.GetContainingContextBoundPortlet(this) as NotificationEditorPortlet).IsSubscriptionNew ) {  %>
            <i><%=GetGlobalResourceObject("Notifications", "EditExisting")%></i>
        <% } %></h2>
        <strong><%=GetGlobalResourceObject("Notifications", "Path")%></strong> <%= this.Content["ContentPath"] %>
    </div>
          
    <div class="sn-infobox">
        <img class="sn-icon sn-icon_32" src="/Root/Global/images/icons/32/info.png" alt="" />
        <%=GetGlobalResourceObject("Notifications", "NotificationInfo")%>
    </div>
</div>

<% if (!string.IsNullOrEmpty(this.Content["UserEmail"] as string)) {  %>
    <sn:ShortText ID="UserEmail" runat="server" FieldName="UserEmail" ControlMode="Browse" />
<% } %>

<sn:RadioButtonGroup ID="DrpDwnFrequency" runat="server" FieldName="Frequency" />

<% if ( !(bool)(ContextBoundPortlet.GetContainingContextBoundPortlet(this) as NotificationEditorPortlet).IsSubscriptionNew ) {  %>
    <sn:Boolean ID="BoolIsActive" runat="server" FieldName="IsActive" />
<% } %>

<sn:DropDown ID="DrpDwnLang" runat="server" FieldName="Language" />

<div class="sn-panel sn-buttons">
    <% if (!string.IsNullOrEmpty(this.Content["UserEmail"] as string)) {  %>
        <asp:Button ID="BtnSave" runat="server" CssClass="sn-submit" Text="<%$ Resources:Notifications,Save %>" />
    <% } else { %>    
        <span class="sn-error"><%= GetGlobalResourceObject("Notification", "MissingEmailAddressError")%></span>
    <% } %>

    <sn:BackButton ID="BackButton" runat="server" class="sn-submit" Text="<%$ Resources:Notifications,Cancel %>" />  
</div>​