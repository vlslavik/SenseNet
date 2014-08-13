using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Packaging.Steps;

namespace SenseNet.Packaging.Steps
{
    public class Trace : Step
    {
        [DefaultProperty]
        public string Text { get; set; }

        public override void Execute(SenseNet.Packaging.ExecutionContext context)
        {
            Logger.LogMessage(this.Text);
        }
    }
}
