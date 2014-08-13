<%@ Control Language="C#" AutoEventWireup="false" Inherits="System.Web.UI.UserControl" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="SenseNet.Portal.Portlets" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>

<% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
   { %>
<style>
    .progress {
        width: 200px;
        height: 200px;
    }

        .progress svg {
            margin: 0px auto;
        }

        .progress g path:last-of-type {
            display: none !important;
        }
</style>
<%} %>

<%
    var portlet = this.Parent as GlobalKPIPortlet;
    var kpiDS = portlet.ContextNode as KPIDatasource;

    var actualMax = kpiDS == null ? 0 : kpiDS.KPIDataList.Max(d => d.Actual);
    var goalMax = kpiDS == null ? 0 : kpiDS.KPIDataList.Max(d => d.Goal);
    var max = actualMax > goalMax ? actualMax : goalMax;
%>

<% string user = (SenseNet.ContentRepository.User.Current).ToString(); %>


<table class="sn-kpi-states">
    <tbody>
        <tr>
            <td>&nbsp;</td>
            <% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
               { %>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %>
            <td class="progress">
                <span class="sn-progress sn-progress-vert sn-kpi-goal"><%= ((double)kpiData.Goal / (double)max * 100).ToString("N") %></span>
                <span class="sn-progress sn-progress-vert sn-kpi-actual"><%= ((double)kpiData.Actual / (double)max * 100).ToString("N") %></span>
            </td>

            <% } %>
            <% }
               else
               { %>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %>
            <td>
                <span class="sn-progress sn-progress-vert sn-kpi-goal"><span style="height: <%= ((double)kpiData.Goal / (double)max * 100).ToString("N") %>%" title="<%= GetGlobalResourceObject("KPIRenderers", "Goal") + ":" + kpiData.Goal.ToString()%>"><%=kpiData.Goal.ToString()%></span></span>
                <span class="sn-progress sn-progress-vert sn-kpi-actual"><span style="heigth: <%= ((double)kpiData.Actual / (double)max * 100).ToString("N") %>%" title="<%=GetGlobalResourceObject("KPIRenderers", "Actual") + ":" + kpiData.Actual.ToString()%>"><%=kpiData.Actual.ToString()%></span></span>
            </td>
            <% } %>

            <% } %>
        </tr>
        <tr>
            <th class="sn-kpi-goal"><%=GetGlobalResourceObject("KPIRenderers", "Goal")%></th>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %><td><%=kpiData.Goal.ToString()%></td>
            <% } %>
        </tr>
        <tr>
            <th class="sn-kpi-actual"><%=GetGlobalResourceObject("KPIRenderers", "Actual")%></th>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %><td><%=kpiData.Actual.ToString()%></td>
            <% } %>
        </tr>
    </tbody>
    <tfoot>
        <tr>
            <th>&nbsp;</th>
            <% foreach (var kpiData in kpiDS.KPIDataList)
               { %><th><%= HttpUtility.HtmlEncode(kpiData.Label)%></th>
            <% } %>
        </tr>
    </tfoot>
</table>

<% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
   { %>
<script>
    $(function () {

        var num = 0;
        $('.progress').each(function () {
            var that = $(this);
            var value1 = that.children('.sn-kpi-goal').text();
            var value2 = that.children('.sn-kpi-actual').text();

            $chart = $('<span id="chart' + num + '"></span>');
            that.html($chart);


            $chart.kendoChart({
                chartArea: {
                    width: 150,
                    height: 150
                },
                seriesDefaults: {
                    type: "column"
                },
                series: [{
                    data: [value1],
                    color: "#0085CF"
                },
                {
                    data: [value2],
                    color: "#3D99CC"
                }],
                tooltip: {
                    visible: false,
                    format: "{0}"
                },
                valueAxis: {
                    majorGridLines: {
                        visible: false
                    },
                    min: 0,
                    max: 100,
                    line: {
                        visible: false
                    },
                    labels: {
                        visible: false
                    }
                },
                categoryAxis: {
                    majorGridLines: {
                        visible: false
                    },
                    line: {
                        visible: false
                    },
                    labels: {
                        visible: false
                    }
                },
                tooltip: {
                    visible: false
                }
            });

        });

    });
</script>
<%} %>