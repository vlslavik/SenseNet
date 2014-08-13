using SenseNet.BackgroundOperations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNetTaskAgentService
{
    public partial class AgentService : ServiceBase
    {
        public AgentService()
        {
            InitializeComponent();

            this.AutoLog = false;
            Logger.Initialize(eventLog1);
        }

        //====================================================================================== Service methods

        protected override void OnStart(string[] args)
        {
            AgentManager.Startup(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), new Logger());

            Logger.WriteInformation("TaskAgentService STARTED.", EventId.ServiceStarted);
        }

        protected override void OnStop()
        {
            AgentManager.Shutdown();
            
            Logger.WriteInformation("TaskAgentService STOPPED.", EventId.ServiceStopped);
        }        
    }
}
