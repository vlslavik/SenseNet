<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>

<sn:DisplayName runat="server" ID="DisplayName" FieldName="DisplayName" />

<div class="sn-panel sn-buttons">
  <sn:CommandButtons ID="CommandButtons1" runat="server" CheckoutSaveCheckinCaption="<%$ Resources:ExpenseClaim, NewClaim%>" />
</div>