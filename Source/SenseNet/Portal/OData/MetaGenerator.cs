﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Portal.OData.Metadata;
using System.Web;

namespace SenseNet.Portal.OData
{
    internal static class MetaGenerator
    {
        internal static readonly string dataServiceVersion = "3.0";
        internal static readonly string schemaNamespace = "SenseNet.OData";
        internal static readonly string entityContainerName = "SenseNet_Entities";
        internal static readonly ContentType ContentListContentType = ContentType.GetByName("ContentList");
        private static readonly IEnumerable<FieldSetting> EmtpyFieldSettings = new FieldSetting[0];

        private static Edmx _edmx;
        internal static string _edmxXml;
        internal static string _edmxJson;

        public static void WriteMetadata(TextWriter writer, ODataFormatter formatter)
        {
            if (_edmx == null)
                CreateEdmx();
            formatter.WriteMetadataInternal(writer);
        }
        public static void WriteMetadata(TextWriter writer, ODataFormatter formatter, Content content, bool isCollection)
        {
            var edmx = CreateEdmx(content, isCollection);
            formatter.WriteMetadataInternal(writer, edmx);
        }

        private static Edmx CreateEdmx(Content content, bool isCollection)
        {
            var context = new SchemaGenerationContext();
            CreateEntityType(content, context, isCollection);
            return new Edmx
            {
                DataServices = new DataServices
                {
                    DataServiceVersion = MetaGenerator.dataServiceVersion,
                    Schemas = new[]{new Schema
                    {
                        EntityTypes = context.EntityTypes,
                        ComplexTypes = context.ComplexTypes,
                        EnumTypes = context.EnumTypes,
                        Associations = context.Associations,
                        EntityContainer = CreateEntityContainer(context)
                    }}
                }
            };
        }
        private static void CreateEntityType(Content content, SchemaGenerationContext context, bool isCollection)
        {
            var list = content.ContentHandler as ContentList;
            if (list == null)
                list = (ContentList)content.ContentHandler.LoadContentList();
            context.ListFieldSettings = list == null ? EmtpyFieldSettings : list.FieldSettings;

            if (isCollection)
            {
                IEnumerable<ContentType> contentTypes = null;

                var gc = content.ContentHandler as GenericContent;
                if (gc != null)
                {
                    if (!isCollection)
                        gc = gc.Parent as GenericContent;
                    if(gc != null)
                        contentTypes = gc.GetAllowedChildTypes();
                }

                if (contentTypes == null || contentTypes.Count() == 0)
                {
                    context.WithChildren = true;
                    contentTypes = ContentType.GetRootTypes();
                }
                else
                {
                    context.WithChildren = false;
                }
                CreateEntityTypes(contentTypes, context);
            }
            else
            {
                var contentType = content.ContentType;

                var entityType = new EntityType
                {
                    Name = contentType.Name,
                    BaseType = contentType.ParentTypeName,
                    Key = GetKey(contentType, context),
                    Properties = new List<Property>(),
                    NavigationProperties = new List<NavigationProperty>()
                };
                context.EntityTypes.Add(entityType);

                context.Flattening = true;
                CreatePropertiesFromFieldSettings(content.ContentType.FieldSettings, contentType, entityType, context);
            }
        }

        private static void CreateEdmx()
        {
            ContentType.TypeSystemRestarted -= new EventHandler(ContentType_TypeSystemRestarted);
            ContentType.TypeSystemRestarted += new EventHandler(ContentType_TypeSystemRestarted);

            var context = new SchemaGenerationContext();
            CreateEntityTypes(context);
            _edmx = new Edmx
            {
                DataServices = new DataServices
                {
                    DataServiceVersion = MetaGenerator.dataServiceVersion,
                    Schemas = new[] { new Schema
                    {
                        EntityTypes = context.EntityTypes,
                        ComplexTypes = context.ComplexTypes,
                        EnumTypes = context.EnumTypes,
                        Associations = context.Associations,
                        EntityContainer = CreateEntityContainer(context)
                    }}
                }
            };

            var writer = new StringWriter();
            _edmx.WriteXml(writer);
            _edmxXml = writer.GetStringBuilder().ToString();

            writer = new StringWriter();
            _edmx.WriteJson(writer);
            _edmxJson = writer.GetStringBuilder().ToString();
        }
        private static void ContentType_TypeSystemRestarted(object sender, EventArgs e)
        {
            _edmx = null;
        }

