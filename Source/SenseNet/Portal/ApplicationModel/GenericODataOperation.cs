using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ApplicationModel;
using System.Web;
using System.Reflection;
using SenseNet.ContentRepository;
using SenseNet.Portal.ApplicationModel;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ApplicationModel
{
    public class GenericODataOperation : ActionBase
    {
        public override string Uri
        {
            get { return null; }
        }

        public override bool IsHtmlOperation { get { return false; } }
        public override bool IsODataOperation { get { return true; } }
        private bool _causesStateChange = true;
        public override bool CausesStateChange { get { return _causesStateChange; } }

        private MethodBase _method;
        private Type[] _paramTypes;
        private string[] _paramNames;
        private ActionParameter[] _actionParameters;
        public override ActionParameter[] ActionParameters { get { return _actionParameters; } }
        private object _callingParameters;

        private static readonly string[] EmptyStringArray = new string[0];
        private static readonly Type[] EmptyTypeArray = new Type[0];

        private bool _hasFunctionAttribute;
        private bool _hasActionAttribute;

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            var app = application as GenericODataApplication;
            var methodName = app.MethodName;
            string[] paramNames;
            _paramTypes = GetMethodParams(app.Parameters, out paramNames);
            _paramNames = paramNames;

            _actionParameters = new ActionParameter[_paramTypes.Length];
            for (int i = 0; i < _paramTypes.Length; i++)
                ActionParameters[i] = new ActionParameter(_paramNames[i], _paramTypes[i], true);

            var type = TypeHandler.GetType(app.ClassName);
            if (type == null)
                throw new InvalidOperationException("Unknown type: " + app.ClassName);

            var prmTypes = new Type[_paramTypes.Length + 1];
            prmTypes[0] = typeof(Content);
            Array.Copy(_paramTypes, 0, prmTypes, 1, _paramTypes.Length);
            _method = type.GetMethod(methodName, prmTypes);
            if (_method == null)
                throw new InvalidOperationException("Unknown method: " + methodName);

            _hasActionAttribute = _method.GetCustomAttributes(typeof(ODataAction), true).Length != 0;
            _hasFunctionAttribute = _method.GetCustomAttributes(typeof(ODataFunction), true).Length != 0;
            _causesStateChange = _hasActionAttribute;

            _callingParameters = parameters;

            base.Initialize(context, backUri, application, parameters);
        }
        private Type[] GetMethodParams(string prms, out string[] prmNames)
        {
            if (String.IsNullOrEmpty(prms))
            {
                prmNames = EmptyStringArray;
                return EmptyTypeArray;
            }

            var prmdefs = prms.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var types = new Type[prmdefs.Length];
            var names = new List<string>();
            for (int i = 0; i < prmdefs.Length; i++)
            {
                var prmDef = prmdefs[i].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var typeName = prmDef[0].Trim();
                var name =prmDef[1].Trim();
                Type type = null;
                switch (typeName)
                {
                    case "string": type = typeof(string); break;
                    case "int": type = typeof(int); break;
                    case "bool": type = typeof(bool); break;
                    case "long": type = typeof(long); break;
                    case "decimal": type = typeof(decimal); break;
                    case "double": type = typeof(double); break;
                    case "object": type = typeof(object); break;

                    case "byte": type = typeof(byte); break;
                    case "sbyte": type = typeof(sbyte); break;
                    case "char": type = typeof(char); break;
                    case "float": type = typeof(float); break;
                    case "uint": type = typeof(uint); break;
                    case "ulong": type = typeof(ulong); break;
                    case "short": type = typeof(short); break;
                    case "ushort": type = typeof(ushort); break;

                    case "DateTime": type = typeof(DateTime); break;

                    case "string[]": type = typeof(string[]); break;
                    case "int[]": type = typeof(int[]); break;

                    case "Node": type = typeof(Node); break;
                    case "Content": type = typeof(Content); break;
                    default: type = TypeHandler.GetType(typeName); break;
                }
                if (type == null)
                    throw new InvalidOperationException("Unknown parameter type: " + prmDef[0]);
                types[i] = type;

                if (names.Contains(name))
                    throw new InvalidOperationException("duplicated parameter name: " + prmDef[1]);

                types[i] = type;
                names.Add(name);
            }
            prmNames = names.ToArray();

            return types;
        }

        public override object Execute(Content content, params object[] parameters)
        {
            if (!_hasActionAttribute && !_hasFunctionAttribute)
                throw new MethodAccessException("Access denied. This method cannot be called through a generic operation.");

            var p = new object[parameters.Length + 1];
            p[0] = content;
            Array.Copy(parameters, 0, p, 1, parameters.Length);

            try
            {
                return _method.Invoke(null, p);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;

                throw;
            }
        }

    }
}
