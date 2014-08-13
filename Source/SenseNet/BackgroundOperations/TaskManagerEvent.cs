using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.BackgroundOperations
{
    public enum LogLevel {Verbose, Information, Warning, Error};

    public class TaskManagerEvent
    {
        public int EventId { get; set; }
        public LogLevel Level { get; set; }
        public string MachineName { get; set; }
        public string AgentName { get; set; }
        public string Message { get; set; }
    }
}
