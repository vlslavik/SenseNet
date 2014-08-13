<span class='<%# "sn-date-" + SenseNet.Portal.UI.UITools.GetClassForField(Container.DataItem, "@@fieldName@@") %>'>
</span>
<script> $(function () { SN.Util.setFriendlyLocalDate('<%# "span.sn-date-" +
    SenseNet.Portal.UI.UITools.GetClassForField(Container.DataItem, "@@fieldName@@")%>', '<%= 
    System.Globalization.CultureInfo.CurrentUICulture %>', '<%# 
    (Container.DataItem as SNCR.Content).Fields.ContainsKey("@@fieldName@@") &&
        ((Container.DataItem as SNCR.Content).Fields["@@fieldName@@"].FieldSetting as SNCR.Fields.DateTimeFieldSetting).DateTimeMode == SNCR.Fields.DateTimeMode.Date 
            ? ((DateTime)Eval("@@bindingName@@")).ToString("M/d/yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")) 
            : ((DateTime)Eval("@@bindingName@@")).ToString(System.Globalization.CultureInfo.GetCultureInfo("en-US")) %>', '<%=
    System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern.ToUpper() %>', <%# 
    (((Container.DataItem as SNCR.Content).Fields["@@fieldName@@"].FieldSetting as SNCR.Fields.DateTimeFieldSetting).DateTimeMode != SNCR.Fields.DateTimeMode.Date).ToString().ToLower() %>); }); </script>