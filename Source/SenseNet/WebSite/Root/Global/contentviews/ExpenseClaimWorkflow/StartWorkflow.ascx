<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.UI.SingleContentView" %>
<%@ Register Assembly="SenseNet.Portal" Namespace="SenseNet.Portal.UI.Controls" TagPrefix="sn" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>

<script runat="server">
    ExpenseClaim relatedContent;
    
    protected override void OnInit(EventArgs e)
    {
        relatedContent = this.Content["RelatedContent"] as ExpenseClaim;
        if (relatedContent != null)
            DataSourceExpenseClaimItems.ContentPath = relatedContent.Path;

        base.OnInit(e);
    }
</script>

<sn:SenseNetDataSource ID="DataSourceExpenseClaimItems" runat="server" />

<% if (relatedContent != null)
   { %>
   <div id="InlineViewContent" runat="server" class="sn-content sn-content-inlineview">

<asp:ListView ID="ListViewExpenseClaimItems" runat="server" DataSourceID="DataSourceExpenseClaimItems" >
    <LayoutTemplate>
        <table class="sn-listgrid ui-widget-content">
            <thead>
                <tr>  
                    <th class="sn-lg-col-1 ui-state-default"><%=GetGlobalResourceObject("Content", "DisplayName")%></th>
                    <th class="sn-lg-col-2 ui-state-default"><%=GetGlobalResourceObject("Content", "Amount")%></th>
                    <th class="sn-lg-col-3 ui-state-default"><%=GetGlobalResourceObject("Content", "Currency")%></th>
                    <th class="sn-lg-col-4 ui-state-default"><%=GetGlobalResourceObject("Content", "Date")%></th>
                    <th class="sn-lg-col-5 ui-state-default"><%=GetGlobalResourceObject("Content", "Description")%></th>
                </tr>
            </thead>
            <tbody>
                <tr runat="server" id="itemPlaceHolder" />
            </tbody>
        </table>
    </LayoutTemplate>
    
    <ItemTemplate>
        <tr class="sn-lg-row0 ui-widget-content">
            <td><%# Eval("DisplayName")%></td>
            <td><%# Eval("Amount") %></td>
            <td><%# Eval("Currency")%></td>
            <td><%# Eval("Date")%></td>
            <td><%# Eval("Description")%></td>
        </tr>
    </ItemTemplate>
    
    <EmptyDataTemplate>
    </EmptyDataTemplate>
</asp:ListView>

<br/>
<div class="sn-inputunit ui-helper-clearfix">
	<div class=sn-iu-label>
		<span class=sn-iu-title><%=GetGlobalResourceObject("Content", "Approver")%></span> <br/>
		<span class=sn-iu-desc><%=GetGlobalResourceObject("Content", "ApproverDesc")%></span> 
	</div>
	<div class=sn-iu-control>
        <span><%= relatedContent.GetApprover() %></span>
	</div>
</div>

</div>
<% } %>

<div class="sn-panel sn-buttons">
      <% 
        var doc = this.Content["RelatedContent"] as ExpenseClaim;
        if (doc == null)
        { 
            %>
            <%= IconHelper.RenderIconTag("warning", null, 32)%>
            <%=GetGlobalResourceObject("Content", "StartWorkflow")%>
            <%
        }
        else
        {
            if (doc.ChildCount == 0)
            {
                %>
                <%= IconHelper.RenderIconTag("warning", null, 32)%>
                <%=GetGlobalResourceObject("Content", "CannotStartWorkflow")%>
                <%
            }
            else if (doc.Version.Status == VersionStatus.Pending)
            {

                if ((bool)this.Content["AllowManualStart"])
                { %>
        <asp:Button class="sn-submit" ID="StartWorkflow" runat="server" Text="<%$ Resources:Content,Start %>" />
    <% }
                else
                {
                %>
                <%= IconHelper.RenderIconTag("warning", null, 32)%>
                <span><strong><%=GetGlobalResourceObject("Content", "CannotStartManually")%></strong></span><%
                }
            }
            else
            { %>
        <%= IconHelper.RenderIconTag("warning", null, 32)%>
        <%=GetGlobalResourceObject("Content", "MustBePending")%>
    <% }
        } %>
    <sn:BackButton CssClass="sn-submit" Text="<%$ Resources:Content,Cancel %>" ID="BackButton1" runat="server" />
</div>
