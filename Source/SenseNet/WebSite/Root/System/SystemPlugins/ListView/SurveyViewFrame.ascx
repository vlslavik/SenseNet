<%@ Import Namespace="SenseNet.ApplicationModel"%>
<%@ Import Namespace="SNCR=SenseNet.ContentRepository"%>
<%@ Import Namespace="SenseNet.ContentRepository.Storage"%>
<%@ Import Namespace="SenseNet.Portal.Virtualization"%>
<%@ Import Namespace="SenseNet.Portal.Portlets" %>
<%@ Control Language="C#" AutoEventWireup="false" Inherits="SenseNet.Portal.UI.ContentListViews.ViewFrame" %>
<sn:ContextInfo runat="server" Selector="CurrentContext" UsePortletContext="true" ID="myContext" />
<sn:ContextInfo runat="server" Selector="CurrentList" UsePortletContext="true" ID="myList" />

<div class="sn-listview">
    <sn:Toolbar runat="server">
        <sn:ToolbarItemGroup Align="Left" runat="server">
            <sn:ActionMenu runat="server" Scenario="New" ContextInfoID="myContext" RequiredPermissions="AddNew">
                <sn:ActionLinkButton runat="server" ActionName="Add" IconUrl="/Root/Global/images/icons/16/newfile.png" ContextInfoID="myContext" Text='<%$ Resources: Scenario, New %>' />
            </sn:ActionMenu>
            <%--<sn:ActionLinkButton runat="server" ActionName="Upload" IconUrl="/Root/Global/images/icons/16/upload.png" ContextInfoID="myContext" Text="<%$ Resources: Action, Upload %>" />--%>
            
            <% if (PortalContext.Current.ActionName == null || PortalContext.Current.ActionName.ToLower() != "explore")
               { %>
            <sn:ActionMenu runat="server" IconUrl="/Root/Global/images/icons/16/wizard.png" Scenario="ListActions" ContextInfoID="myContext" CheckActionCount="True"><%= HttpContext.GetGlobalResourceObject("Scenario", "ActionsMenuDisplayName")%></sn:ActionMenu>
            <% } %>
            
            <sn:ActionLinkButton CssClass="sn-batchaction" runat="server" ActionName="CopyBatch" IconUrl="/Root/Global/images/icons/16/copy.png" ContextInfoID="myContext" Text="<%$ Resources: Action, CopyBatch %>" ParameterString="{PortletClientID}" />
            <sn:ActionLinkButton CssClass="sn-batchaction" runat="server" ActionName="MoveBatch" IconUrl="/Root/Global/images/icons/16/move.png" ContextInfoID="myContext" Text="<%$ Resources: Action, MoveBatch %>" ParameterString="{PortletClientID}" />
            <sn:ActionLinkButton CssClass="sn-batchaction" runat="server" ActionName="DeleteBatch" ContextInfoID="myContext" Text="<%$ Resources: Action, DeleteBatch %>" ParameterString="{PortletClientID}" />
        </sn:ToolbarItemGroup>
        <sn:ToolbarItemGroup runat="server" Align="Right">
            <sn:ActionMenu runat="server" IconUrl="/Root/Global/images/icons/16/settings.png" Scenario="SurveySettings" ContextInfoID="myList" CheckActionCount="True"><%= HttpContext.GetGlobalResourceObject("Scenario", "SettingsMenuDisplayName")%></sn:ActionMenu>
             <% if (this.ContextList != null && ScenarioManager.GetScenario("Views").GetActions(SNCR.Content.Create(this.ContextList), null).Count() > 0) 
               {
                   %>
            <span class="sn-actionlabel"><%= HttpContext.GetGlobalResourceObject("Scenario", "ViewsMenuDisplayName")%></span>
            <sn:ActionMenu runat="server" IconUrl="/Root/Global/images/icons/16/views.png" Scenario="Views" ContextInfoID="myList" CheckActionCount="True" ScenarioParameters="{PortletID}" >
              <% = HttpUtility.HtmlEncode(SNCR.Content.Create(ViewManager.LoadViewInContext(ContextNode, LoadedViewName)).DisplayName)%>
            </sn:ActionMenu>
            <% } %>
         </sn:ToolbarItemGroup>   
    </sn:Toolbar>
    <asp:Panel CssClass="sn-listview-checkbox" ID="ListViewPanel" runat="server"></asp:Panel>
</div>
