using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.Storage
{
    public enum RPType
    {
        Unknown,
        Standard,
        HDRP,
        URP,
    }

    public enum GraphicsQuality
    {
        High,
        Medium,
        Low
    }


    public static class RenderPipelineInfo
    {
        public static bool ForceUseRenderTextures = false;
        public static bool UseRenderTextures
        {
            get { return ForceUseRenderTextures; }
        }

        /// <summary>
        /// Forces the user interface to be rendered in the foreground layer using a Raw Image.
        /// Set to true by default for Universal RP and HDRP, ignored when using standard RP
        /// </summary>
        public static bool UseForegroundLayerForUI = true;

        public static readonly RPType Type;
        public static readonly string DefaultShaderName;
        public static readonly string DefaultTerrainShaderName;
        public static readonly int ColorPropertyID;
        public static readonly int MainTexturePropertyID;

        private static readonly PropertyInfo m_msaaProperty;
        public static int MSAASampleCount
        {
            get
            {
                switch (Type)
                {
                    case RPType.Standard:
                        return QualitySettings.antiAliasing;
                    default:
                        if (m_msaaProperty != null)
                        {
                            try
                            {
                                return Convert.ToInt32(m_msaaProperty.GetValue(GraphicsSettings.renderPipelineAsset));
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }
                        return 1;
                }
            }
        }


        private static Material m_defaultMaterial;
        public static Material DefaultMaterial
        {
            get
            {
                if (m_defaultMaterial == null)
                {
                    m_defaultMaterial = new Material(Shader.Find(DefaultShaderName));
                    m_defaultMaterial.Color(Color.white);
                }

                return m_defaultMaterial;
            }
        }

        private static Material m_defaultTerrainMaterial;
        public static Material DefaultTerrainMaterial
        {
            get
            {
                if (m_defaultTerrainMaterial == null)
                {
                    m_defaultTerrainMaterial = new Material(Shader.Find(DefaultTerrainShaderName));
                }
                return m_defaultTerrainMaterial;
            }
        }

        private static string[] m_builtInRenderPipelineAssetNames;
        public static bool IsBuiltInRendererPipelineAssetName(string name)
        {
            if (m_builtInRenderPipelineAssetNames == null)
            {
                return false;
            }

            return Array.IndexOf(m_builtInRenderPipelineAssetNames, name) >= 0;
        }

        public static string GetBuiltInRendererPipelineAssetName(GraphicsQuality quality)
        {
            if (m_builtInRenderPipelineAssetNames == null)
            {
                return null;
            }

            switch (quality)
            {
                case GraphicsQuality.High:
                    return m_builtInRenderPipelineAssetNames[0];
                case GraphicsQuality.Medium:
                    return m_builtInRenderPipelineAssetNames[1];
                case GraphicsQuality.Low:
                    return m_builtInRenderPipelineAssetNames[2];
                default:
                    return null;
            }
        }

        public static RenderPipelineAsset LoadBuiltInRendererPipelineAsset(GraphicsQuality graphicsQuality)
        {
            string pipelineAssetName = GetBuiltInRendererPipelineAssetName(graphicsQuality);
            if (pipelineAssetName == null)
            {
                return null;
            }

            return Resources.Load<RenderPipelineAsset>(pipelineAssetName);
        }

        static RenderPipelineInfo()
        {
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                Type = RPType.Standard;
                DefaultShaderName = "Standard";
                ColorPropertyID = Shader.PropertyToID("_Color");
                MainTexturePropertyID = Shader.PropertyToID("_MainTex");
            }
            else
            {
                Type pipelineType = GraphicsSettings.renderPipelineAsset.GetType();
                if (pipelineType.Name == "UniversalRenderPipelineAsset")
                {
                    Type = RPType.URP;
                    m_msaaProperty = pipelineType.GetProperty("msaaSampleCount");
                    DefaultShaderName = "Universal Render Pipeline/Lit";
                    DefaultTerrainShaderName = "Universal Render Pipeline/Terrain/Lit";
                    ColorPropertyID = Shader.PropertyToID("_BaseColor");
                    MainTexturePropertyID = Shader.PropertyToID("_BaseMap");

                    m_builtInRenderPipelineAssetNames = new[]
                    {
                        "HighQuality_UniversalRenderPipelineAsset",
                        "MidQuality_UniversalRenderPipelineAsset",
                        "LowQuality_UniversalRenderPipelineAsset"
                    };

#if UNITY_2019
                    //Unity2019_UITransparencyFix();
#endif

                }
                else if (pipelineType.Name == "HDRenderPipelineAsset")
                {
                    Type = RPType.HDRP;
                    m_msaaProperty = pipelineType.GetProperty("msaaSampleCount");
                    DefaultShaderName = "HDRP/Lit";
                    DefaultTerrainShaderName = "HDRP/TerrainLit";
                    ColorPropertyID = Shader.PropertyToID("_BaseColor");
                    MainTexturePropertyID = Shader.PropertyToID("_BaseColorMap");

                }
                else
                {
                    Debug.Log(GraphicsSettings.renderPipelineAsset.GetType());
                    Type = RPType.Unknown;
                    ColorPropertyID = Shader.PropertyToID("_Color");
                    MainTexturePropertyID = Shader.PropertyToID("_MainTex");
                }
            }
        }
    }

    internal static class MaterialExt
    {
        public static int MainTexturePropertyID = Shader.PropertyToID("_MainTex");
        public static int ColorPropertyID = Shader.PropertyToID("_Color");

        public static Texture MainTexture(this Material material)
        {
            if (material.HasProperty(RenderPipelineInfo.MainTexturePropertyID))
            {
                return material.GetTexture(RenderPipelineInfo.MainTexturePropertyID);
            }
            else if (material.HasProperty(MainTexturePropertyID))
            {
                return material.GetTexture(MainTexturePropertyID);
            }
            return null;
        }

        public static void MainTexture(this Material material, Texture texture)
        {
            if (material.HasProperty(RenderPipelineInfo.MainTexturePropertyID))
            {
                material.SetTexture(RenderPipelineInfo.MainTexturePropertyID, texture);
            }
            else if (material.HasProperty(MainTexturePropertyID))
            {
                material.SetTexture(MainTexturePropertyID, texture);
            }
        }

        public static Vector2 MainTextureScale(this Material material)
        {
            if (material.HasProperty(RenderPipelineInfo.MainTexturePropertyID))
            {
                return material.GetTextureScale(RenderPipelineInfo.MainTexturePropertyID);
            }
            else if (material.HasProperty(MainTexturePropertyID))
            {
                return material.GetTextureScale(MainTexturePropertyID);
            }
            return Vector2.one;
        }

        public static void MainTextureScale(this Material material, Vector2 scale)
        {
            if (material.HasProperty(RenderPipelineInfo.MainTexturePropertyID))
            {
                material.SetTextureScale(RenderPipelineInfo.MainTexturePropertyID, scale);
            }
            else if (material.HasProperty(MainTexturePropertyID))
            {
                material.SetTextureScale(MainTexturePropertyID, scale);
            }
        }

        public static Vector2 MainTextureOffset(this Material material)
        {
            if (material.HasProperty(RenderPipelineInfo.MainTexturePropertyID))
            {
                return material.GetTextureOffset(RenderPipelineInfo.MainTexturePropertyID);
            }
            else if (material.HasProperty(MainTexturePropertyID))
            {
                return material.GetTextureOffset(MainTexturePropertyID);
            }
            return Vector2.zero;
        }

        public static void MainTextureOffset(this Material material, Vector2 offset)
        {
            if (material.HasProperty(RenderPipelineInfo.MainTexturePropertyID))
            {
                material.SetTextureOffset(RenderPipelineInfo.MainTexturePropertyID, offset);
            }
            else if (material.HasProperty(MainTexturePropertyID))
            {
                material.SetTextureOffset(MainTexturePropertyID, offset);
            }
        }

        public static Color Color(this Material material)
        {
            if (material.HasProperty(RenderPipelineInfo.ColorPropertyID))
            {
                return material.GetColor(RenderPipelineInfo.ColorPropertyID);
            }
            else if (material.HasProperty(ColorPropertyID))
            {
                return material.GetColor(ColorPropertyID);
            }
            return UnityEngine.Color.white;
        }

        public static void Color(this Material material, Color color)
        {
            if (material.HasProperty(RenderPipelineInfo.ColorPropertyID))
            {
                material.SetColor(RenderPipelineInfo.ColorPropertyID, color);
            }
            else if (material.HasProperty(ColorPropertyID))
            {
                material.SetColor(ColorPropertyID, color);
            }
        }
    }

    public interface IMaterialUtils
    {
        void SetMaterialKeywords(Material material);
    }


    public class URPLitMaterialUtils : IMaterialUtils
    {
#if UNIVERSAL_RP
        public enum WorkflowMode
        {
            Specular = 0,
            Metallic
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        public enum SurfaceType
        {
            Opaque = 0,
            Transparent = 1
        }
        public enum BlendMode
        {
            Alpha = 0,
            Premultiply = 1,
            Additive = 2,
            Multiply = 3
        }
        public enum SmoothnessSource
        {
            BaseAlpha = 0,
            SpecularAlpha = 1
        }
        public enum RenderFace
        {
            Both = 0,
            Back = 1,
            Front = 2
        }

        public void SetMaterialKeywords(Material material)
        {
            if (material == null || material.shader == null || material.shader.name != RenderPipelineInfo.DefaultShaderName)
            {
                return;
            }

            SetMaterialKeywords(material, _SetMaterialKeywords);
            if (material.Color().a == 1)
            {
                material.SetShaderPassEnabled("ShadowCaster", true);
            }
        }

        private const int queueOffsetRange = 50;

        public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;

            return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        private static void _SetMaterialKeywords(Material material)
        {
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            var hasGlossMap = false;
            var isSpecularWorkFlow = false;
            var opaque = ((SurfaceType)material.GetFloat("_Surface") ==
                          SurfaceType.Opaque);
            if (material.HasProperty("_WorkflowMode"))
            {
                isSpecularWorkFlow = (WorkflowMode)material.GetFloat("_WorkflowMode") == WorkflowMode.Specular;
                if (isSpecularWorkFlow)
                    hasGlossMap = material.GetTexture("_SpecGlossMap") != null;
                else
                    hasGlossMap = material.GetTexture("_MetallicGlossMap") != null;
            }
            else
            {
                hasGlossMap = material.GetTexture("_MetallicGlossMap") != null;
            }

            CoreUtils.SetKeyword(material, "_SPECULAR_SETUP", isSpecularWorkFlow);

            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", hasGlossMap);

            if (material.HasProperty("_SpecularHighlights"))
                CoreUtils.SetKeyword(material, "_SPECULARHIGHLIGHTS_OFF",
                    material.GetFloat("_SpecularHighlights") == 0.0f);
            if (material.HasProperty("_EnvironmentReflections"))
                CoreUtils.SetKeyword(material, "_ENVIRONMENTREFLECTIONS_OFF",
                    material.GetFloat("_EnvironmentReflections") == 0.0f);
            if (material.HasProperty("_OcclusionMap"))
                CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                CoreUtils.SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                    GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha && opaque);
            }
        }

        public static void SetMaterialKeywords(Material material, Action<Material> shadingModelFunc = null, Action<Material> shaderFunc = null)
        {
            // Clear all keywords for fresh start
            material.shaderKeywords = null;
            // Setup blending - consistent across all Universal RP shaders
            SetupMaterialBlendMode(material);
            // Receive Shadows
            if (material.HasProperty("_ReceiveShadows"))
                CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);
            // Emission
            //if (material.HasProperty("_EmissionColor"))
            //MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled =
                (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            if (material.HasProperty("_EmissionEnabled") && !shouldEmissionBeEnabled)
                shouldEmissionBeEnabled = material.GetFloat("_EmissionEnabled") >= 0.5f;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
            // Normal Map
            if (material.HasProperty("_BumpMap"))
                CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
            // Shader specific keyword functions
            shadingModelFunc?.Invoke(material);
            shaderFunc?.Invoke(material);
        }

        public static void SetupMaterialBlendMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool alphaClip = material.GetFloat("_AlphaClip") == 1;
            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            var queueOffset = 0; // queueOffsetRange;
            if (material.HasProperty("_QueueOffset"))
                queueOffset = queueOffsetRange - (int)material.GetFloat("_QueueOffset");

            SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
            if (surfaceType == SurfaceType.Opaque)
            {
                if (alphaClip)
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    material.SetOverrideTag("RenderType", "Opaque");
                }
                material.renderQueue += queueOffset;
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.SetShaderPassEnabled("ShadowCaster", true);
            }
            else
            {
                BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");
                var queue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // Specific Transparent Mode Settings
                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Premultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Additive:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Multiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.EnableKeyword("_ALPHAMODULATE_ON");
                        break;
                }
                // General Transparent Material Settings
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_ZWrite", 0);
                material.renderQueue = queue + queueOffset;
                material.SetShaderPassEnabled("ShadowCaster", false);
            }
        }
