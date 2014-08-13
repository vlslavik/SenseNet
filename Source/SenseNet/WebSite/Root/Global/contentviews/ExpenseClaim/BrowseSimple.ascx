<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" EnableViewState="false" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>

<% var status = this.Content.ContentHandler.Version.Status; %>
<h2><sn:ShortText id="DisplayNameField" runat="server" FieldName="DisplayName" /></h2>
<strong><%= this.Content.Fields["Sum"].DisplayName %></strong>: <sn:WholeNumber ID="SumField" runat="server" FieldName="Sum" /> <%=GetGlobalResourceObject("Content", "Currency")%><br/><br/>
<% if (this.Content.Children.Any()) { %>
<sn:ActionLinkButton id="PublishAction" runat="server" ActionName="Publish" Text="<%$ Resources:ExpenseClaim, PublishClaim%>" />
<% } else {%>
<span><%= HttpContext.GetGlobalResourceObject("ExpenseClaim", "NoPublishWithoutItems")%></span>
<% } %>
<% if (status == VersionStatus.Pending)
   { %>
   <strong><%= HttpContext.GetGlobalResourceObject("ExpenseClaim", "ClaimIsWaitingForApproval")%></strong>
<%} else if (status == VersionStatus.Approved)
  { %>
   <strong><%= HttpContext.GetGlobalResourceObject("ExpenseClaim", "ClaimIsApproved")%></strong>
<%}  %>
<% if (status == VersionStatus.Pending || status == VersionStatus.Approved)
   { %>
   <br /><br />
   <%= SenseNet.Portal.UI.IconHelper.RenderIconTag("warning", null, 16) %><span><%= HttpContext.GetGlobalResourceObject("ExpenseClaim", "ModifyItems")%></span>
<%} %>



