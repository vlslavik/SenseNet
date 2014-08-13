using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace SenseNet.Packaging.Steps
{
    public class Copy : Step
    {
        [DefaultProperty]
        public string Source { get; set; }
        public string TargetDirectory { get; set; }
        public string NewName { get; set; }

        private static Assembly[] __assemblies;
        private static Assembly[] Assemblies
        {
            get
            {
                if (__assemblies == null)
                    __assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return __assemblies;
            }
        }
        private static Assembly[] __reflectionOnlyAssemblies;
        private static Assembly[] ReflectionOnlyAssemblies
        {
            get
            {
                if (__reflectionOnlyAssemblies == null)
                    __reflectionOnlyAssemblies = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();
                return __reflectionOnlyAssemblies;
            }
        }

        public override void Execute(ExecutionContext context)
        {
            var sourcePath = ResolvePackagePath(Source, context);
            var name = NewName ?? Path.GetFileName(sourcePath);
            var targetDirectories = ResolveAllTargets(TargetDirectory, context);
            foreach (var targetDirectory in targetDirectories)
                Execute(sourcePath, targetDirectory, name);
        }
        private void Execute(string sourcePath, string targetDirectory, string name)
        {
            var targetFilePath = Path.Combine(targetDirectory, name);

            if (Directory.Exists(sourcePath))
            {
                Logger.LogMessage("Copying {0} to {1}", Source, targetDirectory);

                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                var target = Path.Combine(targetDirectory, Path.GetFileName(sourcePath));
                CopyDirectory(sourcePath, target, true);
                return;
            }

            if (IsAssembly(sourcePath))
            {
                var x = System.Reflection.Assembly.ReflectionOnlyLoadFrom(sourcePath);
                CopyAssembly(sourcePath, targetFilePath);
                return;
            }
            Logger.LogMessage("Copying {0} to {1}", Source, targetDirectory);
            File.Copy(sourcePath, targetFilePath, true);
        }

        private static readonly string[] _assemblyExtensions = ".dll;.exe".Split(';');
        private bool IsAssembly(string path)
        {
            return _assemblyExtensions.Contains(Path.GetExtension(path));
        }
        private void CopyAssembly(string source, string target)
        {
            var fileName = Path.GetFileName(source);
            var sourceVersion = GetAssemblyVersionAndMode(source);
            if (File.Exists(target))
                Logger.LogMessage("Overwriting the {0}. Old version: {1}, new version: {2}."
                    , fileName
                    , GetAssemblyVersionAndMode(target)
                    , sourceVersion);
            else
                Logger.LogMessage("Installing the {0}. Version: {1}"
                    , fileName
                    , sourceVersion);
            File.Copy(source, target, true);
        }
        private static Version GetAssemblyVersion(string path)
        {
            return AssemblyName.GetAssemblyName(path).Version;
        }
        private static string GetAssemblyVersionAndMode(string path)
        {
            //var asm = GetAssembly(path);
            //if (asm == null)
            //    return "?.?";
            //var dbg = SenseNet.ContentRepository.Storage.TypeHandler.IsDebugMode(asm);
            //return asm.GetName().Version.ToString() + (dbg.HasValue ? (dbg.Value ? " Debug" : " Release") : "?" );
            return AssemblyName.GetAssemblyName(path).Version.ToString();
        }
        //private static Dictionary<string, Assembly> _asmCache = new Dictionary<string, Assembly>();
        //private static Assembly GetAssembly(string path)
        //{
        //    var key = path.ToLower();
        //    Assembly asm;
        //    if(_asmCache.TryGetValue(key, out asm))
        //        return asm;
        //    asm = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(path));
        //    _asmCache[key] = asm;
        //    return asm;
        //}

        private static int rootLength;
        private static void CopyDirectory(string source, string target, bool recursive)
        {
            DirectoryInfo dir = new DirectoryInfo(source);
            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + source);

            if (rootLength == 0)
                rootLength = Path.GetDirectoryName(source).Length;

            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                Logger.LogMessage(file.FullName.Substring(rootLength));
                file.CopyTo(Path.Combine(target, file.Name), true);
            }
            if (recursive)
                foreach (DirectoryInfo subdir in dirs)
                    CopyDirectory(subdir.FullName, Path.Combine(target, subdir.Name), recursive);
        }
    }
}
