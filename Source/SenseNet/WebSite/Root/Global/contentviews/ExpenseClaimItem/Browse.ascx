<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" EnableViewState="false" %>

<sn:DisplayName ID="DisplayName" runat="server" FieldName="DisplayName" FrameMode="ShowFrame" />
<sn:LongText ID="Description" runat="server" FieldName="Description" FrameMode="ShowFrame" />
<sn:WholeNumber ID="Amount" runat="server" FieldName="Amount" FrameMode="ShowFrame">
    <BrowseTemplate>
        <%# DataBinder.Eval(Container, "Data") %> <%=GetGlobalResourceObject("Content", "Currency")%>
    </BrowseTemplate>
</sn:WholeNumber>

<sn:DatePicker ID="Date" runat="server" FieldName="Date" FrameMode="ShowFrame" />
<sn:Image ID="ScannedImage" runat="server" FieldName="ScannedImage" FrameMode="ShowFrame" />
<div class="sn-panel sn-buttons">
    <sn:BackButton CssClass="sn-submit" Text="<%$ Resources:Content,Done %>" ID="BackButton1" runat="server" />
</div>