        private static void CreateEntityTypes(SchemaGenerationContext context)
        {
            context.WithChildren = true;
            context.ListFieldSettings = EmtpyFieldSettings;
            CreateEntityTypes(ContentType.GetRootTypes(), context);
        }
        private static void CreateEntityTypes(IEnumerable<ContentType> contentTypes, SchemaGenerationContext context)
        {
            context.Flattening = true;
            foreach (var contentType in contentTypes)
                CreateEntityType(contentType, context);
        }
        private static void CreateEntityType(ContentType contentType, SchemaGenerationContext context)
        {
            var entityType = new EntityType
            {
                Name = contentType.Name,
                BaseType = contentType.ParentTypeName,
                Key = GetKey(contentType, context),
                Properties = new List<Property>(),
                NavigationProperties = new List<NavigationProperty>()
            };
            context.EntityTypes.Add(entityType);

            CreatePropertiesFromFieldSettings(contentType.FieldSettings, contentType, entityType, context);

            if (context.WithChildren)
                CreateChildEntityTypes(contentType, context);
        }
        private static void CreatePropertiesFromFieldSettings(IEnumerable<FieldSetting> fieldSettings, ContentType contentType, EntityType entityType, SchemaGenerationContext context)
        {
            var properties = entityType.Properties;
            var navigationProperties = entityType.NavigationProperties;
            foreach (var fieldSetting in fieldSettings)
                CreatePropertyFromFieldSetting(fieldSetting, contentType, entityType, context);
            foreach (var fieldSetting in context.ListFieldSettings)
                CreatePropertyFromFieldSetting(fieldSetting, contentType, entityType, context);
        }
        private static void CreatePropertyFromFieldSetting(FieldSetting fieldSetting, ContentType contentType, EntityType entityType, SchemaGenerationContext context)
        {
            if (!context.Flattening && fieldSetting.Owner != contentType && fieldSetting.Owner != ContentListContentType)
                return;
            if (ODataHandler.DisabledFieldNames.Contains(fieldSetting.Name))
                return;

            var refField = fieldSetting as ReferenceFieldSetting;
            if (refField != null)
            {
                entityType.NavigationProperties.Add(CreateNavigationProperty(refField, context));
                return;
            }
            entityType.Properties.Add(CreateProperty(fieldSetting, context));
        }
        private static NavigationProperty CreateNavigationProperty(ReferenceFieldSetting refField, SchemaGenerationContext context)
        {
            var associationName = String.Concat(refField.Owner.Name, "_", refField.Name);
            var fromName = String.Concat(associationName, "_From");
            var toName = String.Concat(associationName, "_To");

            if (!context.Associations.Any(x => x.Name == associationName))
            {
                var ancestorName = GetNearestAncestorName(refField.AllowedTypes);
                context.Associations.Add(new Association
                {
                    Name = associationName,
                    End1 = new AssociationEnd
                    {
                        Type = refField.Owner.Name,
                        Role = fromName,
                        Multiplicity = "1"
                    },
                    End2 = new AssociationEnd
                    {
                        Type = ancestorName,
                        Role = toName,
                        Multiplicity = refField.AllowMultiple == true ? "*" : refField.Compulsory == true ? "1" : "0..1"
                    }
                });
            }

            return new NavigationProperty
            {
                Name = refField.Name,
                ToRole = toName,
                FromRole = fromName,
                Relationship = associationName
            };
        }
        private static Property CreateProperty(FieldSetting fieldSetting, SchemaGenerationContext context)
        {
            Property property;
            var attributes = new List<KeyValue>();
            if (fieldSetting.Name == "Name")
            {
                attributes.Add(new KeyValue { Key = "m:FC_TargetPath", Value = "SyndicationTitle" });
                attributes.Add(new KeyValue { Key = "m:FC_ContentKind", Value = "text" });
                attributes.Add(new KeyValue { Key = "m:FC_KeepInContent", Value = "false" });
            }
            if (fieldSetting.Name == "Description")
            {
                attributes.Add(new KeyValue { Key = "m:FC_TargetPath", Value = "SyndicationSummary" });
                attributes.Add(new KeyValue { Key = "m:FC_ContentKind", Value = "text" });
                attributes.Add(new KeyValue { Key = "m:FC_KeepInContent", Value = "false" });
            }

            var choiceFieldSetting = fieldSetting as ChoiceFieldSetting;
            if (choiceFieldSetting != null)
            {
                var choiceTypeName = String.Concat(choiceFieldSetting.Owner.Name, ".", fieldSetting.Name);

                if (!context.EnumTypes.Any(x => x.Name == choiceTypeName))
                {
                    EnumType enumType;
                    if (choiceFieldSetting.FieldDataType.IsEnum)
                        enumType = new EnumType { Name = choiceTypeName, UnderlyingEnumType = choiceFieldSetting.FieldDataType };
                    else
                        enumType = new EnumType { Name = choiceTypeName, UnderlyingFieldSetting = choiceFieldSetting };
                    context.EnumTypes.Add(enumType);
                }

                property = new Property
                {
                    Name = fieldSetting.Name,
                    FieldType = fieldSetting.ShortName,
                    Type = choiceTypeName,
                    Nullable = !fieldSetting.Compulsory.HasValue || !fieldSetting.Compulsory.Value,
                    DefaultValue = fieldSetting.DefaultValue,
                    Attributes = attributes.Count > 0 ? attributes : null,
                };

                return property;
            }

            property = new Property
            {
                Name = fieldSetting.Name,
                Type = GetPropertyType(fieldSetting, context),
                FieldType = fieldSetting.ShortName,
                Nullable = !fieldSetting.Compulsory.HasValue || !fieldSetting.Compulsory.Value,
                DefaultValue = fieldSetting.DefaultValue,
                Attributes = attributes.Count > 0 ? attributes : null,
            };

            var textFieldSetting = fieldSetting as TextFieldSetting;
            if (textFieldSetting != null)
            {
                property.MaxLength = textFieldSetting.MaxLength;
            }
            //FixedLength, Precision, Scale, Unicode

            return property;
        }
        private static void CreateChildEntityTypes(ContentType contentType, SchemaGenerationContext context)
        {
            context.Flattening = false;
            context.WithChildren = true;
            foreach (var node in contentType.Children)
                CreateEntityType((ContentType)node, context);
        }

