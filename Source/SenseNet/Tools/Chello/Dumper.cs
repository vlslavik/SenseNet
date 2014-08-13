using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Threading;
using System.Diagnostics;

namespace SenseNet.Chello
{
    public class Dumper
    {
        private const double IDLETIMEINMILLISECONDS = 5000.0;
        private static DateTime _lastWriteTime = DateTime.UtcNow;
        private static object _lastWriteTimeSync = new object();

        private static System.Timers.Timer _backupTimer;
        static Dumper()
        {
            _backupTimer = new System.Timers.Timer(IDLETIMEINMILLISECONDS);
            _backupTimer.AutoReset = false;
            _backupTimer.Stop();
            _backupTimer.Elapsed += new System.Timers.ElapsedEventHandler(_backupTimer_Elapsed);
        }

        private static string _logDir;
        private static string GetLogDirectory()
        {
            if (_logDir == null)
            {
                var ctx = HttpContext.Current;
                if (ctx != null)
                {
                    var req = ctx.Request;
                    if (req != null)
                        _logDir = ctx.Request.MapPath("/Log");
                }
            }
            return _logDir;
        }

        public static void DumpRequest(HttpRequest request)
        {
            _backupTimer.Stop();

            var chars = new List<char>(1000);
            chars.AddRange(request.HttpMethod.ToCharArray());
            chars.Add(' ');

            chars.AddRange(("E100: " + System.Net.ServicePointManager.Expect100Continue).ToCharArray());
            chars.Add(' ');

            chars.AddRange(request.RawUrl.ToCharArray());
            chars.Add(' ');
            chars.AddRange(request.Url.ToString().ToCharArray());
            chars.Add(' ');
            chars.AddRange(request.ServerVariables["SERVER_PROTOCOL"].ToCharArray());
            chars.Add((char)0x0D);
            chars.Add((char)0x0A);

            var headers = request.Headers;
            foreach (string key in headers.Keys)
            {
                chars.AddRange(key.ToCharArray());
                chars.Add(':');
                chars.Add(' ');
                chars.AddRange(headers[key].ToCharArray());
                chars.Add((char)0x0D);
                chars.Add((char)0x0A);
            }
            chars.Add((char)0x0D);
            chars.Add((char)0x0A);

            var stream = request.InputStream;
            var savedPos = stream.Position;
            if (savedPos != 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            try
            {
                int data;
                while ((data = stream.ReadByte()) > -1)
                    chars.Add((char)data);
            }
            finally
            {
                stream.Seek(savedPos, SeekOrigin.Begin);
            }

            WriteToLog(chars, GetFileName("Request", request));

            _backupTimer.Start();
        }

        public static void DumpResponse(HttpResponse response, HttpRequest request)
        {
            _backupTimer.Stop();

            var chars = new List<char>(1000);
            //chars.AddRange(response.......ToCharArray());
            //chars.Add(' ');
            chars.AddRange(response.ContentType.ToCharArray());
            chars.Add(' ');
            chars.AddRange(("StatusCode: " + response.StatusCode.ToString()).ToCharArray());
            chars.Add(' ');
            chars.Add((char)0x0D);
            chars.Add((char)0x0A);

            var headers = response.Headers;
            foreach (string key in headers.Keys)
            {
                chars.AddRange(key.ToCharArray());
                chars.Add(':');
                chars.Add(' ');
                chars.AddRange(headers[key].ToCharArray());
                chars.Add((char)0x0D);
                chars.Add((char)0x0A);
            }
            chars.Add((char)0x0D);
            chars.Add((char)0x0A);

            try
            {
                if (response.Filter is OutputFilterStream)
                    WriteToLog(chars.Concat(((OutputFilterStream)response.Filter).ReadStream().ToCharArray()), GetFileName("Response", request));
                else
                    WriteToLog(chars.Concat(" DUMPER EROR: Unknown Response stream format.".ToCharArray()), GetFileName("Response", request));
            }
            catch (Exception ex)
            {
                //TODO: Log the error
                WriteToLog(("ERROR: " + ex.ToString()).ToCharArray(), GetFileName("Response", request));
                //EventLog.WriteEntry("SenseNet.Chello", "WriteToLog error: " + ex.ToString(), EventLogEntryType.Error);
            }

            _backupTimer.Start();
        }

        private static string GetFileName(string prefix, HttpRequest request)
        {
            var now = DateTime.UtcNow;

            var url = String.Concat(request.RawUrl, request.Url);
            var hash = url.GetHashCode();
            Debug.WriteLine(String.Concat("#chello#> ", hash, ": ", url, " | ", request.RawUrl));

            //var filename = String.Concat(hash, "_", prefix/*, "_", now.ToString("yyyy-MM-dd_HH-mm-ss.ffff")*/, ".log");
            var filename = String.Format("{0}_{1}_{2}.log", now.ToString("yyyy-MM-dd_HH-mm-ss-ffff"), hash, prefix);
            var path = Path.Combine(GetLogDirectory(), "" + filename);
            var path1 = path;
            while (File.Exists(path1))
                path1 = Increment(path);
            return path1;
        }
        private static string Increment(string refpath)
        {
            var fileName = Path.GetFileName(refpath);
            var mask = fileName.Insert(fileName.Length - 4, "*");
            var dir = Path.GetDirectoryName(refpath);
            var paths = Directory.GetFiles(dir, mask);

            var lastpath = paths.OrderByDescending(p => p.Length).OrderByDescending(p => p).First();
            var digits = lastpath.Substring(refpath.Length - 4, lastpath.Length - refpath.Length);
            int number = 1;
            int.TryParse(digits, out number);
            number++;
            var path = refpath.Insert(refpath.Length - 4, number.ToString());
            return path;
        }
        private static void WriteToLog(IEnumerable<char> chars, string fileName)
        {
            try
            {
                using (var writer = new StreamWriter(fileName))
                {
                    var i = 0;
                    var ascii = new char[32];
                    var hexString = new StringBuilder();

                    char c;
                    foreach (var ch in chars)
                    {
                        c = (ch < ' ' || ch > (char)127) ? '.' : ch;
                        ascii[i] = c;
                        hexString.Append(GetHex(ch)).Append(' ');

                        if (++i == 32)
                        {
                            writer.Write(hexString);
                            writer.Write("  ");
                            writer.Write(new String(ascii));
                            writer.WriteLine();

                            for (var j = 0; j < 32; j++)
                                ascii[j] = (char)0;
                            i = 0;
                            hexString.Length = 0;
                        }
                    }

                    if (i > 0)
                    {
                        while (i < 32)
                        {
                            hexString.Append("   ");
                            ascii[i] = ' ';
                            i++;
                        }
                        writer.Write(hexString);
                        writer.Write("  ");
                        writer.Write(new String(ascii));
                    }
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                //EventLog.WriteEntry("SenseNet.Chello", "WriteToLog error: " + ex.ToString(), EventLogEntryType.Error);
            }
        }
        private static char[] hexChars = "0123456789ABCDEF".ToCharArray();
        private static string GetHex(char c)
        {
            var i = (int)c;
            var l = i & 0x0F;
            var h = (i >> 4) & 0x0F;
            if ((i >> 8) > 0)
            {
                int q = 0;
            }
            return string.Concat(hexChars[h], hexChars[l]);
        }

        static void _backupTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _backupTimer.Stop();
            BackupLogs();
            _backupTimer.Start();
        }
        private static void BackupLogs()
        {
            var logDir = GetLogDirectory();
            bool ok = false;
            var count = 3;
            Exception lastEx = null;
            string targetPath = null;
            while (!ok)
            {
                try
                {
                    var paths = Directory.GetFiles(logDir);
                    if (paths.Length == 0)
                        return;

                    var dirName = GetNextDirectoryName(logDir);
                    Directory.CreateDirectory(dirName);

                    foreach (var path in paths)
                    {
                        targetPath =  Path.Combine(dirName, Path.GetFileName(path));
                        File.Move(path, targetPath);
                    }

                    ok = true;
                }
                catch (Exception e)
                {
                    lastEx = e;
                }
                if (--count == 0)
                    break;

                Thread.Sleep(500);
            }
            if (count == 0 && lastEx != null)
                throw lastEx;
        }
        private static string GetNextDirectoryName(string logDir)
        {
            var paths = Directory.GetDirectories(logDir);
            var lastpath = paths.OrderByDescending(p => p.Length).OrderByDescending(p => p).FirstOrDefault();

            string path = null;
            if (lastpath == null)
            {
                path = Path.Combine(logDir, "1");
            }
            else
            {
                var digits = Path.GetFileName(lastpath);
                int number = 0;
                int.TryParse(digits, out number);
                number++;
                path = Path.Combine(logDir, number.ToString());
            }

            return path;
        }
    }
}
