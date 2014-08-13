﻿<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.Controls.RatingSearch" %>

<asp:ScriptManagerProxy ID="ScmProxy" runat="server">
    <Scripts>
		<asp:ScriptReference Path="/Root/System/Skins/DefaultSkin/js/rating.js" />
    </Scripts>
</asp:ScriptManagerProxy>

<div id="sn-rating-search">
	<div class="sn-rating-search-row"><%=GetGlobalResourceObject("RatingSearch", "From")%>: <asp:TextBox ID="tbRatingSearchFrom" CssClass="sn-rating-search-from" runat="server"></asp:TextBox></div>
    <div class="sn-rating-search-row"><%=GetGlobalResourceObject("RatingSearch", "To")%>: <asp:TextBox ID="tbRatingSearchTo" CssClass="sn-rating-search-to" runat="server"></asp:TextBox></div>
    <asp:Button ID="btnRatingSearch" CssClass="sn-rating-search-btn" runat="server" onclick="btnSearch_Click" Text="<%$ Resources:RatingSearch,Search %>" />
</div>

<asp:ListView ID="RatingSearchListView" runat="server">
<LayoutTemplate>
    <div id="ratingSearchTable">
        <table style="border-collapse:collapse; border:1px solid #ccc;">
            <thead>
                <tr id="Tr3" runat="server" style="background:#eee">       
                    <th style="height: 25px; border:1px solid #ccc;"><%=GetGlobalResourceObject("RatingSearch", "DisplayName")%></th>        
                    <th style="height: 25px; border:1px solid #ccc;"><%=GetGlobalResourceObject("RatingSearch", "Description")%></th>
                    <th style="height: 25px; border:1px solid #ccc;"><%=GetGlobalResourceObject("RatingSearch", "CreatedBy")%></th>        
                    <th style="height: 25px; border:1px solid #ccc;"><%=GetGlobalResourceObject("RatingSearch", "CreationDate")%></th>
                    <th style="height: 25px; border:1px solid #ccc;"><%=GetGlobalResourceObject("RatingSearch", "ModificationDate")%></th>        
                </tr>
            </thead>
            <tbody>
                <tr runat="server" id="itemPlaceHolder" />
            </tbody>
        </table>
       </div>
      </LayoutTemplate>
      <ItemTemplate>
                <tr id="Tr5" runat="server">        
                  <td style="border:1px solid #ccc;"><a href='<%# Eval("Path") %>'><%# HttpUtility.HtmlEncode(Eval("DisplayName")) %></a></td>   
	              <td style="border:1px solid #ccc;"><%# Eval("Description ")%></td>    
	              <td style="border:1px solid #ccc;"><%# Eval("CreatedBy ")%></td>
	              <td style="border:1px solid #ccc;"><%# Eval("CreationDate")%></td>    
	              <td style="border:1px solid #ccc;"><%# Eval("ModificationDate ")%></td>   
                </tr>
      </ItemTemplate>
      <AlternatingItemTemplate>
                <tr id="Tr5" runat="server" style="background-color:#f0f0f0">          
                  <td style="border:1px solid #ccc;"><a href='<%# Eval("Path") %>'><%# HttpUtility.HtmlEncode(Eval("DisplayName")) %></a></td>      
	              <td style="border:1px solid #ccc;"><%# Eval("Description ")%></td>   
	              <td style="border:1px solid #ccc;"><%# Eval("CreatedBy ")%></td>
	              <td style="border:1px solid #ccc;"><%# Eval("CreationDate")%></td>    
	              <td style="border:1px solid #ccc;"><%# Eval("ModificationDate ")%></td>   
                </tr>
      </AlternatingItemTemplate>
</asp:ListView>
