using System;
using System.Threading.Tasks;

namespace Battlehub.Storage
{
    public interface IExternalAssetLoader 
    {
        Task<object> LoadAsync(string key, object root, IProgress<float> progress = null);
        
        void Release(object obj);
    }
}