#else
        public void SetMaterialKeywords(Material material)
        {
        }
#endif
    }


    public class StandardMaterialUtils : IMaterialUtils
    {
        public void SetMaterialKeywords(Material material)
        {
            if (material == null || material.shader == null || (material.shader.name != "Standard" && material.shader.name != "Standard (Specular setup)"))
            {
                return;
            }

            SetupMaterialWithBlendMode(material, GetBlendMode(material));
            SetMaterialKeywords(material, material.shader.name == "Standard" ? WorkflowMode.Metallic : WorkflowMode.Specular);
        }

        public enum WorkflowMode
        {
            Specular,
            Metallic,
            Dielectric
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,       // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        public enum UVSec
        {
            UV0 = 0,
            UV1 = 1
        }

        public static BlendMode GetBlendMode(Material material)
        {
            return (BlendMode)material.GetFloat("_Mode");
        }

        public static void SetBlendMode(Material material, BlendMode blendMode)
        {
            material.SetFloat("_Mode", (float)blendMode);
        }

        public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;
            else
                return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        public static void SetSmoothnessMapChannel(Material material, SmoothnessMapChannel channel)
        {
            material.SetFloat("_SmoothnessTextureChannel", (int)channel);
        }

        public static bool ShouldEmissionBeEnabled(Material mat, Color color)
        {
            var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            return color.maxColorComponent > 0.1f / 255.0f || realtimeEmission;
        }

        public static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
        {
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
            if (workflowMode == WorkflowMode.Specular)
                SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            else if (workflowMode == WorkflowMode.Metallic)
                SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
            SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

            bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(material, material.GetColor("_EmissionColor"));
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }

            // Setup lightmap emissive flags
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (!shouldEmissionBeEnabled)
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }

        public static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
            }
        }
    }
}

