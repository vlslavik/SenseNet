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
using SenseNet.Portal.UI.PortletFramework;
using System.Xml;
using System.Xml.Xsl;

namespace SenseNet.Tools.ContentImporter
{
    public class ImporterClass
    {
        private static string CR = Environment.NewLine;

        public string SchemaPath { get; set; }
        public string AsmPath { get; set; }
        public string FSPath { get; set; }
        public string RepositoryPath { get; set; }
        public string SourceFile { get; set; }
        public bool Continuing { get; set; }
        public string TransformerPath { get; set; }
        public bool Validate { get; set; }
        public bool HasReference { get; set; }

        public string RefLogFilePath { get; set; }
        public string ErrorLogFilePath { get; set; }
        public string LogFilePath { get { return _logFilePath; } }

        private string _continueFrom;
        public string ContinueFrom
        {
            set { _continueFrom = value; }
            get { return _continueFrom; }
        }
        private int _exceptions;

        public int Exceptions
        {
            get { return _exceptions; }
            set { _exceptions = value; }
        }

        TransformerContext _xsltOptions = null;
        TransformerContext XsltOptions
        {
            get
            {
                if (_xsltOptions == null)
                    _xsltOptions = new TransformerContext(this.TransformerPath);
                return _xsltOptions;
            }
        }

        internal void Run(string schemaPath, string asmPath, string fsPath, string repositoryPath, bool validate)
        {
            string ctdPath = null;
            string aspectsPath = null;

            if (schemaPath != null)
            {
                ctdPath = Directory.GetDirectories(schemaPath, "ContentTypes").FirstOrDefault();
                aspectsPath = Directory.GetDirectories(schemaPath, "Aspects").FirstOrDefault();
            }

            if (ctdPath == null && aspectsPath == null && String.IsNullOrEmpty(fsPath) && String.IsNullOrEmpty(SourceFile))
            {
                LogWriteLine("No changes");
                return;
            }

            var startSettings = new RepositoryStartSettings
            {
                Console = Console.Out,
                StartLuceneManager = StorageContext.Search.IsOuterEngineEnabled,
                PluginsPath = asmPath
            };
            using (Repository.Start(startSettings))
            {
                var installationMode = false;
                try
                {
                    installationMode = IsInstallationMode();
                }
                catch (Exception e)
                {
                    PrintException(e, null);
                    return;
                }

                ApplicationInfo.CreateInitialSenseNetVersion("Sense/Net ECM", "Unofficial", new Version(6, 3, 1),
                    "This edition can only be used for development and testing purposes.");

                // Elevation: there can be folders where even admins
                // do not have any permissions. This is why we need to
                // use system account for the whole import process.
                using (new SystemAccount())
                {
                    //-- Install ContentTypes
                    if (ctdPath == null && aspectsPath == null)
                    {
                        LogWriteLine("Schema is not changed");
                    }
                    else
                    {
                        if (installationMode)
                        {
                            StorageContext.Search.DisableOuterEngine();
                            LogWriteLine("Indexing is temporarily switched off." + CR);
                        }

                        InstallContentTypeDefinitionsAndAspects(ctdPath, aspectsPath);

                        if (installationMode)
                        {
                            StorageContext.Search.EnableOuterEngine();
                            CreateInitialIndex();
                            LogWriteLine("Indexing is switched on." + CR);
                        }
                    }

                    //-- Create missing index documents
                    var firstImport = SaveInitialIndexDocuments();
                    if (firstImport)
                    {
                        var admin = Node.Load<User>(User.Administrator.Id);
                        var admins = Node.Load<Group>(Group.Administrators.Id);
                        var operators = Node.Load<Group>(RepositoryConfiguration.OperatorsGroupPath);

                        admins.AddMember(admin);
                        admins.Save();

                        operators.AddMember(admins);
                        operators.Save();
                    }

                    //-- Import Contents
                    if (!String.IsNullOrEmpty(fsPath) || !String.IsNullOrEmpty(SourceFile))
                        ImportContents(fsPath, repositoryPath, validate, false);
                    else
                        LogWriteLine("Contents are not changed");
                }
            }
        }
        private bool SaveInitialIndexDocuments()
        {
            LogWriteLine("Create initial index documents.");
            var idSet = DataProvider.LoadIdsOfNodesThatDoNotHaveIndexDocument();
            var nodes = Node.LoadNodes(idSet);
            var count = 0;
            foreach (var node in nodes)
            {
                bool hasBinary;
                DataBackingStore.SaveIndexDocument(node, false, out hasBinary);
                LogWriteLine("    ", node.Path);
                count++;
            }
            LogWriteLine("Ok.");
            return count > 0;
        }

