using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.UI;

using SenseNet.ContentRepository;
using System.Globalization;

namespace SenseNet.Portal.UI.Controls
{
    [ToolboxData("<{0}:DatePicker ID=\"DatePicker1\" runat=server></{0}:DatePicker>")]
    public class DatePicker : FieldControl, INamingContainer, ITemplateFieldControl
    {
        public enum DateTimePatternType { Default, ShortDate, ShortTime }

        // Members //////////////////////////////////////////////////////////////////////
        protected string TimeControlID = "InnerTimeTextBox";
        protected string TimeZoneOffsetControlID = "InnerTimeZoneOffsetTextBox";
        public event EventHandler OnDateChanged;

        private object _innerData;
        private string _dateTimeText;
        private string _timeText;

        #region properties

        private string _configuration = "{format:'yy.m.d',allowBlank:true,firstDay:" + GetCurrentFirstDay() + "}"; // Default configuration
        [PersistenceMode(PersistenceMode.Attribute)]
        public string Configuration
        {
            get { return _configuration; }
            set { _configuration = value; }
        }

        [PersistenceMode(PersistenceMode.Attribute)]
        public bool AutoPostBack { get; set; }

        #endregion

        public DateTimeMode Mode
        {
            get
            {
                var fieldSetting = Field.FieldSetting as DateTimeFieldSetting;
                if (fieldSetting == null)
                    return DateTimeMode.None;

                return !fieldSetting.DateTimeMode.HasValue ? DateTimeMode.None : fieldSetting.DateTimeMode.Value;
            }
        }

