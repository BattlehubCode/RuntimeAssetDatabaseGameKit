using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Battlehub.Storage
{
    public class ObjectEnumeratorFactoryGen
    {
        private static readonly string nl = Environment.NewLine;

        private static readonly string s_template =
            "// This file is autogenerated using Battlehub.Storage.ObjectEnumeratorFactoryGen" + nl +
            "namespace Battlehub.Storage" + nl +
            "{{" + nl +
            "   public class ObjectEnumeratorFactory : ObjectEnumeratorFactoryBase" + nl +
            "   {{" + nl +
            "       public ObjectEnumeratorFactory(IShaderUtil shaderUtil) : base(shaderUtil)" + nl +
            "       {{" + nl +
            "{0}" +
            "       }}" + nl +
            "   }}" + nl +
            "}}";

        private static readonly string s_registerEnumeratorTemplate =
            "           Register(typeof({0}), typeof({1}));" + nl;

        
        //[MenuItem("Tools/Runtime Asset Database/Create Object Enumerator Factory")]
        public static void Generate()
        {
            var types = ReflectionHelpers.GetAllTypesImplementingInterface(typeof(IObjectEnumerator));
            var typeToEnumeratorType = new Dictionary<Type, string>();
            foreach (var enumeratorType in types)
            {
                ObjectEnumeratorAttribute attribute = (ObjectEnumeratorAttribute)Attribute.GetCustomAttribute(enumeratorType, typeof(ObjectEnumeratorAttribute));
                if (attribute == null)
                {
                    continue;
                }

                foreach(Type type in attribute.Types)
                {
                    if (typeToEnumeratorType.TryGetValue(type, out var existingType))
                    {
                        Debug.LogError($"{existingType} already registered for type {type.FullName}");
                        continue;
                    }

                    typeToEnumeratorType.Add(type, $"global::{enumeratorType.FullName}");
                }
            }

            var registerEnumeratorsBody = new StringBuilder();
            
            foreach (var kvp in typeToEnumeratorType.OrderBy(kvp => kvp.Key.Name))
            {
                string typeName = $"global::{kvp.Key.FullName.Replace("+", ".")}";
                string enumeratorTypeName = kvp.Value;
                
                registerEnumeratorsBody.Append(string.Format(s_registerEnumeratorTemplate, typeName, enumeratorTypeName));
            }

            string code = string.Format(
                s_template,
                registerEnumeratorsBody.ToString());

            string dir = StoragePath.GeneratedDataFolder;
            string path = $"{dir}/ObjectEnumeratorFactory.cs";

            File.WriteAllText(path, code);
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}