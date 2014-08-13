using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    /// <summary>Holds safe queries in public static readonly string properties.</summary>
    public class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"</summary>
        public static string AllDevices { get { return "+InTree:/Root/System/Devices +TypeIs:Device .AUTOFILTERS:OFF"; } }
        /// <summary>Returns with the following query: "+TypeIs:Aspect +Name:@0 .AUTOFILTERS:OFF .COUNTONLY"</summary>
        public static string AspectExists { get { return "+TypeIs:Aspect +Name:@0 .AUTOFILTERS:OFF .COUNTONLY"; } }

        /// <summary>Returns with the following query: "InTree:@0 .SORT:Path"</summary>
        public static string InTreeOrderByPath { get { return "InTree:@0 .SORT:Path"; } }

        /// <summary>Returns with the following query: "+InFolder:@0"</summary>
        public static string InFolder { get { return "+InFolder:@0"; } }
        /// <summary>Returns with the following query: "+InFolder:@0 +TypeIs:@1"</summary>
        public static string InFolderAndTypeIs { get { return "+InFolder:@0 +TypeIs:@1"; } }

        /// <summary>Returns with the following query: "+InFolder:@0 .COUNTONLY"</summary>
        public static string InFolderCountOnly { get { return "+InFolder:@0 .COUNTONLY"; } }
        /// <summary>Returns with the following query: "+InFolder:@0 +TypeIs:@1 .COUNTONLY"</summary>
        public static string InFolderAndTypeIsCountOnly { get { return "+InFolder:@0 +TypeIs:@1 .COUNTONLY"; } }

        /// <summary>Returns with the following query: "+InTree:@0"</summary>
        public static string InTree { get { return "+InTree:@0"; } }
        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:@1"</summary>
        public static string InTreeAndTypeIs { get { return "+InTree:@0 +TypeIs:@1"; } }
        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:@1 +Name:@2"</summary>
        public static string InTreeAndTypeIsAndName { get { return "+InTree:@0 +TypeIs:@1 +Name:@2"; } }

        /// <summary>Returns with the following query: "+InTree:@0 .COUNTONLY"</summary>
        public static string InTreeCountOnly { get { return "+InTree:@0 .COUNTONLY"; } }
        /// <summary>Returns with the following query: "+InTree:@0 +TypeIs:@1 .COUNTONLY"</summary>
        public static string InTreeAndTypeIsCountOnly { get { return "+InTree:@0 +TypeIs:@1 .COUNTONLY"; } }

        /// <summary>Returns with the following query: "+TypeIs:@0 +Name:@1"</summary>
        public static string TypeIsAndName { get { return "+TypeIs:@0 +Name:@1"; } }

        /// <summary>Returns with the following query: "+TypeIs:Settings +Name:@0 -Id:@1 +InTree:@2 .AUTOFILTERS:OFF .COUNTONLY"</summary>
        public static string SettingsByNameAndSubtree { get { return "+TypeIs:Settings +Name:@0 -Id:@1 +InTree:@2 .AUTOFILTERS:OFF .COUNTONLY"; } }
    }
}
