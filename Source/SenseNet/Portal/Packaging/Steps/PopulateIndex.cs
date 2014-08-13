using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Packaging.Steps;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging.Steps
{
    public class PopulateIndex : Step
    {
        [DefaultProperty]
        [Description("Optional path of the subtree to populate. Default: /Root.")]
        public string Path { get; set; }

        private int _count;

        public override void Execute(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Path))
            {
                Logger.LogMessage("Populating...");

                var savedMode = RepositoryConfiguration.WorkingMode.Populating;
                RepositoryConfiguration.WorkingMode.Populating = true;

                //if (indexPath != null)
                //    StorageContext.Search.SetIndexDirectoryPath(indexPath);

                var p = StorageContext.Search.SearchEngine.GetPopulator();
                p.NodeIndexed += new EventHandler<NodeIndexedEvenArgs>(Populator_NodeIndexed);

                try
                {
                    p.ClearAndPopulateAll();
                }
                finally
                {
                    p.NodeIndexed -= new EventHandler<NodeIndexedEvenArgs>(Populator_NodeIndexed);
                    RepositoryConfiguration.WorkingMode.Populating = savedMode;
                }
                Logger.LogMessage("...finished: " + _count + " content indexed.");
                return;
            }
            throw new NotImplementedException("Populating subtree is not implemented in this version.");
        }
        private void Populator_NodeIndexed(object sender, NodeIndexedEvenArgs e)
        {
            _count++;
        }

    }
}
