<%@ Control Language="C#" AutoEventWireup="true" %>


<asp:ListView ID="ContentList" runat="server" EnableViewState="false">
    <LayoutTemplate>
        <div>
            <asp:PlaceHolder ID="itemPlaceHolder" runat="server" />
        </div>
    </LayoutTemplate>
    <ItemTemplate>
        <div>
            <sn:ActionLinkButton NodePath='<%#Eval("Path") %>'
                ActionName="Browse"
                IconVisible="false" runat="server">
                <span><%#HttpUtility.HtmlEncode(Eval("DisplayName")) %></span>    
            </sn:ActionLinkButton>
            <%=GetGlobalResourceObject("Renderers", "Created")%><span class='<%# "sn-date-" + Eval("Id") + "-CreationDate" %>'></span>
        </div>
        <script>
            $(function () {
                SN.Util.setFullLocalDate('<%# "span.sn-date-" + Eval("Id") + "-CreationDate" %>', '<%= System.Globalization.CultureInfo.CurrentUICulture%>',
                    '<%#Eval("CreationDate") %>',
                    '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern %>',
                    '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern %>');
            });
        </script>
    </ItemTemplate>
</asp:ListView>