        // Methods //////////////////////////////////////////////////////////////////////
        public override void SetData(object data)
        {
            //collect controls
            var title = GetLabelForTitleControl() as Label;
            var desc = GetLabelForDescription() as Label;
            var dateControl = GetInnerControl() as TextBox;
            var timeControl = GetTimeControl() as TextBox;

            _innerData = data;

            switch (Mode)
            {
                case DateTimeMode.None:
                    // only one textbox appears on page that handles datetime
                    // in this case, no scripts are rendered
                    ProcessNoneMode(data);
                    break;
                case DateTimeMode.Date:
                    // one textbox appears on page that handles Date
                    ProcessDateMode(data);
                    break;
                case DateTimeMode.DateAndTime:
                    if (timeControl != null)
                    {
                        // two textboxes appear on the page, one for handling date and one for handling time value
                        ProcessDateTimeMode(data);
                    }
                    else
                    {
                        // only one textbox appears on the page that handles date and time
                        ProcessNoneMode(data);
                    }
                    break;
                default:
                    break;
            }

            if (title != null) title.Text = HttpUtility.HtmlEncode(Field.DisplayName);
            if (desc != null)
            {
                desc.Text = Sanitizer.Sanitize(Field.Description);
                var dateTimeFormat = System.Threading.Thread.CurrentThread.CurrentUICulture.DateTimeFormat;
                var shortDatePattern = dateTimeFormat.ShortDatePattern;
                var timePattern = dateTimeFormat.ShortTimePattern;
                var pattern = string.Empty;
                switch (Mode)
                {
                    case DateTimeMode.None:
                    case DateTimeMode.DateAndTime:
                        var patternWithTime = HttpContext.GetGlobalResourceObject("Portal", "DateFieldDateTimeFormatDescription") as string ?? "{0} - {1}";
                        pattern = String.Format(patternWithTime, shortDatePattern, timePattern);
                        break;
                    case DateTimeMode.Date:
                        var patternWithoutTime = HttpContext.GetGlobalResourceObject("Portal", "DateFieldDateFormatDescription") as string ?? "{0}";
                        pattern = String.Format(patternWithoutTime, shortDatePattern);
                        break;
                    default:
                        break;
                }

                var text = desc.Text.TrimEnd();
                if (!string.IsNullOrEmpty(text))
                    text = string.Concat(text, "<br />");
                desc.Text = string.Concat(text, pattern);
            }

            if (dateControl != null)
                dateControl.Text = _dateTimeText;
            if (timeControl != null && Mode == DateTimeMode.DateAndTime)
                timeControl.Text = GetTime(data);
        }
        public override object GetData()
        {
            var format = new DateTimeFormatInfo { ShortDatePattern = CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern };

            var innerDateTextBox = GetInnerControl() as TextBox;
            var innerTimeTextBox = GetTimeControl() as TextBox;
            var innerTimeZoneOffsetTextBox = GetTimeZoneOffsetControl() as TextBox;

            // in browse mode we don't have fieldcontrols
            if (innerDateTextBox == null)
                return _innerData;

            // two textboxes appear on the page, one for handling date and one for handling time value
            var innerDateValue = innerDateTextBox.Text;
            var innerTimeValue = innerTimeTextBox != null ? innerTimeTextBox.Text : string.Empty;

            switch (Mode)
            {
                case DateTimeMode.None:
                    if (string.IsNullOrEmpty(innerDateValue))
                        return null;
                    return DateTime.Parse(innerDateValue);
                case DateTimeMode.Date:
                    if (string.IsNullOrEmpty(innerDateValue))
                        return null;
                    return DateTime.Parse(innerDateValue, format);
                case DateTimeMode.DateAndTime:
                    if (string.IsNullOrEmpty(innerDateValue) && string.IsNullOrEmpty(innerTimeValue))
                        return null;

                    var date = string.IsNullOrEmpty(innerDateValue) ? DateTime.Today : Convert.ToDateTime(innerDateValue);

                    var time = DateTime.Today.TimeOfDay;
                    var checkTime = true;

                    if (string.IsNullOrEmpty(innerTimeValue))
                    {
                        if (innerTimeTextBox != null)
                        {
                            //time textbox is empty, use the current time
                            time = DateTime.Today.TimeOfDay;
                            checkTime = false;
                        }
                        else
                        {
                            //there is no time textbox, use the time from 
                            //the first textbox, same as in None mode
                            time = date.TimeOfDay;
                            date = date.Date;

                            innerTimeValue = time.ToString();
                            checkTime = true;
                        }
                    }

                    if (checkTime)
                    {
                        // check if user did not change time (time string is same as originally generated)
                        // if user did not change we don't update the time (since time displayed is not necessarily the same as in db due to precision hacks)
                        time = innerTimeValue == GetTime(_innerData)
                                   ? ((DateTime)_innerData).TimeOfDay
                                   : TimeSpan.Parse(innerTimeValue);
                    }

                    return ShiftTimeToUTC(date + time, innerTimeZoneOffsetTextBox == null ? string.Empty : innerTimeZoneOffsetTextBox.Text);
                default:
                    break;
            }
            return null;
        }

        // Events ///////////////////////////////////////////////////////////////////////
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (!AutoPostBack)
                return;

