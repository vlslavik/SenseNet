using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using IO = System.IO;
using SNC = SenseNet.ContentRepository;
using SenseNet.Portal;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Tools.ContentImporter
{
    static class Importer
    {
        #region Usage screen
        private static string UsageScreen = String.Concat(
        //  0         1         2         3         4         5         6         7         |
        //  01234567890123456789012345678901234567890123456789012345678901234567890123456789|
            "Sense/Net Content Repository Import tool Usage:", CR,
            "Import [-?] [-HELP]", CR,
            "Import [-SCHEMA <schema>] [-SOURCE <source> [-TARGET <target>]] [-ASM <asm>]", CR,
            "       [-TRANSFORM <xsltpath>] [-CONTINUEFROM <continuepath>] [-NOVALIDATE]", CR,
            "", CR,
            "Parameters:", CR,
            "<schema>:       The filesystem path of the directory that contains Content", CR,
            "                Type Definitions and Aspects.", CR,
            "<source>:       The filesystem path of the file or directory that contains", CR,
            "                content to import.", CR,
            "<target>:       Sense/Net Content Repository path (folder, site, workspace,", CR,
            "                etc) as the import target (default: /Root).", CR,
            "<asm>:          The filesystem path of the directory that contains the", CR,
            "                required assemblies (default: location of Import.exe).", CR,
            "<continuepath>: The filesystem path of the file or directory as the restart", CR,
            "                point of the <source> tree.", CR,
            "<xsltpath>:     The filesystem path of the xslt file to transform content", CR,
            "                xml. For migration purposes.", CR,
            "NOVALIDATE:     Disables content validaton.", CR,
            "", CR,
            "Comments:", CR,
            "The <schema>, <source> and <asm> paths can be valid local or network", CR,
            "    filesystem paths.", CR,
            "Schema elements (content type definitions and aspects) will be imported before", CR,
            "    any other contents if the -SCHEMA parameter is presented. During content", CR,
            "    import schema elements will be skipped even if <schema> is contained by", CR,
            "    <source>.", CR,
            CR
        );
        #endregion

        private static string CR = Environment.NewLine;
        internal static List<string> ArgNames = new List<string>(new string[] { "SCHEMA", "SOURCE", "TARGET", "ASM", "CONTINUEFROM", "NOVALIDATE", "WAIT", "TRANSFORM" });

        static void Main(string[] args)
        {
            Dictionary<string, string> parameters;
            string message;
            if (!ParseParameters(args, ArgNames, out parameters, out message))
            {
                Usage(message);
                return;
            }

            var importerInstance = new ImporterClass();
            importerInstance.SchemaPath = parameters.ContainsKey("SCHEMA") ? parameters["SCHEMA"] : null;
            importerInstance.AsmPath = parameters.ContainsKey("ASM") ? parameters["ASM"] : null;
            importerInstance.FSPath = parameters.ContainsKey("SOURCE") ? parameters["SOURCE"] : null;
            importerInstance.RepositoryPath = parameters.ContainsKey("TARGET") ? parameters["TARGET"] : "/Root";
            importerInstance.ContinueFrom = parameters.ContainsKey("CONTINUEFROM") ? parameters["CONTINUEFROM"] : null;
            importerInstance.Continuing = !string.IsNullOrEmpty(importerInstance.ContinueFrom);
            importerInstance.SourceFile = parameters.ContainsKey("SOURCEFILE") ? parameters["SOURCEFILE"] : null;
            importerInstance.TransformerPath = parameters.ContainsKey("TRANSFORM") ? parameters["TRANSFORM"] : null;

            bool validate = !parameters.ContainsKey("NOVALIDATE");
            bool waitForAttach = parameters.ContainsKey("WAIT");

            //-- Path existence checks
            StringBuilder errorSb = new StringBuilder();
            if (importerInstance.SchemaPath != null && !Directory.Exists(importerInstance.SchemaPath) && !IO.File.Exists(importerInstance.SchemaPath))
                errorSb.Append("Path does not exist: -SCHEMA \"").Append(importerInstance.SchemaPath).Append("\"").Append(CR);
            if (importerInstance.FSPath != null && !Directory.Exists(importerInstance.FSPath) && !IO.File.Exists(importerInstance.FSPath))
                errorSb.Append("Path does not exist: -SOURCE \"").Append(importerInstance.FSPath).Append("\"").Append(CR);
            if (importerInstance.AsmPath != null && !Directory.Exists(importerInstance.AsmPath) && !IO.File.Exists(importerInstance.AsmPath))
                errorSb.Append("Path does not exist: -ASM \"").Append(importerInstance.AsmPath).Append("\"").Append(CR);
            if (importerInstance.ContinueFrom != null && !Directory.Exists(importerInstance.ContinueFrom) && !IO.File.Exists(importerInstance.ContinueFrom))
                errorSb.Append("Path does not exist: -CONTINUEFROM \"").Append(importerInstance.ContinueFrom).Append("\"").Append(CR);
            if (importerInstance.TransformerPath != null && !IO.File.Exists(importerInstance.TransformerPath))
                errorSb.Append("File does not exist: -TRANSFORM \"").Append(importerInstance.TransformerPath).Append("\"").Append(CR);
            if (errorSb.Length > 0)
            {
                Usage(errorSb.ToString());
                return;
            }

            try
            {
                if (waitForAttach)
                {
                    Console.WriteLine("Running in wait mode - now you can attach to the process with a debugger.");
                    Console.WriteLine("Press ENTER to continue.");
                    Console.ReadLine();
                }

                importerInstance.CreateLog(importerInstance.ContinueFrom == null);
                importerInstance.CreateRefLog(importerInstance.ContinueFrom == null);

                importerInstance.Run(importerInstance.SchemaPath, importerInstance.AsmPath, importerInstance.FSPath, importerInstance.RepositoryPath, validate);
            }
            catch (Exception e)
            {
                importerInstance.LogWriteLine();
                importerInstance.LogWriteLine("========================================");
                importerInstance.LogWriteLine("Import ends with error:");
                importerInstance.PrintException(e, null);
            }

            importerInstance.LogWriteLine("========================================");
            if (importerInstance.Exceptions == 0)
            {
                importerInstance.LogWriteLine("Import is successfully finished.");
            }
            else
            {
                importerInstance.LogWriteLine("Import is finished with ", importerInstance.Exceptions, " errors.");
                Console.Error.WriteLine("Import did not complete successfully.");

                if (!string.IsNullOrEmpty(importerInstance.ErrorLogFilePath))
                    importerInstance.LogWriteLine("Detailed error log: ", importerInstance.ErrorLogFilePath);
            }

            importerInstance.LogWriteLine("Read log file: ", importerInstance.LogFilePath);
        }

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
    }
}