        private static EntityContainer CreateEntityContainer(SchemaGenerationContext context)
        {
            var container = new EntityContainer
            {
                Name = entityContainerName,
                EntitySets = CreateEntitySets(context),
                AssociationSets = CreateAssociationSets(context),
                FunctionImports = CreateFunctionImports(context)
            };
            return container;
        }
        private static List<EntitySet> CreateEntitySets(SchemaGenerationContext context)
        {
            var entitySets = new List<EntitySet>();
            foreach (var entityType in context.EntityTypes)
                entitySets.Add(new EntitySet
                {
                    Name = entityType.Name,
                    EntityType = string.Concat(schemaNamespace, ".", entityType.Name),
                });
            return entitySets;
        }
        private static List<AssociationSet> CreateAssociationSets(SchemaGenerationContext context)
        {
            var associationSets = new List<AssociationSet>();
            foreach (var association in context.Associations)
                associationSets.Add(new AssociationSet
                {
                    Name = association.Name,
                    Association = string.Concat(schemaNamespace, ".", association.Name),
                    End1 = new AssociationSetEnd { Role = association.End1.Role, EntitySet = association.End1.Type },
                    End2 = new AssociationSetEnd { Role = association.End2.Role, EntitySet = association.End2.Type }
                });
            return associationSets;
        }
        private static List<FunctionImport> CreateFunctionImports(SchemaGenerationContext context)
        {
            return new List<FunctionImport>();
        }

