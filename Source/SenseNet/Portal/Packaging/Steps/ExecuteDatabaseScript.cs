using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using SenseNet.ContentRepository.Storage.Data;
using System.IO;

namespace SenseNet.Packaging.Steps
{
    public class ExecuteDatabaseScript : Step
    {
        private class SqlScriptReader : IDisposable
        {
            TextReader _reader;
            public string Script { get; private set; }
            public SqlScriptReader(TextReader reader)
            {
                _reader = reader;
            }

            public void Dispose()
            {
                this.Close();
            }
            public virtual void Close()
            {
                _reader.Close();
                GC.SuppressFinalize(this);
            }

            public bool ReadScript()
            {
                var sb = new StringBuilder();

                string line;
                while (true)
                {
                    line = _reader.ReadLine();

                    if (line == null)
                        break;

                    if (String.Equals(line, "GO", StringComparison.OrdinalIgnoreCase))
                    {
                        Script = sb.ToString();
                        return true;
                    }
                    sb.AppendLine(line);
                }

                if (sb.Length <= 0)
                    return false;

                Script = sb.ToString();
                return true;
            }
        }

        public override string ElementName { get { return "ExecuteDatabaseScript"; } }

        [DefaultProperty]
        public string Query { get; set; }

        public override void Execute(ExecutionContext context)
        {
            string queryPath = null;
            try
            {
                queryPath = ResolvePackagePath(Query, context);
                if(!File.Exists(queryPath))
                    queryPath = null;
            }
            catch
            {
                queryPath = null;
            }

            if (queryPath != null)
                ExecuteFromFile(queryPath, context);
            else
                ExecuteFromText(Query, context);
        }
        private void ExecuteFromFile(string path, ExecutionContext context)
        {
            using (var reader = new StreamReader(path))
            using (var sqlReader = new SqlScriptReader(reader))
                ExecuteSql(sqlReader, context);
        }
        private void ExecuteFromText(string text, ExecutionContext context)
        {
            using (var reader = new StringReader(text))
            using (var sqlReader = new SqlScriptReader(reader))
                ExecuteSql(sqlReader, context);
        }
        private void ExecuteSql(SqlScriptReader sqlReader, ExecutionContext context)
        {
            while (sqlReader.ReadScript())
            {
                var script = sqlReader.Script;

                var sb = new StringBuilder();
                using (var proc = DataProvider.CreateDataProcedure(script))
                {
                    proc.CommandType = CommandType.Text;
                    using (var reader = proc.ExecuteReader())
                    {
                        do
                        {
                            if (reader.HasRows)
                            {
                                var first = true;
                                while (reader.Read())
                                {
                                    if (first)
                                    {
                                        for (int i = 0; i < reader.FieldCount; i++)
                                            sb.Append(reader.GetName(i)).Append("\t");
                                        Logger.LogMessage(sb.ToString());
                                        sb.Clear();
                                        first = false;
                                    }
                                    for (int i = 0; i < reader.FieldCount; i++)
                                        sb.Append(reader[i]).Append("\t");
                                    Logger.LogMessage(sb.ToString());
                                    sb.Clear();
                                }
                            }
                            //Logger.LogMessage("---------------------");
                        } while (reader.NextResult());
                    }
                }
            }
            Logger.LogMessage("Script is successfully executed.");
        }
    }
}
