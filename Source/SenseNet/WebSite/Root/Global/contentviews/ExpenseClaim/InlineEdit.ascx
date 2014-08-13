<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>

<sn:DisplayName runat="server" ID="DisplayName" FieldName="DisplayName" />
<sn:ShortText runat="server" ID="Sum" FieldName="Sum" />

<div class="sn-panel sn-buttons">
<% if (!hidePublsh)
   { %>
  <sn:CommandButtons ID="CommandButtons1" runat="server" CheckoutSaveCheckinCaption="<%$ Resources:ExpenseClaim, SaveClaim%>" SaveCheckinCaption="<%$ Resources:ExpenseClaim, SaveClaim%>" HideButtons="Save,CheckoutSave"/>
  <% }
   else
   { %>
   <%= IconHelper.RenderIconTag("warning", null, 32)%>
   <span><%= HttpContext.GetGlobalResourceObject("ExpenseClaim", "NoPublishWithoutItems")%></span>
  <sn:CommandButtons ID="CommandButtons2" runat="server" CheckoutSaveCheckinCaption="<%$ Resources:ExpenseClaim, SaveClaim%>" SaveCheckinCaption="<%$ Resources:ExpenseClaim, SaveClaim%>" HideButtons="Save,CheckoutSave,Publish"/>
  <% } %>
</div>

<script runat="server">

    bool hidePublsh;
    
    protected override void OnInit(EventArgs e)
    {
        var ec = this.Content.ContentHandler as SenseNet.ContentRepository.ExpenseClaim;
        if (ec != null && ec.ChildCount == 0)
            hidePublsh = true;

        base.OnInit(e);
    }
    
</script>