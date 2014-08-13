<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.Controls.LoginDemo" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Security" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>

<h1><%=GetGlobalResourceObject("LoginPortlet", "LoginAs")%></h1>
<ul>
    <li>
        <asp:LinkButton ID="link1" runat="server" CommandArgument="BuiltIn\\alba" CssClass="sn-logindemo-link">
        <% using (new SystemAccount())
           {
            var user1 = User.Load("BuiltIn", "alba");
           var avatar1 = UITools.GetAvatarUrl(user1, 128, 128);
        %>
        <div class="sn-logindemo-leftdiv">
                <img src='<%=avatar1 %>' width="36" height="36" />
        </div>
        <div class="sn-logindemo-userdatadiv">
            <div class="sn-logindemo-username"><%= user1 == null ? string.Empty : user1.FullName%> <%=GetGlobalResourceObject("LoginPortlet", "Manager")%></div>
            <div class="sn-logindemo-userdata">
                 <%=GetGlobalResourceObject("LoginPortlet", "AlbaMonday")%>
            </div>
        </div>
        <div style="clear:both;"></div>
        <% } %>
        </asp:LinkButton>        
    </li>
    <li>
        <asp:LinkButton ID="link2" runat="server" CommandArgument="BuiltIn\\mike" CssClass="sn-logindemo-link">
        <% using (new SystemAccount())
           {
               var user1 = User.Load("BuiltIn", "mike");
               var avatar1 = UITools.GetAvatarUrl(user1, 128, 128);
        %>
        <div class="sn-logindemo-leftdiv">
                <img src='<%=avatar1 %>' width="36" height="36" />
        </div>
        <div class="sn-logindemo-userdatadiv">
            <div class="sn-logindemo-username"><%= user1 == null ? string.Empty : user1.FullName%> <%=GetGlobalResourceObject("LoginPortlet", "Developer")%></div>
            <div class="sn-logindemo-userdata">
                <%=GetGlobalResourceObject("LoginPortlet", "MikeScroll")%>
            </div>
        </div>
        <div style="clear:both;"></div>
        <% } %>
        </asp:LinkButton>
    </li>
    <li class="last">
        <asp:LinkButton ID="link3" runat="server" CommandArgument="BuiltIn\\admin" CssClass="sn-logindemo-link">
        <% using (new SystemAccount())
           {
               var user1 = User.Load("Builtin", "admin");
               var avatar1 = UITools.GetAvatarUrl(user1, 128, 128);
        %>
        <div class="sn-logindemo-leftdiv">
                <img src='<%=avatar1 %>' width="36" height="36" />
        </div>
        <div class="sn-logindemo-userdatadiv">
            <div class="sn-logindemo-username"><%= user1 == null ? string.Empty : user1.FullName%> <%=GetGlobalResourceObject("LoginPortlet", "Administrator")%></div>
            <div class="sn-logindemo-userdata">
                <%=GetGlobalResourceObject("LoginPortlet", "Admin")%>
            </div>
        </div>
        <div style="clear:both;"></div>
        <% } %>
        </asp:LinkButton>
    </li>
</ul>
