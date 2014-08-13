<%@ Control Language="C#" AutoEventWireup="false" Inherits="System.Web.UI.UserControl" %>

 <div class="sn-pt-body-border ui-widget-content">
     <div class="sn-pt-body">
         <p class="snContentLead snDialogLead">
             <%=GetGlobalResourceObject("Calendar", "AboutToApprove")%>
         </p>
         <p>
         <%=GetGlobalResourceObject("Calendar", "Subscriber")%>: <%# Eval("CreatedBy") %><br/>
         <%=GetGlobalResourceObject("Calendar", "SubscriptionDate")%>: <%# Eval("CreationDate") %>
         </p>
         <asp:PlaceHolder runat="server" ID="ErrorPanel">
             <div style="background-color:Red;font-weight:bold;color:White">
                <asp:Label runat="server" ID="ErrorLabel" />
             </div>
         </asp:PlaceHolder>
     </div>
</div>   
        
<div class="sn-pt-body-border ui-widget-content snDialogButtons">
    <div class="sn-pt-body">
        <asp:Button ID="Approve" runat="server" Text="<%$ Resources:Notifications,Approve %>" CommandName="Approve" CssClass="sn-submit" />
        <asp:Button ID="Reject" runat="server" Text="<%$ Resources:Notifications,Reject %>" CommandName="Reject" CssClass="sn-submit" />
        <sn:BackButton Text="<%$ Resources:Notifications,Cancel %>" ID="BackButton1" runat="server" CssClass="sn-submit" />
    </div>
</div>
