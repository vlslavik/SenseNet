using System.Linq;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage;
using SenseNet.BackgroundOperations;
using System.Web;

namespace SenseNet.ContentRepository.Preview
{
    public class DocumentPreviewObserver : NodeObserver
    {
        private static readonly string[] MONITORED_FIELDS = new[] { "Binary", "Version", "Locked", "SavingState" };

        // ================================================================================= Observer methods

        protected override void OnNodeCreated(object sender, NodeEventArgs e)
        {
            base.OnNodeCreated(sender, e);
            if (e.SourceNode.CopyInProgress)
                return;

            DocumentPreviewProvider.StartPreviewGeneration(e.SourceNode, GetPriority(e.SourceNode as File));
        }
        protected override void OnNodeModified(object sender, NodeEventArgs e)
        {
            base.OnNodeModified(sender, e);

            // check: fire only when the relevant fields had been modified (binary, version, ...)
            if (!e.ChangedData.Any(d => MONITORED_FIELDS.Contains(d.Name)))
                return;

            DocumentPreviewProvider.StartPreviewGeneration(e.SourceNode, GetPriority(e.SourceNode as File));
        }

        private static TaskPriority GetPriority(File file)
        {
            if (file != null)
                return file.PreviewGenerationPriority;

            return TaskPriority.Normal;
        }
    }
}
