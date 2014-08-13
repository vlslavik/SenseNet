using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Portal.Virtualization;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using System.IO;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using SenseNet.Portal.UI;
using System.Collections;
using System.Linq.Expressions;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.OData
{
    public abstract class ODataFormatter
    {
        public abstract string FormatName { get; }
        public abstract string MimeType { get; }

        internal ODataRequest ODataRequest { get; private set; }
        internal protected PortalContext PortalContext { get; protected set; }


        internal static Dictionary<string, Type> FormatterTypes { get; private set; }

        internal static ODataFormatter Create(PortalContext portalContext, ODataRequest odataReq)
        {
            var formatName = portalContext.OwnerHttpContext.Request["$format"];
            if (string.IsNullOrEmpty(formatName))
                formatName = odataReq == null ? "json" : odataReq.IsMetadataRequest ? "xml" : "json";
            else if (formatName != null)
                formatName = formatName.ToLower();

            return Create(formatName, portalContext);
        }
        internal static ODataFormatter Create(string formatName, PortalContext portalContext)
        {
            if (FormatterTypes == null)
            {
                FormatterTypes = new Dictionary<string, Type>();
                var types = TypeHandler.GetTypesByBaseType(typeof(ODataFormatter));
                foreach (var type in types)
                {
                    var protoType = (ODataFormatter)Activator.CreateInstance(type);
                    FormatterTypes[protoType.FormatName] = type;
                }
            }

            Type formatterType;
            if (!FormatterTypes.TryGetValue(formatName, out formatterType))
                return null;

            var formatter = (ODataFormatter)Activator.CreateInstance(formatterType);
            formatter.PortalContext = portalContext;
            return formatter;
        }
        
        internal void Initialize(ODataRequest odataRequest)
        {
            this.ODataRequest = odataRequest;
        }

        //----------------------------------------------------------------------------------------------------------------------------------- metadata

        internal void WriteServiceDocument(PortalContext portalContext, ODataRequest req)
        {
            WriteServiceDocument(portalContext, GetTopLevelNames(req));

            var mimeType = this.MimeType;
            if (mimeType != null)
                portalContext.OwnerHttpContext.Response.ContentType = mimeType;
        }
        private string[] GetTopLevelNames(ODataRequest req)
        {
            var site = Node.Load<Site>(req.RepositoryPath);
            return site.Children/*.Where(n => n is IFolder)*/.Select(n => n.Name).ToArray();
        }
        protected abstract void WriteServiceDocument(PortalContext portalContext, IEnumerable<string> names);

        internal void WriteMetadata(HttpContext context, ODataRequest req)
        {
            var content = ODataHandler.LoadContentByVersionRequest(req.RepositoryPath);
            if (content == null)
            {
                ODataHandler.ResourceNotFound();
                return;
            }

            var isRoot = content.ContentType.IsInstaceOfOrDerivedFrom("Site");
            if (isRoot)
                MetaGenerator.WriteMetadata(context.Response.Output, this);
            else
                MetaGenerator.WriteMetadata(context.Response.Output, this, content, req.IsCollection);

            var mimeType = this.MimeType;
            if (mimeType != null)
                context.Response.ContentType = mimeType;
        }
        internal void WriteMetadataInternal(TextWriter writer)
        {
            WriteMetadata(writer);
        }
        protected abstract void WriteMetadata(TextWriter writer);
        internal void WriteMetadataInternal(TextWriter writer, Metadata.Edmx edmx)
        {
            WriteMetadata(writer, edmx);
        }
        protected abstract void WriteMetadata(TextWriter writer, Metadata.Edmx edmx);

        //----------------------------------------------------------------------------------------------------------------------------------- contents

        internal void WriteSingleContent(String path, PortalContext portalContext)
        {
            WriteSingleContent(ODataHandler.LoadContentByVersionRequest(path), portalContext);
        }
        internal void WriteSingleContent(Content content, PortalContext portalContext)
        {
            var resp = portalContext.OwnerHttpContext.Response;
            var fields = CreateFieldDictionary(content, portalContext, false);
            WriteSingleContent(portalContext, fields);
        }
        protected abstract void WriteSingleContent(PortalContext portalContext, Dictionary<string, object> fields);

        internal void WriteChildrenCollection(String path, PortalContext portalContext, ODataRequest req)
        {
            var content = Content.Load(path);
            var chdef = content.ChildrenDefinition;
            if (string.IsNullOrEmpty(chdef.ContentQuery))
                chdef.ContentQuery = ContentQuery.AddClause(chdef.ContentQuery, String.Concat("InFolder:'", path, "'"), ContentRepository.Storage.Search.ChainOperator.And);
            chdef.EnableAutofilters = FilterStatus.Disabled;
            int count;
            var contents = ProcessOperationQueryResponse(chdef, portalContext, req, out count);
            if (req.CountOnly)
                WriteCount(portalContext, count);
            else
                WriteMultipleContent(portalContext, contents, count);
        }
        private void WriteMultiRefContents(object references, PortalContext portalContext, ODataRequest req) //UNDONE: x: Rename: WriteMultiRefContents
        {
            var resp = portalContext.OwnerHttpContext.Response;

            if (references != null)
            {
                Node node = references as Node;
                var projector = Projector.Create(req, true);
                if (node != null)
                {
                    var contents = new List<Dictionary<string, object>>();
                    contents.Add(CreateFieldDictionary(Content.Create(node), portalContext, projector)); //TODO: ODATA: multiref item: get available types from reference property
                    WriteMultipleContent(portalContext, contents, 1);
                }
                else
                {
                    var enumerable = references as System.Collections.IEnumerable;
                    if (enumerable != null)
                    {
                        var skipped = 0;
                        var allcount = 0;
                        var count = 0;
                        var realcount = 0;
                        var contents = new List<Dictionary<string, object>>();
                        if (req.HasFilter)
                        {
                            var filtered = new FilteredEnumerable(enumerable, (System.Linq.Expressions.LambdaExpression)req.Filter, req.Top, req.Skip);
                            foreach (Node item in filtered)
                                contents.Add(CreateFieldDictionary(Content.Create(item), portalContext, projector));
                            allcount = filtered.AllCount;
                            realcount = contents.Count;
                        }
                        else
                        {
                            foreach (Node item in enumerable)
                            {
                                allcount++;
                                if (skipped++ < req.Skip)
                                    continue;
                                if (req.Top == 0 || count++ < req.Top)
                                {
                                    contents.Add(CreateFieldDictionary(Content.Create(item), portalContext, projector));
                                    realcount++;
                                }
                            }
                        }
                        WriteMultipleContent(portalContext, contents, req.InlineCount == InlineCount.AllPages ? allcount : realcount);
                    }
                }
            }
        }
        private void WriteSingleRefContent(object references, PortalContext portalContext)
        {
            if (references != null)
            {
                Node node = references as Node;
                if (node != null)
                {
                    WriteSingleContent(portalContext, CreateFieldDictionary(Content.Create(node), portalContext, false));
                }
                else
                {
                    var enumerable = references as System.Collections.IEnumerable;
                    if (enumerable != null)
                    {
                        foreach (Node item in enumerable)
                        {
                            WriteSingleContent(portalContext, CreateFieldDictionary(Content.Create(item), portalContext, false));
                            break;
                        }
                    }
                }
            }
        }
        protected abstract void WriteMultipleContent(PortalContext portalContext, List<Dictionary<string, object>> contents, int count);
        protected abstract void WriteCount(PortalContext portalContext, int count);

        internal void WriteContentProperty(String path, string propertyName, bool rawValue, PortalContext portalContext, ODataRequest req)
        {
            var content = ODataHandler.LoadContentByVersionRequest(path);
            if (content == null)
            {
                ODataHandler.ContentNotFound(portalContext.OwnerHttpContext, path);
                return;
            }

            if (propertyName == ODataHandler.PROPERTY_ACTIONS)
            {
                var backUrl = portalContext.BackUrl;

                //Get actions without back url: let the client append the back parameter,
                //as we are in a service here that does not know about the redirect url.
                var snActions = ODataHandler.ActionResolver.GetActions(content, req.Scenario, string.IsNullOrEmpty(backUrl) ? null : backUrl);

                var actions = snActions.Where(a => a.IsHtmlOperation).Select(a => new ODataActionItem
                {
                    Name = a.Name,
                    DisplayName = SNSR.GetString(a.Text),
                    Icon = a.Icon,
                    Index = a.Index,
                    Url = a.Uri,
                    IncludeBackUrl = a.GetApplication() == null ? 0 : (int)a.GetApplication().IncludeBackUrl,
                    ClientAction = a is ClientAction && !string.IsNullOrEmpty(((ClientAction)a).Callback),
                    Forbidden = a.Forbidden
                });
                WriteActionsProperty(portalContext, actions.ToArray(), rawValue);
                return;
            }

            Field field;
            if (content.Fields.TryGetValue(propertyName, out field))
            {
                var refField = field as ReferenceField;
                if (refField != null)
                {
                    var refFieldSetting = refField.FieldSetting as ReferenceFieldSetting;
                    var isMultiRef = true;
                    if (refFieldSetting != null)
                        isMultiRef = refFieldSetting.AllowMultiple == true;
                    if (isMultiRef)
                    {
                        WriteMultiRefContents(refField.GetData(), portalContext, req);
                    }
                    else
                    {
                        WriteSingleRefContent(refField.GetData(), portalContext);
                    }
                }
                else if (!rawValue)
                {
                    WriteSingleContent(portalContext, new Dictionary<string, object> { { propertyName, field.GetData() } });
                }
                else
                {
                    WriteRaw(field.GetData(), portalContext);
                }
            }
            else
            {
                //ResourceNotFound(content, propertyName);
                WriteOperationResult(portalContext, req);
            }
        }
        protected abstract void WriteActionsProperty(PortalContext portalContext, ODataActionItem[] actions, bool raw);

        internal void WriteQueryResult(PortalContext portalContext, OData.ODataRequest req)
        {
            var settings = new QuerySettings { Top = req.Top, Skip = req.Skip, Sort = req.Sort };
            var contentQuery = new ContentQuery { Settings = settings, Text = req.ContentQueryText };
            if (req.RepositoryPath != Repository.RootPath)
                contentQuery.AddClause("InTree:\"" + req.RepositoryPath + "\"");

            var result = contentQuery.Execute();

            var contents = new List<Dictionary<string, object>>();
            var projector = Projector.Create(req, true);
            foreach (var node in result.Nodes)
            {
                var content = Content.Create(node);
                var fields = CreateFieldDictionary(content, portalContext, projector);
                contents.Add(fields);
            }
            var count = req.InlineCount == InlineCount.AllPages ? result.Count : contents.Count;
            if (req.CountOnly)
                WriteCount(portalContext, count);
            else
                WriteMultipleContent(portalContext, contents, count);
        }
        private int ExecuteQueryWithCountOnly(LucQuery lucQuery, FilterStatus enableAutofilters, FilterStatus enableLifespanFilter)
        {
            lucQuery.CountOnly = true;
            lucQuery.Top = 0;
            lucQuery.Skip = 0;
            lucQuery.SortFields = null;
            lucQuery.EnableAutofilters = enableAutofilters;
            lucQuery.EnableLifespanFilter = enableLifespanFilter;

            lucQuery.Execute();

            return lucQuery.TotalCount;
        }

        internal void WriteErrorResponse(HttpContext context, ODataException oe)
        {
            var error = new Error
            {
                Code = string.IsNullOrEmpty(oe.ErrorCode) ? Enum.GetName(typeof(ODataExceptionCode), oe.ODataExceptionCode) : oe.ErrorCode,
                ExceptionType = oe.InnerException == null ? oe.GetType().Name : oe.InnerException.GetType().Name,
                Message = new ErrorMessage
                {
                    Lang = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower(),
                    Value = SNSR.GetString(oe.Message).Replace(Environment.NewLine, "\\n").Replace('"', ' ').Replace('\'', ' ').Replace(" \\ ", " ")
                },
                InnerError =
#if DEBUG
new StackInfo
{
    Trace = Utility.CollectExceptionMessages(oe)//.Replace(Environment.NewLine, "\\n").Replace(@"\", @"\\").Replace('"', ' ').Replace('\'', ' ').Replace(" \\ ", " ")
}
#else
                null
#endif
            };
            context.Response.ContentType = "application/json";
            WriteError(context, error);
            context.Response.StatusCode = oe.HttpStatusCode;
            context.Response.TrySkipIisCustomErrors = true;

        }
        protected abstract void WriteError(HttpContext context, Error error);

        //----------------------------------------------------------------------------------------------------------------------------------- operations

        /// <summary>
        /// Handles GET operations. Parameters come from the URL or the request stream.
        /// </summary>
        /// <param name="portalContext"></param>
        /// <param name="odataReq"></param>
        internal void WriteOperationResult(PortalContext portalContext, ODataRequest odataReq)
        {
            object response = null;
            var content = ODataHandler.LoadContentByVersionRequest(odataReq.RepositoryPath);
            if (content == null)
                throw new ContentNotFoundException(string.Format(SNSR.GetString("$Action,ErrorContentNotFound"), odataReq.RepositoryPath));

            var action = ODataHandler.ActionResolver.GetAction(content, odataReq.Scenario, odataReq.PropertyName, null, null);
            if (action == null)
            {
                // check if this is a versioning action (e.g. a checkout)
                SavingAction.AssertVersioningAction(content, odataReq.PropertyName, true);

                throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, content.Path);
            }

            if (!action.IsODataOperation)
                throw new ODataException("Not an OData operation.", ODataExceptionCode.IllegalInvoke);
            if (action.CausesStateChange)
                throw new ODataException("OData action cannot be invoked with HTTP GET.", ODataExceptionCode.IllegalInvoke);

            if (action.Forbidden || (action.GetApplication() != null && !action.GetApplication().Security.HasPermission(PermissionType.RunApplication)))
                throw new InvalidContentActionException("Forbidden action: " + odataReq.PropertyName);

            var parameters = GetOperationParameters(action, portalContext.OwnerHttpContext.Request);
            response = action.Execute(content, parameters);

            var responseAsContent = response as Content;
            if (responseAsContent != null)
            {
                WriteSingleContent(responseAsContent, portalContext);
                return;
            }

            int count;
            response = ProcessOperationResponse(response, portalContext, odataReq, out count);
            //Write(response, portalContext);
            WriteOperationResult(response, portalContext, odataReq);
        }
        /// <summary>
        /// Handles POST operations. Parameters come from request stream.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="portalContext"></param>
        /// <param name="odataReq"></param>
        internal void WriteOperationResult(Stream inputStream, PortalContext portalContext, ODataRequest odataReq)
        {
            object response = null;
            var content = ODataHandler.LoadContentByVersionRequest(odataReq.RepositoryPath);

            if (content == null)
                throw new ContentNotFoundException(string.Format(SNSR.GetString("$Action,ErrorContentNotFound"), odataReq.RepositoryPath));

            var action = ODataHandler.ActionResolver.GetAction(content, odataReq.Scenario, odataReq.PropertyName, null, null);
            if (action == null)
            {
                // check if this is a versioning action (e.g. a checkout)
                SavingAction.AssertVersioningAction(content, odataReq.PropertyName, true);

                throw new InvalidContentActionException(InvalidContentActionReason.UnknownAction, content.Path);
            }

            if (action.Forbidden || (action.GetApplication() != null && !action.GetApplication().Security.HasPermission(PermissionType.RunApplication)))
                throw new InvalidContentActionException("Forbidden action: " + odataReq.PropertyName);

            var parameters = GetOperationParameters(action, inputStream);
            response = action.Execute(content, parameters);

            var responseAsContent = response as Content;
            if (responseAsContent != null)
            {
                WriteSingleContent(responseAsContent, portalContext);
                return;
            }

            int count;
            response = ProcessOperationResponse(response, portalContext, odataReq, out count);
            //Write(response, portalContext);
            WriteOperationResult(response, portalContext, odataReq);
        }
        private void WriteOperationResult(object result, PortalContext portalContext, ODataRequest odataReq)
        {
            var content = result as Content;
            if (content != null)
            {
                WriteSingleContent(content, portalContext);
                return;
            }

            var enumerable = result as IEnumerable<Content>;
            if (enumerable != null)
            {
                WriteMultiRefContents(enumerable, portalContext, odataReq);
                return;
            }

            WriteOperationCustomResult(portalContext, result);
        }
        protected abstract void WriteOperationCustomResult(PortalContext portalContext, object result);

        private object ProcessOperationResponse(object response, PortalContext portalContext, ODataRequest odataReq, out int count)
        {
            var qdef = response as ChildrenDefinition;
            if (qdef != null)
                return ProcessOperationQueryResponse(qdef, portalContext, odataReq, out count);

            var coll = response as IEnumerable<Content>;
            if (coll != null)
                return ProcessOperationCollectionResponse(coll, portalContext, odataReq, out count);

            var dict = response as IDictionary;
            if (dict != null)
            {
                count = dict.Count;
                var targetTypized = new Dictionary<Content, object>();
                foreach (var item in dict.Keys)
                {
                    var content = item as Content;
                    if (content == null)
                        return response;
                    targetTypized.Add(content, dict[content]);
                }
                return ProcessOperationDictionaryResponse(targetTypized, portalContext, odataReq, out count);
            }

            // get real count from an enumerable
            var enumerable = response as IEnumerable;
            if (enumerable != null)
            {
                var c = 0;
                foreach (var x in enumerable)
                    c++;
                count = c;
            }
            else
            {
                count = 1;
            }

            if (response != null && response.ToString() == "{ PreviewAvailable = True }")
                return true;
            if (response != null && response.ToString() == "{ PreviewAvailable = False }")
                return false;
            return response;
        }
        private List<Dictionary<string, object>> ProcessOperationQueryResponse(ChildrenDefinition qdef, PortalContext portalContext, ODataRequest req, out int count)
        {
            var cdef = new ChildrenDefinition
            {
                PathUsage = qdef.PathUsage,
                ContentQuery = qdef.ContentQuery,
                Top = req.Top > 0 ? req.Top : qdef.Top,
                Skip = req.Skip > 0 ? req.Skip : qdef.Skip,
                Sort = req.Sort != null && req.Sort.Count() > 0 ? req.Sort : qdef.Sort,
                EnableAutofilters = req.AutofiltersEnabled != FilterStatus.Default ? req.AutofiltersEnabled : qdef.EnableAutofilters,
                EnableLifespanFilter = req.LifespanFilterEnabled != FilterStatus.Default ? req.AutofiltersEnabled : qdef.EnableLifespanFilter
            };

            var sourceCollectionItemType = typeof(Content);
            var lucQuery = SnExpression.BuildQuery(req.Filter, typeof(Content), null, cdef);
            var result = lucQuery.Execute();
            var idResult = result.Select(x => x.NodeId);
            //var count = req.InlineCount == InlineCount.AllPages ? ExecuteQueryWithCountOnly(lucQuery, cdef.EnableAutofilters, cdef.EnableLifespanFilter) : idResult.Count();
            count = req.InlineCount == InlineCount.AllPages ? lucQuery.TotalCount : idResult.Count();
            
            if (req.CountOnly)
                return null;

            var contents = new List<Dictionary<string, object>>();
            var projector = Projector.Create(req, true);
            foreach (var id in idResult)
            {
                var content = Content.Load(id);
                var fields = CreateFieldDictionary(content, portalContext, projector);
                contents.Add(fields);
            }
            return contents;
        }
        private List<Dictionary<string, object>> ProcessOperationDictionaryResponse(IDictionary<Content, object> input, PortalContext portalContext, ODataRequest req, out int count)
        {
            var x = ProcessODataFilters(input.Keys, portalContext, req);

            var output = new List<Dictionary<string, object>>();
            var projector = Projector.Create(req, true);
            foreach (var content in x)
            {
                var fields = CreateFieldDictionary(content, portalContext, projector);
                var item = new Dictionary<string, object>
                {
                    {"key", fields},
                    {"value", input[content]}
                };
                output.Add(item);
            }
            count = req.InlineCount == InlineCount.AllPages ? input.Count() : output.Count;
            if (req.CountOnly)
                return null;
            return output;
        }
        private List<Dictionary<string, object>> ProcessOperationCollectionResponse(IEnumerable<Content> inputContents, PortalContext portalContext, ODataRequest req, out int count)
        {
            var x = ProcessODataFilters(inputContents, portalContext, req);

            var outContents = new List<Dictionary<string, object>>();
            var projector = Projector.Create(req, true);
            foreach (var content in x)
            {
                var fields = CreateFieldDictionary(content, portalContext, projector);
                outContents.Add(fields);
            }

            count = req.InlineCount == InlineCount.AllPages ? inputContents.Count() : outContents.Count;
            if (req.CountOnly)
                return null;
            return outContents;
        }

        private IEnumerable<Content> ProcessODataFilters(IEnumerable<Content> inputContents, PortalContext portalContext, ODataRequest req)
        {
            var x = inputContents;
            if (req.HasFilter)
            {
                var y = x as IQueryable<Content>;
                if (y != null)
                {
                    x = y.Where((Expression<Func<Content, bool>>)req.Filter);
                }
                else
                {
                    var lambdaExpr = (LambdaExpression)req.Filter;
                    x = x.Where((Func<Content, bool>)lambdaExpr.Compile());
                }
            }
            if (req.HasSort)
                x = AddSortToCollectionExpression(x, req.Sort);
            if (req.HasSkip)
                x = x.Skip(req.Skip);
            if (req.HasTop)
                x = x.Take(req.Top);

            return x;
        }
        private IEnumerable<Content> AddSortToCollectionExpression(IEnumerable<Content> contents, IEnumerable<SortInfo> sort)
        {
            IOrderedEnumerable<Content> sortedContents = null;
            foreach (var sortInfo in sort)
            {
                if (sortedContents == null)
                {
                    if (sortInfo.Reverse)
                        sortedContents = contents.OrderByDescending(c => c[sortInfo.FieldName]);
                    else
                        sortedContents = contents.OrderBy(c => c[sortInfo.FieldName]);
                }
                else
                {
                    if (sortInfo.Reverse)
                        sortedContents = sortedContents.ThenByDescending(c => c[sortInfo.FieldName]);
                    else
                        sortedContents = sortedContents.OrderBy(c => c[sortInfo.FieldName]);
                }
            }
            return sortedContents ?? contents;
        }

        //----------------------------------------------------------------------------------------------------------------------------------- utilities

        private object[] GetOperationParameters(ActionBase action, HttpRequest request)
        {
            if (action.ActionParameters.Length == 0)
                return ActionParameter.EmptyValues;

            var values = new object[action.ActionParameters.Length];
            //if (inputStream == null || inputStream.Length == 0)
            //    return values;

            var parameters = action.ActionParameters;
            if (parameters.Length == 1 && parameters[0].Name == null)
            {
                throw new ArgumentException("Cannot parse unnamed parameter from URL. This operation expects POST verb.");
            }
            else
            {
                var i = 0;
                foreach (var parameter in parameters)
                {
                    var name = parameter.Name;
                    var type = parameter.Type;
                    var val = request[name];
                    if (val == null)
                    {
                        if (parameter.Required)
                            throw new ArgumentNullException(parameter.Name);
                    }
                    else
                    {
                        var valStr = val.ToString();

                        if (type == typeof(string))
                        {
                            values[i] = valStr;
                        }
                        else if (type == typeof(Boolean))
                        {
                            // we handle "True", "true" and "1" as boolean true values
                            values[i] = JsonConvert.DeserializeObject(valStr.ToLower(), type);
                        }
                        else if (type == typeof(string[]))
                        {
                            var parsed = false;
                            try
                            {
                                values[i] = JsonConvert.DeserializeObject(valStr, type);
                                parsed = true;
                            }
                            catch // recompute
                            {
                            }
                            if (!parsed)
                            {
                                if (valStr.StartsWith("'"))
                                    values[i] = GetStringArrayFromString(name, valStr, '\'');
                                else if (valStr.StartsWith("\""))
                                    values[i] = GetStringArrayFromString(name, valStr, '"');
                                else
                                    values[i] = valStr.Split(',').Select(s => s == null ? s : s.Trim()).ToArray();
                            }
                        }
                        else
                        {
                            values[i] = JsonConvert.DeserializeObject(valStr, type);
                        }
                    }
                    i++;
                }
            }
            return values;
        }
        private string[] GetStringArrayFromString(string paramName, string src, char stringEnvelope)
        {
            var result = new List<string>();
            int startPos = -1;
            bool started = false;
            for (int i = 0; i < src.Length; i++)
            {
                var c = src[i];
                if (c == stringEnvelope)
                {
                    if (!started)
                    {
                        started = true;
                        startPos = i + 1;
                    }
                    else
                    {
                        started = false;
                        result.Add(src.Substring(startPos, i - startPos));
                    }
                }
                else if (!started)
                {
                    if (c != ' ' && c != ',')
                        throw new ODataException("Parameter error: cannot parse a string array. Name: " + paramName, ODataExceptionCode.NotSpecified);
                }
            }
            return result.ToArray();
        }
        private object[] GetOperationParameters(ActionBase action, Stream inputStream)
        {
            if (action.ActionParameters.Length == 0)
                return ActionParameter.EmptyValues;

            var values = new object[action.ActionParameters.Length];
            //if (inputStream == null || inputStream.Length == 0)
            //    return values;

            var parameters = action.ActionParameters;
            if (parameters.Length == 1 && parameters[0].Name == null)
            {
                var parameter = parameters[0];
                if (parameter.Type == null)
                {
                    using (var reader = new StreamReader(inputStream))
                        values[0] = reader.ReadToEnd();
                    if (parameter.Required && values[0] == null)
                        throw new ArgumentNullException("[unnamed]", "Request parameter is required.");
                }
                else
                {
                    values[0] = ODataHandler.Read(inputStream, parameter.Type);
                    if (parameter.Required && values[0] == null)
                        throw new ArgumentNullException("[unnamed]", "Request parameter is required. Type: " + parameter.Type.FullName);
                }
            }
            else
            {
                var model = ODataHandler.Read(inputStream);
                var i = 0;
                foreach (var parameter in parameters)
                {
                    var name = parameter.Name;
                    var type = parameter.Type;
                    var val = model == null ? null : model[name];
                    if (val == null)
                    {
                        if (parameter.Required)
                            throw new ArgumentNullException(parameter.Name);
                    }
                    else
                    {
                        var valStr = val.ToString();

                        if (type == typeof(string))
                        {
                            values[i] = valStr;
                        }
                        else if (type == typeof(Boolean))
                        {
                            // we handle "True", "true" and "1" as boolean true values
                            values[i] = JsonConvert.DeserializeObject(valStr.ToLower(), type);
                        }
                        else
                        {
                            values[i] = JsonConvert.DeserializeObject(valStr, type);
                        }
                    }
                    i++;
                }
            }
            return values;
        }
        private Dictionary<string, object> CreateFieldDictionary(Content content, PortalContext portalContext, Projector projector)
        {
            return projector.Project(content);
        }
        private Dictionary<string, object> CreateFieldDictionary(Content content, PortalContext portalContext, bool isCollectionItem)
        {
            var projector = Projector.Create(this.ODataRequest, isCollectionItem, content);
            return projector.Project(content);
        }

        //UNDONE: X: Bad name: GetJsonObject is a method for odata serializing
        internal static object GetJsonObject(Field field, string selfUrl)
        {
            object data;
            if (field is ReferenceField)
            {
                return ODataReference.Create(String.Concat(selfUrl, "/", field.Name));
            }
            else if (field is BinaryField)
            {
                var binaryField = (BinaryField)field;
                var binaryData = (BinaryData)binaryField.GetData();

                return ODataBinary.Create(ContentTools.GetBinaryUrl(field.Content.Id, field.Name, binaryData.Timestamp), null, binaryData.ContentType, null);
            }
            else if (ODataHandler.DeferredFieldNames.Contains(field.Name))
            {
                return ODataReference.Create(String.Concat(selfUrl, "/", field.Name));
            }
            data = field.GetData();
            var nodeType = data as NodeType;
            if (nodeType != null)
                return nodeType.Name;
            return data;
        }

        protected void Write(object response, PortalContext portalContext)
        {
            var resp = portalContext.OwnerHttpContext.Response;

            if (response == null)
            {
                resp.StatusCode = 204;
                return;
            }

            if (response is string) //HACK: maybe deletable
            {
                WriteRaw(response, portalContext);
                return;
            }

            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = Formatting.Indented,
                Converters = ODataHandler.JsonConverters
            };
            var serializer = JsonSerializer.Create(settings);
            serializer.Serialize(portalContext.OwnerHttpContext.Response.Output, response);
            resp.ContentType = "application/json;odata=verbose;charset=utf-8";
        }
        protected void WriteRaw(object response, PortalContext portalContext)
        {
            var resp = portalContext.OwnerHttpContext.Response;
            resp.Write(response);
        }

    }
}
