<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Schema" %>
<%@ Import Namespace="SenseNet.Portal.Virtualization" %>
<%@ Import Namespace="SenseNet.Portal.OData" %>
<%@ Import Namespace="SenseNet.Search" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Preview" %>
<sn:ScriptRequest ID="ScriptRequest" runat="server" Path="/Root/Global/scripts/sn/SN.WebDav.js" />
<sn:ScriptRequest ID="ScriptRequest36" runat="server" Path="/Root/Global/scripts/sn/SN.Wall.js" />
<sn:ScriptRequest ID="Scriptrequest2" runat="server" Path="$skin/scripts/plugins/iscroll.js" />
<sn:ScriptRequest ID="ScriptRequest1" runat="server" Path="$skin/scripts/plugins/docviewer-2.0.js" />
<sn:ScriptRequest ID="Scriptrequest3" runat="server" Path="$skin/scripts/plugins/excanvas.compiled.js" />
<sn:CssRequest ID="CssRequest" runat="server" CSSPath="$skin/styles/plugins/snDocViewer.css" />
<% var file = this.Content.ContentHandler as SenseNet.ContentRepository.File;
   var filePath = file.Path;
   var version = file.Version.ToString();
   var ext = SenseNet.ContentRepository.ContentNamingHelper.GetFileExtension(file.Name);
   var xls = ext == ".xls" || ext == ".xlsx";
   var doc = ext == ".doc" || ext == ".docx";
   var ppt = ext == ".ppt" || ext == ".pptx";
   var pdf = ext == ".pdf";
   //var binarySize = file.Size;
   //var size = ((double)binarySize) / 1024 / 1024;
   //size = size < 1 ? size : (binarySize / 1024 / 1024);
   var previews = SenseNet.ContentRepository.Storage.RepositoryPath.Combine(file.Path, "Previews");
   var previewCount = file.PageCount;
   var doctype = xls ? "Microsoft Office Excel" : doc ? "Microsoft Office Word" : ppt ? "Microsoft Office Powerpoint" : pdf ? "Adobe PDF" : "Other Document";
   //var sizestr = size < 1 ? String.Format("{0:0.##}", size) : size.ToString("N0");
   var createdby = (file.CreatedBy as SenseNet.ContentRepository.User).FullName;
   var modifiedby = (file.ModifiedBy as SenseNet.ContentRepository.User).FullName;
   var lockedby = file.Locked ? (file.LockedBy as SenseNet.ContentRepository.User).FullName : string.Empty;
   var createdbyc = SenseNet.ContentRepository.Content.Create(file.CreatedBy);
   var modifiedbyc = SenseNet.ContentRepository.Content.Create(file.ModifiedBy);
   var lockedbyc = file.Locked ? SenseNet.ContentRepository.Content.Create(file.LockedBy as SenseNet.ContentRepository.User) : null;

   // These are needed on the client side; eliminating needless AJAX requests
   var canSave = PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, PermissionType.Save);
   var canPreviewWithoutRedaction = PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, PermissionType.PreviewWithoutRedaction);
   var canPreviewWithoutWatermark = PortalContext.Current.ContextNode.Security.HasPermission(SenseNet.ContentRepository.User.Current, PermissionType.PreviewWithoutWatermark);

   var resourceScript = SenseNet.Portal.Resources.ResourceScripter.RenderResourceScript("DocViewer", System.Globalization.CultureInfo.CurrentUICulture);

   var enterpriseEdition = RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community";
