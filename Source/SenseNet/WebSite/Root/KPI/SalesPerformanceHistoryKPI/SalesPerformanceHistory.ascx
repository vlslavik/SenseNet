<%@ Control Language="C#" AutoEventWireup="false" Inherits="System.Web.UI.UserControl" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="SenseNet.Portal.Portlets" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
<%@ Import Namespace="SenseNet.ContentRepository" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>

<%  
    var portlet = this.Parent as GlobalKPIPortlet;
    var kpiDS = portlet.ContextNode as KPIDatasource;

    var actualMax = kpiDS == null ? 0 : kpiDS.KPIDataList.Max(d => d.Actual);
    var goalMax = kpiDS == null ? 0 : kpiDS.KPIDataList.Max(d => d.Goal);
    var max = actualMax > goalMax ? actualMax : goalMax;
%>
<% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
                { %>
<style>
    .sn-kpi-history div {
        height: 120px;
        width: 160px;
    }
        .sn-kpi-history g path:last-of-type {
            display: none !important;
        }
</style>
<%} %>

<% string user = (SenseNet.ContentRepository.User.Current).ToString(); %>
<%if (user == "Visitor")
  {%>
<div class="sn-pt-body-border ui-widget-content ui-corner-all">
    <div class="sn-pt-body ui-corner-all">
        <%=GetGlobalResourceObject("Portal", "WSContentList_Visitor")%>
    </div>
</div>
<% }%>
<%else
  {%>

<div>
    <% foreach (var kpiData in kpiDS.KPIDataList)
       { %>

    <div class="sn-kpi-history">
        <h3><%= HttpUtility.HtmlEncode(kpiData.Label)%></h3>
        <div class="sn-progress sn-kpi-goal">
            <% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
                { %>
                 <%=kpiData.Goal.ToString()%>
            <% } else { %>
                <span style="width:<%= ((double)kpiData.Goal / (double)max * 100).ToString("N") %>%"></span>
                <em><%=GetGlobalResourceObject("KPIRenderers", "Goal")%>: <%=kpiData.Goal.ToString()%></em>
            <%} %>
        </div>
        <div class="sn-progress sn-kpi-actual">
            <% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
                { %>
                <%=kpiData.Actual.ToString()%>
            <% } else { %>
                <span style="width:<%= ((double)kpiData.Actual / (double)max * 100).ToString("N") %>%"></span>
                <em><%=GetGlobalResourceObject("KPIRenderers", "Actual")%>: <%=kpiData.Actual.ToString()%></em>
            <%} %>
        </div>
    </div>

    <% } %>
</div>
<%} %>
<% if (RepositoryVersionInfo.Instance.OfficialSenseNetVersion.Edition != "Community")
   { %>
<script>
    $(function () {
        $('.sn-kpi-history').each(function () {
            var that = $(this);
            var goal = parseInt(that.children('.sn-kpi-goal').text().trim());
            var actual = parseInt(that.children('.sn-kpi-actual').text().trim());
            console.log(goal);
            console.log(actual);
            that.children('div').remove();
            that.append('<div class="sint-chart"></div>');
            that.children('.sint-chart').kendoChart({
                legend: {
                    visible: false
                },
                seriesDefaults: {
                    type: "bar",
                    width: 100
                },
                series: [{
                    name: "Goal",
                    width: 150,
                    height: 150,
                    labels: {
                        visible: true,
                        position: "insideBase",
                        background: "transparent",
                        template: "Goal: #: value #"
                    },
                    gap: 2,
                    spacing: 0.4,
                    data: [goal],
                    color: '#FF5A65',
                    width: 150,
                    height: 150
                }, {
                    name: "Actual",
                    labels: {
                        visible: true,
                        position: "insideBase",
                        background: "transparent",
                        template: "Actual: #: value #"
                    },
                    gap: 2,
                    spacing: 0.4,
                    data: [actual],
                    color: '#FF8F96'
                }],
                valueAxis: {
                    line: {
                        visible: false
                    },
                    minorGridLines: {
                        visible: false
                    },
                    majorGridLines: {
                        visible: false
                    },
                    visible: false,
                    min: 0,
                    max: 1300000
                },
                categoryAxis: {
                    majorGridLines: {
                        visible: false
                    },
                    line: {
                        visible: false
                    },
                    visible: false
                },
                tooltip: {
                    visible: false
                }
            });
        });
    });
</script>
<%} %>
