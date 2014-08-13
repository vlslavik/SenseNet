using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using SenseNet.Packaging;
using SenseNet.ContentRepository;
using System.Diagnostics;
using Ionic.Zip;
using System.Configuration;

namespace SenseNet.Tools.SnAdmin
{
    class Program
    {
        #region Constants
        private static string CR = Environment.NewLine;
        private static string ToolTitle = "Sense/Net Admin v1.0";
        private static string UsageScreen = String.Concat(
            //         1         2         3         4         5         6         7         8
            //12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
            CR,
            "Usage:", CR,
            "SnAdmin <package> [<target>]", CR,
            CR,
            "Parameters:", CR,
            "<package>: File contains a package (*.zip or directory).", CR,
            "<target>: Directory contains web folder of a stopped SenseNet instance.", CR
        );

        #endregion

        static int Main(string[] args)
        {
            if (args.FirstOrDefault(a => a.ToUpper() == "-WAIT") != null)
            {
                Console.WriteLine("Running in wait mode - now you can attach to the process with a debugger.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }

            string packagePath;
            string targetDirectory;
            int phase = -1;
            string logFilePath;
            LogLevel logLevel;
            bool help;
            bool wait;
            string[] parameters;

            if (!ParseParameters(args, out packagePath, out targetDirectory, out phase, out parameters, out logFilePath, out logLevel, out help, out wait))
                return -1;
            if (!CheckTargetDirectory(targetDirectory))
                return -1;

            if (!CheckPackage(ref packagePath))
                return -1;

            Logger.PackageName = Path.GetFileName(packagePath);

            Logger.Create(logLevel, logFilePath);
            Debug.WriteLine("##> " + Logger.Level);

            if (phase < 0)
                return ExecuteGlobal(packagePath, targetDirectory, parameters, help, wait);
            return ExecutePhase(packagePath, targetDirectory, phase, parameters, logFilePath, help);
        }
        private static bool ParseParameters(string[] args, out string packagePath, out string targetDirectory, out int phase, out string[] parameters, out string logFilePath, out LogLevel logLevel, out bool help, out bool wait)
        {
            packagePath = null;
            targetDirectory = null;
            phase = -1;
            logFilePath = null;
            wait = false;
            help = false;
            logLevel = LogLevel.Default;
            var prms = new List<string>();

            foreach (var arg in args)
            {
                if (SenseNet.Packaging.StepParameter.IsValidParameter(arg))
                {
                    prms.Add(arg);
                    continue;
                }

                if (arg.StartsWith("-"))
                {
                    var verb = arg.Substring(1).ToUpper();
                    switch (verb)
                    {
                        case "?": help = true; break;
                        case "HELP": help = true; break;
                        case "WAIT": wait = true; break;
                    }
                }
                else if (arg.StartsWith("PHASE:", StringComparison.OrdinalIgnoreCase))
                {
                    phase = int.Parse(arg.Substring(6));
                }
                else if (arg.StartsWith("LOG:", StringComparison.OrdinalIgnoreCase))
                {
                    logFilePath = arg.Substring(4);
                }
                else if (arg.StartsWith("LOGLEVEL:", StringComparison.OrdinalIgnoreCase))
                {
                    logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), arg.Substring(9));
                }
                else if (packagePath == null)
                {
                    packagePath = arg;
                }
                else
                {
                    targetDirectory = arg;
                }
            }
            if (targetDirectory == null)
                targetDirectory = SearchTargetDirectory();
            parameters = prms.ToArray();
            return true;
        }
        private static bool CheckTargetDirectory(string targetDirectory)
        {
            if (Directory.Exists(targetDirectory))
                return true;
            PrintParameterError("Given target directory does not exist: " + targetDirectory);
            return false;
        }
        private static bool CheckPackage(ref string packagePath)
        {
            if (packagePath == null)
            {
                PrintParameterError("Missing package");
                return false;
            }

            if (!Path.IsPathRooted(packagePath))
                packagePath = Path.Combine(DefaultPackageDirectory(), packagePath);

            if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (!System.IO.File.Exists(packagePath))
                {
                    PrintParameterError("Given package file does not exist: " + packagePath);
                    return false;
                }
            }
            else
            {
                if (!Directory.Exists(packagePath))
                {
                    var packageZipPath = packagePath + ".zip";
                    if (!System.IO.File.Exists(packageZipPath))
                    {
                        PrintParameterError("Given package zip file or directory does not exist: " + packagePath);
                        return false;
                    }
                    else
                    {
                        packagePath = packageZipPath;
                    }
                }
            }
            return true;
        }
        private static void PrintParameterError(string message)
        {
            Console.WriteLine(ToolTitle);
            Console.WriteLine(message);
            Console.WriteLine(UsageScreen);
            Console.WriteLine("Aborted.");
        }

