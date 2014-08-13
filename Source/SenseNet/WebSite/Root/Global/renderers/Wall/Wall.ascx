<%@ Control Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>
<%@ Import Namespace="SenseNet.Portal.Virtualization" %>
<%@ Import Namespace="SenseNet.Portal" %>


<div>
    <div>
        <asp:PlaceHolder ID="WorkspaceIsWallContainer" runat="server" Visible="false">
            <div class="sn-wall-workspacewarning">
                <img src="/Root/Global/images/icons/16/warning.png" class="sn-wall-smallicon" />
                <asp:Literal runat="server" ID="Literal1" Text='<%$ Resources:Wall,CurrentWorkspaceHasntAWall1 %>' />
                <asp:HyperLink ID="PortletContextNodeLink" runat="server" />
                <asp:Literal runat="server" ID="Literal2" Text='<%$ Resources:Wall,CurrentWorkspaceHasntAWall2 %>' />
                <br /><br />
                <strong><asp:Literal runat="server" ID="Literal3" Text='<%$ Resources:Wall,CurrentWorkspaceHasntAWall3 %>' /></strong>
                <asp:Button ID="ConfigureWorkspaceWall" runat="server" Text="<%$ Resources:Wall,ConfigureWorkspaceAsWallContainer %>" CssClass="sn-submit sn-button" />
            </div>
        </asp:PlaceHolder>
    </div>
    <div class="sn-likelist">
        <div class="sn-likelist-items">
        </div>
        <div class="sn-likelist-buttoncontainer">
            <div class="sn-likelist-buttondiv">
                <input type="button" class="sn-submit sn-button sn-notdisabled" value="<%= SNSR.GetString("Controls", "Close") %>" onclick="SN.Wall.closeLikeList();return false;" />
            </div>
        </div>
    </div>
    
    <% 
        var contentTypeList = PortalContext.Current.ContextWorkspace.GetAllowedChildTypes().ToList();
        var isListEmpty = contentTypeList.Count == 0;
        var allowed = PortalContext.Current.ArbitraryContentTypeCreationAllowed;
        var wsPath = PortalContext.Current.ContextWorkspace.Path;
            
        var postAllowed = SenseNet.Portal.Wall.WallHelper.HasWallPermission(wsPath, null);
    %>
    <% if (postAllowed) { %>
    <div class="sn-dropbox">
        <div class="sn-dropbox-buttons">
            <span class="sn-dp-post sn-dropbox-buttons-selected" onclick="SN.Wall.dropboxSelect($(this),$('.sn-dropbox-postboxdiv')); return false;"><asp:Literal runat="server" ID="Literal4" Text='<%$ Resources:Wall,Post %>' /></span>
            <span class="sn-dp-post"  onclick="SN.Wall.dropboxSelect($(this),$('.sn-dropbox-createboxdiv')); return false;"><asp:Literal runat="server" ID="Literal5" Text='<%$ Resources:Wall,CreateContent %>' /></span>
            <span class="sn-dp-post" onclick="SN.Wall.dropboxSelect($(this),$('.sn-dropbox-uploadboxdiv')); return false;"><asp:Literal runat="server" ID="Literal6" Text='<%$ Resources:Wall,UploadFiles %>' /></span>
        </div>
        <div class="sn-dropbox-postboxdiv sn-dropboxdiv">
            <textarea class="sn-unfocused-postbox sn-postbox" onkeydown="if (SN.Wall.createPost(event, $(this))) return false;" onfocus="SN.Wall.onfocusPostBox($(this));" onblur="SN.Wall.onblurPostBox($(this));"><asp:Literal runat="server" ID="Literal7" Text='<%$ Resources:Wall,PostSomething %>' /></textarea>
        </div>
        <div class="sn-dropbox-createboxdiv sn-dropboxdiv ui-helper-clearfix">
            <div class="sn-dropbox-createboxcolumn">
                <% if (Node.Exists(RepositoryPath.Combine(wsPath, "Tasks"))) { %><div><img src="/Root/Global/images/icons/16/FormItem.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('Task','/Tasks');return false;"><asp:Literal runat="server" ID="Literal8" Text='<%$ Resources:Ctd-Task,DisplayName %>' /></span></div><% } %>
                <% if (Node.Exists(RepositoryPath.Combine(wsPath, "Memos"))) { %><div><img src="/Root/Global/images/icons/16/Document.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('Memo','/Memos');return false;"><asp:Literal runat="server" ID="Literal9" Text='<%$ Resources:Ctd-Memo,DisplayName %>' /></span></div><% } %>
                <% if (Node.Exists(RepositoryPath.Combine(wsPath, "Links"))) { %><div><img src="/Root/Global/images/icons/16/link.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('Link','/Links');return false;"><asp:Literal runat="server" ID="Literal10" Text='<%$ Resources:Ctd-Link,DisplayName %>' /></span></div><% } %>
                <% if (Node.Exists(RepositoryPath.Combine(wsPath, "Calendar"))) { %><div><img src="/Root/Global/images/icons/16/CalendarEvent.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('CalendarEvent','/Calendar');return false;"><asp:Literal runat="server" ID="Literal11" Text='<%$ Resources:Ctd-CalendarEvent,DisplayName %>' /></span></div><% } %>
                <% if (Node.Exists(RepositoryPath.Combine(wsPath, "Wiki/Articles"))) { %><div><img src="/Root/Global/images/icons/16/WikiArticle.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('WikiArticle','/Wiki/Articles');return false;"><asp:Literal runat="server" ID="Literal12" Text='<%$ Resources:Ctd-WikiArticle,DisplayName %>' /></span></div><% } %>
                <div><img src="/Root/Global/images/icons/16/Folder.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('Folder','');return false;"><asp:Literal runat="server" ID="Literal13" Text='<%$ Resources:Ctd-Folder,DisplayName %>' /></span></div>
                <% if (Node.Exists(RepositoryPath.Combine(wsPath, "Document_Library"))) { %>
                <div><img src="/Root/Global/images/icons/16/document.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('/Root/ContentTemplates/File/Empty text document.txt','/Document_Library');return false;">Empty text document.txt</span></div>
                <div><img src="/Root/Global/images/icons/16/word.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('/Root/ContentTemplates/File/Empty document.docx','/Document_Library');return false;">Empty document.docx</span></div>
                <div><img src="/Root/Global/images/icons/16/excel.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('/Root/ContentTemplates/File/Empty workbook.xlsx','/Document_Library');return false;">Empty workbook.xlsx</span></div>
                <div><img src="/Root/Global/images/icons/16/powerpoint.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('/Root/ContentTemplates/File/Empty presentation.pptx','/Document_Library');return false;">Empty presentation.pptx</span></div>
                <% } %>
            </div>
            <div class="sn-dropbox-createboxcolumn">
                <% if ((isListEmpty && allowed) || contentTypeList.Any(ct => ct.Name == "DocumentLibrary")) { %><div><img src="/Root/Global/images/icons/16/ContentList.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('<%= ContentTemplate.HasTemplate("DocumentLibrary") ? ContentTemplate.GetTemplate("DocumentLibrary").Path : "DocumentLibrary" %>','');return false;"><asp:Literal runat="server" ID="Literal16" Text='<%$ Resources:Ctd-DocumentLibrary,DisplayName %>' /></span></div><% } %>
                <% if ((isListEmpty && allowed) || contentTypeList.Any(ct => ct.Name == "TaskList")) { %><div><img src="/Root/Global/images/icons/16/ContentList.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('<%= ContentTemplate.HasTemplate("TaskList") ? ContentTemplate.GetTemplate("TaskList").Path : "TaskList" %>','');return false;"><asp:Literal runat="server" ID="Literal17" Text='<%$ Resources:Ctd-TaskList,DisplayName %>' /></span></div><% } %>
                <% if ((isListEmpty && allowed) || contentTypeList.Any(ct => ct.Name == "MemoList")) { %><div><img src="/Root/Global/images/icons/16/ContentList.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('<%= ContentTemplate.HasTemplate("MemoList") ? ContentTemplate.GetTemplate("MemoList").Path : "MemoList" %>','');return false;"><asp:Literal runat="server" ID="Literal18" Text='<%$ Resources:Ctd-MemoList,DisplayName %>' /></span></div><% } %>
                <% if ((isListEmpty && allowed) || contentTypeList.Any(ct => ct.Name == "LinkList")) { %><div><img src="/Root/Global/images/icons/16/ContentList.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('<%= ContentTemplate.HasTemplate("LinkList") ? ContentTemplate.GetTemplate("LinkList").Path : "LinkList" %>','');return false;"><asp:Literal runat="server" ID="Literal19" Text='<%$ Resources:Ctd-LinkList,DisplayName %>' /></span></div><% } %>
                <% if ((isListEmpty && allowed) || contentTypeList.Any(ct => ct.Name == "EventList")){ %><div><img src="/Root/Global/images/icons/16/ContentList.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('<%= ContentTemplate.HasTemplate("EventList") ? ContentTemplate.GetTemplate("EventList").Path : "EventList" %>','');return false;"><asp:Literal runat="server" ID="Literal20" Text='<%$ Resources:Ctd-EventList,DisplayName %>' /></span></div><% } %>
                <% if ((isListEmpty && allowed) || contentTypeList.Any(ct => ct.Name == "Wiki"))     { %><div><img src="/Root/Global/images/icons/16/Wiki.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('<%= ContentTemplate.HasTemplate("Wiki") ? ContentTemplate.GetTemplate("Wiki").Path : "Wiki" %>','');return false;"><asp:Literal runat="server" ID="Literal21" Text='<%$ Resources:Ctd-Wiki,DisplayName %>' /></span></div><% } %>
                <% if ((isListEmpty && allowed) || contentTypeList.Any(ct => ct.Name == "Blog"))     { %><div><img src="/Root/Global/images/icons/16/ContentList.png" class="sn-wall-smallicon" /><span onclick="SN.Wall.createContent('<%= ContentTemplate.HasTemplate("Blog") ? ContentTemplate.GetTemplate("Blog").Path : "Blog" %>','');return false;"><asp:Literal runat="server" ID="Literal22" Text='<%$ Resources:Ctd-Blog,DisplayName %>' /></span></div><% } %>
            </div>
            <div class='sn-dropbox-createothersdiv ui-helper-clearfix'>
                <div class='sn-dropbox-createothers-left'><asp:Literal runat="server" ID="Literal23" Text='<%$ Resources:Wall,CreateOther %>' /></div>
                <div class='sn-dropbox-createothers-right'>
                    <input class="sn-dropbox-createother sn-unfocused-postbox" onfocus="SN.Wall.onfocusPostBox($(this), false, true);" onblur="SN.Wall.onblurPostBox($(this), false, true);" onkeydown="if (event.keyCode == 13) return false;" onkeyup="SN.Wall.onchangeCreateOtherBox(event, $(this)); return false;" type="text" value="<%= SNSR.GetString("$Wall:StartTyping") %>"/>
                    <span class='sn-dropbox-createothers-showalllink' tabindex='-1' title='Show all types' onclick='SN.Wall.createOtherShowAll();'><img class='sn-dropbox-createothers-showallimg' src='/Root/Global/images/actionmenu_down.png'/></span>
                    <input class="sn-dropbox-createother-value" type="hidden" />
                    <input type="button" class="sn-dropbox-createother-submit sn-submit sn-button sn-notdisabled" value="Create" onclick="SN.Wall.createOther(); return false;" /><input type="button" class="sn-dropbox-createother-submitfake sn-submit sn-button sn-notdisabled sn-disabled" disabled="disabled" value="<%= SNSR.GetString("$Wall:Create") %>" />
                    <div class="sn-dropbox-createother-autocomplete"></div>
                </div>
            </div>
        </div>
        <div class="sn-dropbox-uploadboxdiv sn-dropboxdiv">
            <input type='button' class='sn-submit sn-button sn-notdisabled' value='<%= SNSR.GetString("$Wall:UploadFilesTo") %>' onclick='SN.Wall.dropboxUpload(); return false;' />
        </div>
    </div>    
    <% } %>
    <% var pageSize = (this.Parent as SenseNet.Portal.Portlets.Wall.WallPortlet).PageSize; %>
    <input type="hidden" class="sn-posts-workspace-path" value=<%= SenseNet.Portal.Virtualization.PortalContext.Current.ContextWorkspace.Path %> />
    <input type="hidden" class="sn-posts-pagesize" value=<%= pageSize %> />
    <div id="sn-posts">
        <asp:PlaceHolder ID="Posts" runat="server"></asp:PlaceHolder>
    </div>
    <div class="sn-wall-olderposts">
        <span onclick="SN.Wall.getPosts($(this))"><asp:Literal runat="server" ID="Literal14" Text='<%$ Resources:Wall,OlderPosts %>' /></span>
        <div id="sn-wall-nomoreposts" style="display:none;"><asp:Literal runat="server" ID="Literal15" Text='<%$ Resources:Wall,NoMorePosts %>' /></div>
    </div>
</div>
