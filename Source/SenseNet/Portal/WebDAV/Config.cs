using System.Configuration;
using System.Linq;
using System.Collections.Specialized;
using SenseNet.Portal;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository;
using System.Collections.Generic;


namespace SenseNet.Services.WebDav
{
	internal class Config
	{
        public static PageTemplate DefaultPageTemplate
        {
            get
            {
                var nq = new NodeQuery();                
                nq.Add(new TypeExpression(ActiveSchema.NodeTypes["PageTemplate"]));
                string path = Repository.PageTemplatesFolderPath;

                var section = ConfigurationManager.GetSection("sensenet/webdavSettings") as NameValueCollection;
                if (section != null)
                {
                    string defaultPageTemplate = section["WebdavDefaultPageTemplate"];
                    if (!string.IsNullOrEmpty(defaultPageTemplate))
                        path = RepositoryPath.Combine(path, defaultPageTemplate);
                }

                nq.Add(new StringExpression(StringAttribute.Path, StringOperator.StartsWith, path));

                return nq.Execute().Nodes.FirstOrDefault() as PageTemplate;
            }
        }
	    public static List<string> MockExistingFiles
	    {
	        get
	        {
                var section = ConfigurationManager.GetSection("sensenet/webdavSettings") as NameValueCollection;
                if (section == null)
                    return null;
	            
                string mockedFiles = section["MockExistingFiles"];
                if (string.IsNullOrEmpty(mockedFiles))
                    return null;

	            return mockedFiles.Split(new char[] {';', ','}, System.StringSplitOptions.RemoveEmptyEntries).ToList();
	        }
	    }
        private static bool? _autoCheckoutFiles;
        private static object _autoCheckoutFilesSync = new object();
        public static bool AutoCheckoutFiles
        {
            get
            {
                if (!_autoCheckoutFiles.HasValue)
                {
                    lock (_autoCheckoutFilesSync)
                    {
                        if (!_autoCheckoutFiles.HasValue)
                        {
                            _autoCheckoutFiles = false;
                            var section = ConfigurationManager.GetSection("sensenet/webdavSettings") as NameValueCollection;
                            if (section != null)
                            {
                                string autoCheckoutFilesStr = section["AutoCheckoutFiles"];
                                bool val;
                                if (bool.TryParse(autoCheckoutFilesStr, out val))
                                    _autoCheckoutFiles = val;
                            }
                        }
                    }
                }
                return _autoCheckoutFiles.Value;
            }
        }
	}
}
