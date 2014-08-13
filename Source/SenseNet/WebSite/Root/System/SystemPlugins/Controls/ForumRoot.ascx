<%@ Control Language="C#" AutoEventWireup="false" Inherits="SenseNet.Portal.DiscussionForum.ForumView" %>

<sn:ContextInfo ID="ViewContext" runat="server" UsePortletContext="true" />
<sn:SenseNetDataSource ID="ViewDatasource" runat="server" ContextInfoID="ViewContext" />

<div class="sn-forum-main">

    <div class="sn-content">
        <%= ContextElement.Description %>
    </div>
    
    <asp:Repeater ID="ForumBody" DataSourceID="ViewDatasource" runat="server">
        <HeaderTemplate>
        <table class="sn-topiclist">
            <thead>
                <tr>
                    <th class="sn-first"><asp:Literal ID="Literal1" runat="server" Text="<%$ Resources: Controls, Topics %>" /></th>
                    <th><asp:Literal ID="Literal2" runat="server" Text="<%$ Resources: Controls, Posts %>" /></th>
                    <th class="sn-last"><asp:Literal ID="Literal3" runat="server" Text="<%$ Resources: Controls, LastEntry %>" /></th>
                </tr>
            </thead>
        </HeaderTemplate>
        <ItemTemplate>
            <tr class="sn-topic sn-topic-row<%#Container.ItemIndex % 2 %>">
                <td class="sn-topics-col-1 sn-first">
                    <sn:SNIcon ID="SNIcon1" Icon="topics" Size="32" runat="server" />
                    <p class="sn-topic-title">
                        <big><sn:ActionLinkButton runat="server" ID="BrowseLink" IconVisible="false" /></big><br />
                        <small><%# ListHelper.GetValueByOutputMethod(Container.DataItem, "Description")%></small>
                    </p>
                </td>
                <td class="sn-topics-col-2"><asp:Label runat="server" ID="PostNum" /></td>
                <td class="sn-topics-col-3 sn-last"><asp:Label runat="server" ID="PostDate" /></td>
            </tr>
        </ItemTemplate>
        <FooterTemplate>
        </table>
        </FooterTemplate>
    </asp:Repeater>
    <% var isAllowedToAddNewTopic = SenseNet.Portal.Virtualization.PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, SenseNet.ContentRepository.Storage.Schema.PermissionType.AddNew); %>
    <%if (isAllowedToAddNewTopic)
      {%>
    <p style="text-align:right;"><br /><b><sn:ActionLinkButton runat="server" ID="ReplyLink" IconName="add" Text="<%$ Resources: Controls, NewTopic %>" ActionName="Add" ContextInfoID="ViewContext" ParameterString="ContentTypeName=/Root/ContentTemplates/ForumTopic/NewTopic" /></b></p>
    <%} %>
</div>
