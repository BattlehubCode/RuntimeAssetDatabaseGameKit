using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.Storage
{
    public static class TextureUtils
    {
        private static ComputeShader s_hasTransparencyCompute;
        private static ComputeShader HasTransparencyCompute
        {
            get
            {
                if (s_hasTransparencyCompute == null)
                {
                    s_hasTransparencyCompute = Resources.Load<ComputeShader>("HasTransparencyCompute");
                }
                return s_hasTransparencyCompute;
            }
        }

        public static int CountTransparentPixels(Texture texture)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("!supportsComputeShader");
                throw new System.NotSupportedException();
            }

            ComputeShader cShader = HasTransparencyCompute;
            return Count(texture, cShader);
        }

        public static bool HasTransparentPixels(Texture texture)
        {
            return CountTransparentPixels(texture) > 0;
        }

        private static ComputeShader s_isNormalMapCompute;
        private static ComputeShader IsNormalMapCompute
        {
            get
            {
                if (s_isNormalMapCompute == null)
                {
                    s_isNormalMapCompute = Resources.Load<ComputeShader>("IsNormalMapCompute");
                }
                return s_isNormalMapCompute;
            }
        }

        private static bool IsNormalMapGpu(Texture texture)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("!supportsComputeShader");
                throw new System.NotSupportedException();
            }

            ComputeShader cShader = IsNormalMapCompute;
            return Count(texture, cShader) > (texture.width * texture.height * 0.8f);
        }

        private static int Count(Texture texture, ComputeShader cShader)
        {
            int kernalMain = cShader.FindKernel("CSMain");
            int kernalInit = cShader.FindKernel("CSInit");
            ComputeBuffer cBuffer = new ComputeBuffer(1, sizeof(int));
            int[] res = new int[1];
            int width = texture.width;
            int height = texture.height;

            cShader.SetTexture(kernalMain, "InputImage", texture);
            cShader.SetTexture(kernalInit, "InputImage", texture);
            cShader.SetBuffer(kernalMain, "ResultBuffer", cBuffer);
            cShader.SetBuffer(kernalInit, "ResultBuffer", cBuffer);

            cShader.Dispatch(kernalInit, 1, 1, 1);
            cShader.Dispatch(kernalMain, Mathf.Max(1, width / 8), Mathf.Max(1, height / 8), 1);

            cBuffer.GetData(res);
            cBuffer.Release();

            return res[0];
        }

        private static ValueTask<bool> IsNormalMapCpu(Texture2D texture)
        {
            //if(SystemInfo.supportsAsyncGPUReadback)
            //{
            //    return IsNormalMapCpuAsync(texture);
            //}

            if (texture.isReadable)
            {
                return new ValueTask<bool>(IsNormalMapCpuSlow(texture));
            }

            Texture2D readableTexture = MakeReadable(texture);

            bool isNormalMap = IsNormalMapCpuSlow(readableTexture);

            Object.Destroy(readableTexture);

            return new ValueTask<bool>(isNormalMap);
        }


        private static bool IsNormalMapCpuSlow(Texture2D texture)
        {
            int w = texture.width;
            int h = texture.height;
            int unitVectors = 0;
            for (int i = 0; i < 100; ++i)
            {
                Color color = texture.GetPixel(Random.Range(0, w), Random.Range(0, h));

                Vector3 n = new Vector3(color.a * 2 - 1, color.g * 2 - 1);
                n.z = Mathf.Sqrt(1 - Vector2.Dot(n, n));

                Vector3 normal = new Vector3(n.x, n.z, n.y);
                if (Mathf.Approximately(normal.magnitude, 1))
                {
                    unitVectors++;
                }
            }

            return unitVectors > Mathf.Min(w * h * 0.8f, 80);
        }

        public static ValueTask<bool> IsNormalMap(Texture2D texture)
        {
            if(SystemInfo.supportsComputeShaders)
            {
                return new ValueTask<bool>(IsNormalMapGpu(texture));
            }

            return IsNormalMapCpu(texture);
        }

        public static Texture2D MakeReadable(Texture2D source)
        {
            var rtReadWrite = RenderTextureReadWrite.sRGB;
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        rtReadWrite);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        private static async Task<byte[]> AsyncGpuReadback(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;


            using (var buffer = new NativeArray<byte>(height * width * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory))
            {
                bool readCompleted = false;

                var bufferref = buffer;
                AsyncGPUReadback.RequestIntoNativeArray(ref bufferref, texture, 0, request =>
                {
                    readCompleted = true;
                    if (request.hasError)
                    {
                        Debug.Log("GPU readback error detected.");
                        return;
                    }
                });

                while (!readCompleted)
                {
                    await Task.Yield();
                }

                return buffer.ToArray();
            }
        }


        
        private static bool s_useAsyncReadback = true;
        public static Task<byte[]> Encode(Texture2D texture)
        {
            bool hasAlpha = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(texture.graphicsFormat);
            return Encode(texture, hasAlpha);
        }

        public static async Task<byte[]> Encode(Texture2D texture, bool hasAlpha)
        {
            byte[] data;
            if (s_useAsyncReadback && SystemInfo.supportsAsyncGPUReadback)
            { 
                int width = texture.width;
                int height = texture.height;
                var format = texture.graphicsFormat;

                Texture2D decompressed = null;
                if (texture.format !=  TextureFormat.ARGB32 && texture.format != TextureFormat.RGB24 && texture.format != TextureFormat.RGBA32)
                {
                    decompressed = MakeReadable(texture);
                    texture = decompressed;
                    format = texture.graphicsFormat;
                }
                
                var buffer = await AsyncGpuReadback(texture);
                data = await Task.Run(() =>
                {
                    var imageData = hasAlpha ?
                        ImageConversion.EncodeArrayToPNG(buffer, format, (uint)width, (uint)height) :
                        ImageConversion.EncodeArrayToJPG(buffer, format, (uint)width, (uint)height);

                    return imageData;
                });

                if (decompressed != null)
                {
                    Object.Destroy(decompressed);
                }
            }
            else
            {
                if (texture.isReadable)
                {
                    data = hasAlpha ?
                        texture.EncodeToPNG() :
                        texture.EncodeToJPG();
                }
                else
                {
                    Texture2D readableTexture = MakeReadable(texture);
                    data = hasAlpha ?
                        readableTexture.EncodeToPNG() :
                        readableTexture.EncodeToJPG();
                    Object.Destroy(readableTexture);
                }
            }

            return data;
        }

    }
}
