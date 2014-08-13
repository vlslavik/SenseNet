<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>

<sn:DisplayName ID="DisplayName" runat="server" FieldName="DisplayName" />
<sn:LongText ID="Description" runat="server" FieldName="Description" />
<sn:WholeNumber ID="Amount" runat="server" FieldName="Amount">
    <EditTemplate>
        <asp:TextBox CssClass="sn-ctrl sn-ctrl-number" ID="InnerWholeNumber" runat="server"></asp:TextBox><asp:Label ID="LabelForPercentage" runat="server" Visible="false" /> <%=GetGlobalResourceObject("Content", "Currency")%>
    </EditTemplate>
</sn:WholeNumber>
<sn:DatePicker ID="Date" runat="server" FieldName="Date" />
<sn:Image ID="ScannedImage" runat="server" FieldName="ScannedImage" />

<div class="sn-panel sn-buttons">
  <sn:CommandButtons ID="CommandButtons1" runat="server" SaveCaption="<%$ Resources:ExpenseClaim, NewClaimItem%>"/>
</div>