        private void LoadAssemblies()
        {
            LoadAssemblies(AppDomain.CurrentDomain.BaseDirectory);
        }
        public void LoadAssemblies(string localBin)
        {
            LogWrite("Loading Assemblies from ");
            LogWrite(localBin);
            LogWriteLine(":");

            string[] names = TypeHandler.LoadAssembliesFrom(localBin);
            foreach (string name in names)
                LogWriteLine(name);

            LogWriteLine("Ok.");
            LogWriteLine();
        }
        private bool IsInstallationMode()
        {
            LogWriteLine("========================================");
            LogWriteLine("Indexing: " + (StorageContext.Search.IsOuterEngineEnabled ? "enabled" : "disabled"));

            var startupMode = false;
            if (StorageContext.Search.IsOuterEngineEnabled)
            {
                try
                {
                    startupMode = ContentQuery.Query("Type:PortalRoot").Count == 0;
                }
                catch (Exception e)
                {
                    var s = e.Message;
                }
                LogWriteLine("Startup mode: " + (startupMode ? "ON" : "off"));
                LogWriteLine("========================================");
            }
            return startupMode;
        }
        private void CreateInitialIndex()
        {
            LogWriteLine("========================================");
            LogWriteLine("Create initial index.");
            LogWriteLine();
            var p = StorageContext.Search.SearchEngine.GetPopulator();
            p.NodeIndexed += new EventHandler<NodeIndexedEvenArgs>(Populator_NodeIndexed);
            p.ClearAndPopulateAll();
            LogWriteLine("Ok.");
            LogWriteLine("========================================");
        }

        public void InstallContentTypeDefinitionsAndAspects(string ctdPath, string aspectsPath)
        {
            LogWriteLine("Loading content types...");

            if (ctdPath != null)
            {
                ContentTypeInstaller installer = ContentTypeInstaller.CreateBatchContentTypeInstaller();
                foreach (string ctdFilePath in Directory.GetFiles(ctdPath, "*.xml"))
                {
                    LogWrite(Path.GetFileName(ctdFilePath));
                    LogWrite(" ...");

                    using (var stream = new FileStream(ctdFilePath, FileMode.Open, FileAccess.Read))
                    {
                        try
                        {
                            installer.AddContentType(stream);
                        }
                        catch (ApplicationException e)
                        {
                            _exceptions++;
                            LogWriteLine(" SKIPPED: " + e.Message);
                        }
                    }

                    LogWriteLine(" Ok");
                }

                LogWriteLine();
                LogWrite("Installing content types...");
                installer.ExecuteBatch();

                LogWriteLine(" Ok");
                LogWriteLine();
            }
            else
            {
                LogWriteLine("CTDs not changed");
            }

            //==============================================================

            LogWriteLine("---------------------------------------");
            LogWriteLine();

            if (aspectsPath != null)
            {
                if (!Node.Exists(Repository.AspectsFolderPath))
                {
                    LogWrite("Creating aspect container (", Repository.AspectsFolderPath, ")...");
                    Content.CreateNew(typeof(SystemFolder).Name, Repository.SchemaFolder, "Aspects").Save();
                    LogWriteLine("Ok");
                }

                LogWriteLine("Installing aspects...");

                ImportContents(aspectsPath, Repository.AspectsFolderPath, true, true);

                LogWriteLine("Ok");
            }
            else
            {
                LogWriteLine("Aspects not changed.");
            }
            LogWriteLine("=======================================");
            LogWriteLine();
            LogWrite("Schema import finished");
            LogWriteLine();

            //==============================================================
        }

