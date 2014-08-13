using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Configuration;

namespace SenseNet.Packaging.Steps
{
    /// <summary>Represents one activity in the package execution sequence</summary>
    public abstract class Step
    {
        private static Dictionary<string, Type> _stepTypes;
        internal static Dictionary<string, Type> StepTypes { get { return _stepTypes; } }

        static Step()
        {
            var stepTypes = new Dictionary<string,Type>();
            foreach (var stepType in SenseNet.ContentRepository.Storage.TypeHandler.GetTypesByBaseType(typeof(Step)))
            {
                if (!stepType.IsAbstract)
                {
                    var step = (Step)Activator.CreateInstance(stepType);
                    stepTypes[step.ElementName] = stepType;
                    stepTypes[stepType.FullName] = stepType;
                }
            }
            _stepTypes = stepTypes;
        }

        internal static Step BuildStep(int stepId, string stepName, Dictionary<string, string> parameters)
        {
            Type stepType;
            if (!_stepTypes.TryGetValue(stepName, out stepType))
                throw new InvalidPackageException(String.Format(SR.Errors.StepParsing.UnknownStep_1, stepName));

            var step = (Step)Activator.CreateInstance(stepType);
            step.StepId = stepId;
            foreach (var item in parameters)
                step.SetProperty(item.Key, item.Value);

            return step;
        }

        /*--------------------------------------------------------------------------------------------------------------------------------------------*/

        internal void SetProperty(string name, string value)
        {
            var formatProvider = System.Globalization.CultureInfo.InvariantCulture;
            var stepType = this.GetType();
            PropertyInfo prop = null;
            var propertyName = name;
            if (propertyName == string.Empty)
            {
                prop = GetDefaultProperty(stepType);
                if (prop == null)
                    throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.DefaultPropertyNotFound_1, stepType.FullName));
            }
            else
            {
                prop = stepType.GetProperty(propertyName);
                if (prop == null)
                    throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.UnknownProperty_2, stepType.FullName, propertyName));
            }

            if (!(prop.PropertyType.GetInterfaces().Any(x => x == typeof(IConvertible))))
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.PropertyTypeMustBeConvertible_2, stepType.FullName, propertyName));

            try
            {
                var val = prop.PropertyType.IsEnum
                        ? Enum.Parse(prop.PropertyType, value, true)
                        : ((IConvertible)(value)).ToType(prop.PropertyType, formatProvider);
                var setter = prop.GetSetMethod();
                setter.Invoke(this, new object[] { val });
            }
            catch (Exception e)
            {
                throw new InvalidPackageException(string.Format(SR.Errors.StepParsing.CannotConvertToPropertyType_3, stepType.FullName, propertyName, prop.PropertyType), e);
            }
        }
        private static PropertyInfo GetDefaultProperty(Type stepType)
        {
            var props = stepType.GetProperties();
            foreach (var prop in props)
                if (prop.GetCustomAttributes(true).Any(x => x is DefaultPropertyAttribute))
                    return prop;
            return null;
        }

        /*=========================================================== Public instance part ===========================================================*/

        /// <summary>Returns the XML name of the step element in the manifest. Default: simple or fully qualified name of the class.</summary>
        public virtual string ElementName { get { return this.GetType().Name; } }
        /// <summary>Order number in the phase.</summary>
        public int StepId { get; private set; }
        /// <summary>The method that executes the activity. Called by packaging framework.</summary>
        public abstract void Execute(ExecutionContext context);

        /*=========================================================== Common tools ===========================================================*/

        /// <summary>Returns with a full path under the package if the path is relative.</summary>
        protected static string ResolvePackagePath(string path, ExecutionContext context)
        {
            return ResolvePath(context.PackagePath, path);
        }
        /// <summary>Returns with a full path under the target directory on the local server if the path is relative.</summary>
        protected static string ResolveTargetPath(string path, ExecutionContext context)
        {
            return ResolvePath(context.TargetPath, path);
        }
        private static string ResolvePath(string basePath, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;
            var path = Path.Combine(basePath, relativePath);
            var result = Path.GetFullPath(path);
            return result;
        }
        /// <summary>Returns with a full paths under the target directories on the network servers if the path is relative.</summary>
        protected static string[] ResolveNetworkTargets(string path, ExecutionContext context)
        {
            if (Path.IsPathRooted(path))
                return new string[0];
            var resolved = context.NetworkTargets.Select(x => Path.GetFullPath(Path.Combine(x, path))).ToArray();
            return resolved;
        }
        /// <summary>Returns with a full paths under the target directories on all servers if the path is relative.</summary>
        protected static string[] ResolveAllTargets(string path, ExecutionContext context)
        {
            var allTargets = new List<string>(context.NetworkTargets.Length + 1);
            allTargets.Add(ResolveTargetPath(path, context));
            allTargets.AddRange(ResolveNetworkTargets(path, context));
            return allTargets.ToArray();
        }
    }
}
