using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.BackgroundOperations;
using Newtonsoft.Json;
using System.IO;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Storage;
using Newtonsoft.Json.Linq;

namespace SenseNet.ContentRepository.Preview
{
    public class PreviewGeneratorTaskFinalizer : ITaskFinalizer
    {
        public void Finalize(SnTaskResult result)
        {
            if (result.Successful || result.Task == null || result.Task.Type.CompareTo("AsposePreviewGenerator") != 0 || string.IsNullOrEmpty(result.Task.TaskData))
                return;

            try
            {
                var settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };
                var serializer = JsonSerializer.Create(settings);
                using (var jreader = new JsonTextReader(new StringReader(result.Task.TaskData)))
                {
                    var previewData = serializer.Deserialize(jreader) as JObject;
                    var contentId = previewData["Id"].Value<int>();

                    using (new SystemAccount())
                    {
                        DocumentPreviewProvider.SetPreviewStatus(Node.Load<File>(contentId), PreviewStatus.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
            }
        }
    }
}