        //-- ImportContents
        public void ImportContents(string srcPath, string targetPath, bool validate, bool aspects)
        {
            bool pathIsFile = false;
            if (IO.File.Exists(srcPath))
            {
                pathIsFile = true;
            }
            else if (!Directory.Exists(srcPath) && !IO.File.Exists(SourceFile))
            {
                LogWrite("Source directory or file was not found: ");
                LogWriteLine(srcPath);
                return;
            }

            if (!string.IsNullOrWhiteSpace(this.TransformerPath))
            {
                LogWrite("Xslt: " + this.TransformerPath + "... ");
                if (XsltOptions != null && XsltOptions.IsValid)
                    LogWrite("Ok");
                LogWriteLine("");
            }

            var importTarget = Repository.Root as Node;
            if (!aspects)
            {
                LogWriteLine();
                LogWriteLine("=================== Continuing Import ========================");
                if (!string.IsNullOrEmpty(srcPath))
                    LogWriteLine("From: ", srcPath);
                if (!string.IsNullOrEmpty(SourceFile))
                    LogWriteLine("File Source: ", SourceFile);
                LogWriteLine("To:   ", targetPath);
                if (_continueFrom != null)
                    LogWriteLine("Continuing from: ", _continueFrom);
                if (!validate)
                    LogWriteLine("Content validation: OFF");
                LogWriteLine("==============================================================");
            }

            if (targetPath != null)
            {
                importTarget = Node.LoadNode(targetPath);
                if (importTarget == null)
                {
                    LogWrite("Target container was not found: ");
                    LogWriteLine(targetPath);
                    return;
                }
            }

            try
            {
                HasReference = false;

                if (!string.IsNullOrEmpty(SourceFile))
                {
                    var files = IO.File.ReadLines(SourceFile).ToArray();

                    foreach (var item in files)
                    {
                        string[] contentData = item.ToLower().Split(';');
                        bool isFile = IO.File.Exists(contentData[0]);

                        string parentPath = string.Empty;
                        if (contentData.Length > 1)
                        {
                            parentPath = contentData[1];
                        }
                        else if (!string.IsNullOrEmpty(srcPath))
                        {
                            string target = contentData[0].Replace(srcPath.ToLower(), RepositoryPath).Replace('\\', '/');
                            parentPath = SenseNet.ContentRepository.Storage.RepositoryPath.GetParentPath(target);
                        }

                        Node folder = Node.LoadNode(parentPath);
                        TreeWalker(item, isFile, folder, "  ", validate, aspects);
                    }
                }
                else if (!string.IsNullOrEmpty(srcPath))
                    TreeWalker(srcPath, pathIsFile, importTarget, "  ", validate, aspects);

                if (HasReference)
                    UpdateReferences(validate);
            }
            catch (Exception e)
            {
                PrintException(e, null);
            }
        }

