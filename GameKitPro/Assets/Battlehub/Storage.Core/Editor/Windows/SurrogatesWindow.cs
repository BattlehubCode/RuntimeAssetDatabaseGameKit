using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battlehub.Storage.Editor
{
    public abstract class SurrogatesWindow : EditorWindow
    {
        protected string SearchText { get; private set; }

        protected TypeFinder TypeFinder { get; private set; }

        protected TypeFinder SurrogateTypeFinder { get; private set; }

        protected TypeFinder EnumeratorTypeFinder { get; private set; }

        protected ISurrogatesGen SurrogatesGen { get; private set; }

        private ListView m_listView { get; set; }

        protected HashSet<Type> SelectedTypes { get; private set; } = new HashSet<Type>();

        protected HashSet<Type> GetSelectedTypesAndDependencies(bool recursive)
        {
            HashSet<Type> selectTypesAndDependencies = new HashSet<Type>(SelectedTypes);
            foreach (Type selectedType in SelectedTypes)
            {
                foreach (Type dependencyType in SurrogatesGen.GetDependencies(selectedType, recursive))
                {
                    selectTypesAndDependencies.Add(dependencyType);
                }
            }

            return selectTypesAndDependencies;
        }

        protected HashSet<Type> GetDependenciesOfSelectedTypes(bool recursive)
        {
            var hs = new HashSet<Type>();
            foreach (Type selectedType in SelectedTypes)
            {
                foreach (Type dependencyType in SurrogatesGen.GetDependencies(selectedType, recursive))
                {
                    if (!SelectedTypes.Contains(dependencyType))
                    {
                        hs.Add(dependencyType);
                    }
                }
            }

            return hs;
        }

        protected Dictionary<Type, Type> TypeToSurrogateType { get; private set; } = new Dictionary<Type, Type>();
        protected HashSet<Type> EnumeratorTypes { get; private set; } = new HashSet<Type>();

        protected abstract string UXMLFile
        {
            get;
        }

        protected virtual string UXMLListItemFile
        {
            get { return "Editor/Windows/SurrogateListViewItem.uxml"; }
        }

        protected virtual string PackagePath
        {
            get { return "Packages/net.Battlehub.Storage/"; }
        }

        protected virtual string AssetsPath
        {
            get { return "Assets/Battlehub/Storage.Core/"; }
        }


        protected virtual bool SurrogateTypeFilter(Type type)
        {
            return type != null && type.GetCustomAttribute<SurrogateAttribute>() != null && type.GetInterfaces()
                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISurrogate<>));
        }

        protected virtual bool EnumeratorTypeFilter(Type type)
        {
            return type != null && typeof(IObjectEnumerator).IsAssignableFrom(type);
        }

        protected virtual bool TypeFilter(Type type)
        {
            return type != null && (SearchText == null || type.FullName.ToLower().Contains(SearchText.ToLower()))
                && BaseTypeFinder.DefaultTypeFilter(type);
        }

        protected int GetTypeIndex(SurrogatesGenConfig config, Type type)
        {
            int typeIndex;
            if (TypeToSurrogateType.TryGetValue(type, out Type surrogateType))
            {
                var surrogateAttribute = surrogateType.GetCustomAttribute<SurrogateAttribute>();
                if (surrogateAttribute != null)
                {
                    typeIndex = surrogateAttribute.TypeIndex;
                }
                else
                {
                    typeIndex = config.TypeIndex++;
                }
            }
            else
            {
                typeIndex = config.TypeIndex++;
            }

            return typeIndex;
        }

        private void OnSearch(ChangeEvent<string> evt)
        {
            SearchText = evt.newValue;
            TypeFinder.Find();
            m_listView.itemsSource = TypeFinder.Types;
#if UNITY_2021_3_OR_NEWER
            m_listView.Rebuild();
#endif
        }

        private void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            Toggle toggle = (Toggle)evt.target;
            Type type = (Type)toggle.userData;


            if (evt.newValue)
            {
                SelectedTypes.Add(type);
            }
            else
            {
                SelectedTypes.Remove(type);
            }
        }

        public virtual void CreateGUI()
        {
            SurrogatesGen = new SurrogatesGen();

            SurrogateTypeFinder = new TypeFinder(BaseTypeFinder.DefaultAssemblyFilter, SurrogateTypeFilter);
            SurrogateTypeFinder.Find();
            TypeToSurrogateType.Clear();
            
            for (int i = 0; i < SurrogateTypeFinder.Types.Length; i++)
            {
                Type surrogateType = SurrogateTypeFinder.Types[i];
                SurrogateAttribute surrogateAttribute = surrogateType.GetCustomAttribute<SurrogateAttribute>();
                if (TypeToSurrogateType.ContainsKey(surrogateAttribute.Type))
                {
                    Debug.LogWarning($"Can't add {surrogateType.FullName}. Surrogate for {surrogateAttribute.Type} already exists. " + TypeToSurrogateType[surrogateAttribute.Type].FullName);
                }
                else
                {
                    TypeToSurrogateType.Add(surrogateAttribute.Type, surrogateType);
                }
            }

            EnumeratorTypeFinder = new TypeFinder(BaseTypeFinder.DefaultAssemblyFilter, EnumeratorTypeFilter);
            EnumeratorTypeFinder.Find();
            EnumeratorTypes.Clear();
            for (int i = 0; i < EnumeratorTypeFinder.Types.Length; i++)
            {
                Type enumeratorType = EnumeratorTypeFinder.Types[i];
                EnumeratorTypes.Add(enumeratorType);
            }

            TypeFinder = new TypeFinder(BaseTypeFinder.DefaultAssemblyFilter, TypeFilter);
            TypeFinder.Find();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PackagePath + UXMLFile);
            if (visualTree == null)
            {
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetsPath + UXMLFile);
            }

            var listItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PackagePath + UXMLListItemFile);
            if (listItem == null)
            {
                listItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetsPath + UXMLListItemFile);
            }

            VisualElement main = visualTree.Instantiate();
            rootVisualElement.Add(main);
            rootVisualElement.style.flexGrow = 1.0f;
            main.style.flexGrow = 1.0f;

            Func<VisualElement> makeItem = () => listItem.Instantiate();
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var toggle = e.Q<Toggle>();

                toggle.UnregisterValueChangedCallback(OnToggleValueChanged);
                toggle.RegisterValueChangedCallback(OnToggleValueChanged);

                Type type = TypeFinder.Types[i];
                toggle.text = $" {type.Name}";
                toggle.userData = type;
                toggle.SetValueWithoutNotify(SelectedTypes.Contains(type));

                var nslabel = e.Q<Label>("ns-label");
                nslabel.text = $"({type.Namespace})";
            };

            const int itemHeight = 25;

            m_listView = main.Q<ListView>();
