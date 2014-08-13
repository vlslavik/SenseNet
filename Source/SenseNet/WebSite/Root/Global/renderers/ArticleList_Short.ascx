<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="SenseNet.Portal.Portlets" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>

<div class="sn-article-list sn-article-list-short">
    <%foreach (var content in this.Model.Items)
      { %>
      
        <div class="sn-article-list-item">
            <%if (Security.IsInRole("Editors")) { %>
            <div><%= Actions.ActionMenu(content.Path, HttpContext.GetGlobalResourceObject("Portal", "ManageContent") as string, "ListItem")%></div>
            <%} %>
            <h2 class="sn-article-title"><a href="<%=Actions.BrowseUrl(content)%>"><%=HttpUtility.HtmlEncode(content.DisplayName) %></a></h2>
            <small class="sn-article-info"><span class='<%= "sn-date-" + content["Id"] %>'><%= content["ModificationDate"]%></span></small>
            <div class="sn-article-lead">
                <%=content["Lead"] %>
            </div>
        </div>
        <script>
            $(function () {
                SN.Util.setFullLocalDate('<%= "sn-date-" + content.Id %>', '<%= System.Globalization.CultureInfo.CurrentUICulture%>',
                    '<%= content["ModificationDate"] %>',
                    '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern %>',
                    '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern %>');
        });
    </script>
    <%} %>
</div>
<div style="display:none">
    <sn:ActionMenu ID="ActionMenu1" runat="server" Text="<%$ Resources:Renderers,hello %>" NodePath="/root" Scenario="ListItem"></sn:ActionMenu>
</div>