﻿<%@ Page Language="C#" CompilationMode="Never" MasterPageFile="~/Root/Global/PageTemplates/sn-layout-intra.Master" %><asp:Content ID="Content_LeftCol" ContentPlaceHolderID="CPLeftCol" runat="server"><asp:WebPartZone ID="LeftCol" name="LeftCol" HeaderText="Left Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate><snpe:ContentCollectionPortlet Title="Main Menu" ChromeType="BorderOnly" BindTarget="CurrentWorkspace" Renderer="/Root/Global/renderers/SiteMenu/SiteMenuCollectionView.ascx" SortBy="Index" ID="MainMenu" runat="server" /></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_WideCol" ContentPlaceHolderID="CPWideCol" runat="server"><asp:WebPartZone ID="WideCol" name="WideCol" HeaderText="Wide Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_RightCol" ContentPlaceHolderID="CPRightCol" runat="server"><asp:WebPartZone ID="RightCol" name="RightCol" HeaderText="Right Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterCol" ContentPlaceHolderID="CPCenterCol" runat="server"><asp:WebPartZone ID="CenterCol" name="CenterCol" HeaderText="Center Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_HalfColLeft" ContentPlaceHolderID="CPHalfColLeft" runat="server"><asp:WebPartZone ID="HalfColLeft" name="HalfColLeft" HeaderText="Half Column - Left" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_HalfColRight" ContentPlaceHolderID="CPHalfColRight" runat="server"><asp:WebPartZone ID="HalfColRight" name="HalfColRight" HeaderText="Half Column - Right" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterRightCol" ContentPlaceHolderID="CPCenterRightCol" runat="server"><asp:WebPartZone ID="CenterRightCol" name="CenterRightCol" HeaderText="Center / Left Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content><asp:Content ID="Content_CenterLeftCol" ContentPlaceHolderID="CPCenterLeftCol" runat="server"><asp:WebPartZone ID="CenterLeftCol" name="CenterLeftCol" HeaderText="Center / Right Column" PartChromeType="TitleAndBorder"  runat="server"><ZoneTemplate></ZoneTemplate></asp:WebPartZone></asp:Content>