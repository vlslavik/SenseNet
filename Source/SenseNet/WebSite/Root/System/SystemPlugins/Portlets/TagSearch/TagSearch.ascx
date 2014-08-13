<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.Controls.TagSearch" %>

<asp:ScriptManagerProxy ID="ScmProxy" runat="server">
    <Scripts>
		<asp:ScriptReference Path="/Root/Global/scripts/jquery/plugins/jquery.autocomplete.js" />
		<asp:ScriptReference Path="/Root/Global/scripts/sn/SN.AutoComplete.js" />
    </Scripts>
</asp:ScriptManagerProxy>


<div class="sn-tags-search">
    <asp:TextBox ID="tbTagSearch" CssClass="sn-tags-input" runat="server"></asp:TextBox>
    <asp:Button ID="btnTagSearch" runat="server" onclick="btnSearch_Click" Text="<%$ Resources:TagAdmin,Search %>" />
</div>

<asp:ListView ID="TagSearchListView" runat="server">
<LayoutTemplate>
    <div class="sn-tags-table">
        <table>
            <thead>
                <tr id="Tr3" runat="server">       
                    <th><%=GetGlobalResourceObject("TagAdmin", "DisplayName")%></th>        
                    <th><%=GetGlobalResourceObject("TagAdmin", "Description")%></th>
                    <th><%=GetGlobalResourceObject("TagAdmin", "CreatedBy")%></th>        
                    <th><%=GetGlobalResourceObject("TagAdmin", "CreationDate")%></th>
                    <th><%=GetGlobalResourceObject("TagAdmin", "ModificationDate")%></th>        
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
                  <td><a href='<%# Eval("Path") %>'><%# HttpUtility.HtmlEncode(Eval("DisplayName")) %></a></td>   
	              <td><%# Eval("Description ")%></td>    
	              <td><%# Eval("CreatedBy ")%></td>
	              <td><%# Eval("CreationDate")%></td>    
	              <td><%# Eval("ModificationDate ")%></td>   
                </tr>
      </ItemTemplate>
      <AlternatingItemTemplate>
                <tr id="Tr5" runat="server" style="background-color:#f0f0f0">          
                  <td><a href='<%# Eval("Path") %>'><%# HttpUtility.HtmlEncode(Eval("DisplayName")) %></a></td>      
	              <td><%# Eval("Description ")%></td>   
	              <td><%# Eval("CreatedBy ")%></td>
	              <td><%# Eval("CreationDate")%></td>    
	              <td><%# Eval("ModificationDate ")%></td>   
                </tr>
      </AlternatingItemTemplate>
</asp:ListView>