        private static int ExecuteGlobal(string packagePath, string targetDirectory, string[] parameters, bool help, bool wait)
        {
            Console.WriteLine();

            Logger.LogTitle(ToolTitle);
            Logger.LogMessage("Start at {0}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            Logger.LogMessage("Target:  " + targetDirectory);
            Logger.LogMessage("Package: " + packagePath);

            packagePath = Unpack(packagePath);

            var result = 0;
            var phase = 0;
            var errors = 0;
            while (true)
            {
                var workerExe = CreateSandbox(targetDirectory, Path.GetDirectoryName(packagePath));
                var appBasePath = Path.GetDirectoryName(workerExe);
                var workerDomain = AppDomain.CreateDomain("SnAdminWorkerDomain" + phase, null, appBasePath, null, false);

                var phaseParameter = "PHASE:" + phase.ToString();
                var logParameter = "LOG:" + Logger.GetLogFileName();
                var logLevelParameter = "LOGLEVEL:" + Logger.Level.ToString();

                var prms = new List<string> { packagePath, targetDirectory, phaseParameter, logParameter, logLevelParameter };
                prms.AddRange(parameters);
                if (help)
                    prms.Add("-HELP");
                if (wait)
                    prms.Add("-WAIT");
                 
                var processArgs =  string.Join(" ", prms);
                var startInfo = new ProcessStartInfo(workerExe, processArgs)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(workerExe),
                    CreateNoWindow = false,
                };

                Process process;
                try
                {
                    process = Process.Start(startInfo);
                    process.WaitForExit();
                    result = process.ExitCode;
                }
                catch (Exception e)
                {
                    var preEx = e as PackagePreconditionException;
                    if (preEx == null)
                        preEx = e.InnerException as PackagePreconditionException;
                    if (preEx != null)
                    {
                        Logger.LogMessage("PRECONDITION FAILED:");
                        Logger.LogMessage(preEx.Message);
                    }
                    else
                    {
                        var pkgEx = e as InvalidPackageException;
                        if (pkgEx == null)
                            pkgEx = e.InnerException as InvalidPackageException;
                        if (pkgEx != null)
                        {
                            Logger.LogMessage("INVALID PACKAGE:");
                            Logger.LogMessage(pkgEx.Message);
                        }
                        else
                        {
                            Logger.LogMessage("#### UNHANDLED EXCEPTION:");
                            Logger.LogException(e);
                        }
                    }
                    result = -1;
                }
                if (result > 0)
                {
                    errors += (result & -2) / 2;
                    result = result & 1;
                }

                if (result < 1)
                    break;

                phase++;
            }

            Logger.LogMessage("===============================================================================");
            if (result < 0)
                Logger.LogMessage("SnAdmin stopped with error.");
            else if (errors == 0)
                Logger.LogMessage("SnAdmin has been successfully finished.");
            else
                Logger.LogMessage("SnAdmin has been finished with {0} errors.", errors);

            Logger.LogMessage("Ok");
            Console.WriteLine("See log file: {0}", Logger.GetLogFileName());
            if (Debugger.IsAttached)
            {
                Console.Write("[press any key] ");
                Console.ReadKey();
                Console.WriteLine();
            }
            return result;
        }
        private static int ExecutePhase(string packagePath, string targetDirectory, int phase, string[] parameters, string logFilePath, bool help)
        {
            var sandboxPath = Path.Combine(Path.GetDirectoryName(packagePath), PackageManager.SANDBOXDIRECTORYNAME);
            var preloaded = SenseNet.ContentRepository.Storage.TypeHandler.LoadAssembliesFrom(sandboxPath);

            var packageCustomizationPath = Path.Combine(packagePath, "PackageCustomization");
            if (Directory.Exists(packageCustomizationPath))
            {
                Console.WriteLine("Loading package customizations:");
                var loaded = SenseNet.ContentRepository.Storage.TypeHandler.LoadAssembliesFrom(packageCustomizationPath);
                foreach (var item in loaded)
                {
                    Console.Write("  ");
                    Console.WriteLine(item);
                }
            }

            if (help)
            {
                LogAssemblies();
                Logger.LogMessage(Environment.NewLine + PackageManager.GetHelp());
                var sb = new StringBuilder();
                return 0;
            }

            PackagingResult result = null;
            try
            {
                result = PackageManager.Execute(packagePath, targetDirectory, phase, parameters, Console.Out);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }

            // result: 0: successful with no errors, 1: need restart, -1: error
            if (result == null || !result.Successful)
                return -1;
            if (result.NeedRestart)
                return 1 + Logger.Errors * 2;
            return Logger.Errors * 2;
        }
        private static void LogAssemblies()
        {
            Logger.LogMessage("Assemblies:");
            foreach (var asm in SenseNet.ContentRepository.Storage.TypeHandler.GetAssemblyInfo())
                Logger.LogMessage("  {0} {1}", asm.Name, asm.Version);
        }

