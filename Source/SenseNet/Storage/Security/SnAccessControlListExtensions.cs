using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Security
{
    public partial class SnAccessControlList
    {
        public IEnumerable<SnAccessControlEntry> Entries { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendFormat("  NodeId: {0},", NodeId).AppendLine();
            sb.AppendFormat("  Path: \"{0}\",", Path).AppendLine();
            sb.AppendFormat("  Inherits: {0},", Inherits).AppendLine();
            sb.AppendFormat("  Creator: {0},", Creator == null ? 0 : Creator.NodeId).AppendLine();
            sb.AppendFormat("  LastModifier: {0},", LastModifier == null ? 0 : LastModifier.NodeId).AppendLine();
            sb.AppendLine("  Entries: [");
            foreach (var entry in Entries)
            {
                sb.AppendLine("    {");
                sb.AppendLine("      Identity: {");
                sb.AppendFormat("        NodeId: {0},", entry.Identity.NodeId).AppendLine();
                sb.AppendFormat("        Path: \"{0}\",", entry.Identity.Path).AppendLine();
                sb.AppendFormat("        Name: \"{0}\",", entry.Identity.Name).AppendLine();
                sb.AppendFormat("        Kind: \"{0}\",", entry.Identity.Kind).AppendLine();
                sb.AppendLine("      },");
                sb.AppendFormat("      Propagates: {0},", entry.Propagates).AppendLine();
                sb.AppendFormat("      Permissions: [", entry.PermissionsToString()).AppendLine();
                foreach (var perm in entry.Permissions)
                {
                    var value = perm.Allow ? "Allow" : (perm.Deny ? "Deny" : "");
                    var from = perm.Allow ? perm.AllowFrom : (perm.Deny ? perm.DenyFrom : "");
                    sb.AppendLine("        {");
                    sb.AppendFormat("          Value: {0},", value).AppendLine();
                    sb.AppendFormat("          From: {0}", from).AppendLine();
                    sb.AppendLine("        }");
                }
                sb.AppendLine("      ]");
                sb.AppendLine("    }");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
