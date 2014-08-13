<%@ Control Language="C#" AutoEventWireup="false" Inherits="System.Web.UI.UserControl" %>

<div class="sn-pt-body-border ui-widget-content" >
    <div class="sn-pt-body" >
        <%=GetGlobalResourceObject("ContentLinker", "AboutToLink")%><strong><asp:Label ID="ContentName" runat="server" /></strong>. <br/>
    </div> 
</div>

<div class="sn-pt-body-border ui-widget-content sn-dialog-buttons">
    <div class="sn-pt-body">
        <asp:Button ID="LinkerButton" runat="server" Text="<%$ Resources:ContentLinker,LinkContents %>" CssClass="sn-submit" />
        <sn:BackButton Text="<%$ Resources:ContentLinker,Cancel %>" ID="CancelButton" runat="server" CssClass="sn-submit" />
    </div>
</div>