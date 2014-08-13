<%@ Control Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="SenseNet.Portal.Virtualization" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage.Data" %>
<%@ Import Namespace="SenseNet.ContentRepository.i18n" %>
<sn:scriptrequest id="respond" runat="server" path="$skin/scripts/respond.js" />
<sn:scriptrequest id="signalR" runat="server" path="$skin/scripts/jquery/plugins/jquery.signalR-2.1.0.min.js" />
<sn:scriptrequest id="Scriptrequest1" runat="server" path="$skin/scripts/sn/SN.BackgroundOperations.js" />
<sn:scriptrequest id="hub" runat="server" path="/signalr/hubs" />
<sn:scriptrequest id="Scriptrequest2" runat="server" path="$skin/scripts/kendoui/kendo.web.min.js" />
<sn:scriptrequest id="kendo1" runat="server" path="$skin/scripts/kendoui/kendo.data.min.js" />
<sn:scriptrequest id="kendo2" runat="server" path="$skin/scripts/kendoui/kendo.dataviz.min.js" />
<sn:cssrequest id="kendocss1" runat="server" csspath="$skin/styles/kendoui/kendo.dataviz.min.css" />
<sn:cssrequest id="kendocss2" runat="server" csspath="$skin/styles/kendoui/kendo.dataviz.metro.min.css" />
<sn:cssrequest id="CssRequest0" runat="server" csspath="$skin/styles/SN.BackgroundOperations.css" />

<div class="backgroundOperations">
    <h1>Background Operations</h1>
    <span id="connectionState" class="sn-icon disconnected" title='<%= SenseNetResourceManager.Current.GetString("$BackgroundOperations,Disconnected") %>'></span>
    <!-- original source code of the connection icon made by gyebi --(= -->
    <div id="machines">
        <!--<div id="SNBPPC073" class="machine">
            <div class="machinehead">
                <div class="name">SNBPPC999</div><div class="cpu">3,613649 %</div>
                <div class="ram">1440 MB</div>
            </div>
            <div id="42683a2a-63b0-4280-aa81-ffda2a6f49fe" class="agent idle" onclick="viewAgentLog(this)">Idle</div>
        </div>-->
    </div>
    <div id="agentLog"></div>
    <ul id="log"></ul>
</div>
