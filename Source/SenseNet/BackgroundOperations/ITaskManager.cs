using System;

namespace SenseNet.BackgroundOperations
{
    public interface ITaskManager
    {
        bool Distributed { get; }
        void Start();
        void Stop();

        event EventHandler<TaskFinishedEventArgs> TaskFinished;
        void RegisterTask(string type, TaskPriority priority, string data);
    }
}
