<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.ContentCollectionView" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="SenseNet.Portal.Portlets" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>

<% if (this.Model.Items.Count() > 0) {  %>
<div class="sn-userbox-list ui-helper-clearfix">

    <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl || this.Model.VisibleFieldNames.Length > 0) { %>
    <div class="sn-list-navigation ui-widget-content ui-corner-all ui-helper-clearfix">
    
        <% if (this.Model.VisibleFieldNames.Length > 0) { %>
        <select class="sn-sorter sn-ctrl-select ui-widget-content ui-corner-all" onchange="if (this.value!='') window.location.href=this.value;">
            <option value=""><asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:Renderers,SelectOrdering%>" /></option>   
            <%foreach (var field in this.Model.VisibleFieldNames) { %>
                <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == false).Url %>"><%=field %> <asp:Literal ID="Literal2" runat="server" Text="<%$ Resources:Renderers,ascending%>" /></option>
                <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == true).Url %>"><%=field %> <asp:Literal ID="Literal3" runat="server" Text="<%$ Resources:Renderers,descending%>" /></option>
            <% } %>
        </select>
        <% } %>
    
        <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl) { %>
        <div class="sn-pager">
            <% foreach (var pageAction in this.Model.Pager.PagerActions) {
        
                if (pageAction.CurrentlyActive) {  %>
                    <span class="sn-pager-item sn-pager-active"><%=pageAction.PageNumber%></span>
                <% } else { %>
                    <a class="sn-pager-item" href="<%=pageAction.Url %>"><%=pageAction.PageNumber%></a>
                <% } %>
        
            <% } %>
        </div>
        <% } %>
    </div>
    <% } %>


    <%foreach (var content in this.Model.Items) {
          var profileUrl = Actions.ActionUrl(content, "Profile");
    %>
         <div class="sn-userbox">
                <a href="<%=profileUrl%>" class="sn-avatar"><img src="<%= UITools.GetAvatarUrl(content.ContentHandler as User, 90, 90) %>" alt="" title="<%= content["FullName"] %>" /></a>
                <div class="sn-userbox-desc">
                    <h2><a href="<%=profileUrl%>"><%=content["FullName"] %></a></h2>
                    <a href="<%=profileUrl%>"><span><%=content["Domain"] + "\\" + content["Name"] %></span></a>
                </div>
         </div>    
    <% } %>


    <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl || this.Model.VisibleFieldNames.Length > 0) { %>
    <div class="sn-list-navigation ui-widget-content ui-corner-all ui-helper-clearfix">
    
        <% if (this.Model.VisibleFieldNames.Length > 0) { %>
        <select class="sn-sorter sn-ctrl-select ui-widget-content ui-corner-all" onchange="if (this.value!='') window.location.href=this.value;">
            <option value=""><asp:Literal ID="Literal4" runat="server" Text="<%$ Resources:Renderers,SelectOrdering%>" />></option>   
            <% foreach (var field in this.Model.VisibleFieldNames) { %>
                <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == false).Url %>"><%=field %> <asp:Literal ID="Literal5" runat="server" Text="<%$ Resources:Renderers,ascending%>" /></option>
                <option value="<%=this.Model.SortActions.First(sa => sa.SortColumn == field && sa.SortDescending == true).Url %>"><%=field %> <asp:Literal ID="Literal6" runat="server" Text="<%$ Resources:Renderers,descending%>" /></option>
            <% } %>
        </select>
        <% } %>
    
        <% if (((ContentCollectionPortlet)this.Model.State.Portlet).ShowPagerControl) { %>
        <div class="sn-pager">
            <% foreach (var pageAction in this.Model.Pager.PagerActions) {
        
                if (pageAction.CurrentlyActive) {  %>
                    <span class="sn-pager-item sn-pager-active"><%=pageAction.PageNumber%></span>
                <% } else { %>
                    <a class="sn-pager-item" href="<%=pageAction.Url %>"><%=pageAction.PageNumber%></a>
                <% } %>
        
            <% } %>
        </div>
        <% } %>
    </div>
    <% } %>

</div>
<% } %>