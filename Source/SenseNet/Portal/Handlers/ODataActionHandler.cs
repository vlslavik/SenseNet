using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.Handlers
{
    [ContentHandler]
    public class ODataActionHandler : Application, IHttpHandler
    {
        public ODataActionHandler(Node parent) : this(parent, null) { }
        public ODataActionHandler(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ODataActionHandler(NodeToken nt) : base(nt) { }

        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            var action = (IHttpHandler)this.CreateAction(this.Content, null, null);
            //var action = (IHttpHandler)TypeHandler.CreateInstance(TypeName);
            action.ProcessRequest(context);
        }

        public const string TYPENAME = "TypeName";
        [RepositoryProperty(TYPENAME, RepositoryDataType.String)]
        public virtual string TypeName
        {
            get { return base.GetProperty<string>(TYPENAME); }
            set { this[TYPENAME] = value; }
        }

    }
}
