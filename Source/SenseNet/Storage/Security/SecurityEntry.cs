using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Storage.Security
{
    [DebuggerDisplay("{ToString()}")]
    public sealed class SecurityEntry : PermissionSet
    {
        internal SecurityEntry(int definedOnNodeId, int principalId, bool isInheritable, PermissionValue[] permissionValues)
            : base(principalId, isInheritable, permissionValues)
        {
            DefinedOnNodeId = definedOnNodeId;
            //_principalId = principalId;
            //_isInheritable = isInheritable;
            //_permissionValues = permissionValues;
        }

        public int DefinedOnNodeId { get; private set; }

        public override string ToString()
        {
            return String.Format("DefinedOn={0}, Principal={1}, Propagates={2}, Values={3}",
                DefinedOnNodeId, PrincipalId, Propagates.ToString().ToLower(), ValuesString);
        }
        public string ValuesToString()
        {
            return ValuesString;
        }
        public void Export(XmlWriter writer)
        {
            writer.WriteStartElement("Identity");
            writer.WriteAttributeString("path", NodeHead.Get(PrincipalId).Path);
            if (!this.Propagates)
                writer.WriteAttributeString("propagation", "LocalOnly");
            foreach (var permType in ActiveSchema.PermissionTypes)
            {
                var value = this.PermissionValues[permType.Index - 1]; //@@ PermissionType.Id ????
                if (value == PermissionValue.NonDefined)
                    continue;
                writer.WriteElementString(permType.Name, value.ToString());
            }
            writer.WriteEndElement();
        }
        public void Export1(XmlWriter writer)
        {
            //TODO: ? write propagates ?
            writer.WriteStartElement("Identity");
            writer.WriteAttributeString("values", ValuesString);
            writer.WriteAttributeString("path", NodeHead.Get(PrincipalId).Path);
            writer.WriteEndElement();
        }

        internal void Combine(PermissionSet permissionSet)
        {
            AllowBits |= permissionSet.AllowBits;
            DenyBits |= permissionSet.DenyBits;
        }
    }
}