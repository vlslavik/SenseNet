<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>
<%@ Import Namespace="SenseNet.ContentRepository.Workspaces" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>

<%  
    var contentId = Request["contentid"]; 
    var id = Convert.ToInt32(contentId);

    var currentUser = SenseNet.ContentRepository.User.Current as SenseNet.ContentRepository.User;    
    var contextNode = Node.LoadNode(id);
    var ws = Workspace.GetWorkspaceWithWallForNode(contextNode);
    if (ws == null)
        ws = Workspace.GetWorkspaceForNode(contextNode);

    var workspacename = string.Empty;
    var workspacepath = string.Empty;
    if (ws != null)
    {
        workspacepath = ws.Path;
        workspacename = ws.DisplayName;
    }
    var mywallpath = Actions.ActionUrl(SenseNet.ContentRepository.Content.Create(currentUser), "Profile");
    var workspacedisplay = ws != null ? "inline" : "none";

    var contextGc = contextNode as GenericContent;
    var shareicon = IconHelper.ResolveIconPath(contextGc.Icon, 32);
    var sharedisplayname = HttpUtility.HtmlEncode(contextGc.DisplayName);
    var sharecontenttype = contextGc.NodeType.Name;
    var sharepath = contextGc.Path;
%>

<div class="sn-sharecontent-maindiv">
    <input class="sn-postid" type="hidden" value="<%= contentId %>" />
    <div class="sn-sharecontent-targetdiv">
        <div class="sn-sharecontent-targetdiv-left">
            <%=GetGlobalResourceObject("WallRenderers", "TargetWall")%>: 
        </div>
        <div class="sn-sharecontent-targetdiv-right">
            <a class="sn-sharetarget-path" href='<%= workspacepath %>'><%= workspacename%></a><input type="button" value="..." onclick="SN.Wall.shareTargetPick('<%= contentId %>'); return false;" />
            <br />
            <span><a javascript:void(0);" onclick="SN.Wall.setShareTarget('<%= contentId %>','<%= mywallpath%>','My Wall');return false;">[<%=GetGlobalResourceObject("WallRenderers", "MyWall")%>]</a>&nbsp;&nbsp;-&nbsp;</span>
            <span style='display:<%= workspacedisplay %>'><a javascript:void(0);" onclick="SN.Wall.setShareTarget('<%= contentId %>','<%= workspacepath %>','<%=workspacename %>');return false;">[<%=GetGlobalResourceObject("WallRenderers", "CurrentWorkspace")%>]</a></span>
        </div>
        <div class="sn-wall-clear">
        </div>
    </div>
    <div class="sn-sharecontent-403 sn-error-msg">
        <%=GetGlobalResourceObject("WallRenderers", "DontHavePermission")%><a class="sn-sharetarget-path" href='<%= workspacepath %>'><%= workspacename%></a>
    </div>
    <div class="sn-sharecontent-postboxdiv">
        <textarea class="sn-unfocused-postbox sn-share-text" onfocus="SN.Wall.onfocusPostBox($(this), true);" onblur="SN.Wall.onblurPostBox($(this), true);"><%=GetGlobalResourceObject("WallRenderers", "WriteSomething")%></textarea>
    </div>
    <div class="sn-sharecontent-contentcarddiv">
	    <div class="sn-sharecontent-contentcarddiv-left">
		    <img src=<%= shareicon %> />
	    </div>
	    <div class="sn-sharecontent-contentcarddiv-right">
		    <strong><%=sharedisplayname %></strong>&nbsp;(<%=sharecontenttype%>)
		    <div class="sn-wrap"><small><%=sharepath%></small></div>
	    </div>
	    <div class="sn-wall-clear">
        </div>
    </div>
</div>
<div class="sn-sharecontent-successful">
    <%=GetGlobalResourceObject("WallRenderers", "SuccesfullyShared")%><a class="sn-sharetarget-path" href='<%= workspacepath %>'><%= workspacename%></a>
</div>
<div class="sn-sharecontent-buttoncontainer">
    <div class="sn-sharecontent-buttondiv">
        <input type="button" class="sn-submit sn-button sn-notdisabled sn-sharebutton" value="<%=GetGlobalResourceObject("WallRenderers", "Share")%>" onclick="SN.Wall.share('<%= contentId %>');return false;" />
        <input type="button" class="sn-submit sn-button sn-notdisabled" value="<%=GetGlobalResourceObject("WallRenderers", "Close")%>" onclick="SN.Wall.closeShareDialog('<%= contentId %>');return false;" />
    </div>
</div>