        //private static string SearchTargetDirectory()
        //{
        //    var targetDir = ConfigurationManager.AppSettings["TargetDirectory"];
        //    if (!string.IsNullOrEmpty(targetDir))
        //        return targetDir;

        //    var workerExe = Assembly.GetExecutingAssembly().Location;
        //    var path = workerExe;
        //    while (true)
        //    {
        //        path = Path.GetDirectoryName(path);
        //        if (path == null)
        //            throw new PackagingException("Cannot execute SnAdmin from here: " + workerExe);

        //        if (System.IO.File.Exists(Path.Combine(path, "web.config")))
        //            // product scenario
        //            return path;

        //        var webSitePath = Path.Combine(path, "WebSite");
        //        if (Directory.Exists(webSitePath))
        //            if (System.IO.File.Exists(Path.Combine(webSitePath, "WebSite.csproj")))
        //                // debug scenario
        //                return webSitePath;
        //    }
        //}
        private static string SearchTargetDirectory()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDirectory"];
            if (!string.IsNullOrEmpty(targetDir))
                return targetDir;

            var workerExe = Assembly.GetExecutingAssembly().Location;
            var path = workerExe;
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);
            if (path.EndsWith("_admin", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - "_admin".Length);
                if (System.IO.File.Exists(Path.Combine(path, "web.config")))
                    return path;
            }
            throw new ApplicationException("Configure the TargetPath. This path does not exist or not a valid target: " + path);
        }
        private static string DefaultPackageDirectory()
        {
            var pkgDir = ConfigurationManager.AppSettings["PackageDirectory"];
            if (!string.IsNullOrEmpty(pkgDir))
                return pkgDir;
            var workerExe = Assembly.GetExecutingAssembly().Location;
            pkgDir = Path.GetDirectoryName(Path.GetDirectoryName(workerExe));
            return pkgDir;
        }

        private static string CreateSandbox(string targetDirectory, string packageDirectory)
        {
            var sandboxPath = EnsureEmptySandbox(packageDirectory);
            var webBinPath = Path.Combine(targetDirectory, "bin");

            // #1 copy assemblies from webBin to sandbox
            var paths = GetRelevantFiles(webBinPath);
            foreach (var filePath in paths)
                System.IO.File.Copy(filePath, Path.Combine(sandboxPath, Path.GetFileName(filePath)));

            // #2 copy missing files from running directory
            var runningDir = AppDomain.CurrentDomain.BaseDirectory;
            var runningPaths = GetRelevantFiles(runningDir);
            var missingNames = runningPaths.Select(p => Path.GetFileName(p))
                .Except(paths.Select(q => Path.GetFileName(q))).OrderBy(r => r)
                .Where(r => !r.ToLower().Contains(".vshost.exe"))
                .ToArray();
            foreach (var fileName in missingNames)
                System.IO.File.Copy(Path.Combine(runningDir, fileName), Path.Combine(sandboxPath, fileName));


            // #x return with path of the worker exe
            return Path.Combine(sandboxPath, Path.GetFileName(Assembly.GetExecutingAssembly().Location));
        }
        private static string[] _relevantExtensions = ".dll;.exe;.pdb;.config".Split(';');
        private static string[] GetRelevantFiles(string dir)
        {
            return Directory.EnumerateFiles(dir, "*.*").Where(p => _relevantExtensions.Contains(Path.GetExtension(p).ToLower())).ToArray();
        }
        private static string EnsureEmptySandbox(string packagesDirectory)
        {
            var sandboxFolder = Path.Combine(packagesDirectory, PackageManager.SANDBOXDIRECTORYNAME);
            if (!Directory.Exists(sandboxFolder))
                Directory.CreateDirectory(sandboxFolder);
            else
                DeleteAllFrom(sandboxFolder);
            return sandboxFolder;
        }
        private static void DeleteAllFrom(string sandboxFolder)
        {
            var sandboxInfo = new DirectoryInfo(sandboxFolder);
            foreach (FileInfo file in sandboxInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in sandboxInfo.GetDirectories())
                dir.Delete(true);
        }

        private static string Unpack(string package)
        {
            if (Directory.Exists(package))
                return package;

            var pkgFolder = Path.GetDirectoryName(package);
            var zipTarget = Path.Combine(pkgFolder, Path.GetFileNameWithoutExtension(package));

            Logger.LogMessage("Package directory: " + zipTarget);

            if (Directory.Exists(zipTarget))
            {
                DeleteAllFrom(zipTarget);
                Logger.LogMessage("Old files and directories are deleted.");
            }
            else
            {
                Directory.CreateDirectory(zipTarget);
                Logger.LogMessage("Package directory created.");
            }

            Logger.LogMessage("Extracting ...");
            using (ZipFile zip = ZipFile.Read(package))
            {
                foreach (var e in zip.Entries)
                    e.Extract(zipTarget);
            }
            Logger.LogMessage("Ok.");

            return zipTarget;
        }
    }
}
