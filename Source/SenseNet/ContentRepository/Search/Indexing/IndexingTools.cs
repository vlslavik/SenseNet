using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Contains detailed per field indexing information
    /// </summary>
    public sealed class ExplicitPerFieldIndexingInfo
    {
        public string ContentTypeName { get; internal set; }
        public string ContentTypePath { get; internal set; }
        public string FieldName { get; internal set; }
        public string FieldTitle { get; internal set; }
        public string FieldDescription { get; internal set; }
        public string FieldType { get; internal set; }
        public string Analyzer { get; internal set; }
        public string IndexHandler { get; internal set; }
        public string IndexingMode { get; internal set; }
        public string IndexStoringMode { get; internal set; }
        public string TermVectorStoringMode { get; internal set; }
    }

    /// <summary>
    /// Provides information about indexing.
    /// </summary>
    public static class IndexingTools
    {
        /// <summary>
        /// Gets the name of every field in the system.
        /// </summary>
        /// <param name="includeNonIndexedFields">Whether or not to include non-indexed fields. Default is true.</param>
        /// <returns>A list which contains the name of every field in the system which meets the specificed criteria.</returns>
        public static IEnumerable<string> GetAllFieldNames(bool includeNonIndexedFields = true)
        {
            if (includeNonIndexedFields)
                return ContentTypeManager.Current.AllFieldNames;

            return ContentTypeManager.Current.AllFieldNames.Where(x => ContentTypeManager.Current.IndexingInfo.ContainsKey(x) && ContentTypeManager.Current.IndexingInfo[x].IsInIndex);
        }

        /// <summary>
        /// Gets detailed indexing information about all fields in the repository.
        /// </summary>
        /// <param name="includeNonIndexedFields">Whether to include non-indexed fields.</param>
        /// <returns>Detailed indexing information about all fields in the repository.</returns>
        public static IEnumerable<ExplicitPerFieldIndexingInfo> GetExplicitPerFieldIndexingInfo(bool includeNonIndexedFields)
        {
            var infoArray = new List<ExplicitPerFieldIndexingInfo>(ContentTypeManager.Current.ContentTypes.Count * 5);

            foreach (var contentType in ContentTypeManager.Current.ContentTypes.Values)
            {
                var xml = new System.Xml.XmlDocument();
                var nsmgr = new System.Xml.XmlNamespaceManager(xml.NameTable);
                var fieldCount = 0;

                nsmgr.AddNamespace("x", ContentType.ContentDefinitionXmlNamespace);
                xml.Load(contentType.Binary.GetStream());
                foreach (System.Xml.XmlElement fieldElement in xml.SelectNodes("/x:ContentType/x:Fields/x:Field", nsmgr))
                {
                    var typeAttr = fieldElement.Attributes["type"];
                    if (typeAttr == null)
                        typeAttr = fieldElement.Attributes["handler"];

                    var info = new ExplicitPerFieldIndexingInfo
                    {
                        ContentTypeName = contentType.Name,
                        ContentTypePath = contentType.Path.Replace(Repository.ContentTypesFolderPath + "/", String.Empty),
                        FieldName = fieldElement.Attributes["name"].Value,
                        FieldType = typeAttr.Value
                    };

                    var fieldTitleElement = fieldElement.SelectSingleNode("x:DisplayName", nsmgr);
                    if (fieldTitleElement != null)
                        info.FieldTitle = fieldTitleElement.InnerText;

                    var fieldDescElement = fieldElement.SelectSingleNode("x:Description", nsmgr);
                    if (fieldDescElement != null)
                        info.FieldDescription = fieldDescElement.InnerText;

                    var hasIndexing = false;
                    foreach (System.Xml.XmlElement element in fieldElement.SelectNodes("x:Indexing/*", nsmgr))
                    {
                        hasIndexing = true;
                        switch (element.LocalName)
                        {
                            case "Analyzer": info.Analyzer = element.InnerText.Replace("Lucene.Net.Analysis", "."); break;
                            case "IndexHandler": info.IndexHandler = element.InnerText.Replace("SenseNet.Search", "."); break;
                            case "Mode": info.IndexingMode = element.InnerText; break;
                            case "Store": info.IndexStoringMode = element.InnerText; break;
                            case "TermVector": info.TermVectorStoringMode = element.InnerText; break;
                        }
                    }

                    fieldCount++;

                    if (hasIndexing || includeNonIndexedFields)
                        infoArray.Add(info);
                }

                //hack: content type without fields
                if (fieldCount == 0 && includeNonIndexedFields)
                {
                    var info = new ExplicitPerFieldIndexingInfo
                    {
                        ContentTypeName = contentType.Name,
                        ContentTypePath = contentType.Path.Replace(Repository.ContentTypesFolderPath + "/", String.Empty),
                    };

                    infoArray.Add(info);
                }
            }

            return infoArray;
        }

        /// <summary>
        /// Gets explicit per-field indexing information collected into a table.
        /// </summary>
        /// <param name="fullTable">Whether or not to include non-indexed fields.</param>
        /// <returns>A table containing detailed indexing information.</returns>
        public static string GetExplicitIndexingInfo(bool fullTable)
        {
            var infoArray = GetExplicitPerFieldIndexingInfo(fullTable);

            var sb = new StringBuilder();
            sb.AppendLine("TypePath\tType\tField\tFieldTitle\tFieldDescription\tFieldType\tMode\tStore\tTVect\tHandler\tAnalyzer");
            foreach (var info in infoArray)
            {
                sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                    info.ContentTypePath,
                    info.ContentTypeName,
                    info.FieldName,
                    info.FieldTitle,
                    info.FieldDescription,
                    info.FieldType,
                    info.IndexingMode,
                    info.IndexStoringMode,
                    info.TermVectorStoringMode,
                    info.IndexHandler,
                    info.Analyzer);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
