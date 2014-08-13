﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using SenseNet.ContentRepository;
using SnCR = SenseNet.ContentRepository;
using System.Globalization;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.i18n;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace SenseNet.Search.Indexing
{
    public enum IndexFieldType
    {
        String, Int, Long, Float, Double, DateTime
    }
    public interface IIndexableDocument
    {
        IEnumerable<IIndexableField> GetIndexableFields();
    }
    public interface IIndexableField
    {
        bool IsInIndex { get; }
        string Name { get; }
        IEnumerable<IndexFieldInfo> GetIndexFieldInfos(out string textExtract);
    }
    public interface IIndexValueConverter<T>
    {
        T GetBack(string lucFieldValue);
    }
    public interface IIndexValueConverter
    {
        object GetBack(string lucFieldValue);
    }

    public abstract class FieldIndexHandler
    {
        public PerFieldIndexingInfo OwnerIndexingInfo { get; internal set; }
        public virtual int SortingType { get { return Lucene.Net.Search.SortField.STRING; } }
        public virtual IndexFieldType IndexFieldType { get { return IndexFieldType.String; } }
        /// <summary>
        /// For SnLucParser
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract bool TryParseAndSet(Parser.QueryFieldValue value);
        /// <summary>
        /// For LINQ
        /// </summary>
        /// <param name="value"></param>
        public abstract void ConvertToTermValue(Parser.QueryFieldValue value);
        public abstract IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract);
        public abstract IEnumerable<string> GetParsableValues(SnCR.Field snField);

        private static NumericField GetNumericField(string fieldName, PerFieldIndexingInfo indexingInfo)
        {
            // Do not reusing any fields.
            var index = indexingInfo.IndexingMode ?? PerFieldIndexingInfo.DefaultIndexingMode;
            var store = indexingInfo.IndexStoringMode ?? PerFieldIndexingInfo.DefaultIndexStoringMode;
            var lucField = new Lucene.Net.Documents.NumericField(fieldName, store, index != Lucene.Net.Documents.Field.Index.NO);
            return lucField;
        }

        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, string value)
        {
            return CreateFieldInfo(name, FieldInfoType.StringField, value);
        }
        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, Int32 value)
        {
            return CreateFieldInfo(name, FieldInfoType.IntField, value.ToString(CultureInfo.InvariantCulture));
        }
        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, Int64 value)
        {
            return CreateFieldInfo(name, FieldInfoType.LongField, value.ToString(CultureInfo.InvariantCulture));
        }
        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, Single value)
        {
            return CreateFieldInfo(name, FieldInfoType.SingleField, value.ToString(CultureInfo.InvariantCulture));
        }
        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, Double value)
        {
            return CreateFieldInfo(name, FieldInfoType.DoubleField, value.ToString(CultureInfo.InvariantCulture));
        }
        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, IEnumerable<string> value)
        {
            return CreateFieldInfo(name, value, null);
        }
        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, IEnumerable<string> value, string sortTerm)
        {
            var indexingInfo = this.OwnerIndexingInfo;
            var index = indexingInfo.IndexingMode ?? PerFieldIndexingInfo.DefaultIndexingMode;
            var store = indexingInfo.IndexStoringMode ?? PerFieldIndexingInfo.DefaultIndexStoringMode;
            var termVector = indexingInfo.TermVectorStoringMode ?? PerFieldIndexingInfo.DefaultTermVectorStoringMode;
            var fields = value.Select(v => new IndexFieldInfo(name, v, FieldInfoType.StringField, store, index, termVector)).ToList();
            if (!String.IsNullOrEmpty(sortTerm))
                fields.Add(new IndexFieldInfo(
                    GetSortFieldName(name),
                    sortTerm, FieldInfoType.StringField,
                    PerFieldIndexingInfo.DefaultIndexStoringMode,
                    PerFieldIndexingInfo.DefaultIndexingMode,
                    PerFieldIndexingInfo.DefaultTermVectorStoringMode));
            return fields;
        }

        protected IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, IEnumerable<Int32> value)
        {
            var indexingInfo = this.OwnerIndexingInfo;
            var index = indexingInfo.IndexingMode ?? PerFieldIndexingInfo.DefaultIndexingMode;
            var store = indexingInfo.IndexStoringMode ?? PerFieldIndexingInfo.DefaultIndexStoringMode;
            var termVector = indexingInfo.TermVectorStoringMode ?? PerFieldIndexingInfo.DefaultTermVectorStoringMode;
            var x = value.Select(v =>  new IndexFieldInfo(name, v.ToString(CultureInfo.InvariantCulture), FieldInfoType.IntField, store, index, termVector)).ToArray();
            return x;
        }

        private IEnumerable<IndexFieldInfo> CreateFieldInfo(string name, FieldInfoType type, string value)
        {
            var indexingInfo = this.OwnerIndexingInfo;
            var index = indexingInfo.IndexingMode ?? PerFieldIndexingInfo.DefaultIndexingMode;
            var store = indexingInfo.IndexStoringMode ?? PerFieldIndexingInfo.DefaultIndexStoringMode;
            var termVector = indexingInfo.TermVectorStoringMode ?? PerFieldIndexingInfo.DefaultTermVectorStoringMode;
            return new[] { new IndexFieldInfo(name, value, type, store, index, termVector) };
        }

        public virtual string GetDefaultAnalyzerName() { return typeof(KeywordAnalyzer).FullName; }
        public virtual string GetSortFieldName(string fieldName) { return fieldName; }
    }

    // Not IIndexValueConverters.
    public class NotIndexedIndexFieldHandler : FieldIndexHandler
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            textExtract = string.Empty;
            return new IndexFieldInfo[0];
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            return false;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            value.Set(value.InputObject.ToString());
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            return null;
        }
    }
    public class BinaryIndexHandler : FieldIndexHandler
    {
        public override string GetDefaultAnalyzerName() { return typeof(StandardAnalyzer).FullName; }
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var data = snField.GetData() as SenseNet.ContentRepository.Storage.BinaryData;
            textExtract = data == null ? String.Empty : SenseNet.Search.TextExtractor.GetExtract(data, snField.Content.ContentHandler);
            return CreateFieldInfo(snField.Name, textExtract);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue);
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            value.Set((string)value.InputObject);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            return null;
        }
    }
    public class TypeTreeIndexHandler : FieldIndexHandler
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            textExtract = String.Empty;
            var nodeType = snField.Content.ContentHandler.NodeType;
            var types = nodeType.NodeTypePath.Split('/').Select(p => p.ToLower());
            return CreateFieldInfo(snField.Name, types);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue.ToLower());
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(String.Empty);
            else
                value.Set(((string)value.InputObject).ToLower());
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            return snField.Content.ContentHandler.NodeType.NodeTypePath.Split('/').Select(p => p.ToLower());
        }
    }

    // Not implemented IIndexValueConverters.
    public class HyperLinkIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var data = (SenseNet.ContentRepository.Fields.HyperLinkField.HyperlinkData)snField.GetData();
            if (data == null)
            {
                textExtract = String.Empty;
                return null;
            }
            var strings = new List<string>();
            if (data.Href != null)
                strings.Add(data.Href.ToLower());
            if (data.Target != null)
                strings.Add(data.Target.ToLower());
            if (data.Text != null)
                strings.Add(data.Text.ToLower());
            if (data.Title != null)
                strings.Add(data.Title.ToLower());
            textExtract = String.Join(" ", strings.ToArray());
            return CreateFieldInfo(snField.Name, strings);

        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue.ToLower());
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(String.Empty);
            else
                value.Set(((string)value.InputObject).ToLower());
        }
        public object GetBack(string lucFieldValue)
        {
            throw new NotImplementedException();
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var data = (SenseNet.ContentRepository.Fields.HyperLinkField.HyperlinkData)snField.GetData();
            if (data == null)
            {
                return null;
            }
            var strings = new List<string>();
            if (data.Href != null)
                strings.Add(data.Href.ToLower());
            if (data.Target != null)
                strings.Add(data.Target.ToLower());
            if (data.Text != null)
                strings.Add(data.Text.ToLower());
            if (data.Title != null)
                strings.Add(data.Title.ToLower());
            return strings;
        }
    }
    public class ChoiceIndexHandler : FieldIndexHandler, IIndexValueConverter<object>, IIndexValueConverter
    {
        public override string GetSortFieldName(string fieldName)
        {
            return fieldName + "_sort";
        }

        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var data = snField.GetData() ?? string.Empty;

            var stringData = data as string;
            if (stringData != null)
            {
                textExtract = stringData.ToLower();
                return CreateFieldInfo(snField.Name, textExtract);
            }

            var listData = data as IEnumerable<string>;
            if (listData != null)
            {
                // words to choice field
                var wordList = new List<string>();
                // words to sort field
                var sortList = new List<string>();
                // words to full text field
                var localizedWords = new List<string>();
                foreach (var inputWord in listData)
                {
                    // process every word
                    var cfs = snField.FieldSetting as SnCR.Fields.ChoiceFieldSetting;
                    if (cfs != null)
                    {
                        // field with ChoiceFieldSetting
                        var optionKey = cfs.Options.Where(x => x.Value == inputWord).Select(x => x.StoredText).FirstOrDefault();
                        if (optionKey != null)
                        {
                            // identified option
                            var optionTerm = "$" + inputWord.ToLower();
                            wordList.Add(optionTerm);
                            sortList.Add(optionTerm);
                            string className;
                            string name;
                            var localized = SenseNetResourceManager.ParseResourceKey(optionKey, out className, out name);
                            if (localized && className != null && name != null)
                            {
                                // localized texts: add all known mutations
                                var lw = SenseNetResourceManager.Current.GetStrings(className, name);
                                localizedWords.AddRange(lw.Select(x => x.ToLower()));
                            }
                            else
                            {
                                // not localized: add the word
                                localizedWords.Add(optionKey.ToLower());
                            }
                        }
                        else
                        {
                            // unidentified option: extra value
                            if (inputWord.StartsWith(SnCR.Fields.ChoiceField.EXTRAVALUEPREFIX))
                            {
                                // drives ordering (additional '~' hides this information)
                                sortList.Add("~" + inputWord);
                                // add 
                                var splitted = inputWord.Split('.');
                                wordList.Add(splitted[0]);
                                localizedWords.Add(splitted[1].ToLower());
                            }
                            else
                            {
                                // add as a lowercase word
                                wordList.Add(inputWord.ToLower());
                                localizedWords.Add(inputWord.ToLower());
                            }
                        }
                    }
                    else
                    {
                        // field with another field setting
                        wordList.Add(inputWord.ToLower());
                    }
                }
                sortList.Sort();
                var sortTerm = String.Join("-", sortList);
                textExtract = String.Join(" ", localizedWords);
                wordList.AddRange(localizedWords);
                return CreateFieldInfo(snField.Name, wordList, sortTerm);
            }

            var enumerableData = data as System.Collections.IEnumerable;
            if (enumerableData != null)
            {
                var words = new List<string>();
                foreach (var item in enumerableData)
                    words.Add(Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture).ToLower());
                var wordArray = words.ToArray();
                textExtract = String.Join(" ", wordArray);
                return CreateFieldInfo(snField.Name, words);
            }

            throw new NotSupportedException(String.Concat("Cannot create index from this type: ", data.GetType().FullName,
                ". Indexable data can be string, IEnumerable<string> or IEnumerable."));
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue.ToLower());
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(String.Empty);
            else
                value.Set(((string)value.InputObject).ToLower());
        }
        public object GetBack(string lucFieldValue)
        {
            throw new NotImplementedException();
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var data = snField.GetData() ?? string.Empty;

            var stringData = data as string;
            if (stringData != null)
                return new[] { stringData.ToLower() };

            var listData = data as IEnumerable<string>;
            if (listData != null)
                return listData.Select(s => s.ToLower()).ToArray();

            var enumerableData = data as System.Collections.IEnumerable;
            if (enumerableData != null)
                return (from object item in enumerableData select Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture).ToLower()).ToList();

            return new[] { string.Empty };

        }
    }

    // IIndexValueConverters.
    public class LowerStringIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            string className, name;
            if (SenseNetResourceManager.Running && snField.LocalizationEnabled && snField.IsLocalized && SenseNetResourceManager.ParseResourceKey(snField.GetStoredValue(), out className, out name))
            {
                var strings = SenseNetResourceManager.Current.GetStrings(className, name);
                textExtract = string.Join(" ", strings);
                return CreateFieldInfo(snField.Name, strings);
            }
            var data = snField.GetData();
            var stringValue = data == null ? String.Empty : data.ToString().ToLower();
            textExtract = stringValue;

            return CreateFieldInfo(snField.Name, stringValue);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue.ToLower());
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(String.Empty);
            else
                value.Set(((string)value.InputObject).ToLower());
        }
        public string GetBack(string lucFieldValue)
        {
            return lucFieldValue;
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var data = snField.GetData();
            return new[] { data == null ? String.Empty : data.ToString().ToLower() };
        }
    }
    public class BooleanIndexHandler : FieldIndexHandler, IIndexValueConverter<bool>, IIndexValueConverter
    {
        public static readonly string YES = "yes";
        public static readonly string NO = "no";
        public static readonly List<string> YesList = new List<string>(new string[] { "1", "true", "y", YES });
        public static readonly List<string> NoList = new List<string>(new string[] { "0", "false", "n", NO });

        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var value = snField.GetData();
            var boolValue = value == null ? false : (bool)value;
            textExtract = String.Empty;
            return CreateFieldInfo(snField.Name, boolValue ? YES : NO);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            var v = value.StringValue.ToLower();
            if (YesList.Contains(v))
            {
                value.Set(YES);
                return true;
            }
            if (NoList.Contains(v))
            {
                value.Set(NO);
                return true;
            }
            bool b;
            if (Boolean.TryParse(v, out b))
            {
                value.Set(b ? YES : NO);
                return true;
            }
            return false;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            value.Set(((bool)value.InputObject) ? YES : NO);
        }
        public bool GetBack(string lucFieldValue)
        {
            return ConvertBack(lucFieldValue);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }

        public static bool ConvertBack(string lucFieldValue)
        {
            return lucFieldValue == YES;
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var value = snField.GetData();
            var boolValue = value == null ? false : (bool)value;
            return new[] { boolValue ? YES : NO };
        }
    }
    public class IntegerIndexHandler : FieldIndexHandler, IIndexValueConverter<Int32>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.INT; } }
        public override IndexFieldType IndexFieldType { get { return IndexFieldType.Int; } }

        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var value = snField.GetData();
            var intValue = 0;
            try
            {
                intValue = value == null ? 0 : (int)value;
            }
            catch (Exception e)
            {
                Logger.WriteVerbose(String.Format("IntegerIndexHandler ERROR: content: {0} field: {1}, value: {2}", snField.Content.Path, snField.Name, value));
                throw;
            }
            textExtract = intValue.ToString();
            return CreateFieldInfo(snField.Name, intValue);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            Int32 intValue;
            if (!Int32.TryParse(value.StringValue, out intValue))
                return false;
            value.Set(intValue);
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            value.Set((int)value.InputObject);
        }
        public Int32 GetBack(string lucFieldValue)
        {
            return ConvertBack(lucFieldValue);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }

        public static Int32 ConvertBack(string lucFieldValue)
        {
            Int32 intValue;
            if (Int32.TryParse(lucFieldValue, out intValue))
                return intValue;
            return 0;
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var value = snField.GetData();
            var intValue = value == null ? 0 : (int)value;
            return new[] { intValue.ToString() };
        }
    }
    public class NumberIndexHandler : FieldIndexHandler, IIndexValueConverter<Decimal>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.DOUBLE; } }
        public override IndexFieldType IndexFieldType { get { return IndexFieldType.Double; } }

        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var value = snField.GetData();
            var decimalValue = value == null ? (Decimal)0.0 : (Decimal)value;
            var doubleValue = Convert.ToDouble(decimalValue);
            textExtract = decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return CreateFieldInfo(snField.Name, doubleValue);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            Double doubleValue;
            if (!Double.TryParse(value.StringValue, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out doubleValue))
                return false;
            value.Set(doubleValue);
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            var doubleValue = Convert.ToDouble(value.InputObject);
            value.Set(doubleValue);
        }
        public Decimal GetBack(string lucFieldValue)
        {
            return Convert.ToDecimal(lucFieldValue, System.Globalization.CultureInfo.InvariantCulture);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var value = snField.GetData();
            var decimalValue = value == null ? (Decimal)0.0 : (Decimal)value;
            var doubleValue = Convert.ToDouble(decimalValue);
            return new[] { decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        }
    }
    public class DateTimeIndexHandler : FieldIndexHandler, IIndexValueConverter<DateTime>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.LONG; } }
        public override IndexFieldType IndexFieldType { get { return IndexFieldType.DateTime; } }

        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            textExtract = String.Empty;
            var data = snField.GetData();
            var ticks = data == null ? 0 : ((DateTime)data).Ticks;
            return CreateFieldInfo(snField.Name, SetPrecision(snField, ticks));
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            DateTime dateTimeValue;
            if (!DateTime.TryParse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeValue))
                return false;
            value.Set(dateTimeValue.Ticks);
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            value.Set(((DateTime)value.InputObject).Ticks);
        }
        public DateTime GetBack(string lucFieldValue)
        {
            return new DateTime(Int64.Parse(lucFieldValue));
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }

        private long SetPrecision(SnCR.Field snField, long ticks)
        {
            var setting = snField.FieldSetting as SenseNet.ContentRepository.Fields.DateTimeFieldSetting;
            SenseNet.ContentRepository.Fields.DateTimePrecision? precision = null;
            if (setting != null)
                precision = setting.Precision;
            if (precision == null)
                precision = SenseNet.ContentRepository.Fields.DateTimeFieldSetting.DefaultPrecision;

            switch (precision.Value)
            {
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Millisecond:
                    return ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Second:
                    return ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Minute:
                    return ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Hour:
                    return ticks / TimeSpan.TicksPerHour * TimeSpan.TicksPerHour;
                case SenseNet.ContentRepository.Fields.DateTimePrecision.Day:
                    return ticks / TimeSpan.TicksPerDay * TimeSpan.TicksPerDay;
                default:
                    throw new NotImplementedException("Unknown DateTimePrecision: " + precision.Value);
            }
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var data = snField.GetData();
            try
            {
                var dateData = Convert.ToDateTime(data);
                if (dateData != DateTime.MinValue)
                    return new[] {"'" + dateData.ToString("yyyy.MM.dd HH:mm:ss") + "'"};
            }
            catch (Exception ex)
            {
                Logger.WriteInformation(Logger.EventId.NotDefined, ex);
            }
            return new[] { string.Empty };
        }
    }
    public class LongTextIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override string GetDefaultAnalyzerName() { return typeof(StandardAnalyzer).FullName; }
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var data = snField.GetData() as string;
            textExtract = data == null ? String.Empty : data;
            return CreateFieldInfo(snField.Name, textExtract);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue);
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            value.Set((string)value.InputObject);
        }
        public string GetBack(string lucFieldValue)
        {
            throw new NotSupportedException();
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var data = snField.GetData() as string;
            return new[] { data == null ? String.Empty : data.ToString() };
        }
    }
    public class ReferenceIndexHandler : FieldIndexHandler, IIndexValueConverter<Int32>, IIndexValueConverter
    {
        public override int SortingType { get { return Lucene.Net.Search.SortField.STRING; } }

        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            textExtract = String.Empty;
            var data = snField.GetData();
            var node = data as Node;
            if (node != null)
                return CreateFieldInfo(snField.Name, node.Id.ToString());
            var nodes = data as System.Collections.IEnumerable;
            if (nodes != null)
                return CreateFieldInfo(snField.Name, nodes.Cast<Node>().Select(n => n.Id.ToString()));
            return CreateFieldInfo(snField.Name, LucQuery.NullReferenceValue);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            int intValue;
            if (Int32.TryParse(value.StringValue, out intValue))
                value.Set(intValue.ToString());
            else
                value.Set(LucQuery.NullReferenceValue);
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            if (value.InputObject == null)
            {
                value.Set(LucQuery.NullReferenceValue);
                return;
            }
            var node = value.InputObject as Node;
            if (node != null)
            {
                value.Set(node.Id.ToString());
                return;
            }
            var enumerable = value as System.Collections.IEnumerable;
            if (enumerable != null)
                throw new NotImplementedException("ReferenceIndexHandler.ConvertToTermValue() isn't implemented when value is IEnumerable.");
            throw new NotSupportedException(String.Format("Type {0} is not supported as value of ReferenceField",value.InputObject.GetType().ToString()));
        }
        public Int32 GetBack(string lucFieldValue)
        {
            if (lucFieldValue == LucQuery.NullReferenceValue)
                return 0;
            Int32 singleRef;
            if (Int32.TryParse(lucFieldValue, out singleRef))
                return singleRef;
            return 0;
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var data = snField.GetData();
            var node = data as Node;
            if (node != null)
                return new[] { node.Id.ToString() };
            var nodes = data as System.Collections.IEnumerable;
            if (nodes != null)
                return nodes.Cast<Node>().Select(n => n.Id.ToString());
            return null;
        }
    }
    public class ExclusiveTypeIndexHandler : FieldIndexHandler, IIndexValueConverter<ContentType>, IIndexValueConverter
    {
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue.ToLower());
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(String.Empty);
            else
                value.Set(((string)value.InputObject).ToLower());
        }
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var nodeTypeName = snField.Content.ContentHandler.NodeType.Name.ToLower();
            textExtract = nodeTypeName;
            return CreateFieldInfo(snField.Name, nodeTypeName);
        }
        public ContentType GetBack(string lucFieldValue)
        {
            if (String.IsNullOrEmpty(lucFieldValue))
                return null;
            return ContentType.GetByName(lucFieldValue);
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            throw new NotImplementedException();
        }
    }
    public class InFolderIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            var value = (string)snField.GetData() ?? String.Empty;
            textExtract = value.ToLower();
            var parentPath = RepositoryPath.GetParentPath(textExtract) ?? "/";
            return CreateFieldInfo(snField.Name, parentPath);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue.ToLower());
            if (value.StringValue.StartsWith("/root"))
                return true;
            return false;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            var path = ((string)value.InputObject).ToLower();
            if (!path.StartsWith("/root"))
                throw new ApplicationException(String.Concat("Invalid path: '", path, "'. It must be absolute: '/root' or '/root/...'."));
            value.Set(path);
        }
        public string GetBack(string lucFieldValue)
        {
            return lucFieldValue;
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var value = (string)snField.GetData() ?? String.Empty;
            var parentPath = RepositoryPath.GetParentPath(value.ToLower()) ?? "/";
            return new[] { parentPath.ToLower() };
        }
    }
    public class InTreeIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            textExtract = String.Empty;
            var value = (string)snField.GetData() ?? String.Empty;
            return CreateFieldInfo(snField.Name, value);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            value.Set(value.StringValue.ToLower());
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            var path = ((string)value.InputObject).ToLower();
            if (!path.StartsWith("/root"))
                throw new ApplicationException(String.Concat("Invalid path: '", path, "'. It must be absolute: '/root' or '/root/...'."));
            value.Set(path);
        }
        public string GetBack(string lucFieldValue)
        {
            throw new NotSupportedException();
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var path = (string)snField.GetData() ?? String.Empty;
            var separator = "/";
            string[] fragments = path.ToLower().Split(separator.ToCharArray(), StringSplitOptions.None);
            string[] pathSteps = new string[fragments.Length];
            for (int i = 0; i < fragments.Length; i++)
                pathSteps[i] = string.Join(separator, fragments, 0, i + 1);
            return pathSteps;
        }
    }
    public class TagIndexHandler : FieldIndexHandler, IIndexValueConverter<string>, IIndexValueConverter
    {
        // IndexHandler for comma or semicolon separated strings (e.g. Red,Green,Blue) used in tagging fields
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            // Ensure initial textExtract for out parameter. It is used if the field value is null or empty.
            textExtract = String.Empty;
            // Get the value. A field type is indexable with this handler that provides a string value
            // but ShortText and LongText are recommended.
            var snFieldValue = (string)snField.GetData();
            // Return null if the value is not indexable. Lucene field and text extract won't be created.
            if (String.IsNullOrEmpty(snFieldValue))
                return null;
            // Convert to lowercase for case insensitive index handling
            snFieldValue = snFieldValue.ToLower();
            // Create an array of words. Words can be separated by comma or semicolon. Whitespaces will be removed.
            var terms = snFieldValue.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim()).ToArray();
            // Concatenate the words with space separator for text extract.
            textExtract = String.Join(" ", terms);
            // Produce the lucene multiterm field with a base's tool and return with it.
            return CreateFieldInfo(snField.Name, terms);
        }
        public override bool TryParseAndSet(Parser.QueryFieldValue value)
        {
            // Set the parsed value.
            value.Set(value.StringValue.ToLower());
            // Successful.
            return true;
        }
        public override void ConvertToTermValue(Parser.QueryFieldValue value)
        {
            if (value.InputObject == null)
                value.Set(String.Empty);
            else
                value.Set(((string)value.InputObject).ToLower());
        }
        public string GetBack(string lucFieldValue)
        {
            return lucFieldValue;
            //throw new NotSupportedException();
        }
        object IIndexValueConverter.GetBack(string lucFieldValue)
        {
            return GetBack(lucFieldValue);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var snFieldValue = (string)snField.GetData();
            if (String.IsNullOrEmpty(snFieldValue))
                return null;
            snFieldValue = snFieldValue.ToLower();
            var terms = snFieldValue.Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim()).ToArray();
            return terms;
        }
    }

    // Inherited IIndexValueConverters.
    public class DepthIndexHandler : IntegerIndexHandler
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(SnCR.Field snField, out string textExtract)
        {
            textExtract = String.Empty;
            return CreateFieldInfo(snField.Name, GetDepth(snField.Content.Path));
        }
        internal static int GetDepth(string path)
        {
            var depth = path.Split(RepositoryPath.PathSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length - 1;
            return depth;
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var depth = GetDepth(snField.Content.Path);
            return new[] { depth.ToString() };
        }
    }


    // Not finalized feature. After finalizing make public.
    class SystemContentIndexHandler : BooleanIndexHandler
    {
        public override IEnumerable<IndexFieldInfo> GetIndexFieldInfos(ContentRepository.Field snField, out string textExtract)
        {
            textExtract = String.Empty;

            var content = snField.Content;
            var boolValue = false;

            //check Trash
            //if (TrashBin.IsInTrash(content.ContentHandler as GenericContent))
            //    boolValue = true;

            //Do not index documents sent to the trash as system content, because when
            //restored (moved back to the original location) they will not be re-indexed
            //and would remain system content. Only the container TrashBags are system content.
            if (content.ContentHandler is TrashBag)
                boolValue = true;

            //check SystemFile
            if (!boolValue)
            {
                if (content.ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("SystemFile"))
                    boolValue = true;
            }

            //check SystemFolder
            if (!boolValue)
            {
                var parent = content.ContentHandler;

                using (new SystemAccount())
                {
                    while (parent != null)
                    {
                        if (parent.NodeType.IsInstaceOfOrDerivedFrom("SystemFolder"))
                        {
                            boolValue = true;
                            break;
                        }

                        parent = parent.Parent;
                    }
                }
            }

            return CreateFieldInfo(snField.Name, boolValue ? BooleanIndexHandler.YES : BooleanIndexHandler.NO);
        }
        public override IEnumerable<string> GetParsableValues(SnCR.Field snField)
        {
            var content = snField.Content;
            var boolValue = false;

            //check Trash
            if (TrashBin.IsInTrash(content.ContentHandler as GenericContent))
                boolValue = true;

            //check SystemFile
            if (!boolValue)
            {
                if (content.ContentHandler.NodeType.IsInstaceOfOrDerivedFrom("SystemFile"))
                    boolValue = true;
            }

            //check SystemFolder
            if (!boolValue)
            {
                var parent = content.ContentHandler;

                using (new SystemAccount())
                {
                    while (parent != null)
                    {
                        if (parent.NodeType.IsInstaceOfOrDerivedFrom("SystemFolder"))
                        {
                            boolValue = true;
                            break;
                        }

                        parent = parent.Parent;
                    }
                }
            }

            return new[] { boolValue ? BooleanIndexHandler.YES : BooleanIndexHandler.NO };
        }
    }
}
