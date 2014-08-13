<%@ Control Language="C#" AutoEventWireup="false" Inherits="SenseNet.Portal.DiscussionForum.TopicView" %>
<%@ Import Namespace="SNCR=SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>

<sn:ContextInfo ID="ViewContext" runat="server" UsePortletContext="true" />
<sn:SenseNetDataSource ID="ViewDatasource" runat="server" ContextInfoID="ViewContext" />
<div class="sn-forum-topic">
    <% var isAllowedToAddNewComment = SenseNet.Portal.Virtualization.PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, SenseNet.ContentRepository.Storage.Schema.PermissionType.AddNew); %>
    <%if (isAllowedToAddNewComment)
      {%>
    <p style="float:right;">
       <sn:ActionLinkButton runat="server" ID="ReplyLink" Text="<%$ Resources:Controls,AddComment %>" ActionName="Add" ContextInfoID="ViewContext" ParameterString="ContentTypeName=ForumEntry" />
    </p>
    <%} %>
    <div class="sn-content">
       <%= ContextElement.Description %>
    </div>
    <asp:Repeater ID="TopicBody" DataSourceID="ViewDatasource" runat="server">
        <HeaderTemplate>
        <div class="sn-entries">
        </HeaderTemplate>
        <ItemTemplate>
            <div id="<%# Eval("SerialNo") %>" class="sn-entry sn-entry-row<%#Container.ItemIndex % 2 %> ui-helper-clearfix">
                <div class="sn-entry-container">
                    <div class="sn-entry-number"><%# Eval("SerialNo") %></div>
                    <div class="sn-entry-content">
                        <h3 class="sn-entry-title"><%# HttpUtility.HtmlEncode(Eval("DisplayName")) %></h3>
                        <div class="sn-entry-text"><%# Eval("Description") %></div>
                    </div>
                </div>
                <div class="sn-entry-controls">
                    <img src='<%# UITools.GetAvatarUrl(UITools.GetReferenceElevated((SNCR.Content)Container.DataItem, "PostedBy"), 32, 32) %>' class='sn-entry-avatar' alt='<%# Eval("CreatedBy") %>' title='' style='vertical-align: middle' />
                    <%# Eval("PostedBy.FullName") %> <span class="sn-separator">|</span>
                    <%# Eval("CreationDate") %> <span class="sn-separator">|</span> 
                    <%# (int)Eval("ReplyToNo") >= 0 ? "Previous in thread: <a href='#" + Eval("ReplyToNo") + "'>#" + Eval("ReplyToNo") + "</a> <span class='sn-separator'>|</span> " : ""%>
                    <sn:ActionLinkButton runat="server" ID="ReplyLink" Text="<%$ Resources:Controls,Reply %>" IconName="reply"/>
                </div>
            </div>
        </ItemTemplate>
        <FooterTemplate>
        </div>
        </FooterTemplate>
    </asp:Repeater>

</div>
