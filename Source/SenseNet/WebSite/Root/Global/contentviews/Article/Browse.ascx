﻿<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" EnableViewState="false" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>

<sn:ContextInfo runat="server" Selector="CurrentContext" UsePortletContext="true" ID="myContext" />

<div class="sn-article-content">
    
    <div class="sn-content-actions">
        <sn:ActionLinkButton ID="ActionLinkButton1" runat="server" IconUrl="/Root/Global/images/icons/16/edit.png" ActionName="Edit" Text="<%$ Resources:Content,Edit %>" ContextInfoID="myContext" /> 
    </div>
    
    <% if (!String.IsNullOrEmpty(GetValue("DisplayName"))) { %><h1 class="sn-content-title sn-article-title"><%= HttpUtility.HtmlEncode(GetValue("DisplayName")) %></h1><% } %>
    <% if (!String.IsNullOrEmpty(GetValue("Subtitle"))) { %><h3 class="sn-content-subtitle sn-article-subtitle"><%=GetValue("Subtitle") %></h3><% } %>
    
    <div class="sn-article-info">
        <% if (!String.IsNullOrEmpty(GetValue("Author")))
           { %>
        <span><%=GetGlobalResourceObject("Content", "Author")%> <strong><%=GetValue("Author") %></strong></span>
        <span class="sn-article-info-separator">|</span>
        <% } %>
        <span><%=GetGlobalResourceObject("Content", "Published")%> <strong><span class='sn-date'><%=GetValue("CreationDate") %></span></strong></span>
    </div>

    <% if (!String.IsNullOrEmpty(this.Image.ImageUrl)) { %>
    <div class="sn-article-img">
        <sn:Image ID="Image" runat="server" FieldName="Image" RenderMode="Browse" Width="510" Height="290">
            <BrowseTemplate>
                <asp:Image ImageUrl="/Root/Global/images/missingphoto.png" ID="ImageControl" runat="server" alt=""/>
            </BrowseTemplate>
        </sn:Image>
    </div>
    <% } %>
    
    <div class="sn-article-lead sn-richtext">
        <sn:RichText ID="RichText1" FieldName="Lead" runat="server"/>
    </div>
    <div class="sn-article-body sn-richtext">
        <sn:RichText ID="RichText2" FieldName="Body" runat="server"/>
    </div>

</div>


<script>
    $(function () {
        SN.Util.setFullLocalDate('span.sn-date', '<%= System.Globalization.CultureInfo.CurrentUICulture%>',
            '<%=GetValue("CreationDate") %>',
            '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern %>',
            '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern %>');
    });
</script>
    
    
