<%@  Language="C#" %>
<%@ Import Namespace="System.Globalization" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>
<%@ Import Namespace="SNControls=SenseNet.Portal.UI.Controls" %>
<%@ Import Namespace="SNFields=SenseNet.ContentRepository.Fields" %>
<%@ Import Namespace="SenseNet.ContentRepository.Storage" %>
<%@ Import Namespace="SenseNet.Portal.UI" %>

<sn:ScriptRequest ID="ScriptRequest1" runat="server" Path='<%# ((SNControls.DatePicker)Container).Mode == SNFields.DateTimeMode.None ? string.Empty : "$skin/scripts/jqueryui/minified/jquery-ui.min.js" %>' />
<sn:ScriptRequest ID="ScriptRequest2" runat="server" Path='<%# CultureInfo.CurrentUICulture.Name == "en-US" || ((SNControls.DatePicker)Container).Mode == SNFields.DateTimeMode.None ? string.Empty : RepositoryPath.Combine(UITools.ClientScriptConfigurations.JQueryUIFolderPath, "i18n/jquery.ui.datepicker-" + CultureInfo.CurrentUICulture.Name + (UITools.ClientScriptConfigurations.JQueryUIFolderPath.Contains("/minified") ? ".min" : "") + ".js") %>' />
<sn:ScriptRequest ID="ScriptRequest3" runat="server" Path='$skin/scripts/sn/SN.Util.js' />
<sn:ScriptRequest ID="ScriptRequest5" runat="server" Path='$skin/scripts/moment/moment.min.js' />
<sn:ScriptRequest ID="ScriptRequest4" runat="server" Path='$skin/scripts/ODataManager.js' />

<sn:CssRequest ID="CssRequest1" runat="server" CSSPath='<%# ((SNControls.DatePicker)Container).Mode == SNFields.DateTimeMode.None ? string.Empty : "$skin/styles/jqueryui/jquery-ui.css" %>' />

<asp:PlaceHolder ID="InnerDateHolder" runat="server">
    <%=GetGlobalResourceObject("FieldControlTemplates", "Date")%><asp:TextBox ID="InnerControl" runat="server" CssClass='<%# "sn-ctrl sn-ctrl-text sn-ctrl-date sn-datepicker-" + UITools.GetFieldNameClass(Container) %>' Style="width: 100px;"></asp:TextBox>
</asp:PlaceHolder>
<asp:PlaceHolder ID="InnerTimeHolder" runat="server">
    <%=GetGlobalResourceObject("FieldControlTemplates", "Time")%><asp:TextBox ID="InnerTimeTextBox" runat="server" CssClass='<%# "sn-ctrl sn-ctrl-text sn-ctrl-time-" + UITools.GetFieldNameClass(Container) %>' Style="width: 100px;"></asp:TextBox>
</asp:PlaceHolder>
<asp:PlaceHolder ID="InnerTimZoneOffsetHolder" runat="server">
    <asp:TextBox ID="InnerTimeZoneOffsetTextBox" runat="server" CssClass='<%# "sn-ctrl sn-ctrl-text sn-ctrl-timezoneoffset-" + UITools.GetFieldNameClass(Container) %>' Style="display: none;"></asp:TextBox>
</asp:PlaceHolder>
<asp:Label ID="DateFormatLabel" runat="server" CssClass="sn-iu-desc" /><br />
<asp:Label ID="TimeFormatLabel" runat="server" CssClass="sn-iu-desc" />

<script type="text/javascript" language="javascript">
    $(function () {
        if (<%# ((SNControls.DatePicker)Container).Mode != SNFields.DateTimeMode.None ? "true" : "false" %>) {
  
            //get date and time field values
            $dateField = $('<%# "input.sn-datepicker-" + UITools.GetFieldNameClass(Container) %>');
            $timeField = $('<%# "input.sn-ctrl-time-" + UITools.GetFieldNameClass(Container) %>');

            //init jqueryui datepicker
            $dateField.datepicker(<%# ((SNControls.DatePicker)Container).Configuration %>);

            var originalDate = $dateField.val();

            if(originalDate.length === 0){
                $dateField.val();
            }
            else{
                //set date field's value (converted from utc to local based on datepicker control's dateformat)
                $dateField.val($.datepicker.formatDate($dateField.datepicker( "option", "dateFormat" ), new Date(SN.Util.mergeDateAndTimeFieldLocal($dateField.val(), $timeField.val()))));
            }

            // set local time only if there is a time control
            if ($timeField.length > 0) {
                if ($timeField.val().length === 0) {
                    $timeField.val();
                } else {
                    //set time field's value
                    $timeField.val(SN.Util.formatTimeBasedOnTimeFormat(24, new Date(SN.Util.mergeDateAndTimeFieldLocal(originalDate, $timeField.val()))));
                }
            }

            //store the difference from UTC time in minutes
            $timeZoneOffset = $('<%# "input.sn-ctrl-timezoneoffset-" + UITools.GetFieldNameClass(Container) %>');

            var today = new Date(new Date(SN.Util.mergeDateAndTimeFieldLocal(originalDate, $dateField.next('<%# "input.sn-ctrl-time-" + UITools.GetFieldNameClass(Container) %>').val())));
            $dateField.siblings('<%# "input.sn-ctrl-timezoneoffset-" + UITools.GetFieldNameClass(Container) %>').val(today.getTimezoneOffset());

            $dateField.on('change', function(){
                var today = new Date(new Date(SN.Util.mergeDateAndTimeFieldLocal($(this).val(), $(this).next('<%# "input.sn-ctrl-time-" + UITools.GetFieldNameClass(Container) %>').val())));
                $(this).siblings('<%# "input.sn-ctrl-timezoneoffset-" + UITools.GetFieldNameClass(Container) %>').val(today.getTimezoneOffset());
            });
            $dateField.on('keyup', function(){
                var today = new Date(new Date(SN.Util.mergeDateAndTimeFieldLocal($(this).val(), $(this).next('<%# "input.sn-ctrl-time-" + UITools.GetFieldNameClass(Container) %>').val())));
                $timeZoneOffset.val(today.getTimezoneOffset());
                $(this).siblings('<%# "input.sn-ctrl-timezoneoffset-" + UITools.GetFieldNameClass(Container) %>').val(today.getTimezoneOffset());
            });
        }
    }); 
</script>
