<%@ Control Language="C#" AutoEventWireup="false" Inherits="System.Web.UI.UserControl" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="SenseNet.Portal.Portlets" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>


<% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
   { %>
<sn:ScriptRequest ID="Scriptrequest2" runat="server" Path="$skin/scripts/kendoui/kendo.web.min.js" />
<sn:ScriptRequest ID="kendo1" runat="server" Path="$skin/scripts/kendoui/kendo.data.min.js" />
<sn:ScriptRequest ID="kendo2" runat="server" Path="$skin/scripts/kendoui/kendo.dataviz.min.js" />
<sn:CssRequest ID="Cssrequest1" runat="server" CSSPath="$skin/styles/kendoui/kendo.common.min.css" />
<sn:CssRequest ID="kendocss1" runat="server" CSSPath="$skin/styles/kendoui/kendo.dataviz.min.css" />
<sn:CssRequest ID="kendocss2" runat="server" CSSPath="$skin/styles/kendoui/kendo.dataviz.metro.min.css" />
<%} %>

<style>
    .sn-kpi-gauge > div {
        width: 200px;
        height: 200px;
        margin: 0px auto;
    }
</style>

<%
    var portlet = this.Parent as GlobalKPIPortlet;
    var kpiDS = portlet.ContextNode as KPIDatasource; 
%>

<% string user = (SenseNet.ContentRepository.User.Current).ToString(); %>
<%if (user == "Visitor" || kpiDS == null)
  {%>
<div class="sn-pt-body-border ui-widget-content ui-corner-all">
    <div class="sn-pt-body ui-corner-all">
        <%=GetGlobalResourceObject("Portal", "WSContentList_Visitor")%>
    </div>
</div>
<% }%>
<%else
  {%>

<table class="sn-kpi-meter">
    <thead>
        <tr>
            <th>&nbsp;</th>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %><th><%= HttpUtility.HtmlEncode(kpiData.Label)%></th>
            <% } %>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>&nbsp;</td>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               {

                   int pic = 0;
                   double percent = ((double)kpiData.Actual / (double)kpiData.Goal * 100);
                   if (percent < 40)
                       pic = 10;
                   else if (percent < 50)
                       pic = 40;
                   else if (percent < 90)
                       pic = 50;
                   else
                       pic = 90;
            %>
            <td class="sn-kpi-gauge">
                <% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
                   { %>
                <%= percent %>
                <%}
                   else
                   {%>
                <img src="/Root/Global/images/gauge<%= pic %>.gif" alt="<%= percent %>%" title="<%= percent %>%" />
                <% } %>
            </td>
            <% } %>
        </tr>
        <tr>
            <th><%=GetGlobalResourceObject("KPIRenderers", "Goal")%></th>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %><td><%=kpiData.Goal.ToString()%></td>
            <% } %>
        </tr>
        <tr>
            <th><%=GetGlobalResourceObject("KPIRenderers", "Actual")%></th>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %><td><%=kpiData.Actual.ToString()%></td>
            <% } %>
        </tr>
    </tbody>
</table>
<%} %>

<% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
   { %>
<script>
    $(function () {
        var num = 0;
        $('.sn-kpi-gauge').each(function () {
            var that = $(this);
            var value = that.text();

            $gauge = $('<div id="gauge' + num + '"></div>');
            that.html($gauge);
            $gauge.kendoRadialGauge({

                pointer: {
                    value: value
                },
                gaugeArea: {
                    width: 200,
                    heihgt: 200
                },
                scale: {
                    minorUnit: 5,
                    startAngle: -30,
                    endAngle: 210,
                    max: 100,
                    labels: {
                        position: "inside"
                    },
                    ranges: [
                        {
                            from: 20,
                            to: 40,
                            color: "#ffc700"
                        }, {
                            from: 40,
                            to: 70,
                            color: "#ff7a00"
                        }, {
                            from: 70,
                            to: 100,
                            color: "#c20000"
                        }
                    ]
                }
            });

        });
    });
</script>
<%} %>