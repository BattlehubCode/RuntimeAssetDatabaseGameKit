using UnityEditor;
using UnityEngine;
using System;
using System.IO;

namespace Battlehub.Storage.Editor
{
    public class CreateSurrogatesWindow : SurrogatesWindow
    {
        [MenuItem("Tools/Runtime Asset Database/Create Surrogates ...")]
        public static void ShowWindow()
        {
            CreateSurrogatesWindow wnd = GetWindow<CreateSurrogatesWindow>();
            wnd.titleContent = new GUIContent("Create Surrogates");
        }

        protected override string UXMLFile => "Editor/Windows/CreateSurrogatesWindow.uxml";

        protected override bool TypeFilter(Type type)
        {
            return base.TypeFilter(type) && !TypeToSurrogateType.TryGetValue(type, out _) && !EnumeratorTypes.Contains(type);
        }

        protected override void OnDefaultAction()
        {
            string dir = StoragePath.DataFolder;
            string generatedDir = StoragePath.GeneratedDataFolder;

            string surrogatesGenConfigPath = $"{dir}/Surrogates/Editor/SurrogatesGenConfig.json";
            Directory.CreateDirectory(Path.GetDirectoryName(surrogatesGenConfigPath));

            SurrogatesGenConfig config = SurrogatesGenConfig.Instance;
            if (File.Exists(surrogatesGenConfigPath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(surrogatesGenConfigPath), config);
            }

            try
            {
                var selectTypesAndDependencies = GetSelectedTypesAndDependencies(recursive: true);
                foreach (Type type in selectTypesAndDependencies)
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
                File.WriteAllText(surrogatesGenConfigPath, JsonUtility.ToJson(config));
                base.OnDefaultAction();
            }
        }
    }
}

