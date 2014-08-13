using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Configuration;
using System.Diagnostics;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository.Storage
{
    public class ApplicationInfo
    {
        public string Name { get; set; }
        public string Edition { get; set; }
        public string AppId { get; set; }
        public Version Version { get; set; }
        public Version AcceptableVersion { get; set; }
        public string Description { get; set; }

        public static ApplicationInfo CreateInitialSenseNetVersion(string name, string edition, Version version, string description)
        {
            return SenseNet.ContentRepository.Storage.Data.DataProvider.Current.CreateInitialSenseNetVersion(name, edition, version, description);
        }
    }

    public class AssemblyInfo
    {
        public string Name { get; set; }
        public bool IsDynamic { get; set; }
        public string CodeBase { get; set; }
        public string Version { get; set; }
    }

    public static class TypeHandler
    {
        private static Dictionary<string, Type> _typecacheByName = new Dictionary<string, Type>();
        private static Dictionary<Type, Type[]> _typecacheByBase = new Dictionary<Type, Type[]>();

        private static object _typeCacheSync = new object();

        public static T CreateInstance<T>(string typeName) where T : new()
        {
			return (T)CreateInstance(typeName);
        }
		public static T CreateInstance<T>(string typeName, params object[] args)
		{
			return (T)CreateInstance(typeName, args);
		}
        public static object CreateInstance(string typeName)
        {
			Type t = GetType(typeName);
			if (t == null)
				throw new TypeNotFoundException(typeName);
			return Activator.CreateInstance(t, true);
        }
		public static object CreateInstance(string typeName, params object[] args)
		{
			Type t = GetType(typeName);
			if (t == null)
				throw new TypeNotFoundException(typeName);
			return Activator.CreateInstance(t, args);
		}

        public static Type GetType(string typeName)
        {
            //assume its an assembly qualified type name
            Type t = Type.GetType(typeName, false);
            //if fusion loader fails lets find the type in what we have
            return t ?? FindTypeInAppDomain(typeName);
        }
        internal static Type FindTypeInAppDomain(string typeName)
        {
            return FindTypeInAppDomain(typeName, true);
        }
        internal static Type FindTypeInAppDomain(string typeName, bool throwOnError)
        {
            Type type = null;
            if (!_typecacheByName.TryGetValue(typeName, out type))
            {
                lock (_typeCacheSync)
                {
                    if (!_typecacheByName.TryGetValue(typeName, out type))
                    {
                        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                type = assembly.GetType(typeName);
                                if (type != null)
                                    break;
                            }
                            catch (Exception e)
                            {
                                if (!IgnorableException(e))
                                    throw;
                            }
                        }
                        if (type == null)
                        {
                            var split = typeName.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            var tname = split[0];
                            var asmName = split.Length > 1 ? split[1].ToLower().Trim() : null;
                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                if (asmName != null && asmName != assembly.GetName().Name.ToLower())
                                    continue;
                                try
                                {
                                    type = assembly.GetType(tname);
                                    if (type != null)
                                        break;
                                }
                                catch (Exception e)
                                {
                                    if (!IgnorableException(e))
                                        throw;
                                }
                            }
                        }

                        //
                        //  Important: leave this comment here
                        //  There was an error adding NULL type to _typecachaByName dictionary, after restarting the AddDomain with iisreset. 
                        //  It is fixed by BuildManager.GetReferencedAssemblies() call when Application_OnStart event occurs.
                        //

                        if (!_typecacheByName.ContainsKey(typeName))
                            _typecacheByName.Add(typeName, type);
                    }
                }
            }
            if (throwOnError && type == null)
                new TypeNotFoundException(typeName);

            return type;
        }

		public static Assembly[] GetAssemblies()
		{
			return AppDomain.CurrentDomain.GetAssemblies();
		}
		public static string[] LoadAssembliesFrom(string path)
		{
			if (path == null)
				throw new ArgumentNullException("path");
			if (path.Length == 0)
				throw new ArgumentException("Path cannot be empty.", "path");

			List<string> assemblyNames = new List<string>();
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
				assemblyNames.Add(new AssemblyName(asm.FullName).Name);

			List<string> loaded = new List<string>();
			string[] dllPaths = Directory.GetFiles(path, "*.dll");
            var badImageFormatMessages = new List<string>();
			foreach (string dllPath in dllPaths)
			{
                try
                {
                    string asmName = AssemblyName.GetAssemblyName(dllPath).Name;
                    if (!assemblyNames.Contains(asmName))
                    {
                        Assembly.LoadFrom(dllPath);
                        assemblyNames.Add(asmName);
                        loaded.Add(Path.GetFileName(dllPath));
                    }
                }
                catch (BadImageFormatException e) //logged
                {
                    badImageFormatMessages.Add(e.Message);
                }
			}
            if (badImageFormatMessages.Count > 0)
                Logger.WriteInformation(Logger.EventId.NotDefined, String.Format("Skipped assemblies from {0} on start: {1}{2}", path, Environment.NewLine, String.Join(Environment.NewLine, badImageFormatMessages)));

			return loaded.ToArray();
		}

		public static Type[] GetTypesByInterface(Type interfaceType)
		{
		    Type[] temp;
            if (!_typecacheByBase.TryGetValue(interfaceType, out temp))
            {
                lock (_typeCacheSync)
                {
                    if (!_typecacheByBase.TryGetValue(interfaceType, out temp))
                    {
                        var list = new List<Type>();
                        foreach (Assembly asm in GetAssemblies())
                        {
                            try
                            {
                                var types = asm.GetTypes();
                                foreach (Type type in types)
                                    foreach (var interf in type.GetInterfaces())
                                        if (interf == interfaceType)
                                            list.Add(type);
                            }
                            catch (Exception e)
                            {
                                if (!IgnorableException(e))
                                    throw;
                            }
                        }
                        temp = list.ToArray();

                        if (!_typecacheByBase.ContainsKey(interfaceType))
                            _typecacheByBase.Add(interfaceType, temp);
                    }
                }
            }
            var result = new Type[temp.Length];
            temp.CopyTo(result, 0);
            return result;
		}
        public static Type[] GetTypesByBaseType(Type baseType)
        {
            Type[] temp;
            if (!_typecacheByBase.TryGetValue(baseType, out temp))
            {
                lock (_typeCacheSync)
                {
                    if (!_typecacheByBase.TryGetValue(baseType, out temp))
                    {
                        var list = new List<Type>();
                        foreach (Assembly asm in GetAssemblies())
                        {
                            try
                            {
                                var types = asm.GetTypes();
                                foreach (Type type in types)
                                {
                                    try
                                    {
                                        if (type.IsSubclassOf(baseType))
                                            list.Add(type);
                                    }
                                    catch (Exception e)
                                    {
                                        if (!IgnorableException(e))
                                            throw TypeDiscoveryError(e, type.FullName, asm);
                                        //throw new ApplicationException(String.Concat("Type discovery error. Type: ", type.FullName, ", assembly: ", asm), e);
                                    }
                                }
                            }
                            catch (ReflectionTypeLoadException rtle)
                            {
                                Logger.WriteException(rtle);

                                // Logging each exception
                                foreach (var exc in rtle.LoaderExceptions)
                                {
                                    Logger.WriteException(exc);
                                }

                                throw;
                            }
                            catch (Exception e)
                            {
                                if (!IgnorableException(e))
                                    throw TypeDiscoveryError(e, null, asm);
                                //throw new ApplicationException(String.Concat("Type discovery error. Assembly: ", asm), e);
                            }
                        }
                        temp = list.ToArray();

                        if (!_typecacheByBase.ContainsKey(baseType))
                            _typecacheByBase.Add(baseType, temp);
                    }
                }
            }
            var result = new Type[temp.Length];
            temp.CopyTo(result, 0);
            return result;
        }

        private static bool IgnorableException(Exception e)
        {
            if (!Debugger.IsAttached)
                return false;
            var rte = e as ReflectionTypeLoadException;
            if (rte != null)
            {
                if (rte.LoaderExceptions.Length == 2)
                {
                    var te0 = rte.LoaderExceptions[0] as TypeLoadException;
                    var te1 = rte.LoaderExceptions[1] as TypeLoadException;
                    if (te0 != null && te1 != null)
                    {
                        if (te0.TypeName == "System.Web.Mvc.CompareAttribute" && te1.TypeName == "System.Web.Mvc.RemoteAttribute")
                            return true;
                    }
                }
            }
            return false;
        }
        private static Exception TypeDiscoveryError(Exception innerEx, string typeName, Assembly asm)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var duplicates = assemblies.GroupBy(f => f.ToString()).Where(g => g.Count() > 1).ToArray();
            
            //--
            var msg = new StringBuilder();
            msg.Append("Type discovery error. Assembly: ").Append(asm);
            if (typeName != null)
                msg.Append(", type: ").Append(typeName).Append(".");
            if (duplicates.Count() > 0)
            {
                msg.AppendLine().AppendLine("DUPLICATED ASSEMBLIES:");
                var count = 0;
                foreach (var item in duplicates)
                    msg.Append("    #").Append(count++).Append(": ").AppendLine(item.Key);
            }
            return new ApplicationException(msg.ToString(), innerEx);
        }
        //=========================================================================

        public static T ResolveProvider<T>()
        {
            try
            {
                return Container.Resolve<T>();
            }
            catch (ResolutionFailedException)
            {
                return (T)(object)null;
            }
        }

        public static T ResolveNamedType<T>(string name)
        {
            try
            {
                return Container.Resolve<T>(name);
            }
            catch (ResolutionFailedException)
            {
                return (T)(object)null;
            }
        }

        public static T ResolveInstance<T>(string name)
        {
            try
            {
                return Container.Resolve<T>(name);
            }
            catch (ResolutionFailedException)
            {
                return (T)(object)null;
            }
        }

        private static UnityContainer _container;
        private static object _containerLock = new object();


        public static UnityContainer Container
        {
            get
            {
                if (_container == null)
                {
                    lock (_containerLock)
                    {
                        if (_container == null)
                        {
                            _container = GetUnityContainer();
                        }
                    }
                }
                return _container;
            }
        }
        private static UnityContainer GetUnityContainer()
        {
            var container = new UnityContainer();
            var section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            if (section == null)
                throw new ConfigurationErrorsException("Unity section was not found. There is no configuration or it is invalid.");
            section.Configure(container, "Providers");

            return container;
        }

        //=========================================================================

        /// <summary>
        /// Returns a sorted  list of 
        /// </summary>
        /// <returns></returns>
        public static AssemblyInfo[] GetAssemblyInfo()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => GetAssemblyInfo(a)).OrderBy(x => x.CodeBase).ToArray();
        }
        public static AssemblyInfo GetAssemblyInfo(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            return new AssemblyInfo
            {
                Name = asm.FullName,
                IsDynamic = asm.IsDynamic,
                CodeBase = GetCodeBase(asm),
                Version = GetAssemblyVersionString(asm)
            };
        }

        public static string GetCodeBase(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (asm.IsDynamic)
                return string.Empty;
            return asm.CodeBase.Replace("file:///", "").Replace("file://", "//").Replace("/", "\\");
        }

        public static Version GetVersion(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (asm.IsDynamic)
                return null;
            return asm.GetName().Version;
        }
        public static string GetAssemblyVersionString(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (asm.IsDynamic)
                return string.Empty;
            var ver = asm.GetName().Version.ToString();

            var dbg = IsDebugMode(asm);
            if (dbg.HasValue)
            {
                if (dbg.Value)
                    return ver + " Debug";
                return ver + " Release";
            }
            return ver;
        }

        public static bool? IsDebugMode(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (!asm.ReflectionOnly)
            {
                DebuggableAttribute dbgAttr;
                if ((dbgAttr = (DebuggableAttribute)(asm.GetCustomAttributes(typeof(DebuggableAttribute), false).FirstOrDefault())) != null)
                    return dbgAttr.IsJITTrackingEnabled;
                return false;
            }

            var data = asm.GetCustomAttributesData().FirstOrDefault(x => x.Constructor.ReflectedType == typeof(DebuggableAttribute));
            if (data == null)
                return false;
            if (data.ConstructorArguments.Count == 2)
                return (bool)(data.ConstructorArguments[0].Value);
            if (data.ConstructorArguments.Count == 1)
                return ((DebuggableAttribute.DebuggingModes)data.ConstructorArguments[0].Value & DebuggableAttribute.DebuggingModes.Default)
                    != DebuggableAttribute.DebuggingModes.None;
            return null;
        }

    }
}