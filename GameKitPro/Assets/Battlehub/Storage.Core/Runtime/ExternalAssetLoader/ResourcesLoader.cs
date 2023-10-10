using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Battlehub.Storage
{
    public class ResourcesLoader : IExternalAssetLoader
    {
        public async Task<object> LoadAsync(string key, object root, IProgress<float> progress = null)
        {
            var ao = Resources.LoadAsync(key);
            while (!ao.isDone)
            {
                await Task.Yield();
                if (progress != null)
                {
                    progress.Report(ao.progress);
                }
            }
            return ao.asset;
        }

        public void Release(object obj)
        {
            if (obj is UnityEngine.Object)
            {
                bool canUnload = !(obj is GameObject) && !(obj is Component); // unity rules see Resources.UnloadAsset docs
                if (canUnload) 
                {
                    Resources.UnloadAsset((UnityEngine.Object)obj);
                }
            }
        }
    }
}
