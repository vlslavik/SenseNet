<%@ Control Language="C#" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.UI.Controls" TagPrefix="sn" %>
<sn:MessageControl CssClass="snConfirmDialog" ID="MessageControl" runat="server" Buttons="YesNo">
     <footertemplate>
        <div class="sn-pt-footer"></div>    
    
        <div class="sn-pt-body-border ui-widget-content sn-dialog-buttons">
            <div class="sn-pt-body">

                <asp:Label CssClass="sn-confirmquestion" ID="RusLabel" runat="server" Text="<%$ Resources:Calendar,Unregistering %>" />
 
                <asp:Button ID="OkBtn" runat="server" Text="<%$ Resources:Calendar,Ok %>" CommandName="Ok" CssClass="sn-submit" Visible="false" /> 
                <asp:Button ID="YesBtn" runat="server" Text="<%$ Resources:Calendar,Yes %>" CommandName="Yes" CssClass="sn-submit" Visible="true" /> 
                <asp:Button ID="NoBtn" runat="server" Text="<%$ Resources:Calendar,No %>" CommandName="No" CssClass="sn-submit" Visible="true" /> 
                <asp:Button ID="CancelBtn" runat="server" Text="<%$ Resources:Calendar,Cancel %>" CommandName="Cancel" CssClass="sn-submit" Visible="false" /> 
            </div>
        </div>
        <div class="sn-pt-footer"></div>    
    </footertemplate>
</sn:MessageControl>
