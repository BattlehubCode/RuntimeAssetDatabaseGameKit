using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Battlehub.Storage.Editor
{
    public class UpdateSurrogatesWindow : SurrogatesWindow
    {
        [MenuItem("Tools/Runtime Asset Database/Update Surrogates ...")]
        public static void ShowWindow()
        {
            UpdateSurrogatesWindow wnd = GetWindow<UpdateSurrogatesWindow>();
            wnd.titleContent = new GUIContent("Update Surrogates");   
        }

        protected Dictionary<Type, Type> TypeToEnumeratorType { get; private set; } = new Dictionary<Type, Type>();

        protected override bool EnumeratorTypeFilter(Type type)
        {
            return type != null && type.GetCustomAttribute<ObjectEnumeratorAttribute>() != null && typeof(IObjectEnumerator).IsAssignableFrom(type);
        }

        protected override string UXMLFile => "Editor/Windows/UpdateSurrogatesWindow.uxml";

        protected override bool TypeFilter(Type type)
        {
            return base.TypeFilter(type) && (CanUpdateSurrogate(type) || CanUpdateEnumerator(type));

        }

        private bool CanUpdateSurrogate(Type type)
        {
            return TypeToSurrogateType.TryGetValue(type, out var surrogateType) && 
                SurrogatesGen.CanUpdateSurrogate(type, surrogateType); 
        }

        private bool CanUpdateEnumerator(Type type)
        {
            string generatedDir = StoragePath.GeneratedDataFolder;
            return TypeToSurrogateType.TryGetValue(type, out var surrogateType) &&
                SurrogatesGen.CanUpdateSurrogate(surrogateType) &&
                SurrogatesGen.CanCreateEnumerator(type) && !File.Exists(GetEnumeratorPath(type, generatedDir));
        }

        public override void CreateGUI()
        {
            //EnumeratorTypeFinder.Find();
            //TypeToEnumeratorType.Clear();

            //for (int i = 0; i < EnumeratorTypeFinder.Types.Length; i++)
            //{
            //    Type enumeratorType = EnumeratorTypeFinder.Types[i];
            //    ObjectEnumeratorAttribute enumeratorAttribute = enumeratorType.GetCustomAttribute<ObjectEnumeratorAttribute>();
            //    foreach (Type type in enumeratorAttribute.Types)
            //    {
            //        if (TypeToEnumeratorType.ContainsKey(type))
            //        {
            //            Debug.LogWarning($"Can't add {enumeratorType.FullName}. Enumerator for {type} already exists. " + TypeToEnumeratorType[type].FullName);
            //        }
            //        else
            //        {
            //            TypeToEnumeratorType.Add(type, enumeratorType);
            //        }
            //    }
            //}

            base.CreateGUI();
   
        }

        protected override void OnDefaultAction()
        {
            string generatedDir = StoragePath.GeneratedDataFolder;
            string dir = StoragePath.DataFolder;
            string surrogatesGenConfigPath = $"{dir}/Surrogates/Editor/SurrogatesGenConfig.json";
            Directory.CreateDirectory(Path.GetDirectoryName(surrogatesGenConfigPath));

            SurrogatesGenConfig config = SurrogatesGenConfig.Instance;
            if (File.Exists(surrogatesGenConfigPath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(surrogatesGenConfigPath), config);
            }
            
            try
            {
                var selectedTypes = SelectedTypes;
                var dependenciesHs = GetDependenciesOfSelectedTypes(recursive:false);
                
                foreach (Type type in selectedTypes)
                {
                    if (CanUpdateSurrogate(type))
                    {
                        UpdateSurrogate(type);
                    }

                    UpdateEnumerator(generatedDir, type); 
                }

                foreach (Type type in dependenciesHs)
                {
                    if (!TypeToSurrogateType.ContainsKey(type))
                    {
                        CreateSurrogate(dir, config, type);
                        CreateEnumerator(generatedDir, type);
                    }   
                }
            }
            finally
            {
                base.OnDefaultAction();
            }
        }

        private void UpdateEnumerator(string generatedDir, Type type)
        {
            string enumeratorsPath = $"{generatedDir}/Enumerators";
            Directory.CreateDirectory(Path.GetDirectoryName(enumeratorsPath));

            string enumeratorText = SurrogatesGen.GetUpdatedEnumeratorCode(type, TypeToSurrogateType[type]);
            if (!string.IsNullOrEmpty(enumeratorText))
            {
                string enumeratorPath = GetEnumeratorPath(type, generatedDir);
                File.WriteAllText(enumeratorPath, enumeratorText);
            }
        }

        private string GetEnumeratorPath(Type type, string generatedDir)
        {
            string enumeratorsPath = $"{generatedDir}/Enumerators";
            string enumeratorPath;
            if (TypeToEnumeratorType.TryGetValue(type, out var enumeratorType))
            {
                enumeratorPath = enumeratorType.GetCustomAttribute<ObjectEnumeratorAttribute>().FilePath;
            }
            else
            {
                enumeratorPath = $"{enumeratorsPath}/{type.FullName}Enumerator.cs";
            }

            return enumeratorPath;
        }

        private void UpdateSurrogate(Type type)
        {
            string surrogatePath = TypeToSurrogateType[type].GetCustomAttribute<SurrogateAttribute>().FilePath;
            string surrogateText = File.ReadAllText(surrogatePath);

            surrogateText = SurrogatesGen.GetUpdatedSurrogateCode(type, TypeToSurrogateType[type], surrogateText);

            File.WriteAllText(surrogatePath, surrogateText);
        }
    }
}

