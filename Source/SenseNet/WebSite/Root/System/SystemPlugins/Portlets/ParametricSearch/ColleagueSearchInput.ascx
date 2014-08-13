﻿<%@ Control Language="C#" ClassName="WebUserControl1" %>

<div class="sn-coll-search-input">
    <table>
    <tr>
        <td><%=GetGlobalResourceObject("ParametricSearchPortlet", "Name")%>:</td><td><asp:TextBox CssClass="sn-ctrl-text sn-ctrl-medium ui-widget-content ui-corner-all" runat="server" ID="name"></asp:TextBox></td>
    </tr>
    <tr>
        <td><%=GetGlobalResourceObject("ParametricSearchPortlet", "UserName")%>:</td><td> <asp:TextBox CssClass="sn-ctrl-text sn-ctrl-medium ui-widget-content ui-corner-all" runat="server" ID="username"></asp:TextBox></td>
    </tr>
    <tr>
        <td><%=GetGlobalResourceObject("ParametricSearchPortlet", "Email")%>: </td><td><asp:TextBox CssClass="sn-ctrl-text sn-ctrl-medium ui-widget-content ui-corner-all" runat="server" ID="email"></asp:TextBox></td>
    </tr>
    <tr>
        <td><%=GetGlobalResourceObject("ParametricSearchPortlet", "PhoneNumber")%>:</td><td> <asp:TextBox CssClass="sn-ctrl-text sn-ctrl-medium ui-widget-content ui-corner-all" runat="server" ID="phone"></asp:TextBox></td>
    </tr>
    <tr>
        <td><%=GetGlobalResourceObject("ParametricSearchPortlet", "LanguagesSpoken")%>: </td><td><asp:TextBox CssClass="sn-ctrl-text sn-ctrl-medium ui-widget-content ui-corner-all" runat="server" ID="language"></asp:TextBox></td>
    </tr>
    <tr>
        <td><%=GetGlobalResourceObject("ParametricSearchPortlet", "Manager")%>: </td><td><asp:TextBox CssClass="sn-ctrl-text sn-ctrl-medium ui-widget-content ui-corner-all" runat="server" ID="manager"></asp:TextBox></td>
    </tr>
    <tr>
        <td><%=GetGlobalResourceObject("ParametricSearchPortlet", "Department")%>: </td><td><asp:TextBox CssClass="sn-ctrl-text sn-ctrl-medium ui-widget-content ui-corner-all" runat="server" ID="department"></asp:TextBox></td>
    </tr>
    </table>

    <asp:Button CssClass="sn-button sn-submit ui-button ui-widget ui-state-default ui-corner-all ui-button-text-only" runat="server" ID="btnSearch" Text="<%$ Resources:ParametricSearchPortlet,Search %>"/>
</div>