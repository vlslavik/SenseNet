using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Tests
{
    internal class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "Description:@0"</summary>
        public static string Description { get { return "Description:@0"; } }
    }
}
