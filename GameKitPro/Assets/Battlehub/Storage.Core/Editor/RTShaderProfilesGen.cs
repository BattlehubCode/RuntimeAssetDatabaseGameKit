using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.Storage
{
    public class RTShaderProfilesGen
    {
        private static readonly Dictionary<ShaderUtil.ShaderPropertyType, RTShaderPropertyType> m_typeToType = new Dictionary<ShaderUtil.ShaderPropertyType, RTShaderPropertyType>
            { { ShaderUtil.ShaderPropertyType.Color, RTShaderPropertyType.Color },
              { ShaderUtil.ShaderPropertyType.Float, RTShaderPropertyType.Float },
              { ShaderUtil.ShaderPropertyType.Range, RTShaderPropertyType.Range },
              { ShaderUtil.ShaderPropertyType.TexEnv, RTShaderPropertyType.TexEnv },
              { ShaderUtil.ShaderPropertyType.Vector, RTShaderPropertyType.Vector }};


        //[MenuItem("Tools/Runtime Asset Database/Create Shader Proflies")]
        public static RTShaderProfilesAsset Generate()
        {
            RTShaderProfilesAsset asset = Create();
            string dir = StoragePath.GeneratedDataFolder;

            if (!Directory.Exists(Path.GetFullPath(dir)))
            {
                Directory.CreateDirectory(Path.GetFullPath(dir));
            }

            if (!Directory.Exists(Path.GetFullPath(dir + "/Resources")))
            {
                UnityEditor.AssetDatabase.CreateFolder(dir, "Resources");
            }

            string path = $"{dir}/Resources/ShaderProfiles.asset";

            UnityEditor.AssetDatabase.DeleteAsset(path);
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            return asset;
        }

        private static RTShaderProfilesAsset Create()
        {
            RTShaderProfilesAsset asset = ScriptableObject.CreateInstance<RTShaderProfilesAsset>();
            asset.ShaderInfo = new List<RTShaderInfo>();

            ShaderInfo[] allShaderInfo = ShaderUtil.GetAllShaderInfo().OrderBy(si => si.name).ToArray();
            HashSet<string> shaderNames = new HashSet<string>();
            for (int i = 0; i < allShaderInfo.Length; ++i)
            {
                ShaderInfo shaderInfo = allShaderInfo[i];
                Shader shader = Shader.Find(shaderInfo.name);

                RTShaderInfo profile = Create(shader);
                asset.ShaderInfo.Add(profile);

                if (shaderNames.Contains(shaderInfo.name))
                {
                    Debug.LogWarning("Shader with same name already exists. Consider renaming " + shaderInfo.name + " shader. File: " + UnityEditor.AssetDatabase.GetAssetPath(shader));
                }
                else
                {
                    shaderNames.Add(shaderInfo.name);
                }
            }
            return asset;
        }

        private static RTShaderInfo Create(Shader shader)
        {
            if (shader == null)
            {
                throw new System.ArgumentNullException("shader");
            }

            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            RTShaderInfo shaderInfo = new RTShaderInfo();
            shaderInfo.Name = shader.name;
            shaderInfo.PropertyCount = propertyCount;
            shaderInfo.PropertyDescriptions = new string[propertyCount];
            shaderInfo.PropertyNames = new string[propertyCount];
            shaderInfo.PropertyRangeLimits = new RTShaderInfo.RangeLimits[propertyCount];
            shaderInfo.PropertyTexDims = new TextureDimension[propertyCount];
            shaderInfo.PropertyTypes = new RTShaderPropertyType[propertyCount];
            shaderInfo.IsHidden = new bool[propertyCount];

            for (int i = 0; i < propertyCount; ++i)
            {
                shaderInfo.PropertyDescriptions[i] = ShaderUtil.GetPropertyDescription(shader, i);
                shaderInfo.PropertyNames[i] = ShaderUtil.GetPropertyName(shader, i);

                try
                {
                    ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, i);
                    if (propType == ShaderUtil.ShaderPropertyType.Range)
                    {
                        shaderInfo.PropertyRangeLimits[i] = new RTShaderInfo.RangeLimits(
                            ShaderUtil.GetRangeLimits(shader, i, 0),
                            ShaderUtil.GetRangeLimits(shader, i, 1),
                            ShaderUtil.GetRangeLimits(shader, i, 2));
                    }
                    else
                    {
                        shaderInfo.PropertyRangeLimits[i] = new RTShaderInfo.RangeLimits();
                    }

                    if (propType == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        shaderInfo.PropertyTexDims[i] = ShaderUtil.GetTexDim(shader, i);
                    }
                    else
                    {
                        shaderInfo.PropertyTexDims[i] = TextureDimension.None;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }

                RTShaderPropertyType rtType = RTShaderPropertyType.Unknown;
                ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(shader, i);
                if (m_typeToType.ContainsKey(type))
                {
                    rtType = m_typeToType[type];
                }

                shaderInfo.PropertyTypes[i] = rtType;
                shaderInfo.IsHidden[i] = ShaderUtil.IsShaderPropertyHidden(shader, i);
            }
            return shaderInfo;
        }
    }
}
