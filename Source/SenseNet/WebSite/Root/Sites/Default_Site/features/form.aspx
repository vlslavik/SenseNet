﻿<%@ Page Language="C#" CompilationMode="Never" MasterPageFile="~/Root/Global/pagetemplates/sn-layout-inter.Master" %><asp:Content ID="Content_FullCol" ContentPlaceHolderID="CPFullCol" runat="server"><asp:WebPartZone ID="FullCol" name="FullCol" HeaderText="Full Column" PartChromeType="None"  runat="server"><ZoneTemplate><snpe:BreadCrumbPortlet BindTarget="CurrentContent" ID="BreadCrumb" runat="server" Separator=" / " ShowSite="True" SiteDisplayName="Home" ChromeType="BorderOnly" /></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_LeftCol" ContentPlaceHolderID="CPLeftCol" runat="server"><asp:WebPartZone ID="LeftCol" name="LeftCol" HeaderText="Left Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate><snpe:SiteMenu Title="Features" BindTarget="CurrentWorkspace" ShowPagesOnly="false" GetContextChildren="true" OmitContextNode="true" ExpandToContext="true" Depth="1" ID="MainMenu" runat="server" /></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_WideCol" ContentPlaceHolderID="CPWideCol" runat="server"><asp:WebPartZone ID="WideCol" name="WideCol" HeaderText="Wide Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterCol" ContentPlaceHolderID="CPCenterCol" runat="server"><asp:WebPartZone ID="CenterCol" name="CenterCol" HeaderText="Center Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterLeftCol" ContentPlaceHolderID="CPCenterLeftCol" runat="server"><asp:WebPartZone ID="CenterLeftCol" name="CenterLeftCol" HeaderText="Center / Left Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterRightCol" ContentPlaceHolderID="CPCenterRightCol" runat="server"><asp:WebPartZone ID="CenterRightCol" name="CenterRightCol" HeaderText="Center / Right Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_RightCol" ContentPlaceHolderID="CPRightCol" runat="server"><asp:WebPartZone ID="RightCol" name="RightCol" HeaderText="Right Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_FooterLeft" ContentPlaceHolderID="CPFooterLeft" runat="server"><asp:WebPartZone ID="FooterLeft" name="FooterLeft" headertext="Footer" partchrometype="None"  runat="server"><ZoneTemplate><snpe:SingleContentPortlet UsedContentTypeName="HTMLContent" ContentPath="/Root/YourContents/FooterContent" ID="FooterContent2" runat="server" /></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_Footer" ContentPlaceHolderID="CPFooter" runat="server"><asp:WebPartZone ID="Footer" name="Footer" headertext="FooterRight" partchrometype="None"  runat="server"><ZoneTemplate><snpe:ContentCollectionPortlet Title="FooterMenu1" ChromeType="None" BindTarget="CustomRoot" CustomRootPath="/Root/Sites/Default_Site/workspaces" Renderer="/Root/Global/renderers/sitemapMenu.xslt" ID="FooterMenu1" runat="server" SkinPreFix="footer-ws" /><snpe:ContentCollectionPortlet Title="FooterMenu2" ChromeType="None" BindTarget="CustomRoot" CustomRootPath="/Root/Sites/Default_Site/NewsDemo" Renderer="/Root/Global/renderers/sitemapMenu.xslt" ID="FooterMenu2" runat="server" SkinPreFix="footer-newsdemo" /><snpe:ContentCollectionPortlet Title="FooterMenu3" ChromeType="None" BindTarget="CustomRoot" CustomRootPath="/Root/Sites/Default_Site/features" Renderer="/Root/Global/renderers/sitemapMenu.xslt" ID="FooterMenu3" runat="server" SkinPreFix="footer-features" /></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_FooterLink" ContentPlaceHolderID="CPFooterLink" runat="server"><asp:WebPartZone ID="FooterLink" name="FooterLink" headertext="FooterLinks" partchrometype="None"  runat="server"><ZoneTemplate><snpe:SingleContentPortlet UsedContentTypeName="HTMLContent" ContentPath="/Root/YourContents/footer-links" ID="FooterContent1" runat="server" CssClass="usefulLinks" /></ZoneTemplate></asp:WebPartZone></asp:Content>