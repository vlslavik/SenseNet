<%@  Language="C#" EnableViewState="false" %>
<asp:ListView ID="InnerListView" runat="server" EnableViewState="false" >
    <LayoutTemplate>
        <table class="sn-ctrl-urllist" >
          <tr style="background-color:#F5F5F5">  
              <th><%=GetGlobalResourceObject("FieldControlTemplates", "SiteName")%></th>    
              <th><%=GetGlobalResourceObject("FieldControlTemplates", "AuthenticationType")%></th>	   
          </tr>
          <tr runat="server" id="itemPlaceHolder" />
        </table>
    </LayoutTemplate>
    <ItemTemplate>
        <tr>      		
          <td><asp:Label ID="LabelSiteName" runat="server" /></td>
          <td><asp:Label ID="LabelAuthType" runat="server" /></td> 	              	              
        </tr>
    </ItemTemplate>
    <EmptyDataTemplate>
    </EmptyDataTemplate>
</asp:ListView>   

