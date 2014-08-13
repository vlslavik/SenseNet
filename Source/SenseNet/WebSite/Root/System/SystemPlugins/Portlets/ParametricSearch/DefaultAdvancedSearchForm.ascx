<%@ Control Language="C#" AutoEventWireup="true" Inherits="System.Web.UI.UserControl" %>
<div>
    <span><%=GetGlobalResourceObject("ParametricSearchPortlet", "Name")%>:</span>
    <asp:TextBox ID="DisplayName" runat="server"></asp:TextBox>
</div>
<div>
    <span><%=GetGlobalResourceObject("ParametricSearchPortlet", "Type")%>:</span>
    <asp:TextBox ID="Type" runat="server"></asp:TextBox>
</div>
<div>
    <asp:Button ID="btnSearch" runat="server" Text="<%$ Resources:ParametricSearchPortlet,Search %>" />
</div>
