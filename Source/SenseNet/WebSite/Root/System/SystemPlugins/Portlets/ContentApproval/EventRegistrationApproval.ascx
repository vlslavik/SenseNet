<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>

 <div class="sn-pt-body-border ui-widget-content">
     <div class="sn-pt-body">
         <p class="snContentLead snDialogLead">
             <%=GetGlobalResourceObject("ContentApproval", "ApproveSubscription")%>
         </p>
         <p>
         <%=GetGlobalResourceObject("ContentApproval", "Subscriber")%>: <%=GetValue("CreatedBy") %><br/>
         <%=GetGlobalResourceObject("ContentApproval", "SubscriptionDate")%>: <%=GetValue("CreationDate") %>
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
        <asp:Button ID="Approve" runat="server" Text="<%$ Resources:ContentApproval,Approve %>" CommandName="Approve" CssClass="sn-submit" />
        <asp:Button ID="Reject" runat="server" Text="<%$ Resources:ContentApproval,Reject %>" CommandName="Reject" CssClass="sn-submit" />
        <sn:BackButton Text="<%$ Resources:ContentApproval,Cancel %>" ID="BackButton1" runat="server" CssClass="sn-submit" />
    </div>
</div>
