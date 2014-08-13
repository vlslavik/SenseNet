using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using SenseNet.ContentRepository.Storage;

using  SenseNet.ContentRepository.Schema;
using System.Xml;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("DateTime")]
    [DataSlot(0, RepositoryDataType.DateTime, typeof(DateTime))]
    [DefaultFieldSetting(typeof(DateTimeFieldSetting))]
    [DefaultFieldControl("SenseNet.Portal.UI.Controls.DatePicker")]
    public class DateTimeField : Field
    {
        protected override bool HasExportData { get { return true; } }
        protected override void ExportData(System.Xml.XmlWriter writer, ExportContext context)
        {
            writer.WriteString(GetXmlData());
        }
        protected override void WriteXmlData(XmlWriter writer)
        {
            ExportData(writer, null);
        }
        protected override void ImportData(System.Xml.XmlNode fieldNode, ImportContext context)
        {
            if (String.IsNullOrEmpty(fieldNode.InnerXml))
            {
                this.SetData(ActiveSchema.DateTimeMinValue);
                return;
            }
            DateTime value = Convert.ToDateTime(fieldNode.InnerXml);
            this.SetData(value < ActiveSchema.DateTimeMinValue ? ActiveSchema.DateTimeMinValue : value);
        }

        public override void SetData(object value)
        {
            // This conversion makes sure that the date we handle is in UTC format (e.g. if 
            // the developer provides DateTime.Now, which is in local time by default).
            if (value is DateTime)
                value = Tools.ConvertToUtcDateTime((DateTime)value);

            base.SetData(value);
        }

        protected override string GetXmlData()
        {
            return XmlConvert.ToString((DateTime)GetData(), XmlDateTimeSerializationMode.Unspecified);
        }

        protected override bool ParseValue(string value)
        {
            DateTime dateTimeValue;
            if (!DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTimeValue))
                if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
                    if (!DateTime.TryParse(value, out dateTimeValue))
                        return false;
            this.SetData(dateTimeValue);
            return true;
        }
    }
}