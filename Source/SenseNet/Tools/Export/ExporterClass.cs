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
    public class ExporterClass
    {
        public List<string> ForbiddenFileNames = new List<string>(new string[] { "PRN", "LST", "TTY", "CRT", "CON" });
        private static string CR = Environment.NewLine;

        private int exceptions;
        public void Export(string repositoryPath, string fsPath, string queryPath, string queryString = null)
        {
            try
            {
                //-- check fs folder
                DirectoryInfo dirInfo = new DirectoryInfo(fsPath);
                if (!dirInfo.Exists)
                {
                    LogWrite("Creating target directory: ", fsPath, " ... ");
                    Directory.CreateDirectory(fsPath);
                    LogWriteLine("Ok");
                }
                else
                {
                    LogWriteLine("Target directory exists: ", fsPath, ". Exported contents will override existing subelements.");
                }

                //-- load export root
                Content root = Content.Load(repositoryPath);
                if (root == null)
                {
                    LogWriteLine();
                    LogWriteLine("Content does not exist: ", repositoryPath);
                }
                else
                {
                    LogWriteLine();
                    LogWriteLine("=========================== Export ===========================");
                    LogWriteLine("From: ", repositoryPath);
                    LogWriteLine("To:   ", fsPath);

                    if (queryPath != null)
                        LogWriteLine("Filter: ", queryPath);
                    LogWriteLine("==============================================================");

                    var context = new ExportContext(repositoryPath, fsPath);

                    if (queryString != null)
                        ExportByFilterText(root, context, fsPath, queryString);
                    else if (queryPath != null)
                        ExportByFilter(root, context, fsPath, queryPath);
                    else
                        ExportContentTree(root, context, fsPath, "");

                    LogWriteLine("--------------------------------------------------------------");
                    LogWriteLine("Outer references:");
                    var outerRefs = context.GetOuterReferences();
                    if (outerRefs.Count == 0)
                        LogWriteLine("All references are exported.");
                    else
                        foreach (var item in outerRefs)
                            LogWriteLine(item);
                }
            }
            catch (Exception e)
            {
                PrintException(e, fsPath);
            }

            LogWriteLine("==============================================================");
            if (exceptions == 0)
                LogWriteLine("Export is successfully finished.");
            else
                LogWriteLine("Export is finished with ", exceptions, " errors.");
            LogWriteLine("Read log file: ", _logFilePath);
        }

        private void ExportContentTree(Content content, ExportContext context, string fsPath, string indent)
        {
            try
            {
                ExportContent(content, context, fsPath, indent);
            }
            catch (Exception ex)
            {
                var path = content == null ? fsPath : content.Path;
                PrintException(ex, path);
                return;
            }

            //TODO: SmartFolder may contain real items too
            if (content.ContentHandler is SmartFolder)
                return;

            // create folder only if it has children
            var contentAsFolder = content.ContentHandler as IFolder;
            var contentAsGeneric = content.ContentHandler as GenericContent;

            //try everything that can have children (generic content, content types or other non-gc nodes)
            if (contentAsFolder == null && contentAsGeneric == null)
                return;

            try
            {
                var settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled };
                var queryResult = contentAsFolder == null ? contentAsGeneric.GetChildren(settings) : contentAsFolder.GetChildren(settings);
                if (queryResult.Count == 0)
                    return;

                var children = queryResult.Nodes;
                //var newDir = Path.Combine(fsPath, GetSafeFileNameFromContentName(content.Name));
                var fileName = GetSafeFileNameFromContentName(content.Name);
                var newDir = Path.Combine(fsPath, fileName);
                if (System.IO.File.Exists(newDir))
                    newDir = Path.Combine(fsPath, fileName + ".Children");

                if (!(content.ContentHandler is ContentType))
                    Directory.CreateDirectory(newDir);

                var newIndent = indent + "  ";
                foreach (var childContent in from node in children select Content.Create(node))
                    ExportContentTree(childContent, context, newDir, newIndent);
            }
            catch (Exception ex)
            {
                PrintException(ex, fsPath);
            }
        }
        private void ExportContent(Content content, ExportContext context, string fsPath, string indent)
        {
            if (content.ContentHandler is ContentType)
            {
                LogWriteLine(indent, content.Name);
                ExportContentType(content, context, indent);
                return;
            }
            context.CurrentDirectory = fsPath;
            LogWriteLine(indent, content.Name);
            string metaFilePath = Path.Combine(fsPath, content.Name + ".Content");
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.IndentChars = "  ";
            XmlWriter writer = null;
            try
            {
                writer = XmlWriter.Create(metaFilePath, settings);

                //<?xml version="1.0" encoding="utf-8"?>
                //<ContentMetaData>
                //    <ContentType>Site</ContentType>
                //    <Fields>
                //        ...
                writer.WriteStartDocument();
                writer.WriteStartElement("ContentMetaData");
                writer.WriteElementString("ContentType", content.ContentType.Name);
                writer.WriteElementString("ContentName", content.Name);
                writer.WriteStartElement("Fields");
                try
                {
                    content.ExportFieldData(writer, context);
                }
                catch (Exception e)
                {
                    PrintException(e, fsPath);
                    writer.WriteComment(String.Concat("EXPORT ERROR", CR, e.Message, CR, e.StackTrace));
                }
                writer.WriteEndElement();
                writer.WriteStartElement("Permissions");
                writer.WriteElementString("Clear", null);
                content.ContentHandler.Security.ExportPermissions(writer);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
            }
        }
        private string GetSafeFileNameFromContentName(string name)
        {
            if (ForbiddenFileNames.Contains(name.ToUpper()))
                return name + "!";
            return name;
        }
        private void ExportByFilter(Content root, ExportContext context, string fsRoot, string queryPath)
        {
            string queryText = null;
            using (var reader = new StreamReader(queryPath))
            {
                queryText = reader.ReadToEnd();
            }
            ExportByFilterText(root, context, fsRoot, queryText);
        }
        private void ExportByFilterText(Content root, ExportContext context, string fsRoot, string queryText)
        {
            var query = ContentQuery.CreateQuery(queryText);
            var result = query.Execute();
            var maxCount = result.Count;
            var count = 0;
            foreach (var nodeId in result.Identifiers)
            {
                string fsPath = null;
                Content content = null;

                try
                {
                    content = Content.Load(nodeId);
                    var relPath = content.Path.Remove(0, 1).Replace("/", "\\");
                    fsPath = Path.Combine(fsRoot, relPath);
                    var fsDir = Path.GetDirectoryName(fsPath);
                    var dirInfo = new DirectoryInfo(fsDir);
                    if (!dirInfo.Exists)
                        Directory.CreateDirectory(fsDir);

                    ExportContent(content, context, fsDir, String.Concat(++count, "/", maxCount, ": ", relPath, "\\"));
                }
                catch (Exception ex)
                {
                    PrintException(ex, content == null ? fsPath : content.Path);
                }
            }
        }

        private void ExportContentType(Content content, ExportContext context, string indent)
        {
            BinaryData binaryData = ((ContentType)content.ContentHandler).Binary;

            var name = content.Name + "Ctd.xml";
            var fsPath = Path.Combine(context.ContentTypeDirectory, name);

            Stream source = null;
            FileStream target = null;
            try
            {
                source = binaryData.GetStream();
                target = new FileStream(fsPath, FileMode.Create);
                for (var i = 0; i < source.Length; i++)
                    target.WriteByte((byte)source.ReadByte());
            }
            finally
            {
                if (source != null)
                    source.Close();
                if (target != null)
                {
                    target.Flush();
                    target.Close();
                }
            }
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
            StreamWriter writer = OpenLog();
            WriteToLog(writer, values, false);
            CloseLog(writer);
            _lineStart = false;
        }
        public virtual void LogWriteLine(params object[] values)
        {
            StreamWriter writer = OpenLog();
            WriteToLog(writer, values, true);
            CloseLog(writer);
            _lineStart = true;
        }
        public virtual void CreateLog()
        {
            _logFilePath = Path.Combine(LogFolder, "exportlog.txt");
            FileStream fs = new FileStream(_logFilePath, FileMode.Create);
            StreamWriter wr = new StreamWriter(fs);
            wr.WriteLine("Start exporting ", DateTime.UtcNow, Environment.NewLine, "Log file: ", _logFilePath);
            wr.WriteLine();
            wr.Close();
            _lineStart = true;
        }
        protected virtual StreamWriter OpenLog()
        {
            return new StreamWriter(_logFilePath, true);
        }
        protected virtual void WriteToLog(StreamWriter writer, object[] values, bool newLine)
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
        protected virtual void CloseLog(StreamWriter writer)
        {
            writer.Flush();
            writer.Close();
        }

        protected virtual void PrintException(Exception e, string path)
        {
            exceptions++;
            LogWriteLine("========== Exception:");
            if (!String.IsNullOrEmpty(path))
            {
                LogWriteLine("Path: ", path);
                LogWriteLine("---------------------");
            }

            WriteEx(e);
            while ((e = e.InnerException) != null)
            {
                LogWriteLine("---- Inner Exception:");
                WriteEx(e);
            }
            LogWriteLine("=====================");
        }
        protected virtual void WriteEx(Exception e)
        {
            LogWrite(e.GetType().Name);
            LogWrite(": ");
            LogWriteLine(e.Message);
            LogWriteLine(e.StackTrace);
            var ex = e as ReflectionTypeLoadException;
            if (ex != null)
                PrintReflectionTypeLoadException(ex);
        }
        protected virtual void PrintReflectionTypeLoadException(ReflectionTypeLoadException e)
        {
            LogWriteLine("---- LoaderExceptions:");
            var count = 1;
            foreach (var ex in e.LoaderExceptions)
            {
                LogWriteLine("---- LoaderException #" + count++);
                LogWrite(ex.GetType().Name);
                LogWrite(": ");
                LogWriteLine(ex.Message);
                LogWriteLine(ex.StackTrace);
            }
            LogWriteLine("---- LoaderException Types:");
            foreach (var type in e.Types)
            {
                LogWriteLine(type);
            }
        }

    }
}
