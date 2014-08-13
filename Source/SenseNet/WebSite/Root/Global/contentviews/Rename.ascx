<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.UI.Controls" TagPrefix="sn" %>
<div>
    <asp:PlaceHolder ID="DisplayNamePlaceHolder" runat="server">
        <sn:DisplayName ID="Name" runat="server" FieldName="DisplayName" ControlMode="Edit" FrameMode="ShowFrame" AlwaysUpdateName="true" />
    </asp:PlaceHolder>

    <asp:PlaceHolder ID="NamePlaceHolder" runat="server">
        <sn:Name ID="UrlName" runat="server" FieldName="Name" ControlMode="Edit" FrameMode="ShowFrame" />
    </asp:PlaceHolder>
</div>
