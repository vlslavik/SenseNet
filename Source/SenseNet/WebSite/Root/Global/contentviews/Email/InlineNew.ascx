<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>

  <sn:ShortText ID="DisplayName" runat="server" FieldName="DisplayName" />
  <sn:ShortText ID="From" runat="server" FieldName="From" />
  <sn:DatePicker ID="Sent" runat="server" FieldName="Sent" />
  <sn:GenericFieldControl runat="server" ID="GenericFieldControl1" FieldsOrder="Body" />
  
<div class="sn-panel sn-buttons">
  <sn:CommandButtons ID="CommandButtons1" runat="server"/>
</div>

