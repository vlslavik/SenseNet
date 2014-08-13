<%@ Control Language="C#" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.UI.Controls" TagPrefix="sn" %>
<sn:MessageControl CssClass="snConfirmDialog" ID="PurgeMessage" runat="server"  Buttons="YesNo">
    <headertemplate>
    
        <div class="sn-pt-body-border ui-widget-content sn-dialog-confirmation">
            <div class="sn-pt-body">
            
                <asp:Panel CssClass="sn-dialog-icon sn-dialog-trash" runat="server" id="DialogIcon" />

    </headertemplate>
    <controltemplate>
                <asp:PlaceHolder ID="DialogHeader" runat="server">

                    <p class="sn-lead sn-dialog-lead">
                        <%=GetGlobalResourceObject("Purge", "AboutToPurge")%>
                    </p>
                    
                    <ul class="sn-dialog-properties">
                            <%=GetGlobalResourceObject("Purge", "PurgeProperties")%>
                    </ul>
                                          
                </asp:PlaceHolder>    
    </controltemplate>
    <confirmationtemplate>
                <asp:Panel CssClass="sn-message" ID="DialogMessagePanel" runat="server" Visible="false">
                    <asp:Label CssClass="sn-icon-big sn-icon-left snIconBig_warning" ID="DialogMessageIcon" runat="server" />
                    <asp:Label CssClass="sn-msg-title" ID="DialogMessage" runat="server" /><br />
                    <%--<asp:Label ID="ErrorMessage" runat="server" Visible="false" ForeColor="Red" />--%>
                </asp:Panel>
    </confirmationtemplate>
    <footertemplate>

            </div>
        </div>    
        <div class="sn-pt-footer"></div>    
    
        <div class="sn-pt-body-border ui-widget-content sn-dialog-buttons">
            <div class="sn-pt-body">

                <asp:Label CssClass="sn-confirmquestion" ID="RusLabel" runat="server" Text="<%$ Resources:ContentDelete,AreYouSure %>" />

                <asp:Button ID="OkBtn" runat="server" Text="<%$ Resources:Purge,Ok %>" CommandName="Ok" CssClass="sn-submit" Visible="false" /> 
                <asp:Button ID="YesBtn" runat="server" Text="<%$ Resources:Purge,Yes %>" CommandName="Yes" CssClass="sn-submit" Visible="true" /> 
                <asp:Button ID="NoBtn" runat="server" Text="<%$ Resources:Purge,No %>" CommandName="No" CssClass="sn-submit" Visible="true" /> 
            </div>
        </div>
        <div class="sn-pt-footer"></div>    
    </footertemplate>
</sn:MessageControl>