#if UNITY_2021_3_OR_NEWER
            m_listView.fixedItemHeight = itemHeight;
#else
            m_listView.itemHeight = itemHeight;
#endif
            m_listView.makeItem = makeItem;
            m_listView.bindItem = bindItem;
            m_listView.itemsSource = TypeFinder.Types;
            m_listView.selectionType = SelectionType.None;
            m_listView.style.flexGrow = 1.0f;

            var search = main.Q<ToolbarSearchField>();
            search.SetValueWithoutNotify(SearchText);
            search.RegisterValueChangedCallback(OnSearch);

            var createButton = main.Q<Button>("default-action-button");
            createButton.clicked += OnDefaultAction;
        }

        protected virtual void OnDefaultAction()
        {
            AssetDatabase.Refresh();
            SelectedTypes.Clear();
            m_listView.itemsSource = TypeFinder.Types;
        }

        protected void CreateSurrogate(string dir, SurrogatesGenConfig config, Type type)
        {
            string surrogatesPath = $"{dir}/Surrogates";
            Directory.CreateDirectory(surrogatesPath);

            int typeIndex = GetTypeIndex(config, type);

            string surrogateText = SurrogatesGen.GetSurrogateCode(type, typeIndex);
            string surrogatePath = $"{surrogatesPath}/{type.FullName}Surrogate.cs";
            File.WriteAllText(surrogatePath, surrogateText);
        }

        protected void CreateEnumerator(string generatedDir, Type type)
        {
            string enumeratorsPath = $"{generatedDir}/Enumerators";
            Directory.CreateDirectory(enumeratorsPath);

            string enumeratorText = SurrogatesGen.GetEnumeratorCode(type);
            if (!string.IsNullOrEmpty(enumeratorText))
            {
                string enumeratorPath = $"{enumeratorsPath}/{type.FullName}Enumerator.cs";
                File.WriteAllText(enumeratorPath, enumeratorText);
            }
        }
    }
}
