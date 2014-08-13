<%@  Language="C#" %>
<asp:DropDownList CssClass="sn-ctrl sn-ctrl-select" ID="InnerControl" runat="server" />
<asp:PlaceHolder ID="plcInheritedInfo" runat="server" Visible="false">
    <br /> <%= HttpContext.GetGlobalResourceObject("Ctd-GenericContent", "VersioningApprovingControls-Value") %>: <asp:Label ID="InheritedValueLabel" runat="server" />
</asp:PlaceHolder>