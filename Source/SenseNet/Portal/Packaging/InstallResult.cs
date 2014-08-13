using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging
{
	public class PackagingResult
	{
        public bool Successful { get; internal set; }
        public bool NeedRestart { get; internal set; }
        public int Errors { get; internal set; }

        internal void Combine(PackagingResult other)
		{
			Successful &= other.Successful;
			NeedRestart |= other.NeedRestart;
            Errors += other.Errors;
        }
    }
}