        private void TreeWalker(string path, bool pathIsFile, Node folder, string indent, bool validate, bool aspects)
        {
            // get entries
            // get contents
            // foreach contents
            //   create contentinfo
            //   entries.remove(content)
            //   entries.remove(contentinfo.attachments)
            // foreach entries
            //   create contentinfo
            if (!aspects)
            {
                if (folder != null && (
                    String.Compare(folder.Path, Repository.AspectsFolderPath) == 0 ||
                    String.Compare(folder.Path, Repository.ContentTypesFolderPath) == 0))
                {
                    LogWrite("Skipped path: ");
                    LogWriteLine(path);
                    return;
                }
            }

            string currentDir = pathIsFile ? Path.GetDirectoryName(path) : path;
            List<ContentInfo> contentInfos = new List<ContentInfo>();
            List<string> paths;
            List<string> contentPaths;
            if (pathIsFile)
            {
                paths = new List<string>(new string[] { path });
                contentPaths = new List<string>();
                if (path.ToLower().EndsWith(".content"))
                    contentPaths.Add(path);
            }
            else
            {
                paths = new List<string>(Directory.GetFileSystemEntries(path));
                contentPaths = new List<string>(Directory.GetFiles(path, "*.content"));
            }

            foreach (string contentPath in contentPaths)
            {
                paths.Remove(contentPath);

                try
                {
                    var contentInfo = new ContentInfo(contentPath, folder, XsltOptions);
                    if (!contentInfo.FileIsHidden)
                    {
                        if (!Continuing || _continueFrom.StartsWith(path.ToLower()))
                            contentInfos.Add(contentInfo);
                    }
                    foreach (string attachmentName in contentInfo.Attachments)
                    {
                        var attachmentPath = Path.Combine(path, attachmentName);
                        RemovePath(paths, attachmentPath);

                        if (attachmentName == contentInfo.Name)
                        {
                            // Escaped children folder
                            var childrenPath = attachmentPath + ".Children";
                            if (Directory.Exists(childrenPath))
                            {
                                contentInfo.ChildrenFolder = childrenPath;
                                RemovePath(paths, childrenPath);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PrintException(e, contentPath);
                    LogWriteError(contentPath + ";" + folder.Path);
                }
            }
            while (paths.Count > 0)
            {
                try
                {
                    var contentInfo = new ContentInfo(paths[0], folder, XsltOptions);
                    if (!contentInfo.FileIsHidden)
                        if (!Continuing || _continueFrom.StartsWith(path.ToLower()))
                            contentInfos.Add(contentInfo);
                }
                catch (Exception e)
                {
                    PrintException(e, paths[0]);
                    LogWriteError(paths[0] + ";" + folder.Path);
                }

                paths.RemoveAt(0);
            }

            foreach (ContentInfo contentInfo in contentInfos)
            {
                var stepDown = true;

                if (_continueFrom != null)
                {
                    Continuing = true;
                    if (contentInfo.MetaDataPath == _continueFrom)
                    {
                        _continueFrom = null;
                        Continuing = false;
                    }
                    else
                    {
                        stepDown = _continueFrom.StartsWith(contentInfo.MetaDataPath);
                    }
                }

                var isNewContent = true;
                Content content = null;

                if (!Continuing)
                {
                    try
                    {
                        string mdp = contentInfo.MetaDataPath.Replace(FSPath, RepositoryPath).Replace('\\', '/');
                        string parentPath = SenseNet.ContentRepository.Storage.RepositoryPath.GetParentPath(mdp);
                        if (contentInfo.Delete)
                        {
                            var rpath = SenseNet.ContentRepository.Storage.RepositoryPath.Combine(parentPath, contentInfo.Name);
                            if (Node.Exists(rpath))
                            {
                                LogWriteLine(indent, contentInfo.Name, " : [DELETE]");
                                Content.DeletePhysical(rpath);
                            }
                            else
                            {
                                LogWriteLine(indent, contentInfo.Name, " : [already deleted]");
                            }
                        }
                        else
                        {
                            if (folder == null)
                            {
                                folder = Node.LoadNode(parentPath);
                            }
                            content = CreateOrLoadContent(contentInfo, folder, out isNewContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintException(ex, contentInfo.MetaDataPath);
                        LogWriteError(contentInfo.MetaDataPath + ";" + folder.Path);
                    }
                }

                if (!Continuing && content != null)
                {
                    LogWriteLine(indent, contentInfo.Name, " : ", contentInfo.ContentTypeName, isNewContent ? " [new]" : " [update]");

                    try
                    {
                        if (Console.KeyAvailable)
                            WriteInfo(contentInfo);
                    }
                    catch { }

                    //-- SetMetadata without references. Continue if the setting is false or exception was thrown.
                    try
                    {
                        if (!contentInfo.SetMetadata(content, currentDir, isNewContent, validate, false))
                            PrintFieldErrors(content, contentInfo.MetaDataPath);
                        if (content.ContentHandler.Id == 0)
                            content.ContentHandler.Save();
                    }
                    catch (Exception e)
                    {
                        PrintException(e, contentInfo.MetaDataPath);
                        LogWriteError(contentInfo.MetaDataPath + ";" + folder.Path);
                        continue;
                    }

                    if (contentInfo.ClearPermissions)
                    {
                        content.ContentHandler.Security.RemoveExplicitEntries();
                        if (!(contentInfo.HasReference || contentInfo.HasPermissions || contentInfo.HasBreakPermissions))
                        {
                            content.ContentHandler.Security.RemoveBreakInheritance();
                        }
                    }
                    if (contentInfo.HasReference || contentInfo.HasPermissions || contentInfo.HasBreakPermissions || contentInfo.HasAspect)
                    {
                        LogWriteReference(contentInfo);
                        HasReference = true;
                    }
                }

                if (Continuing)
                {
                    LogWrite("Skipped: ");
                    LogWriteLine(contentInfo.MetaDataPath);
                }

                //-- recursion
                if (stepDown && content != null)
                {
                    Node node = null;
                    if (content != null)
                        node = content.ContentHandler;
                    if (node != null && (contentInfo.IsFolder || contentInfo.ChildrenFolder != null))//ML
                    {
                        TreeWalker(contentInfo.ChildrenFolder, false, node, indent + "  ", validate, aspects);
                    }
                }
            }
        }

        private static void RemovePath(List<string> paths, string attachmentPath)
        {
            if (!paths.Remove(attachmentPath))
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    if (paths[i].Equals(attachmentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        paths.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void UpdateReferences(bool validate)
        {
            LogWriteLine();
            LogWriteLine("=========================== Update references");
            LogWriteLine();
            using (var reader = new StreamReader(RefLogFilePath))
            {
                while (!reader.EndOfStream)
                {
                    var s = reader.ReadLine();
                    var sa = s.Split('\t');
                    var id = int.Parse(sa[0]);
                    var path = sa[1];
                    UpdateReference(id, path, validate);
                }
            }

            LogWriteLine();
        }
        private void UpdateReference(int contentId, string metadataPath, bool validate)
        {
            var contentInfo = new ContentInfo(metadataPath, null, XsltOptions);

            LogWrite("  ");
            LogWriteLine(contentId + "\t" + contentInfo.Name);
            SNC.Content content = SNC.Content.Load(contentId);
            if (content != null)
            {
                try
                {
                    if (!contentInfo.UpdateReferences(content, validate))
                        PrintFieldErrors(content, contentInfo.MetaDataPath);
                }
                catch (Exception e)
                {
                    PrintException(e, contentInfo.MetaDataPath);
                }
            }
            else
            {
                LogWrite("---------- Content does not exist. MetaDataPath: ");
                LogWrite(contentInfo.MetaDataPath);
                LogWrite(", ContentId: ");
                LogWrite(contentInfo.ContentId);
                LogWrite(", ContentTypeName: ");
                LogWrite(contentInfo.ContentTypeName);
            }
        }
        //private static void UpdateReferences(List<ContentInfo> postponedList)
        //{
        //    LogWriteLine();
        //    LogWriteLine("=========================== Update references");
        //    LogWriteLine();

        //    foreach (ContentInfo contentInfo in postponedList)
        //    {
        //        LogWrite("  ");
        //        LogWriteLine(contentInfo.Name);
        //        SNC.Content content = SNC.Content.Load(contentInfo.ContentId);
        //        if (content != null)
        //        {
        //            try
        //            {
        //                if (!contentInfo.UpdateReferences(content))
        //                    PrintFieldErrors(content, contentInfo.MetaDataPath);
        //            }
        //            catch (Exception e)
        //            {
        //                PrintException(e, contentInfo.MetaDataPath);
        //            }
        //        }
        //        else
        //        {
        //            LogWrite("---------- Content does not exist. MetaDataPath: ");
        //            LogWrite(contentInfo.MetaDataPath);
        //            LogWrite(", ContentId: ");
        //            LogWrite(contentInfo.ContentId);
        //            LogWrite(", ContentTypeName: ");
        //            LogWrite(contentInfo.ContentTypeName);
        //        }
        //    }
        //    LogWriteLine();
        //}
        private Content CreateOrLoadContent(ContentInfo contentInfo, Node folder, out bool isNewContent)
        {
            var path = SenseNet.ContentRepository.Storage.RepositoryPath.Combine(folder.Path, contentInfo.Name);
            var content = Content.Load(path);

            if (content != null && !contentInfo.ContentTypeIsInferredFolder && content.ContentType.Name != contentInfo.ContentTypeName)
            {
                throw new Exception(string.Format("Content {0} already exists but with a different type. Expected type: {1}, actual type: {2}.", content.Name, contentInfo.ContentTypeName, content.ContentType.Name));
            }

            if (content == null)
            {
                content = Content.CreateNew(contentInfo.ContentTypeName, folder, contentInfo.Name);
                isNewContent = true;
            }
            else
            {
                isNewContent = false;
            }

            return content;
        }
        public void PrintException(Exception e, string path)
        {
            _exceptions++;
            LogWriteLine("========== Exception:");
            if (!String.IsNullOrEmpty(path))
                LogWriteLine("Path: ", path);
            LogWrite(e.GetType().Name);
            LogWrite(": ");
            LogWriteLine(e.Message);
            PrintTypeLoadError(e as ReflectionTypeLoadException);
            LogWriteLine(e.StackTrace);
            while ((e = e.InnerException) != null)
            {
                LogWriteLine("---- Inner Exception:");
                LogWrite(e.GetType().Name);
                LogWrite(": ");
                LogWriteLine(e.Message);
                PrintTypeLoadError(e as ReflectionTypeLoadException);
                LogWriteLine(e.StackTrace);
            }
            LogWriteLine("=====================");
        }
        private void PrintTypeLoadError(ReflectionTypeLoadException exc)
        {
            if (exc == null)
                return;
            LogWriteLine("LoaderExceptions:");
            foreach (var e in exc.LoaderExceptions)
            {
                LogWrite("-- ");
                LogWrite(e.GetType().FullName);
                LogWrite(": ");
                LogWriteLine(e.Message);

                var fileNotFoundException = e as FileNotFoundException;
                if (fileNotFoundException != null)
                {
                    LogWriteLine("FUSION LOG:");
                    LogWriteLine(fileNotFoundException.FusionLog);
                }
            }
        }
        private void PrintFieldErrors(Content content, string path)
        {
            _exceptions++;
            LogWriteLine("---------- Field Errors (path: ", path, "):");
            foreach (string fieldName in content.Fields.Keys)
            {
                Field field = content.Fields[fieldName];
                if (!field.IsValid)
                {
                    LogWrite(field.Name);
                    LogWrite(": ");
                    LogWriteLine(field.GetValidationMessage());
                }
            }
            LogWriteLine("------------------------");
        }

        internal virtual void WriteInfo(ContentInfo contentInfo)
        {
            var x = Console.ReadKey(true);
            if (x.KeyChar != ' ')
                return;

            LogWriteLine("PAUSED: ", CR, contentInfo.MetaDataPath);
            Console.WriteLine("Errors: " + _exceptions);
            Console.Write("press any key to continue... ");
            Console.ReadKey();
            LogWriteLine("CONTINUED");
        }

        //================================================================================================================= ReferenceLog        

        internal void LogWriteReference(ContentInfo contentInfo)
        {
            using (StreamWriter writer = OpenLog(RefLogFilePath))
            {
                WriteToRefLog(writer, contentInfo.ContentId, '\t', contentInfo.MetaDataPath);
            }
        }

        public void LogWriteError(string log)
        {
            if (!IO.File.Exists(ErrorLogFilePath))
                CreateErrorLog(_continueFrom == null);

            using (StreamWriter writer = OpenLog(ErrorLogFilePath))
            {
                WriteToRefLog(writer, log);
            }
        }

        public void CreateRefLog(bool createNew)
        {
            RefLogFilePath = Path.Combine(LogFolder, "importlog_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".reflog");

            if (!IO.File.Exists(RefLogFilePath) || createNew)
            {
                using (FileStream fs = new FileStream(RefLogFilePath, FileMode.Create))
                {
                    using (StreamWriter wr = new StreamWriter(fs))
                    {
                    }
                }
            }
        }

        private void CreateErrorLog(bool createNew)
        {
            ErrorLogFilePath = Path.Combine(LogFolder, "errorlist_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".txt");
            if (!IO.File.Exists(ErrorLogFilePath) || createNew)
            {
                using (FileStream fs = new FileStream(ErrorLogFilePath, FileMode.Create))
                {
                    using (StreamWriter wr = new StreamWriter(fs))
                    {
                    }
                }
            }
        }

        private static StreamWriter OpenLog(string logFilePath)
        {
            return new StreamWriter(logFilePath, true);
        }

        private void WriteToRefLog(StreamWriter writer, params object[] values)
        {
            foreach (object value in values)
            {
                writer.Write(value);
            }
            writer.WriteLine();
        }

        //================================================================================================================= Logger

        private string _logFilePath;

        private string _logFolder = null;
        public string LogFolder
        {
            get
            {
                if (_logFolder == null)
                    _logFolder = AppDomain.CurrentDomain.BaseDirectory;
                return _logFolder;
            }
            set
            {
                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                _logFolder = value;
            }
        }

        private bool _lineStart;

        public virtual void LogWrite(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, false);
            }
            _lineStart = false;
        }
        public virtual void LogWriteLine(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, true);
            }
            _lineStart = true;
        }
        public void CreateLog(bool createNew)
        {
            _lineStart = true;
            _logFilePath = Path.Combine(LogFolder, "importlog_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".txt");
            if (!IO.File.Exists(_logFilePath) || createNew)
            {
                using (FileStream fs = new FileStream(_logFilePath, FileMode.Create))
                {
                    using (StreamWriter wr = new StreamWriter(fs))
                    {
                        wr.WriteLine("Start importing ", DateTime.UtcNow, CR, "Log file: ", _logFilePath);
                        wr.WriteLine();
                    }
                }
            }
            else
            {
                LogWriteLine(CR, CR, "CONTINUING", CR, CR);
            }
        }
        private StreamWriter OpenLog()
        {
            return new StreamWriter(_logFilePath, true);
        }
        private void WriteToLog(StreamWriter writer, object[] values, bool newLine)
        {
            if (_lineStart)
            {
                writer.Write(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
                writer.Write("\t");
            }
            foreach (object value in values)
            {
                Console.Write(value);
                writer.Write(value);
            }
            if (newLine)
            {
                Console.WriteLine();
                writer.WriteLine();
            }
        }

        //=================================================================================================================

        private void Populator_NodeIndexed(object sender, NodeIndexedEvenArgs e)
        {
            LogWriteLine(e.Path);
        }
    }
}
