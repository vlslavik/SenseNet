﻿<%@ Control Language="C#" Inherits="SenseNet.Portal.Portlets.Controls.TagAdminControl" AutoEventWireup="true" %>

<div class="sn-tags-table">
    <h1>List of tags</h1>
    <asp:ListView ID="LVTags" runat="server">
        <LayoutTemplate>
            <table style="border-collapse: collapse; border: 1px solid #ccc;">
                <thead>
                    <tr id="Tr3" runat="server" style="background: #eee">
                        <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                        <%=GetGlobalResourceObject("TagAdmin", "DisplayName")%><br /><a href="?OrderBy=DisplayName&Direction=ASC">˄</a><a href="?OrderBy=DisplayName&Direction=DESC">˅</a>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "CreatedBy")%><br /><a href="?OrderBy=CreatedBy&Direction=ASC">˄</a><a href="?OrderBy=CreatedBy&Direction=DESC">˅</a>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "CreationDate")%><br /><a href="?OrderBy=CreationDate&Direction=ASC">˄</a><a href="?OrderBy=CreationDate&Direction=DESC">˅</a>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "ModificationDate")%><br /><a href="?OrderBy=ModificationDate&Direction=ASC">˄</a><a href="?OrderBy=ModificationDate&Direction=DESC">˅</a>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "ReferenceCount")%><br /><a href="?OrderBy=RefCount&Direction=ASC">˄</a><a href="?OrderBy=RefCount&Direction=DESC">˅</a>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "Edit")%>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "Delete")%>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "BlaskListed")%><br /><a href="?OrderBy=IsBlackListed&Direction=ASC">˄</a><a href="?OrderBy=IsBlackListed&Direction=DESC">˅</a>
                        </th>
                         <th style="height: 25px; border: 1px solid rgb(204, 204, 204);">
                            <%=GetGlobalResourceObject("TagAdmin", "AddRemoveBlacklist")%>
                        </th>
                    </tr>
                </thead>
                <tbody>
                    <tr id="itemPlaceHolder" runat="server" />
                </tbody>
            </table>
        </LayoutTemplate>
        <ItemTemplate>
            <tr id="Tr5" runat="server">
                <td style="border: 1px solid #ccc;"><sn:ActionLinkButton runat="server" IconVisible="false" ActionName="SearchTag" NodePath='<%# Eval("Path") %>' ParameterString='<%# "TagFilter=" + HttpUtility.UrlEncode(Eval("DisplayName").ToString()) %>' Text='<%# HttpUtility.HtmlEncode(Eval("DisplayName")) %>' /></td>
                <td style="border: 1px solid #ccc;"><%# Eval("CreatedBy") %></td>
                <td style="border: 1px solid #ccc;"><%# Eval("CreationDate") %></td>
                <td style="border: 1px solid #ccc;"><%# Eval("ModificationDate") %></td>
                <td style="border: 1px solid #ccc;"><%# Eval("RefCount") %></td>
                <td style="border: 1px solid #ccc;"><%# GetActionLink(Convert.ToInt32(Eval("ID")), "Edit")%></td>
                <td style="border: 1px solid #ccc;"><%# GetActionLink(Convert.ToInt32(Eval("ID")), "Delete")%></td>
                <td style="border: 1px solid #ccc;"><%# Eval("IsBlackListed")%></td>
                <td style="border: 1px solid #ccc;"><%# GetActionLink(Convert.ToInt32(Eval("ID")), "UpdateBlacklist")%></td>
            </tr>
        </ItemTemplate>
        <AlternatingItemTemplate>
            <tr id="Tr5" runat="server" style="background-color: #f0f0f0">
                <td style="border: 1px solid #ccc;"><sn:ActionLinkButton runat="server" IconVisible="false" ActionName="SearchTag" NodePath='<%# Eval("Path") %>' ParameterString='<%# "TagFilter=" + HttpUtility.UrlEncode(Eval("DisplayName").ToString()) %>' Text='<%# HttpUtility.HtmlEncode(Eval("DisplayName")) %>' /></td>
                <td style="border: 1px solid #ccc;"><%# Eval("CreatedBy") %></td>
                <td style="border: 1px solid #ccc;"><%# Eval("CreationDate") %></td>
                <td style="border: 1px solid #ccc;"><%# Eval("ModificationDate") %></td>
                <td style="border: 1px solid #ccc;"><%# Eval("RefCount") %></td>
                <td style="border: 1px solid #ccc;"><%# GetActionLink(Convert.ToInt32(Eval("ID")), "Edit")%></td>
                <td style="border: 1px solid #ccc;"><%# GetActionLink(Convert.ToInt32(Eval("ID")), "Delete")%></td>
                <td style="border: 1px solid #ccc;"><%# Eval("IsBlackListed")%></td>
                <td style="border: 1px solid #ccc;"><%# GetActionLink(Convert.ToInt32(Eval("ID")), "UpdateBlacklist")%></td>

            </tr>
        </AlternatingItemTemplate>
    </asp:ListView>
</div>
<div class="sn-tagadmin">
    <h2>
        <%=GetGlobalResourceObject("TagAdmin", "CreateNewTag")%></h2>
    <asp:TextBox runat="server" CssClass="tbInput" ID="NewTagTextBox" />
    <asp:Button runat="server" ID="AddTagButton" Text="<%$ Resources:TagAdmin,CreateTag %>" />
</div>
<div class="sn-tagadmin">
    <h2>
        <%=GetGlobalResourceObject("TagAdmin", "SynchronizeTag")%></h2>
    <div class="lbSync"><asp:Label ID="_lblImport" runat="server"></asp:Label></div>
    <asp:Button ID="_btnImport" Text="<%$ Resources:TagAdmin,SyncTags %>" runat="server" OnClick="BtnImportClick" />
</div>