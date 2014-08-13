using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SenseNet.BackgroundOperations
{
    internal class LocalTaskManager : TaskManagerBase
    {
        public override bool Distributed { get { return false; } }

        public override void Start()
        {
            AgentManager.Startup(HttpRuntime.BinDirectory, new AgentLogger());
            Logger.WriteInformation(1, "LocalTaskManager AgentManager STARTED.");
        }
        public override void Stop()
        {
            AgentManager.Shutdown();
            Logger.WriteInformation(1, "LocalTaskManager AgentManager STOPPED.");
        }

        //====================================================================================== Inner classes

        /// <summary>
        /// Helper logger for the agent manager class
        /// </summary>
        internal class AgentLogger : ILogger
        {
            public void WriteVerbose(string message)
            {
                Logger.WriteVerbose(message);
            }

            public void WriteInformation(string message, int eventId)
            {
                Logger.WriteInformation(eventId, message);
            }

            public void WriteError(string message, Exception ex)
            {
                Logger.WriteException(ex);
            }
        }
    }
}