            var ic = GetInnerControl() as TextBox;
            if (ic != null)
                ic.TextChanged += _inputTextBox_TextChanged;
        }
        protected void _inputTextBox_TextChanged(object sender, EventArgs e)
        {
            if (this.OnDateChanged != null)
                this.OnDateChanged(this, e);
        }
        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (UseBrowseTemplate)
            {
                base.RenderContents(writer);
                return;
            }
            if (UseEditTemplate)
            {
                ManipulateTemplateControls();
                base.RenderContents(writer);
                return;
            }
        }

        #region ITemplateFieldControl Members
        public Control GetInnerControl() { return this.FindControlRecursive(InnerControlID); }
        public Control GetLabelForDescription() { return this.FindControlRecursive(DescriptionControlID); }
        public Control GetLabelForTitleControl() { return this.FindControlRecursive(TitleControlID); }
        public Control GetTimeControl()
        {
            return this.FindControlRecursive(TimeControlID);
        }
        public Control GetTimeZoneOffsetControl()
        {
            return this.FindControlRecursive(TimeZoneOffsetControlID);
        }
        #endregion

        // Internals ////////////////////////////////////////////////////////////////////
        private void ManipulateTemplateControls()
        {
            var ic = GetInnerControl() as TextBox;
            var timeControl = GetTimeControl() as TextBox;
            var offsetControl = GetTimeZoneOffsetControl() as TextBox;
            if (ic == null)
                return;

            if (Field.ReadOnly || ReadOnly)
            {
                ic.Enabled = false;
                ic.EnableViewState = true;
                if (timeControl != null) timeControl.Enabled = false;
                if (offsetControl != null) offsetControl.Enabled = false;
            }

            if (timeControl != null && Mode != DateTimeMode.DateAndTime)
            {
                var timeControlPlaceHolder = this.FindControlRecursive("InnerTimeHolder");
                if (timeControlPlaceHolder != null)
                    timeControlPlaceHolder.Visible = false;
            }
        }

        private static string GetTime(object data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (data is DateTime)
            {
                var dateTimeData = (DateTime)data;
                if (dateTimeData == DateTime.MinValue || dateTimeData == System.Data.SqlTypes.SqlDateTime.MinValue)
                    return string.Empty;

                return GetTimeOfDayText(dateTimeData);
            }

            return GetTimeOfDayText(Convert.ToDateTime(data));
        }
        private void ProcessDateTimeMode(object data)
        {
            ProcessDateMode(data);

            _timeText = data == null ? null : GetTime(data);
        }
        private void ProcessDateMode(object data)
        {
            if (data == null)
            {
                _dateTimeText = null;
            }
            else
            {
                if (data is DateTime)
                {
                    var dateTimeValue = Convert.ToDateTime(data);
                    if (dateTimeValue == DateTime.MinValue || dateTimeValue == System.Data.SqlTypes.SqlDateTime.MinValue)
                        _dateTimeText = string.Empty;
                    else
                        _dateTimeText = GetDateTimeTextForClient(dateTimeValue, DateTimePatternType.ShortDate);

                }
                else
                {
                    _dateTimeText = data.ToString();
                }
            }
        }
        private void ProcessNoneMode(object data)
        {
            _dateTimeText = data == null
                ? null
                : (data is DateTime ? GetDateTimeTextForClient(Convert.ToDateTime(data)) : data.ToString());
        }

        private static int GetCurrentFirstDay()
        {
            return (int)CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek;
        }

        private static string GetTimeOfDayText(DateTime dateTime)
        {
            //Workaround for not displaying the millisecond part of the time.
            //Unfortunately there is no culture-independent string format option for that.
            var time = dateTime.TimeOfDay;
            return new TimeSpan(time.Hours, time.Minutes, time.Seconds).ToString("t");
        }

        private static DateTime ShiftTimeToUTC(DateTime originalTime, string offsetInMinutes)
        { 
            int minutes;
            if (string.IsNullOrEmpty(offsetInMinutes) || !int.TryParse(offsetInMinutes, out minutes))
                return DateTime.SpecifyKind(originalTime, DateTimeKind.Utc);

            return DateTime.SpecifyKind(originalTime.AddMinutes(minutes), DateTimeKind.Utc);
        }

        private static string GetDateTimeTextForClient(DateTime datetime, DateTimePatternType dateTimePattern = DateTimePatternType.Default)
        {
            // We need to format everything in English for the client, as Javascript in some of 
            // the browsers does not understand datetime values in other language formats.
            var ci = CultureInfo.GetCultureInfo("en-US");

            switch (dateTimePattern)
            {
                case DateTimePatternType.ShortDate: 
                    return datetime.ToString(ci.DateTimeFormat.ShortDatePattern, ci);
                case DateTimePatternType.ShortTime: 
                    return datetime.ToString(ci.DateTimeFormat.ShortTimePattern, ci);
                default: 
                    return datetime.ToString(ci);
            }
        }
    }
}
