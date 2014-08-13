<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="SenseNet.Portal.Portlets" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>

<div style="display: none">
    <sn:ActionMenu ID="ActionMenu1" runat="server" Text="<%$ Resources:Renderers,hello %>" NodePath="/root" Scenario="ListItem"></sn:ActionMenu>
</div>

<% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl || this.Model.VisibleFieldNames.Length > 0)
   { %>
<div class="sn-list-navigation ui-widget-content ui-corner-all ui-helper-clearfix">

    <% if (this.Model.VisibleFieldNames.Length > 0)
       { %>
    <select class="sn-sorter sn-ctrl-select ui-widget-content ui-corner-all" onchange="if (this.value!='') window.location.href=this.value;">
        <option value=""><%=GetGlobalResourceObject("Renderers", "SelectOrdering")%></option>
        <%foreach (var field in this.Model.VisibleFieldNames)
          { %>
        <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == false).Url %>"><%=field %> <%=GetGlobalResourceObject("Renderers", "ascending")%></option>
        <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == true).Url %>"><%=field %> <%=GetGlobalResourceObject("Renderers", "descending")%></option>
        <% } %>
    </select>
    <%} %>

    <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl)
       { %>
    <div class="sn-pager">
        <%foreach (var pageAction in this.Model.Pager.PagerActions)
          {

              if (pageAction.CurrentlyActive)
              {  %>
        <span class="sn-pager-item sn-pager-active"><%=pageAction.PageNumber%></span>
        <%}
            else
            { %>
        <a class="sn-pager-item" href="<%=pageAction.Url %>"><%=pageAction.PageNumber%></a>
        <%} %>

        <% } %>
    </div>
    <% } %>
</div>
<% } %>

<div class="sn-article-list sn-article-list-shortdetail">
    <%foreach (var content in this.Model.Items)
      { %>

    <div class="sn-article-list-item">
        <% if (Security.IsInRole("Editors"))
           { %>
        <div class="sn-content-actions"><%= Actions.ActionMenu(content.Path, HttpContext.GetGlobalResourceObject("Portal", "ManageContent") as string, "ListItem")%></div>
        <%} %>
        <h2 class="sn-article-title"><a href="<%=Actions.BrowseUrl(content)%>"><%=HttpUtility.HtmlEncode(content.DisplayName) %></a></h2>
        <small class="sn-article-info"><%=GetGlobalResourceObject("Renderers", "PublishedBy")%> <%= HttpUtility.HtmlEncode(content["Author"]) %> on 
                <span class='<%= "sn-date-" + content["Id"] %>'><%= content["ModificationDate"]%></span></small>
        <div class="sn-article-lead">
            <%=content["Lead"] %>
        </div>
    </div>

    <script>
        $(function () {
            $dateField = $('<%= "sn-date-" + content["Id"] %>');
                 var dateFormat = '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern %>';
                 var timeFormat = '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern %>';
                 var date = $.datepicker.formatDate(dateFormat.toLowerCase().replace('yyyy', 'yy'), new Date(new Date('<%= content["ModificationDate"]%><' + ' UTC')));
                 var f = 24;
                 if (timeFormat.indexOf('tt') > -1) {
                     f = 12;
                 }
                 $dateField.text(date + ' ' + SN.Util.formatTimeBasedOnTimeFormat(f, new Date(new Date('<%= content["ModificationDate"]%><' + ' UTC'))));
             });
    </script>

    <%} %>
</div>

<% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl || this.Model.VisibleFieldNames.Length > 0)
   { %>
<div class="sn-list-navigation ui-widget-content ui-corner-all ui-helper-clearfix">

    <% if (this.Model.VisibleFieldNames.Length > 0)
       { %>
    <select class="sn-sorter sn-ctrl-select ui-widget-content ui-corner-all" onchange="if (this.value!='') window.location.href=this.value;">
        <option value=""><%=GetGlobalResourceObject("Renderers", "SelectOrdering")%></option>
        <%foreach (var field in this.Model.VisibleFieldNames)
          { %>
        <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == false).Url %>"><%=field %> <%=GetGlobalResourceObject("Renderers", "ascending")%></option>
        <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == true).Url %>"><%=field %> <%=GetGlobalResourceObject("Renderers", "descending")%></option>
        <% } %>
    </select>
    <%} %>

    <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl)
       { %>
    <div class="sn-pager">
        <%foreach (var pageAction in this.Model.Pager.PagerActions)
          {

              if (pageAction.CurrentlyActive)
              {  %>
        <span class="sn-pager-item sn-pager-active"><%=pageAction.PageNumber%></span>
        <%}
            else
            { %>
        <a class="sn-pager-item" href="<%=pageAction.Url %>"><%=pageAction.PageNumber%></a>
        <%} %>

        <% } %>
    </div>
    <% } %>
</div>
<% } %>

