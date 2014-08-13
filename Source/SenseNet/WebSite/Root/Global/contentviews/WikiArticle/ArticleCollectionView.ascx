<%@ Control Language="C#" AutoEventWireup="true" Inherits="SenseNet.Portal.Portlets.ContentCollectionView" EnableViewState="false" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="SenseNet.Portal.Helpers" %>
    
<div class="sn-contentlist">
 
<%foreach (var content in this.Model.Items)
  { %>
    <div class="sn-content sn-contentlist-item">
        <h1 class="sn-content-title">
            <%=Actions.BrowseAction(HttpUtility.HtmlEncode(content))%>
        </h1>
        <span class='<%= "sn-date-" + content["Id"] %>'><%= content["ModificationDate"]%></span>,  <%= content["ModifiedBy"]%>  
    </div>

    <script>
        $(function () {
            $dateField = $('<%= "sn-date-" + content["Id"] %>');
        var dateFormat = '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern %>';
        var timeFormat = '<%= System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern %>';
            var date = $.datepicker.formatDate(dateFormat.toLowerCase().replace('yyyy', 'yy'), new Date(new Date('<%= content["ModificationDate"]%><' + ' UTC')));
        var f = 24;
        if (timeFormat.indexOf('tt') > -1) {
            f = 12;
        }
        $dateField.text(date + ' ' + SN.Util.formatTimeBasedOnTimeFormat(f, new Date(new Date('<%= content["ModificationDate"]%><' + ' UTC'))));
    });
</script>

<%} %>

</div>