%>
<div class="sn-docviewer-actions">
</div>
<div id="<%= this.ClientID %>">
</div>
<input type="hidden" class="currentcontent" value='<%= SenseNet.Portal.Virtualization.PortalContext.Current.ContextNodePath %>' />
<input type="hidden" class="currentparent" value='<%= SenseNet.Portal.Virtualization.PortalContext.Current.ContextNode.ParentPath %>' />
<input type="hidden" class="currentnode" value='<%= SenseNet.Portal.Virtualization.PortalContext.Current.ContextNode.Name %>' />
<input type="hidden" class="currentuser" value='<%= User.Current.Path %>' />
<sn:ScriptRequest runat="server" Path="$skin/scripts/SN/SN.Util.js" />
<script type="text/javascript">

    <%= resourceScript %>
    $(function () {

        var previewFolder = '<%= previews %>';
        var filePath = '<%= filePath %>';
        var fileVersion = '<%= version %>';
        var actionArray;
        var isAdmin = <%= canSave ? "true" : "false" %>;
        var noWatermark = <%= canPreviewWithoutWatermark ? "true" : "false" %>;
        var noRedaction = <%= canPreviewWithoutRedaction ? "true" : "false" %>;
        var parent = $('.currentparent').val();
        var content = $('.currentnode').val();
        var previewCount = 0;
        var wm = false;
        var enterpriseEdition = <%= enterpriseEdition.ToString().ToLower() %>;
        var loadingString = '<%=GetGlobalResourceObject("DocViewer", "generatingPreview")%>';
        var fileType = '<%= ext %>';
        fileType = fileType.replace('.','');
        var fileSize = '<%= (file.IsHeadOnly || file.IsPreviewOnly) ? 0 : file.Size %>';
        var requests = [];
        var tempRequests = [];
        var modDate = '<%= file.ModificationDate %>';
        var creDate = '<%= file.ModificationDate %>';

        var dateFormat = '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern %>';
        var timeFormat = '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern %>';
        var dateValue1 = $.datepicker.formatDate(dateFormat.toLowerCase().replace('yyyy', 'yy'), new Date(new Date(modDate + ' UTC')));
        var dateValue2 = $.datepicker.formatDate(dateFormat.toLowerCase().replace('yyyy', 'yy'), new Date(new Date(creDate + ' UTC')));
        modDate = SN.Util.getFullyFormattedDate(dateValue1, dateFormat, timeFormat);
        creDate = SN.Util.getFullyFormattedDate(dateValue2, dateFormat, timeFormat);

        var getPageCount = $.ajax({
            url: odata.dataRoot + odata.getItemUrl(filePath) + '/GetPageCount',
            dataType: "json",
            type: "POST",
            success: function (d) {
                previewCount = d;
            }
        });



        function getFileSize(fileSize) {
            var i = -1;
            var byteUnits = ['kB', 'MB', 'GB'];
            do {
                fileSize = fileSize / 1024;
                i++;    
            } while (fileSize > 1024);
            return Math.max(fileSize, 0.1).toFixed(1) + " " + byteUnits[i];
        }
       
        var touch = false;
        if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|BB10/i.test(navigator.userAgent))
            touch = true;

        var currentuser = odata.getItemUrl($(".currentuser").val());

        var noPreviewText;

        
        if(!enterpriseEdition){
            noPreviewText = "Preview generation is only available in the enterprise edition!";
            $('.sn-docviewer-actions').next('div').addClass('community');
            var limitedFunctionalityText = 'The fully functional Document Preview feature is only available in Sense/Net Enterprise.<br /><br /><a href="http://www.sensenet.com/1-on-1-demo">Request a demo of Sense/Net Enterprise now</a>';
            $('body').append('<div class="enterpriseLicence"><img src="/Root/YourDocuments/PressRoom/SenseNet-EnterpriseLicencingProgram.jpg" /><br /><br />' + limitedFunctionalityText + '</div>');
        }
        else
            noPreviewText = "No preview!";
            

        // Fetch the related reachable actions of the document
        var actionsReq = $.ajax({
            url: "/OData.svc" + odata.getItemUrl(filePath) + "/Actions?scenario=DocumentDetails&back=" + encodeURIComponent(document.URL)
        }).done(function (data) {
            actionArray = data.d.Actions;
        }); 
        


        // After the above AJAX requests complete
        //        $.when(previewsReq).done(function() {
        // Initialize the document viewer
        var $dv = $("#<%= this.ClientID %>");
        var pcpromise;
        $.when(getPageCount).done(function(){
            if(previewCount === 0){
                var errorString = '<%=GetGlobalResourceObject("DocViewer", "emptyDocumentMessage")%>';
                    var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                    $('.sn-docviewer-actions').after(errorDiv);
                }
                else if(previewCount === -2){
                    var errorString = '<%=GetGlobalResourceObject("DocViewer", "extensionFailureMessage")%>';
                var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                $('.sn-docviewer-actions').after(errorDiv);
            }
            else if(previewCount === -3){
                var errorString = '<%=GetGlobalResourceObject("DocViewer", "uploadFailureMessage")%>';
                var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                $('.sn-docviewer-actions').after(errorDiv);
            }
            else if(previewCount === -4){
                var errorString = '<%=GetGlobalResourceObject("DocViewer", "uploadFailureMessage")%>';
                var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                $('.sn-docviewer-actions').after(errorDiv);
            }
            else if(previewCount === -5){
                var errorString = '<%=GetGlobalResourceObject("DocViewer", "noPreviewProviderEnabledMessage")%>';
                var errorDiv = '<div class="sn-viewer-errorDiv">' + errorString + '</div>';
                $('.sn-docviewer-actions').after(errorDiv);
            }
            else if(previewCount === -1){
                var loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" /><%= file.DisplayName %><span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div></div></div>';
                if(isAdmin)
                    loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" /><a href="' + filePath + '"><%= file.DisplayName %></a><span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div><a href="' + filePath + '"><span class="load-button">Download</span></a></div></div>';
                $('.sn-docviewer-actions').after(loadingDiv);
                pcpromise = new $.Deferred();
                var path = "/OData.svc" + odata.getItemUrl(filePath) + "?$select=PageCount&metadata=no";

                getPreviewCount(path).done(function(data) {
                    pcpromise.resolve(data);
                    var pc = data.d.PageCount;
                    getImage(1).done(function (data) {
                        $('.preview-loader').remove();
                        docViewerInit(pc);
                    });
                });
            }
            else
            {
                var loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" /><%= file.DisplayName %><span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div></div></div>';
                if(isAdmin)
                    loadingDiv = '<div class="preview-loader"><img src="/Root/Global/images/icons/64/file.png" class="sn-loadingDiv-icon" /><div class="inner"><img src="/Root/Global/images/loading.gif" class="loading-gif" /><a href="' + filePath + '"><%= file.DisplayName %></a><span>(' + getFileSize(fileSize) + ')</span><div>' + loadingString + '</div><a href="' + filePath + '"><span class="load-button">Download</span></a></div></div>';
                $('.sn-docviewer-actions').after(loadingDiv);
                getImage(1).done(function (data) {
                    $('.preview-loader').remove();
                    docViewerInit(previewCount);
                });
            }
            });
        function docViewerInit(previewCount) {
            
            var docViewer = $dv.documentViewer({
                getImage: getImage,
                getThumbnail: getThumbnail,
                showthumbnails: true,                           //show thumbnails, default false
                metadata: true,                                 //show metadata, default false
                showtoolbar: true,                              //show toolbar, default false
                edittoolbar: true,                              //show shape edit toolbar, default false (its not visible if showtoolbar is false)
                title: '<%= file.DisplayName %>',               //content title, default empty string
                containerWidth: function() {
                    var wi = $(window).width();
                    var h = $(window).height();
                   
                    // Desktop
                    if(!touch){    
                        if (wi <= 1030){
                            return $dv.width() * 0.70;
                        }
                        else {
                            return $dv.width() * 0.75;
                        }
                    }
                    else {
                        return $dv.width();
                    }
                }, //container width
                containerHeight: function() {
                    var wh = $(window).height();
                    return wh - 260;
                }, //container height
                reactToResize: true,                                                                   //react to resize or not
                metadataHtml: '\
                            <ul class="docinfo">\
                                <li><label>Document type:</label> <span><%= doctype %></span></li>\
                                <li><label>Created by:</label> <span><a href="<%= Actions.ActionUrl(createdbyc, "Profile")%>"><%= createdby %></a></span></li>\
                                <li><label>Creation date:</label> <span>' + creDate + '</span></li>\
                                <li><label>Modified by:</label> <span><a href="<%= Actions.ActionUrl(modifiedbyc, "Profile")%>"><%= modifiedby %></a></span></li>\
                                <li><label>Last modified:</label> <span>' + modDate + '</span></li>\
                            </ul>', //content metadata, feel free to add more
                isAdmin: isAdmin,                               //current users save permission, default false
                noWatermark: noWatermark,                       //current users noWatermark permission, default false
                noRedaction: noRedaction,                       //current users noRedaction permission, default false  
                showShapes: true,                               //shapes are showing by default, default true
                shapes: '<%=GetValue("Shapes") %>',             //shapes json, default empty string
                SR: {
                    toolbarNotes: SN.Resources.DocViewer["DocViewer-toolbarNotes"],
                    toolbarHighlight: SN.Resources.DocViewer["DocViewer-toolbarHighlight"],
                    toolbarRedaction: SN.Resources.DocViewer["DocViewer-toolbarRedaction"],
                    toolbarFirstPage: SN.Resources.DocViewer["DocViewer-toolbarFirstPage"],
                    toolbarPreviousPage: SN.Resources.DocViewer["DocViewer-toolbarPreviousPage"],
                    toolbarNextPage: SN.Resources.DocViewer["DocViewer-toolbarNextPage"],
                    toolbarLastPage: SN.Resources.DocViewer["DocViewer-toolbarLastPage"],
                    toolbarFitWindow: SN.Resources.DocViewer["DocViewer-toolbarFitWindow"],
                    toolbarFitHeight: SN.Resources.DocViewer["DocViewer-toolbarFitHeight"],
                    toolbarFitWidth: SN.Resources.DocViewer["DocViewer-toolbarFitWidth"],
                    toolbarZoomOut: SN.Resources.DocViewer["DocViewer-toolbarZoomOut"],
                    toolbarZoomIn: SN.Resources.DocViewer["DocViewer-toolbarZoomIn"],
                    toolbarPrint: SN.Resources.DocViewer["DocViewer-toolbarPrint"],
                    toolbarRubberBandZoom: SN.Resources.DocViewer["DocViewer-toolbarRubberBandZoom"],
                    toolbarFullscreen: SN.Resources.DocViewer["DocViewer-toolbarFullscreen"],
                    toolbarExitFullscreen: SN.Resources.DocViewer["DocViewer-toolbarExitFullscreen"],
                    toolbarShowShapes: SN.Resources.DocViewer["DocViewer-toolbarShowShapes"],
                    toolbarHideShapes: SN.Resources.DocViewer["DocViewer-toolbarHideShapes"],
                    toolbarShowWatermark: SN.Resources.DocViewer["DocViewer-toolbarShowWatermark"],
                    toolbarHideWatermark: SN.Resources.DocViewer["DocViewer-toolbarHideWatermark"],
                    toolbarBurn: SN.Resources.DocViewer["DocViewer-toolbarBurn"],
                    annotationDefaultText: SN.Resources.DocViewer["DocViewer-annotationDefaultText"],
                    page: SN.Resources.DocViewer["DocViewer-page"],
                    showThumbnails: SN.Resources.DocViewer["sDocViewer-howThumbnails"],
                    deleteText: SN.Resources.DocViewer["DocViewer-deleteText"],
                    saveText: SN.Resources.DocViewer["DocViewer-saveText"],
                    cancelText: SN.Resources.DocViewer["DocViewer-cancelText"],
                    originalSizeText: SN.Resources.DocViewer["DocViewer-originalSize"],
                    downloadText: SN.Resources.DocViewer["DocViewer-downloadDocument"],
                    noPreview: noPreviewText
                },
                functions: {
                    print: {
                        action: printDocument,
                        title: SN.Resources.DocViewer["DocViewer-toolbarPrint"],
                        icon: '<span class="sn-icon sn-icon-print"></span>',
                        type: 'dataRelated',
                        touch: false
                    },
                    toggleWatermark: {
                        action: toggleWatermark,
                        title: SN.Resources.DocViewer["DocViewer-toolbarShowWatermark"],
                        icon: '<span class="sn-icon sn-icon-watermark"></span>',
                        type: 'dataRelated',
                        permission: noWatermark,
                        touch: false
                    },
                    save: {
                        action: Save,
                        title: SN.Resources.DocViewer["DocViewer-saveText"],
                        icon: '<span class="sn-icon sn-icon-save"></span>',
                        type: 'dataRelated',
                        permission: isAdmin,
                        touch: false
                    }
                },
                previewCount : previewCount,
                filePath : filePath,
                fitContainer : true
            }).data("snDocViewer");

            $viewer = $(".sn-docpreview-container").parent();
            viewer = $viewer.data('snDocViewer');
            var tempPreviewArray = []; 
            
            if(touch){
                $('.sn-portalremotecontrol').remove();
                $('.sn-zooming-tools').append('<span><span class="sn-icon sn-icon-menu"></span></span>');
                

                $.when(actionsReq).done(function(){
                    $('.sn-docpreview-fullscreen-wrapper').append('<div class="sn-docviewer-actions"></div>');
                    $actionList = $('.sn-docviewer-actions');
                    $actionList.append('<span class="sn-action"><a href="' + parent + '"><span class="sn-icon sn-icon-back"></span>Back to the library</a></span>');
                    $.each(actionArray,function(i,item){
                        if(item.Forbidden === false){
                            var title = item.DisplayName;
                            var path = item.Url;
                            var icon = item.Icon;
                            if(path.charAt(0) === '/'){
                                $actionList.append('<span class="sn-action"><a href="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</a></span>');

                            }
                            else{
                                $actionList.append('<span class="sn-action"><span onClick="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</span></span>');
                            }
                        }
                    });
                });
                $('.sn-icon-menu').on('click', function(){
                    if(!$('.sn-docviewer-actions').hasClass('active'))
                        $('.sn-docviewer-actions').slideDown(200).addClass('active');
                    else
                        $('.sn-docviewer-actions').slideUp(200).removeClass('active');
                });
            }

            setInterval(function(){
                checkPromiseArray(docViewer.currentPage());
            },5000);

        }

        if(!touch){
            $.when(actionsReq).done(function(){
                $actionList = $('.sn-docviewer-actions');
                $actionList.append('<span class="sn-action"><a href="' + parent + '"><span class="sn-icon sn-icon-back"></span>Back to the library</a></span>');
                $.each(actionArray,function(i,item){
                    if(item.Forbidden === false){
                        var title = item.DisplayName;
                        var path = item.Url;
                        var icon = item.Icon;
                        if(path.charAt(0) === '/'){
                            $actionList.append('<span class="sn-action"><a href="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</a></span>');

                        }
                        else{
                            $actionList.append('<span class="sn-action"><span onClick="' + path + '" title="' + title + '"><span class="sn-icon sn-icon-' + icon + '"></span>' + title + '</span></span>');
                        }
                    }
                });
            });
        }

       
        function Save() {
            savable = viewer.saveShapes();
            savable.Path = filePath;
            var p = odata.saveContent({
                contentItem: savable
            }).done(function () {
                overlayManager.showMessage({
                    type: "success",
                    title: SN.Resources.DocViewer["MessageBox-Success"],
                    text: SN.Resources.DocViewer["DocViewer-burnSuccessful"]
                });
            });
        }

        function printDocument() {
            // NOTE:
            // This feature works by creating a hidden iframe and calling the print method on its window object.
            // It might not work reliably accross all browsers and the viewer attempts to remedy that by firing a callback if it won't work.
            // ----------
            // Useful reading about the topic:
            // http://stackoverflow.com/questions/7570496/getting-the-document-object-of-an-iframe - getting document and window objects of an iframe
            // https://developer.mozilla.org/en-US/docs/Printing - explains printing and event handling in IE and Firefox
            // http://tjvantoll.com/2012/06/15/detecting-print-requests-with-javascript/ - explains a method for detecting printing in WebKit
            // ----------

            // Remove previous print iframes
            $("#sn-docpreview-print-iframe").remove();
            $('body').append('<div class="loading-print-view"><img src="/Root/Global/images/ajax-loader.gif" /><br />loading</div>');
            // Create HTML for the pictures
            var pics = '<style type="text/css">img{display:block;}</style>';

            var previewsRequest = odata.customAction({
                path: odata.getItemUrl(filePath),
                action: "GetPreviewImages",
                $select: ["Path", "Width", "Height"],
                metadata: "no"
            }).done(function (data) {
                if (!data || !data.d)
                    $.error('OData reply is incorrect for preview images request.');

                images = data.d.results;
            });

            $.when(previewsRequest).done(function(){
                $.each(images, function (i, item) {
                    pics += '<img width="' + item.Width + '" height="' + item.Height + '" " src="' + item.Path + '" />';
                });

                // Create iframe element
                var $iframe = $('<iframe id="sn-docpreview-print-iframe"></iframe>').css({
                    width: 0,
                    height: 0
                });
                // NOTE: browsers will not print() the contents of the iframe if it's not appended to the document
                $iframe.appendTo($("body"));

                // Find the DOM document inside the iframe
                var doc = ($iframe[0].contentWindow) ? ($iframe[0].contentWindow.document) : (($iframe[0].contentDocument) ? (($iframe[0].contentDocument.document) ? $iframe[0].contentDocument.document : $iframe[0].contentDocument) : null);
                doc.open();
                doc.write(pics);
                doc.close();

                // Find the content window
                var win, $win;
                if ($iframe[0].contentWindow && typeof ($iframe[0].contentWindow.print) === "function") {
                    win = $iframe[0].contentWindow;
                }
                else if ($iframe[0].contentDocument && typeof ($iframe[0].contentDocument.print) === "function") {
                    win = $iframe[0].contentDocument;
                }
                else {
                    // There is no content window on the iframe or it doesn't support printing
                    $iframe.remove();
                    null;
                    return;
                }
                $win = $(win);

                // Print event handlers
                var beforePrint = function (e) {
                    null;
                };
                var afterPrint = function (e) {
                    null;
                };

                // This works in WebKit, but the events are fired multiple times
                if (win.matchMedia) {
                    var mediaQueryList = win.matchMedia('print');
                    mediaQueryList.addListener(function (mql) {
                        if (mql.matches) {
                            beforePrint();
                        }
                        else {
                            afterPrint();
                        }
                    });
                }

                // This works in IE and Firefox
                $win.on("beforeprint.snDocViewer", beforePrint);
                $win.on("afterprint.snDocViewer", function () {
                    afterPrint();
                    $iframe.remove(); // Can't remove the element in Chrome in afterPrint() because then it crashes
                });

                // Call print
                win.print();

                $('.loading-print-view').remove();
            });
        }

        function toggleWatermark() {
            var $this = $(this).children('span');
            if($this.hasClass('sn-icon-watermark')){
                $this.removeClass('sn-icon-watermark').addClass('sn-icon-nowatermark').attr('title', SN.Resources.DocViewer["DocViewer-toolbarHideWatermark"]);
                switchWatermark(true);
            }
            else
            {
                $this.removeClass('sn-icon-nowatermark').addClass('sn-icon-watermark').attr('title', SN.Resources.DocViewer["DocViewer-toolbarShowWatermark"]);
                switchWatermark(false); 
            }
        }

        function getImage(item) {
            var promise = new $.Deferred();
            addPromiseToArray(promise, item);
            previewExists(item).done(function(data) {
                if(!wm){
                    promise.resolve(data);
                }
                else{
                    promise.resolve(data + '?watermark=true');
                }
            });
            return promise;
        }

        var Request = function (p, id, dead) {
            this.p = p;
            this.id = id;
            this.dead = dead;
        }

        function addPromiseToArray(p,id){
            var req = new Request();
            requests.push(req);
            requests[requests.length - 1].p = p;
            requests[requests.length - 1].idx = id;
            requests[requests.length - 1].dead = false;
        }

        function checkPromiseArray(id){
            for(var i = 0; i < requests.length; i++){
                if(requests[i].idx < id - 3 || requests[i].idx > id + 3){
                    requests[i].p.resolve();
                    requests[i].dead = true;
                }
            }
            removeDeadPromises();
        }

        function removeDeadPromises(){
            tempRequests = [];
            for(var i = 0; i < requests.length;i++){
                if(!requests[i].dead){
                    tempRequests[tempRequests.length] = requests[i];
                }
            }
            requests = [];
            for (var j = 0; j < tempRequests.length; j++){
                requests[j] = tempRequests[j];
            }
        }

        function getThumbnail(item) {
            var promise = new $.Deferred();
            previewExists(item).done(function(data) {
                promise.resolve(data);
            });
            return promise;
        }

        function switchWatermark(enabled) {
            var $images = $("img[data-loaded=true]", $('#docpreview'));
            var wmParam = '?watermark=true';
            // Iterate through all images
            $images.each(function (i) {
                var $img = $($images[i]);
                var oldsrc = $img.attr('src');

                // Set the src parameter according to the watermark URL parameter
                if (enabled && oldsrc.indexOf(wmParam) + wmParam.length != oldsrc.length) {
                    $img.attr('src', oldsrc + wmParam);
                    wm = true;
                }
                else if (!enabled && oldsrc.indexOf(wmParam) + wmParam.length == oldsrc.length) {
                    $img.attr('src', oldsrc.substring(0, oldsrc.indexOf(wmParam)));
                    wm = false;
                }
            });
        }

        function previewExists(item) {
            var promise = new $.Deferred();

            odata.customAction({
                path: odata.getItemUrl(filePath),
                version: fileVersion,
                action: 'PreviewAvailable',
                params: {
                    page: item
                }
            }).done(function (data) {
                if (data.PreviewAvailable !== null) {
                    promise.resolve(data);
                }
                else {
                    setTimeout(function () {
                        previewExists(item).done(function(data){
                            promise.resolve(data);
                        }).fail(function() {
                            promise.reject();
                        });
                    }, 5000);
                }
                    
            }).fail(function() {
                promise.reject();
            });
            return promise;
        }
        
        function getPreviewCount(path) {
            var promise = new $.Deferred();
            var pcCount = $.ajax({
                url: path
            }).done(function (data) {
                if (data.d.PageCount !== -1) {
                    pcpromise.resolve(data);
                }
                else {
                    setTimeout(function () {
                        getPreviewCount(path);
                    }, 3000);
                }
            }).fail(function() {
                pcpromise.reject();
            });
            return pcpromise;
        }

    });


</script>

<% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition == "Community")
   { %>
<style>
.enterpriseLicence {position: absolute;
bottom: 20px;
background: #fff;
right: 20px;
width: 340px;
z-index: 10;
padding: 20px;font-size: 120%;line-height: 20px; border:solid 1px #dedede}
.enterpriseLicence img {width: 300px;margin: 20px;font-size: 120%;line-height: 25px;}
</style>
<% } %>