<%@  Language="C#" AutoEventWireup="true" EnableViewState="false" %>
<asp:UpdatePanel ID="WizardUpdatePanel" runat="server" ChildrenAsTriggers="true">
    <contenttemplate>
    <asp:Label ID="ErrorMessage" runat="server" CssClass="sn-error-msg"></asp:Label>
    <input type="hidden" id="_settings" runat="server" />
                <asp:Wizard ID="Wizard1" runat="server" EnableViewState="true" DisplaySideBar="false" BorderStyle="None" BorderWidth="0" CellPadding="0" CellSpacing="0" CssClass="snWsWizard">
                    <HeaderTemplate>
                        <div class="snWsWizardHeader">
                            <h1>
                                Sense/Net 6.3<br />
                                <asp:Literal ID="Literal1" runat="server" Text="<%$ Resources: WorkspaceWizard, NewWorkspaceWizard %>" /></h1>
                            <div class="sn-logo">
                                Sense/Net 6.3</div>
                        </div>
                    </HeaderTemplate>
                    <SideBarTemplate>
                        <asp:Label ID="SideBarInfo" runat="server" />
                        <asp:DataList ID="SideBarList" RepeatDirection="Horizontal" RepeatLayout="Flow" Style="display: none;"
                            runat="server">
                            <ItemTemplate>
                                <asp:LinkButton ID="SideBarButton" Visible="false" runat="server"></asp:LinkButton>
                            </ItemTemplate>
                            <SelectedItemStyle Font-Bold="True" CssClass="Wizard-Sidebar-Selected" />
                        </asp:DataList>
                    </SideBarTemplate>
                    <WizardSteps>
                        <asp:TemplatedWizardStep  ID="ChooseWorkspaceStep" runat="server" AllowReturn="true"
                            StepType="Start">
                            <ContentTemplate>
                                <div class="snWsWizardMain">
                                    <div class="snWsWizardMainLeft snWsWizardRoundedTable">
                                      <div class="nw">
                                        <div class="ne">
                                          <div class="n"></div>
                                        </div>
                                      </div>
                                      <div class="w">
                                        <div class="e">
                                          <div class="c">
                                            <asp:RadioButtonList ID="WorkspaceList" EnableViewState="true" CssClass="snWsWizardRbList" runat="server" RepeatDirection="Vertical" RepeatLayout="Flow">
                                            </asp:RadioButtonList>
                                          </div>
                                        </div>
                                      </div>                    
                                      <div class="sw">
                                        <div class="se">
                                          <div class="s"></div>
                                        </div>
                                      </div>                                      
                                    </div>
                                    <div class="snWsWizardMainRight"></div>
                                </div>
                            </ContentTemplate>
                            <CustomNavigationTemplate>
                              <div class="sn-pt-header">
                                <div class="sn-pt-header-tl"></div>
                                <div class="sn-pt-header-center">
                                  <div class="sn-pt-title"><asp:Literal ID="Literal2" runat="server" Text="<%$ Resources: WorkspaceWizard, FewSeconds %>" /></div>
                                </div>
                              </div>
                              <div class="sn-pt-body-border ui-widget-content">
                                <div class="sn-pt-body">
                                  <div class="snWsWizardFooter">
                                      <ol class="snWsWizardProgButtons">
                                          <li class="snWsWizardActive_1">
                                            <asp:Literal ID="Literal3" runat="server" Text="<%$ Resources: WorkspaceWizard, ChooseType %>" />
                                          </li>
                                          <li class="snWsWizardAvailable_2">
                                              <asp:LinkButton UseSubmitBehavior="True" ID="StepNextButton" runat="server" CommandName="MoveNext" Text="Name your workspace" />
                                          </li>
                                          <li class="snWsWizardUnavailable_3">
                                            <asp:Literal ID="Literal4" runat="server" Text="<%$ Resources: WorkspaceWizard, WizardFewSeconds %>" />
                                          </li>
                                      </ol>
                                  </div>
                                </div>  
                              </div>
                            </CustomNavigationTemplate>
                        </asp:TemplatedWizardStep>
                        <asp:TemplatedWizardStep ID="WorkspaceFormStep" runat="server" AllowReturn="True"
                            StepType="Step">
                            <ContentTemplate>
                                <div class="snWsWizardMain">
                                    <div class="snWsWizardForm snWsWizardRoundedTable">
                                      <div class="nw">
                                        <div class="ne">
                                          <div class="n"></div>
                                        </div>
                                      </div>
                                      <div class="w">
                                        <div class="e">
                                          <div class="c">
                                            <h2>
                                            <asp:Literal ID="Literal5" runat="server" Text="<%$ Resources: WorkspaceWizard, Describe %>" />
                                            <asp:Label ID="NewWorkspaceTypeName" runat="server" ForeColor="Red" /> Workspace</h2>
                                            <asp:Label ID="WorkspaceNameLabel" runat="server" Text="<%$ Resources:WorkspaceWizard,WorkspaceName %>" AssociatedControlID="WorkspaceNameText"></asp:Label><br />
                                            <asp:Label ID="WorkspaceNameDescLabel" runat="server" AssociatedControlID="WorkspaceNameText" Text="<%$ Resources:WorkspaceWizard,UrlName %>"></asp:Label>
                                            <asp:TextBox ID="WorkspaceNameText" runat="server" Columns="40"></asp:TextBox>
                                            <br />
                                            <asp:Label ID="WorkspaceDescLabel" runat="server" Text="<%$ Resources:WorkspaceWizard,BriefDesription %>" AssociatedControlID="WorkspaceDescText"></asp:Label><br />
                                            <asp:Label ID="WorkspaceDescDescLabel" runat="server" AssociatedControlID="WorkspaceDescText" Text="<%$ Resources:WorkspaceWizard,BriefDesription %>"></asp:Label>
                                            <asp:TextBox ID="WorkspaceDescText" runat="server" Rows="5" Columns="40" TextMode="MultiLine"></asp:TextBox>                                        
                                          </div>
                                        </div>
                                      </div>                    
                                      <div class="sw">
                                        <div class="se">
                                          <div class="s"></div>
                                        </div>
                                      </div>                                      
                                    </div>
                                </div>
                            </ContentTemplate>
                            <CustomNavigationTemplate>
                              <div class="sn-pt-header">
                                <div class="sn-pt-header-tl"></div>
                                <div class="sn-pt-header-center">
                                  <div class="sn-pt-title">
                                    <asp:Literal ID="Literal6" runat="server" Text="<%$ Resources: WorkspaceWizard, ThreeSteps %>" />
                                  </div>
                                </div>
                              </div>
                              <div class="sn-pt-body-border ui-widget-content">
                                <div class="sn-pt-body>
                                  <div class="snWsWizardFooter">
                                      <ol class="snWsWizardProgButtons">
                                        <li class="snWsWizardAvailable_1">
                                            <asp:LinkButton UseSubmitBehavior="False" ID="MovePrevious" runat="server" CommandName="MovePrevious" Text="Choose the type of Workspace" />
                                        </li>
                                        <li class="snWsWizardActive_2">
                                            <asp:Literal ID="Literal7" runat="server" Text="<%$ Resources: WorkspaceWizard, WorkspaceName %>" />
                                            <br/>
                                            <span class="snWsWizardSubscript">
                                                <asp:Literal ID="Literal8" runat="server" Text="<%$ Resources: WorkspaceWizard, Step %>" />
                                            </span>
                                        </li>
                                        <li class="snWsWizardAvailable_3">
                                            <asp:LinkButton UseSubmitBehavior="True" ID="MoveNext" runat="server" CommandName="MoveNext" Text="<%$ Resources: WorkspaceWizard, WizardFewSeconds %>" />
                                        </li>
                                      </ol>
                                  </div>
                                </div>  
                              </div>
                            </CustomNavigationTemplate>
                        </asp:TemplatedWizardStep>
                        <asp:TemplatedWizardStep ID="Progress" runat="server">
                            <ContentTemplate>
                                <div class="snWsWizardMain">
                                    <div class="snWsWizardMainLeft snWsWizardRoundedTable">
                                      <div class="nw">
                                        <div class="ne">
                                          <div class="n"></div>
                                        </div>
                                      </div>
                                      <div class="w">
                                        <div class="e">
                                          <div class="c">
                                              <h2><asp:Label ID="ProgressHeaderLabel" runat="server" ></asp:Label></h2>                                    
                                              <asp:LinkButton UseSubmitBehavior="False" ID="CreateWorkspaceNowButton" runat="server" CommandName="MoveNext" Text="<%$ Resources:WorkspaceWizard,CreateWorkspaceNow %>" />
                                          </div>
                                        </div>
                                      </div>                    
                                      <div class="sw">
                                        <div class="se">
                                          <div class="s"></div>
                                        </div>
                                      </div>                                      
                                    </div>
                                    <div class="snWsWizardMainRight"></div>
                              </div>
                            </ContentTemplate>
                            <CustomNavigationTemplate>
                              <div class="sn-pt-header">
                                <div class="sn-pt-header-tl"></div>
                                <div class="sn-pt-header-center">
                                  <div class="sn-pt-title">
                                    <asp:Literal ID="Literal9" runat="server" Text="<%$ Resources: WorkspaceWizard, ThreeSteps %>" />
                                  </div>
                                </div>
                              </div>
                              <div class="sn-pt-body-border ui-widget-content">
                                <div class="sn-pt-body">
                                  <div class="snWsWizardFooter">
                                      <ol class="snWsWizardProgButtons">
                                        <li class="snWsWizardUnavailable_1">
                                            <asp:Literal ID="Literal10" runat="server" Text="<%$ Resources: WorkspaceWizard, ChooseType %>" />
                                        </li>
                                        <li class="snWsWizardAvailable_2">
                                            <asp:LinkButton UseSubmitBehavior="False" ID="MovePrevious" runat="server" CommandName="MovePrevious" Text="Name your workspace" />
                                        </li>
                                        <li class="snWsWizardActive_3">
                                            <asp:Literal ID="Literal11" runat="server" Text="<%$ Resources: WorkspaceWizard, WorkspaceName %>" />
                                            <br/>
                                            <span class="snWsWizardSubscript">
                                                <asp:Literal ID="Literal12" runat="server" Text="<%$ Resources: WorkspaceWizard, Step %>" />
                                            </span>
                                        </li>
                                      </ol>
                                  </div>
                                </div>  
                              </div>
                            </CustomNavigationTemplate>
                        </asp:TemplatedWizardStep>
                        <asp:TemplatedWizardStep ID="Complete" runat="server">
                            <ContentTemplate>
                                <div class="snWsWizardMain">
                                <div class="snWsWizardMainLeft">
                                    <h2><asp:Literal ID="Literal13" runat="server" Text="<%$ Resources: WorkspaceWizard, ReadyToUse %>" /></h2>
                                </div>
                                <div class="snWsWizardMainRight"></div>
                                </div>
                            </ContentTemplate>
                            <CustomNavigationTemplate>
                                <div class="snWsWizardFooter">
                                    <div class="snWsWizardReadyButton">
                                        <asp:Literal ID="Literal14" runat="server" Text="<%$ Resources: WorkspaceWizard, JumpToWorkspace %>" />
                                        <asp:Label ID="NewWorkspaceName" runat="server" ForeColor="Red"></asp:Label>
                                        <a href="" id="NewWorkspaceLink" runat="server">GO</a>
                                    </div>
                                </div>
                            </CustomNavigationTemplate>
                        </asp:TemplatedWizardStep>
                    </WizardSteps>
                </asp:Wizard>
            </contenttemplate>
</asp:UpdatePanel>
<asp:UpdateProgress ID="UpdateProgress1" AssociatedUpdatePanelID="WizardUpdatePanel"
    runat="server" DisplayAfter="0" DynamicLayout="true">
    <progresstemplate>
        <div class="snWsWizardWait">
            <asp:Literal ID="Literal" runat="server" Text="<%$ Resources: WorkspaceWizard, WorkInProgress %>" />
        </div>
    </progresstemplate>
</asp:UpdateProgress>
