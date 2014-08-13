<%@ Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SenseNet.Portal.Virtualization" %>

<sn:ContextInfo ID="ContextInfoWs" runat="server" Selector="CurrentContext" UsePortletContext="true" />
<sn:ContextInfo ID="ContextInfoParentWs" runat="server" Selector="ParentWorkspace" />

<ul class="sn-menu">
    <li class="sn-menu-0 sn-index-0"><sn:ActionLinkButton ID="alButton0" runat="server" IconVisible="true" ActionName="Browse" UseContentIcon="true" Text="<%$ Resources:Renderers,WorkspaceHome %>" ContextInfoID="ContextInfoParentWs" /></li>
    <li class="sn-menu-0 sn-index-0"><sn:ActionLinkButton ID="alButton1" runat="server" IconVisible="true" ActionName="Browse" UseContentIcon="true" Text="<%$ Resources:Renderers,WikiMainPage %>" ContextInfoID="ContextInfoWs" /></li>
    <li class="sn-menu-0 sn-index-0"><sn:ActionLinkButton ID="alButton2" runat="server" IconVisible="true" ActionName="Browse" IconName="Versions" Text="<%$ Resources:Renderers,RecentChanges %>" ContextInfoID="ContextInfoWs" NodePath="RecentChanges" /></li>
    <li class="sn-menu-0 sn-index-0"><sn:ActionLinkButton ID="alButton3" runat="server" IconVisible="true" ActionName="Browse" IconName="Image" Text="<%$ Resources:Renderers,Images %>" ContextInfoID="ContextInfoWs" NodePath="Images" /></li>
    <% var aaa = PortalContext.Current.ActionName == null ? "" : PortalContext.Current.ActionName.ToLower();
       if (aaa != "add" && aaa != "upload")
       { %>
    <li class="sn-menu-0 sn-index-0"><sn:ActionLinkButton ID="alButton4" runat="server" IconVisible="true" ActionName="Add" Text="<%$ Resources:Renderers,NewArticle %>" ContextInfoID="ContextInfoWs" NodePath="Articles" IncludeBackUrl="true" ParameterString="backtarget=newcontent" /></li>
    <li class="sn-menu-0 sn-index-0"><sn:ActionLinkButton ID="alButton5" runat="server" IconVisible="true" ActionName="Upload" Text="<%$ Resources:Renderers,UploadImage %>" ContextInfoID="ContextInfoWs" NodePath="Images" /></li>    
    <% } %>
</ul>
