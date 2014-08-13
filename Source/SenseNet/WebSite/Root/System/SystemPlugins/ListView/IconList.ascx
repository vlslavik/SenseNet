<%@ Control Language="C#" AutoEventWireup="false" Inherits="SenseNet.Portal.UI.ContentListViews.ListView" %>
<%@ Import Namespace="SNCR=SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.Portal.UI.ContentListViews" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<sn:SenseNetDataSource ID="ViewDatasource" runat="server" />
<sn:ContextInfo runat="server" Selector="CurrentContext" UsePortletContext="true" ID="myContext" />

<sn:ListGrid ID="ViewBody" DataSourceID="ViewDatasource" runat="server">
    <LayoutTemplate>
        <asp:PlaceHolder runat="server" id="itemPlaceHolder" />
    </LayoutTemplate>
    <ItemTemplate>
        <span class="sn-icon-list-item">
            <a href="<%# Actions.BrowseUrl(((SenseNet.ContentRepository.Content)Container.DataItem)) %>">
                <sn:SNIcon Icon="<%# ((SenseNet.ContentRepository.Content)Container.DataItem).Icon %>" Size="32" runat="server" />
                <span class="sn-wrap"><%# HttpUtility.HtmlEncode(Eval("GenericContent_DisplayName")) %></span>
            </a>
            <% if (Security.IsInRole("Editors"))
               { %>
            <sn:ActionMenu NodePath='<%# Eval("Path") %>' Text="<%$ Resources: Portal, ManageContent %>" RequiredPermissions="Save" runat="server" Scenario="ListItem" />
            <%} %>
        </span>
    </ItemTemplate>
    <EmptyDataTemplate>
        <div class="sn-warning-msg ui-widget-content ui-state-default"><%=GetGlobalResourceObject("List", "EmptyList")%></div>
    </EmptyDataTemplate>
</sn:ListGrid>
  