using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.Storage
{
    public interface IShaderUtil
    {
        RTShaderInfo GetShaderInfo(Shader shader);
    }

    public class RTShaderUtil : IShaderUtil
    {
        private Dictionary<string, RTShaderInfo> m_nameToShaderInfo;

        public RTShaderUtil() : this(Resources.Load<RTShaderProfilesAsset>("ShaderProfiles"), false)
        {
        }

        public RTShaderUtil(RTShaderProfilesAsset asset) : this(asset, false)
        {
        }

        private RTShaderUtil(RTShaderProfilesAsset asset, bool showWarning)
        {
            m_nameToShaderInfo = new Dictionary<string, RTShaderInfo>();
            if (asset == null)
            {
                if (showWarning)
                {
                    Debug.LogWarning("Unable to find ShaderProfilesAsset. Click Tools->Runtime Asset Database->Create Shader Profiles");
                }
                return;
            }
            for (int i = 0; i < asset.ShaderInfo.Count; ++i)
            {
                RTShaderInfo info = asset.ShaderInfo[i];
                if (info != null)
                {
                    if (m_nameToShaderInfo.ContainsKey(info.Name))
                    {
                        //Debug.LogWarning("Shader with " + info.Name + " already exists.");
                    }
                    else
                    {
                        m_nameToShaderInfo.Add(info.Name, info);
                    }
                }
            }
        }

        public RTShaderInfo GetShaderInfo(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }

            if (m_nameToShaderInfo.TryGetValue(shader.name, out var shaderInfo))
            {
                return shaderInfo;
            }
            return null;
        }
    }
}


