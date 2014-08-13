using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using System.Reflection;
using SenseNet.Search;

namespace SenseNet.Tools.ContentExporter
{
    class Exporter
    {
        private static ExporterClass _exporter;
        public static ExporterClass ExporterInstance
        {
            get
            {
                if (_exporter == null)
                {
                    _exporter = new ExporterClass();
                    _exporter.ForbiddenFileNames = ForbiddenFileNames;
                }
                return _exporter;
            }
            set
            {
                _exporter = value;
            }
        }

        private static string CR = Environment.NewLine;
        private static string UsageScreen = String.Concat(
            //   0         1         2         3         4         5         6         7         8
            //   012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
            CR,
            "Sense/Net Content Repository Export tool Usage:", CR,
            "Export [-?] [-HELP]", CR,
            "Export [-SOURCE <source>] -TARGET <target> [-ASM <asm>]", CR,
            CR,
            "Parameters:", CR,
            "<source>: Sense/Net Content Repository path as the export root (default: /Root)", CR,
            "<target>: Directory that will contain exported contents. ", CR,
            "          Can be valid local or network filesystem path.", CR,
            "<asm>:    FileSystem folder containig the required assemblies", CR,
            "          (default: location of Export.exe)", CR
        );
        private static List<string> ForbiddenFileNames = new List<string>(new string[] { "PRN", "LST", "TTY", "CRT", "CON" });
        internal static List<string> ArgNames = new List<string>(new string[] { "SOURCE", "TARGET", "ASM", "FILTER", "WAIT" });
        internal static bool ParseParameters(string[] args, List<string> argNames, out Dictionary<string, string> parameters, out string message)
        {
            message = null;
            parameters = new Dictionary<string, string>();
            if (args.Length == 0)
                return false;

            int argIndex = -1;
            int paramIndex = -1;
            string paramToken = null;
            while (++argIndex < args.Length)
            {
                string arg = args[argIndex];
                if (arg.StartsWith("-"))
                {
                    paramToken = arg.Substring(1).ToUpper();

                    if (paramToken == "?" || paramToken == "HELP")
                        return false;

                    paramIndex = ArgNames.IndexOf(paramToken);
                    if (!argNames.Contains(paramToken))
                    {
                        message = "Unknown argument: " + arg;
                        return false;
                    }
                    parameters.Add(paramToken, null);
                }
                else
                {
                    if (paramToken != null)
                    {
                        parameters[paramToken] = arg;
                        paramToken = null;
                    }
                    else
                    {
                        message = String.Concat("Missing parameter name before '", arg, "'");
                        return false;
                    }
                }
            }
            return true;
        }
        private static void Usage(string message)
        {
            if (!String.IsNullOrEmpty(message))
            {
                Console.WriteLine("--------------------");
                Console.WriteLine(message);
                Console.WriteLine("--------------------");
            }
            Console.WriteLine(UsageScreen);
        }

        static void Main(string[] args)
        {
            Dictionary<string, string> parameters;
            string message;
            if (!ParseParameters(args, ArgNames, out parameters, out message))
            {
                Usage(message);
                return;
            }

            string repositoryPath = parameters.ContainsKey("SOURCE") ? parameters["SOURCE"] : "/Root";
            string asmPath = parameters.ContainsKey("ASM") ? parameters["ASM"] : null;
            string fsPath = parameters.ContainsKey("TARGET") ? parameters["TARGET"] : null;
            bool waitForAttach = parameters.ContainsKey("WAIT");
            string queryPath = parameters.ContainsKey("FILTER") ? parameters["FILTER"] : null;
            if (fsPath == null)
            {
                Usage("Missing -TARGET parameter" + CR);
                return;
            }

            try
            {
                ExporterInstance.CreateLog();
                if (waitForAttach)
                {
                    ExporterInstance.LogWriteLine("Running in wait mode - now you can attach to the process with a debugger.");
                    ExporterInstance.LogWriteLine("Press ENTER to continue.");
                    Console.ReadLine();
                }

                var startSettings = new RepositoryStartSettings
                {
                    Console = Console.Out,
                    StartLuceneManager = true,
                    StartWorkflowEngine = false
                };

                using (Repository.Start(startSettings))
                {
                    ExporterInstance.Export(repositoryPath, fsPath, queryPath);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Export did not complete successfully.");

                ExporterInstance.LogWriteLine("Export ends with error:");
                ExporterInstance.LogWriteLine(e);
                ExporterInstance.LogWriteLine(e.StackTrace);
            }
        }

    }
}
