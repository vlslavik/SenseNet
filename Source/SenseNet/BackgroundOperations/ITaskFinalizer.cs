
namespace SenseNet.BackgroundOperations
{
    public interface ITaskFinalizer
    {
        void Finalize(SnTaskResult result);
    }
}
