using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Battlehub.Storage
{
    public interface ISurrogatesGen
    {
        string GetSurrogateCode(Type type, int typeIndex);

        bool CanCreateEnumerator(Type type);

        string GetEnumeratorCode(Type type);

        bool CanUpdateSurrogate(Type type, Type surrogateType);

        bool CanUpdateSurrogate(Type surrogateType);

        string GetUpdatedSurrogateCode(Type type, Type surrogateType, string currentCode);

        string GetUpdatedEnumeratorCode(Type type, Type surrogateType);

        Type[] GetDependencies(Type type, bool recursive);

        IEnumerable<MemberInfo> GetSerializableProperties(Type type);

        Type GetPropertyType(MemberInfo memberInfo);

        IEnumerable<Type> GetElementTypes(Type type);
        
        bool IsStruct(Type type);

        bool IsPrimitiveStruct(Type type);
       
        bool IsPrimitive(Type type);

        bool IsGenericList(Type type);

        string GetTypeName(Type type);
    }

    public class SurrogatesGen : ISurrogatesGen
    {
        private const string k_surrogateTemplate = @"using MessagePack;
using ProtoBuf;
using System;
using System.Threading.Tasks;

namespace Battlehub.Storage.Surrogates{3}
{{
    [ProtoContract]
    [MessagePackObject]
    [Surrogate(typeof({0}), _PROPERTY_INDEX, _TYPE_INDEX{11})]
    public {12} {4}Surrogate<TID> : ISurrogate<TID> where TID : IEquatable<TID>
    {{   
        const int _PROPERTY_INDEX = {1};
        const int _TYPE_INDEX = {2};

        //_PLACEHOLDER_FOR_EXTENSIONS_DO_NOT_DELETE_OR_CHANGE_THIS_LINE_PLEASE
{5}{8}
        public ValueTask Serialize(object obj, ISerializationContext<TID> ctx)
        {{
            var idmap = ctx.IDMap;
{6}{9}
            return default;
        }}

        public ValueTask<object> Deserialize(ISerializationContext<TID> ctx)
        {{
            var idmap = ctx.IDMap;
{7}{10}
            return new ValueTask<object>(o);
        }}
    }}
}}
";

        private const string k_valueTypeSurrogateTemplate = @"using MessagePack;
using ProtoBuf;
using System;

namespace Battlehub.Storage.Surrogates{3}
{{
    [ProtoContract]
    [MessagePackObject]
    [Surrogate(typeof({0}), _PROPERTY_INDEX, _TYPE_INDEX{11})]
    public {12} {4}Surrogate<TID> : IValueTypeSurrogate<{0}, TID> where TID : IEquatable<TID>
    {{   
        const int _PROPERTY_INDEX = {1};
        const int _TYPE_INDEX = {2};

        //_PLACEHOLDER_FOR_EXTENSIONS_DO_NOT_DELETE_OR_CHANGE_THIS_LINE_PLEASE
{5}{8}
        public void Serialize(in {0} o, ISerializationContext<TID> ctx)
        {{
            var idmap = ctx.IDMap;
{6}{9}
        }}

        public {0} Deserialize(ISerializationContext<TID> ctx)
        {{
            var idmap = ctx.IDMap;
{7}{10}
            return o;
        }}
    }}
}}
";

        private const string k_propertyIndex = "const int _PROPERTY_INDEX";

        private const string k_propertiesPlaceholder = @"
        //_PLACEHOLDER_FOR_NEW_PROPERTIES_DO_NOT_DELETE_OR_CHANGE_THIS_LINE_PLEASE
";

        private const string k_idPropertyTemplate = @"
        [ProtoMember(2), Key(2)]
        public TID id { get; set; }
";

        private const string k_gameObjectIdPropertyTemplate = @"
        [ProtoMember(3), Key(3)]
        public TID gameObjectId { get; set; }
";

        private const string k_propertyTemplate = @"
        [ProtoMember({0}), Key({0})]
        public {1} {2} {{ get; set; }}
";

        private const string k_globalTemplate = "global::{0}";

        private const string k_tidTemplate = "TID";

        private const string k_nonPrimitiveTemplate = "global::Battlehub.Storage.Surrogates.{0}Surrogate<TID>";

        private const string k_arrayTemplate = "{0}[]";

        private const string k_listTemplate = "global::System.Collections.Generic.List<{0}>";

        private static readonly string k_serializableArrayTemplate = "global::Battlehub.Storage.SerializableArray<{0}>";

        private static readonly string k_serializableListTemplate = "global::Battlehub.Storage.SerializableList<{0}>";

        private const string k_serializePlaceholder = @"
            //_PLACEHOLDER_FOR_SERIALIZE_METHOD_BODY_DO_NOT_DELETE_OR_CHANGE_THIS_LINE_PLEASE
";
        private const string k_deserializePlaceholder = @"
            //_PLACEHOLDER_FOR_DESERIALIZE_METHOD_BODY_DO_NOT_DELETE_OR_CHANGE_THIS_LINE_PLEASE
";
        private const string k_beginSerializationTemplate = @"
            var o = ({0})obj;";

        private const string k_getOrCreateObjIDTemplate = @"
            id = idmap.GetOrCreateID(o);";

        private const string k_getGameObjectIDTemplate = @"
            gameObjectId = idmap.GetOrCreateID(o.gameObject);";

        private const string k_getOrCreateIDTemplate = @"
            {0} = idmap.GetOrCreateID(o.{0});";

        private const string k_getOrCreateIDsTemplate = @"
            {0} = idmap.GetOrCreateIDs(o.{0});";

        private const string k_getPropertyTemplate = @"
            {0} = o.{0};";

        private const string k_serializePropertyTemplate = @"
            {0}.Serialize(o.{0}, ctx);";

        private const string k_serializeArrayOrListPropertyTemplate = @"
            {0} = {0}.Serialize(o.{0}, ctx);";

        private const string k_beginDeserializationTemplate = @"
            var o = idmap.GetOrCreateObject<{0}>(id);";

        private const string k_beginDeserializationNoParameterlessCtorTemplate = @"
            {0} o = idmap.GetObject<{0}>(id); 
            if (o == null)
            {{
                #warning There is no parameterless constructor. Please add code to instantiate {0}.
                idmap.SetID(o, id);
                return default;
            }}";

        private const string k_beginDeserializationStructTemplate = @"
            var o = new {0}();";

        private const string k_getComponentTemplate = @"
            var o = idmap.GetComponent<{0}, TID>(id, gameObjectId);";

        private const string k_getObjectTemplate = @"
            o.{0} = idmap.GetObject<{1}>({0});";

        private const string k_getObjectsTemplate = @"
            o.{0} = idmap.GetObjects<{1}, TID>({0});";

        private const string k_setPropertyTemplate = @"
            o.{0} = {0};";

        private const string k_deserializePropertyTemplate = @"
            o.{0} = {0}.Deserialize(ctx);";

        private const string k_deserializeArrayOrListPropertyTemplate = @"
            o.{0} = {0}.Deserialize(o.{0}, ctx);";

        private const string k_enumeratorTemplate = @"namespace Battlehub.Storage.Enumerators{1}
{{
    [ObjectEnumerator(typeof({0}))]
    public class {2}Enumerator : ObjectEnumerator<{0}>
    {{
        public override bool MoveNext()
        {{
            do
            {{
                switch (Index)
                {{
{3}
                    default:
                        return false;
                }}
            }}
            while (true);
        }}
    }}
}}";

        private const string k_moveNextPropertyTemplate = @"
                    case {0}:
                        if (MoveNext(TypedObject.{1}, {2}))
                            return true;
                        break;";

        private const string k_moveNextObjectTemplate = @"
                    case {0}:
                        if (MoveNext(Object, {1}))
                            return true;
                        break;";


        private SurrogatesGenConfig m_config;

        public SurrogatesGen()
        {
            m_config = SurrogatesGenConfig.Instance;
        }

        public string GetTypeName(Type type)
        {
            string fullName = type.FullName.Replace("+", ".");

            return string.Format(k_globalTemplate, fullName);
        }

        private int GetStartingPropertyIndex(Type type)
        {
            int index = 1;
            if (!IsStruct(type))
            {
                index++;
                if (typeof(Component).IsAssignableFrom(type))
                {
                    index++;
                }
            }
            return index;
        }

        private string GetPropertiesBody(Type type, MemberInfo[] properties)
        {
            var sb = new StringBuilder();

            int index = GetStartingPropertyIndex(type);
            if(!IsStruct(type))
            {
                sb.Append(k_idPropertyTemplate);
                if (typeof(Component).IsAssignableFrom(type))
                {
                    sb.Append(k_gameObjectIdPropertyTemplate);
                }
            }
            
            GetPropertiesBody(properties, sb, index);
            return sb.ToString();
        }

        private int GetPropertiesBody(MemberInfo[] properties, StringBuilder sb, int index)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                index++;
                var property = properties[i];
                var propertyType = GetPropertyType(property);
                string propertyTypeName = GetSurrogateTypeName(propertyType);

                sb.Append(string.Format(k_propertyTemplate, index, propertyTypeName, property.Name));
            }

            return index;
        }

        private string GetSerializeMethodBody(Type type, MemberInfo[] properties)
        {
            var sb = new StringBuilder();

            if (!IsPrimitive(type))
            {
                if (!IsStruct(type))
                {
                    sb.Append(string.Format(k_beginSerializationTemplate, GetTypeName(type)));
                    sb.Append(string.Format(k_getOrCreateObjIDTemplate));
                }

                if (typeof(Component).IsAssignableFrom(type))
                {
                    sb.Append(k_getGameObjectIDTemplate);
                }
            }
            else
            {
                sb.Append(string.Format(k_beginSerializationTemplate, GetTypeName(type)));
            }

            GetSerializeMethodBody(properties, sb);
            return sb.ToString();
        }

        private void GetSerializeMethodBody(MemberInfo[] properties, StringBuilder sb)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var propertyType = GetPropertyType(property);

                if (propertyType.IsArray)
                {
                    var elementType = propertyType.GetElementType();
                    if (IsPrimitive(elementType))
                    {
                        sb.Append(string.Format(k_getPropertyTemplate, property.Name));
                    }
                    else
                    {
                        if (IsStruct(elementType))
                        {
                            sb.Append(string.Format(k_serializeArrayOrListPropertyTemplate, property.Name));
                        }
                        else
                        {
                            sb.Append(string.Format(k_getOrCreateIDsTemplate, property.Name));
                        }
                    }
                }
                else if (IsGenericList(propertyType))
                {
                    var elementType = propertyType.GetGenericArguments().Single();
                    if (IsPrimitive(elementType))
                    {
                        sb.Append(string.Format(k_getPropertyTemplate, property.Name));
                    }
                    else
                    {
                        if (IsStruct(elementType))
                        {
                            sb.Append(string.Format(k_serializeArrayOrListPropertyTemplate, property.Name));
                        }
                        else
                        {
                            sb.Append(string.Format(k_getOrCreateIDsTemplate, property.Name));
                        }
                    }
                }
                else
                {
                    if (IsPrimitive(propertyType))
                    {
                        sb.Append(string.Format(k_getPropertyTemplate, property.Name));
                    }
                    else
                    {
                        if (IsStruct(propertyType))
                        {
                            sb.Append(string.Format(k_serializePropertyTemplate, property.Name));
                        }
                        else
                        {
                            sb.Append(string.Format(k_getOrCreateIDTemplate, property.Name));
                        }
                    }
                }
            }
        }

        private string GetDeserializeMethodBody(Type type, MemberInfo[] properties)
        {
            var sb = new StringBuilder();
            if (typeof(Component).IsAssignableFrom(type))
            {
                sb.Append(string.Format(k_getComponentTemplate, GetTypeName(type)));
            }
            else
            {
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null && !type.IsValueType)
                {
                    sb.Append(string.Format(k_beginDeserializationNoParameterlessCtorTemplate, GetTypeName(type)));
                }
                else
                {
                    if (IsPrimitive(type))
                    {
                        sb.Append(string.Format(k_beginDeserializationStructTemplate, GetTypeName(type)));
                    }
                    else
                    {
                        if (IsStruct(type))
                        {
                            sb.Append(string.Format(k_beginDeserializationStructTemplate, GetTypeName(type)));
                        }
                        else
                        {
                            sb.Append(string.Format(k_beginDeserializationTemplate, GetTypeName(type)));
                        }
                    }
                }
            }

            GetDeserializeMethodBody(properties, sb);
            return sb.ToString();
        }

        private void GetDeserializeMethodBody(MemberInfo[] properties, StringBuilder sb)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var propertyType = GetPropertyType(property);
                if (propertyType.IsArray)
                {
                    var elementType = propertyType.GetElementType();
                    if (IsPrimitive(elementType))
                    {
                        sb.Append(string.Format(k_setPropertyTemplate, property.Name));
                    }
                    else
                    {
                        if (IsStruct(elementType))
                        {
                            sb.Append(string.Format(k_deserializeArrayOrListPropertyTemplate, property.Name));
                        }
                        else
                        {
                            sb.Append(string.Format(k_getObjectsTemplate, property.Name, GetTypeName(elementType)));
                        }
                    }
                }
                else if (IsGenericList(propertyType))
                {
                    var elementType = propertyType.GetGenericArguments().Single();
                    if (IsPrimitive(elementType))
                    {
                        sb.Append(string.Format(k_setPropertyTemplate, property.Name));
                    }
                    else
                    {
                        if (IsStruct(elementType))
                        {
                            sb.Append(string.Format(k_deserializeArrayOrListPropertyTemplate, property.Name));
                        }
                        else
                        {
                            sb.Append(string.Format(k_getObjectsTemplate, property.Name, GetTypeName(elementType)));
                        }
                    }
                }
                else
                {
                    if (IsPrimitive(propertyType))
                    {
                        sb.Append(string.Format(k_setPropertyTemplate, property.Name));
                    }
                    else
                    {
                        if (IsStruct(propertyType))
                        {
                            sb.Append(string.Format(k_deserializePropertyTemplate, property.Name));
                        }
                        else
                        {
                            sb.Append(string.Format(k_getObjectTemplate, property.Name, GetTypeName(propertyType)));
                        }
                    }
                }
            }
        }

        public string GetSurrogateCode(Type type, int typeIndex)
        {
            MemberInfo[] properties = GetSerializableProperties(type).ToArray();

        
            string propertiesBody = GetPropertiesBody(type, properties);
            string serializeMethodBody = GetSerializeMethodBody(type, properties);
            string deserializeMethodBody = GetDeserializeMethodBody(type, properties);

            string typeName = GetTypeName(type);

            int propertyIndex = 1 + properties.Length;
            if (!IsPrimitive(type))
            {
                propertyIndex++;
            }

            if (typeof(Component).IsAssignableFrom(type))
            {
                propertyIndex++;
            }

            string surrogateNamespace = !string.IsNullOrEmpty(type.Namespace) ? $".{type.Namespace}" : string.Empty;
            string surrogateTypeName = type.Name;
            bool isEnabled = !IsPrimitive(type);

            string surrogateTemplate = k_surrogateTemplate;
            bool nonPrimitiveStruct = IsStruct(type) && !IsPrimitive(type);
            if (nonPrimitiveStruct)
            {
                surrogateTemplate = k_valueTypeSurrogateTemplate;
            }

            return string.Format(surrogateTemplate,
                typeName,
                propertyIndex,
                typeIndex,
                surrogateNamespace,
                surrogateTypeName,
                propertiesBody,
                serializeMethodBody,
                deserializeMethodBody,
                k_propertiesPlaceholder,
                k_serializePlaceholder,
                k_deserializePlaceholder,
                isEnabled ? "" : ", enabled:false",
                type.IsValueType ? "struct" : "class");
        }

        private string GetEnumeratorBody(MemberInfo[] properties, int[] keys)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < properties.Length; ++i)
            {
                var property = properties[i];
                int key = keys[i];
                sb.Append(string.Format(k_moveNextPropertyTemplate, i, property.Name, key));
            }

            sb.Append(string.Format(k_moveNextObjectTemplate, properties.Length, -1));

            return sb.ToString();
        }

        private bool EnumerablePropertiesFilter(MemberInfo memberInfo)
        {
            Type propertyType = GetPropertyType(memberInfo);
            if (propertyType.IsArray)
            {
                propertyType = propertyType.GetElementType();
            }
            else if (IsGenericList(propertyType))
            {
                propertyType = propertyType.GetGenericArguments().Single();
            }

            return !IsPrimitive(propertyType); //typeof(UnityEngine.Object).IsAssignableFrom(propertyType);
        }

        private void GetEnumerablePropertiesAndKeys(Type type, out List<MemberInfo> enumerableProperties, out List<int> keys)
        {
            var properties = GetSerializableProperties(type);
            enumerableProperties = new List<MemberInfo>();
            keys = new List<int>();
            int index = GetStartingPropertyIndex(type);

            foreach (var property in properties)
            {
                if (EnumerablePropertiesFilter(property))
                {
                    enumerableProperties.Add(property);
                    keys.Add(index);
                }
                index++;
            }
        }

        public bool CanCreateEnumerator(Type type)
        {
            List<MemberInfo> enumerableProperties;
            List<int> keys;
            GetEnumerablePropertiesAndKeys(type, out enumerableProperties, out keys);
            return enumerableProperties.Count > 0;
        }

        public string GetEnumeratorCode(Type type)
        {
            List<MemberInfo> enumerableProperties;
            List<int> keys;
            GetEnumerablePropertiesAndKeys(type, out enumerableProperties, out keys);

            return GetEnumeratorCode(type, enumerableProperties.ToArray(), keys.ToArray());
        }

        private string GetEnumeratorCode(Type type, MemberInfo[] properties, int[] keys)
        {
            if (properties.Length == 0)
            {
                return string.Empty;
            }

            string typeName = GetTypeName(type);
            string enumeratorNamespace = !string.IsNullOrEmpty(type.Namespace) ? $".{type.Namespace}" : string.Empty;
            string enumeratorTypeName = type.Name;
            string enumeratorBody = GetEnumeratorBody(properties, keys);

            return string.Format(k_enumeratorTemplate, typeName, enumeratorNamespace, enumeratorTypeName, enumeratorBody);
        }

        public bool CanUpdateSurrogate(Type type, Type surrogateType)
        {
            var surrogateAttribute = surrogateType.GetCustomAttribute<SurrogateAttribute>();
            if (!surrogateAttribute.EnableUpdates)
            {
                return false;
            }

            return GetNewSerializableProperties(type, surrogateType).Any();
        }

        public bool CanUpdateSurrogate(Type surrogateType)
        {
            var surrogateAttribute = surrogateType.GetCustomAttribute<SurrogateAttribute>();
            return surrogateAttribute.EnableUpdates;
        }

        public string GetUpdatedSurrogateCode(Type type, Type surrogateType, string currentCode)
        {
            if (!currentCode.Contains(k_propertiesPlaceholder))
            {
                Debug.LogError($"Can't update surrogate code. There is no properties placeholder. {k_propertiesPlaceholder}");
                return currentCode;
            }

            if (!currentCode.Contains(k_serializePlaceholder))
            {
                Debug.LogError($"Can't update surrogate code. There is no properties placeholder. {k_serializePlaceholder}");
                return currentCode;
            }

            if (!currentCode.Contains(k_deserializePlaceholder))
            {
                Debug.LogError($"Can't update surrogate code. There is no properties placeholder. {k_deserializePlaceholder}");
                return currentCode;
            }

            if (!currentCode.Contains(k_propertyIndex))
            {
                Debug.LogError($"Can't update surrogate code. There is {k_propertyIndex} field or it is not properly formatted");
                return currentCode;
            }

            int p0 = currentCode.IndexOf(k_propertyIndex);
            int p1 = currentCode.IndexOf(";", p0);

            var newProperties = GetNewSerializableProperties(type, surrogateType).ToArray();
            var sb = new StringBuilder();
            int lastPropertyIndex = GetPropertiesBody(newProperties, sb, GetLastPropertyIndex(surrogateType));
            string newPropertiesBody = sb.ToString();

            sb.Clear();
            GetSerializeMethodBody(newProperties, sb);
            string newSerializeBody = sb.ToString();

            sb.Clear();
            GetDeserializeMethodBody(newProperties, sb);
            string newDeserializeBody = sb.ToString();

            currentCode = currentCode.Substring(0, p0) + $"{k_propertyIndex} = {lastPropertyIndex}" + currentCode.Substring(p1);

            currentCode = currentCode.Replace(k_propertiesPlaceholder, $"{newPropertiesBody}{k_propertiesPlaceholder}");
            currentCode = currentCode.Replace(k_serializePlaceholder, $"{newSerializeBody}{k_serializePlaceholder}");
            currentCode = currentCode.Replace(k_deserializePlaceholder, $"{newDeserializeBody}{k_deserializePlaceholder}");

            return currentCode;
        }

        public string GetUpdatedEnumeratorCode(Type type, Type surrogateType)
        {
            var existingProperties = GetExistingSerializableProperties(type, surrogateType).ToArray();
            var existingKeys = GetExistingSerializablePropertyKeys(type, surrogateType).ToArray();

            var enumerableProperties = new List<MemberInfo>();
            var keys = new List<int>();

            for (int i = 0; i < existingProperties.Length; ++i)
            {
                var property = existingProperties[i];
                var key = existingKeys[i];

                if (EnumerablePropertiesFilter(property))
                {
                    enumerableProperties.Add(property);
                    keys.Add(key);
                }
            }

            var newProperties = GetNewSerializableProperties(type, surrogateType);
            int index = GetLastPropertyIndex(surrogateType) + 1;

            foreach(var property in newProperties)
            {
                if (EnumerablePropertiesFilter(property))
                {
                    enumerableProperties.Add(property);
                    keys.Add(index);
                }

                index++;
            }

            return GetEnumeratorCode(type, enumerableProperties.ToArray(), keys.ToArray());
        }

        private int GetLastPropertyIndex(Type surrogateType)
        {
            var surrogateAttribute = surrogateType.GetCustomAttribute<SurrogateAttribute>();
            return surrogateAttribute.PropertyIndex;
        }

        private IEnumerable<MemberInfo> GetExistingSerializableProperties(Type type, Type surrogateType)
        {
            var serializableProperties = GetSerializableProperties(type).Select(p => ($"{GetSurrogateTypeName(GetPropertyType(p))} {p.Name}", p)).ToDictionary(pair => pair.Item1, pair => pair.Item2);
            var surrogateProperties = new HashSet<string>(GetSurrogateProperties(surrogateType, includeSpecialProperties: false, withKeyAttributeOnly: true).Select(p => PropertyFullName(p)));
            return serializableProperties.Where(kvp => surrogateProperties.Contains(kvp.Key)).Select(kvp => kvp.Value);
        }

        private IEnumerable<int> GetExistingSerializablePropertyKeys(Type type, Type surrogateType)
        {
            var serializableProperties = GetSerializableProperties(type).Select(p => ($"{GetSurrogateTypeName(GetPropertyType(p))} {p.Name}", p)).ToDictionary(pair => pair.Item1, pair => pair.Item2);
            var surrogateProperties = GetSurrogateProperties(surrogateType, includeSpecialProperties: false, withKeyAttributeOnly: true);
            var surrogatePropertiesToKey =  surrogateProperties.ToDictionary(p => PropertyFullName(p), p => GetKey(p));
            return serializableProperties.Where(kvp => surrogatePropertiesToKey.ContainsKey(kvp.Key)).Select(kvp => surrogatePropertiesToKey[kvp.Key]);
        }

        private IEnumerable<MemberInfo> GetNewSerializableProperties(Type type, Type surrogateType)
        {
            KeyValuePair<string, MemberInfo>[] serializableProperties = GetSerializableProperties(type).Select(p => ($"{GetSurrogateTypeName(GetPropertyType(p))} {p.Name}", p)).ToDictionary(pair => pair.Item1, pair => pair.Item2).ToArray();
            var surrogateProperties = new HashSet<string>(GetSurrogateProperties(surrogateType, includeSpecialProperties: false, withKeyAttributeOnly: false).Select(p => PropertyFullName(p))).ToArray();
            return serializableProperties.Where(kvp => !surrogateProperties.Contains(kvp.Key)).Select(kvp => kvp.Value);
        }

        private IEnumerable<MemberInfo> GetSurrogateProperties(Type type, bool includeSpecialProperties, bool withKeyAttributeOnly)
        {
            var result = GetSerializableProperties(type);
            if (!includeSpecialProperties)
            {
                result = result.Where(p => p.Name != "gameObjectId" && p.Name != "id");
            }

            if (withKeyAttributeOnly)
            {
                result = result.Where(p => p.GetCustomAttributes().Any(a => a.GetType().Name == "KeyAttribute" || a.GetType().Name == "ProtoMemberAttribute"));
            }

            return result;
        }

        private int GetKey(MemberInfo memberInfo)
        {
            // HACK: getting property key value using reflection

            var attribute = memberInfo.GetCustomAttributes().Where(a => a.GetType().Name == "ProtoMemberAttribute").FirstOrDefault();
            if (attribute == null)
            {
                return -1;
            }

            return (int)attribute.GetType().GetProperty("Tag").GetValue(attribute);
        }

        public IEnumerable<MemberInfo> GetSerializableProperties(Type type)
        {
            return m_config.GetSerializableProperties(type);
        }

        private string PropertyFullName(MemberInfo memberInfo)
        {
            var propertyType = GetPropertyType(memberInfo);
            if (propertyType.IsArray)
            {
                if (propertyType.GetElementType().IsGenericParameter)
                {
                    return memberInfo.ToString();
                }
            }
            else if (IsGenericList(propertyType))
            {
                if (propertyType.GetGenericArguments().Single().IsGenericParameter)
                {
                    return $"global::{memberInfo.ToString().Replace("`1[TID]", "<TID>")}";
                }
            }

            if (propertyType.IsGenericParameter)
            {
                return memberInfo.ToString();
            }

            static string GetTypeFullName(Type propertyType)
            {
                string fullName = propertyType.FullName;
                if (!string.IsNullOrEmpty(fullName))
                {
                    fullName = $"global::{fullName.Replace("+", ".")}";
                }
                return fullName;
            }

            if (propertyType.IsGenericType)
            {
                Type[] args = propertyType.GetGenericArguments();

                string genericTypeName = $"global::{propertyType.Namespace}.{propertyType.Name.Replace($"`{args.Length}", "")}<";

                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0)
                    {
                        genericTypeName += ", ";
                    }

                    genericTypeName += GetTypeFullName(args[i]);
                }

                genericTypeName += ">";
                return $"{genericTypeName} {memberInfo.Name}";
            }
            
            string fullName = GetTypeFullName(propertyType);
            return $"{fullName} {memberInfo.Name}";
        }

        private string GetSurrogateTypeName(Type propertyType)
        {
            string propertyTypeName;
            if (propertyType.IsArray)
            {
                var elementType = propertyType.GetElementType();
                if (IsPrimitive(elementType))
                {
                    propertyTypeName = string.Format(k_serializableArrayTemplate, GetTypeName(elementType));
                }
                else
                {
                    if (IsStruct(elementType))
                    {
                        propertyTypeName = string.Format(k_serializableArrayTemplate, string.Format(k_nonPrimitiveTemplate, elementType.FullName));
                    }
                    else
                    {
                        propertyTypeName = string.Format(k_arrayTemplate, k_tidTemplate);
                    }   
                }
            }
            else if(IsGenericList(propertyType))
            {
                var elementType = propertyType.GetGenericArguments().Single();
                if (IsPrimitive(elementType))
                {
                    propertyTypeName = string.Format(k_serializableListTemplate, GetTypeName(elementType));
                }
                else
                {
                    if (IsStruct(elementType))
                    {
                        propertyTypeName = string.Format(k_serializableListTemplate, string.Format(k_nonPrimitiveTemplate, elementType.FullName));
                    }
                    else
                    {
                        propertyTypeName = string.Format(k_listTemplate, k_tidTemplate);
                    }
                }
            }
            else
            {
                if (IsPrimitive(propertyType))
                {
                    propertyTypeName = GetTypeName(propertyType);
                }
                else
                {
                    if (IsStruct(propertyType))
                    {
                        propertyTypeName = string.Format(k_nonPrimitiveTemplate, propertyType.FullName);
                    }
                    else
                    {
                        propertyTypeName = k_tidTemplate;
                    }
                }
            }

            return propertyTypeName;
        }

        public Type GetPropertyType(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                return propertyInfo.PropertyType;
            }

            FieldInfo fieldInfo = (FieldInfo)memberInfo;
            return fieldInfo.FieldType;
        }

        public IEnumerable<Type> GetElementTypes(Type type)
        {
            if (type.IsArray)
            {
                yield return type.GetElementType();
            }
            else if(IsGenericList(type))
            {
                yield return type.GetGenericArguments().Single();
            }
        }

        public Type[] GetDependencies(Type type, bool recursive)
        {
            HashSet<Type> dependencies = new HashSet<Type>();
            GetDependencies(type, dependencies, recursive);
            return dependencies.ToArray();
        }

        private void GetDependencies(Type type, HashSet<Type> dependencies, bool recursive)
        {
            foreach (MemberInfo memberInfo in GetSerializableProperties(type))
            {
                Type propertyType = GetPropertyType(memberInfo);
                if (propertyType == typeof(GameObject))
                {
                    continue;
                }

                if (propertyType.IsArray)
                {
                    propertyType = propertyType.GetElementType();
                }
                else if (IsGenericList(propertyType))
                {
                    propertyType = propertyType.GetGenericArguments().Single();
                }

                if (propertyType.IsEnum || TypeFinder.PrimitiveTypes.Contains(propertyType) || propertyType.IsPrimitive)
                {
                    continue;
                }

                if (dependencies.Add(propertyType) && recursive)
                {
                    GetDependencies(propertyType, dependencies, recursive);
                }
            }
        }

        public bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsEnum;
        }

        public bool IsPrimitiveStruct(Type type)
        {
            bool isStruct = IsStruct(type);
            if (!isStruct)
            {
                return false;
            }

            var serializableProperties = GetSerializableProperties(type);
            return serializableProperties.All(property =>
            {
                var propertyType = GetPropertyType(property);
                return IsPrimitive(propertyType);
            });
        }

        public bool IsPrimitive(Type type)
        {
            return type.IsEnum || TypeFinder.PrimitiveTypes.Contains(type) || type.IsPrimitive || IsPrimitiveStruct(type);
        }

        public bool IsGenericList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

    }
}

