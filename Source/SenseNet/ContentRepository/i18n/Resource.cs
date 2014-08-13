using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.ContentRepository.i18n
{
    /// <summary>
    /// Stores the necessary resources. 
    /// </summary>
    [ContentHandler]
    public class Resource : File
    {

		//================================================================================= Construction

        public Resource(Node parent) : this(parent, null) { }
		public Resource(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Resource(NodeToken nt) : base(nt) { }

        //================================================================================= Generic Property handling

        public override object GetProperty(string name)
        {
            switch (name)
            {
                //case "Binary":
                //    return this.Binary;
                //case "Downloads":
                //    return this.Downloads;
                //case "Size":
                //    return this.Size;
                //case "FullSize":
                //    return this.FullSize;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                //case "Binary":
                //    this.Binary = (BinaryData)value;
                //    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        protected override void OnModified(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnModified(sender, e);

            if (e.ChangedData.Any(cd => cd.Name == "Binary"))
                SenseNetResourceManager.Reset();
        }
    }
}