        private static string GetPropertyType(FieldSetting fieldSetting, SchemaGenerationContext context)
        {
            foreach (var converter in SnJsonConverter.FieldConverters)
                if (converter.CanConvert(fieldSetting))
                    return GetPropertyType(converter.TargetType, context);
            var type = GetPropertyType(fieldSetting.FieldDataType, context);
            return type;
        }
        private static string GetPropertyType(Type t, SchemaGenerationContext context)
        {
            if (t == null)
                return "null";

            var type = GetPrimitivePropertyType(t);
            if (type != null)
                return type;

            if (t.IsEnum)
            {
                if (!context.EnumTypes.Any(x => x.Name == t.FullName))
                {
                    var enumType = new EnumType { Name = t.FullName, UnderlyingEnumType = t };
                    context.EnumTypes.Add(enumType);
                }
                return t.FullName;
            }

            if (t.IsInterface)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    type = String.Concat("Collection(", GetPropertyType(t.GetGenericArguments()[0], context), ")");
                }
                else if (typeof(IEnumerable).IsAssignableFrom(t))
                {
                    type = String.Concat("Collection(", typeof(object).Name, ")");
                }
                else
                {
                    CreateComplexType(t, context);
                    type = t.FullName;
                }
            }
            else
            {
                var iface = t.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (iface != null)
                {
                    type = String.Concat("Collection(", GetPropertyType(iface.GetGenericArguments()[0], context), ")");
                }
                else if (typeof(IEnumerable).IsAssignableFrom(t))
                {
                    type = String.Concat("Collection(", typeof(object).Name, ")");
                }
                else
                {
                    CreateComplexType(t, context);
                    type = t.FullName.Replace("+", ".");
                }
            }
            return type;
        }

        private static Type[] _prohibitiveAttributes = new[]
        {
            typeof(System.NonSerializedAttribute), typeof(System.Xml.Serialization.XmlIgnoreAttribute), typeof(System.Web.Script.Serialization.ScriptIgnoreAttribute)
        };
        private static void CreateComplexType(Type type, SchemaGenerationContext context)
        {
            var typeName = type.FullName.Replace("+", ".");
            if (typeName.StartsWith("System."))
                return;
            if (context.ComplexTypes.Any(x => x.Name == typeName))
                return;
            if (IsContentHandler(type))
                return;

            var complexType = new ComplexType { Name = typeName, Properties = new List<Property>() };
            context.ComplexTypes.Add(complexType);
            foreach (var propInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attrs = propInfo.GetCustomAttributes(true).Select(x => x.GetType());
                if (_prohibitiveAttributes.Except(attrs).Count() != _prohibitiveAttributes.Length)
                    continue;

                var property = CreateProperty(propInfo.Name, propInfo.PropertyType, context);
                if (property != null)
                    complexType.Properties.Add(property);
            }
            foreach (var fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var property = CreateProperty(fieldInfo.Name, fieldInfo.FieldType, context);
                if (property != null)
                    complexType.Properties.Add(property);
            }
        }
        private static bool IsContentHandler(Type type)
        {
            return type.GetCustomAttributes(typeof(ContentHandlerAttribute), true).FirstOrDefault() != null;
        }
        
        private static Property CreateProperty(string name, Type type, SchemaGenerationContext context)
        {
            var attributes = new List<KeyValue>();
            if (name == "Name")
            {
                attributes.Add(new KeyValue { Key = "m:FC_TargetPath", Value = "SyndicationTitle" });
                attributes.Add(new KeyValue { Key = "m:FC_ContentKind", Value = "text" });
                attributes.Add(new KeyValue { Key = "m:FC_KeepInContent", Value = "false" });
            }
            if (name == "Description")
            {
                attributes.Add(new KeyValue { Key = "m:FC_TargetPath", Value = "SyndicationSummary" });
                attributes.Add(new KeyValue { Key = "m:FC_ContentKind", Value = "text" });
                attributes.Add(new KeyValue { Key = "m:FC_KeepInContent", Value = "false" });
            }

            var nullable = true;
            var property = new Property
            {
                Name = name,
                Type = GetPropertyType(type, context),
                Nullable = nullable,
                Attributes = attributes.Count > 0 ? attributes : null,
            };

            return property;
        }

        internal static string GetPrimitivePropertyType(Type type)
        {
            if (type == typeof(string)) return "Edm.String";
                if (type == typeof(byte)) return "Edm.Byte";
                if (type == typeof(sbyte)) return "Edm.SByte";
                if (type == typeof(short)) return "Edm.Int16";
                if (type == typeof(int)) return "Edm.Int32";
                if (type == typeof(long)) return "Edm.Int64";
                if (type == typeof(double)) return "Edm.Double";
                if (type == typeof(float)) return "Edm.Single";
                if (type == typeof(bool)) return "Edm.Boolean";
                if (type == typeof(decimal)) return "Edm.Decimal";
                if (type == typeof(DateTime)) return "Edm.DateTime";
                if (type == typeof(Guid)) return "Edm.Guid";
            return null;

            //---- more primitive types
            //Edm.Binary, Edm.Stream
            //Edm.Time, Edm.DateTimeOffset, Edm.Geography, Edm.GeographyPoint, Edm.GeographyLineString, Edm.GeographyPolygon, Edm.GeographyMultiPoint
            //Edm.GeographyMultiLineString, Edm.GeographyMultiPolygon, Edm.GeographyCollection, Edm.Geometry, Edm.GeometryPoint, Edm.GeometryLineString, Edm.GeometryPolygon
            //Edm.GeometryMultiPoint, Edm.GeometryMultiLineString, Edm.GeometryMultiPolygon, Edm.GeometryCollection
        }

        private static string GetNearestAncestorName(List<string> contentTypeNames)
        {
            if (contentTypeNames == null)
                return "GenericContent";
            if (contentTypeNames.Count == 0)
                return "GenericContent";

            var lists = new List<string[]>();
            foreach (var contentTypeName in contentTypeNames)
                lists.Add(ContentType.GetByName(contentTypeName).Path.Split('/').Skip(4).ToArray());

            var index = 0;
            var lastName = lists[0][0];
            var go=true;
            while (go)
            {
                index++;
                if (lists[0].Length < index + 1)
                    break;
                var refName = lists[0][index];
                for (int i = 0; i < lists.Count; i++)
                {
                    if (lists[i].Length < index + 1)
                    {
                        go = false;
                        break;
                    }
                    if (i > 0 && lists[i][index] != refName)
                    {
                        go = false;
                        break;
                    }
                }
                if (go)
                    lastName = lists[0][index];
            }
            return lastName;
        }
        private static Key GetKey(ContentType contentType, SchemaGenerationContext context)
        {
            if (contentType.ParentTypeName != null && !context.Flattening)
                return null;
            if (contentType.GetFieldSettingByName("Id") == null)
                return null;
            return Key.IdKey;
        }
    }
}
