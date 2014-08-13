using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.BackgroundOperations
{
    public class SnTaskResult
    {
        public string AgentName { get; set; }
        public SnTask Task { get; set; }
        public int ResultCode { get; set; }
        public Exception Exception { get; set; }

        public bool Successful
        {
            get { return ResultCode == 0 && Exception == null; }
        }
    }
}
