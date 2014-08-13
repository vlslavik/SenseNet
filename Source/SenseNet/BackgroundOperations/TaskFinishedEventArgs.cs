using System;

namespace SenseNet.BackgroundOperations
{
    public class TaskFinishedEventArgs : EventArgs
    {
        public SnTaskResult TaskResult { get; set; }
    }
}
