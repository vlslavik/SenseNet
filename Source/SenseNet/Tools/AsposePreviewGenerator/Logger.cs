using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsposePreviewGenerator
{
    class Logger
    {
        private static readonly string LOG_PREFIX = "#AsposePreviewGenerator> ";

        internal static void WriteInfo(int contentId, int page, string message)
        {
            Debug.WriteLine(String.Format("{0} {1} Content id: {2}, page number: {3}", LOG_PREFIX, message, contentId, page));
        }

        internal static void WriteWarning(int contentId, int page, string message)
        {
            WriteInfo(contentId, page, message);
        }

        internal static void WriteError(int contentId, int page = 0, string message = "", Exception ex = null, int startIndex = 0, string version = "")
        {
            Debug.WriteLine(String.Format("{0} ERROR {1} Content id: {2}, version: {3}, page number: {4}, start index: {5}, Exception: {6}",
                LOG_PREFIX, 
                message, 
                contentId, 
                version,
                page, 
                startIndex,
                ex == null ? string.Empty : ex.ToString().Replace(Environment.NewLine, " * ")));
        }

        internal static void WriteError(int contentId, int page)
        {
            WriteError(contentId, page, "Error during preview generation.");
        }
    }
}
