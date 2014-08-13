<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" EnableViewState="false" %>

  
  <div style="float:left;">
     <img src="/Root/Global/images/icons/32/car.png" width="60" height="60" />
  </div>
  <div style="float:left; font-size:9pt; font-variant:small-caps;">
    <%=GetGlobalResourceObject("Content", "Make")%><span style="font-size:20pt; margin-right:10px;"><strong><%= GetValue("Make") %></strong></span>
    <%=GetGlobalResourceObject("Content", "Model")%><span style="font-size:16pt;"><%= GetValue("Model") %></span>
    <br/>
    <%=GetGlobalResourceObject("Content", "Style")%><span style="font-size:13pt;"><%= GetValue("Style") %></span>
  </div>
  <div style="clear:both;"></div>
