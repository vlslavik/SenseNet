using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.BackgroundOperations
{
    internal class DistributedTaskManager : TaskManagerBase
    {
        // This class uses the same logic for communication with the agents (implemented 
        // in the base class) as the local provider. In the future this class will be
        // responsible for distributing the task executables and libraries to make
        // agent management easier for operators.

        public override bool Distributed { get { return true; } }
        public override void Start() { }
        public override void Stop() { }
    }
}
