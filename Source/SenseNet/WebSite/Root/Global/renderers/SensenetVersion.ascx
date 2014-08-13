<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>

<div class="sn-copyright">
    <a href="/legal/" target="_blank" class="license"><b>License</b></a><span class="poweredby"><%=GetGlobalResourceObject("Portal", "PoweredBy")%></span>
    <a href="http://www.sensenet.com" title="www.sensenet.com" target="_blank"><b>Sense/Net <%= RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Version %> - <%= RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition %></b></a>
</div>