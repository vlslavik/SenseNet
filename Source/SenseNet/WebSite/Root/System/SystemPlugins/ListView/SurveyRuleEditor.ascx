<%@ Control Language="C#" AutoEventWireup="false" Inherits="SenseNet.Portal.UI.Controls.SurveyRuleUserControl" CodeBehind="SurveyRuleUserControl.cs" %>

<asp:UpdatePanel id="updGridEditor" UpdateMode="Conditional" runat="server">
   <ContentTemplate>
        
		
        <asp:DropDownList ID="ddlSurveyQuestion" runat="server" AutoPostBack="true" OnSelectedIndexChanged="SurveyQuestionSelected" >
		</asp:DropDownList>
        
        <br />
        <br />

        <asp:ListView ID="InnerListView" runat="server" EnableViewState="true" OnItemDataBound="ListItemDataBound" >
            <LayoutTemplate>      
                <table>
                  <tr style="background-color:#F5F5F5">  
                      <th><asp:Literal runat="server" ID="Literal1" Text='<%$ Resources:FieldControlTemplates,RuleEditor_Answers %>' /></th>	   
                      <th><asp:Literal runat="server" ID="Literal2" Text='<%$ Resources:FieldControlTemplates,RuleEditor_PagesToJump %>' /></th>    
                  </tr>
                  <tr runat="server" id="itemPlaceHolder" />
                </table>
            </LayoutTemplate>
            <ItemTemplate>
                <tr>      		
	              <td>
	                <asp:Literal ID="ltrAnswerName" runat="server" />
	                <asp:HiddenField ID="hidAnswerValue" runat="server" />
	              </td> 	              
	              <td>
	                <asp:DropDownList ID="ddlJumpToPage" runat="server" />
	              </td>
                </tr>
            </ItemTemplate>
            <EmptyDataTemplate>
            </EmptyDataTemplate>
        </asp:ListView>   
        
    </ContentTemplate>
</asp:UpdatePanel>