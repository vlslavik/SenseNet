<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.ContentCollectionView" %>

<% foreach (var content in this.Model.Items)
{ %>
    <h3 class="sn-article-title"><%= HttpUtility.HtmlEncode(content.DisplayName) %></h3>
        <%= content["Lead"] %>
<%} %>


