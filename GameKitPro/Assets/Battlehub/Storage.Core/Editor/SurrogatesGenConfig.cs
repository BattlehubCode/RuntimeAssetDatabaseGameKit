using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Battlehub.Storage
{
    [Serializable]
    public class SurrogatesGenConfig
    {
        public static SurrogatesGenConfig Instance = new SurrogatesGenConfig();

        public int TypeIndex = 100;

        protected Type GetPropertyType(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                return ((PropertyInfo)memberInfo).PropertyType;
            }

            FieldInfo fieldInfo = (FieldInfo)memberInfo;
            return fieldInfo.FieldType;
        }

        public virtual IEnumerable<MemberInfo> GetSerializableProperties(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetGetMethod() != null && p.GetSetMethod() != null && p.GetIndexParameters().Length == 0);

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Cast<MemberInfo>();

            var result = fields.Union(properties)
                .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null);

            result = result.Where(p =>
            {
                Type propertyType = GetPropertyType(p);
                if (typeof(IEnumerable).IsAssignableFrom(propertyType))
                {
                    // for now only allow arrays and lists
                    if (propertyType.IsArray)
                    {
                        return true; 
                    }
                    
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        return true;
                    }

                    return false;
                }

                return true;
            });
            

            if (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type))
            {
                result = result.Where(p => p.Name != nameof(UnityEngine.MonoBehaviour.runInEditMode));
                result = result.Where(p => p.Name != nameof(UnityEngine.MonoBehaviour.useGUILayout));
            }

            if (typeof(UnityEngine.Component).IsAssignableFrom(type))
            {
                result = result.Where(p => p.Name != nameof(UnityEngine.MonoBehaviour.name));
                result = result.Where(p => p.Name != nameof(UnityEngine.MonoBehaviour.hideFlags));
                result = result.Where(p => p.Name != nameof(UnityEngine.MonoBehaviour.tag));
            }

            return result;
        }

    }
}
