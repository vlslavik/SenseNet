using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.ContentRepository.Storage.Security
{
    public partial class SnIdentity
    {
        internal static SnIdentity Create(int nodeId)
        {
            Node node = null;
            using (new SystemAccount())
                node = Node.LoadNode(nodeId);

            if (node == null || !SecurityHandler.HasPermission(node, PermissionType.See))
                node = Node.LoadNode(RepositoryConfiguration.SomebodyUserId);

            string name = node.Name;
            SnIdentityKind kind = SnIdentityKind.User;
            var nodeAsUser = node as IUser;
            if (nodeAsUser != null)
            {
                name = nodeAsUser.FullName;
                kind = SnIdentityKind.User;
            }
            else
            {
                var nodeAsGroup = node as IGroup;
                if (nodeAsGroup != null)
                {
                    kind = SnIdentityKind.Group;
                }
                else
                {
                    var nodeAsOrgUnit = node as IOrganizationalUnit;
                    if (nodeAsOrgUnit != null)
                    {
                        kind = SnIdentityKind.OrganizationalUnit;
                    }
                    else
                    {
                        throw new ApplicationException(String.Concat("Cannot create SnIdentity from NodeType ", ActiveSchema.NodeTypes.GetItemById(node.NodeTypeId).Name, ". Path: ", node.Path));
                    }
                }
            }

            return new SnIdentity
            {
                NodeId = node.Id,
                Path = node.Path,
                Name = name,
                Kind = kind
            };
        }

    }
}
