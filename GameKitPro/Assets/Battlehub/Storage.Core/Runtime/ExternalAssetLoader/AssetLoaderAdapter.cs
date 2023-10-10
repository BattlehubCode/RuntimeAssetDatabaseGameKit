using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Battlehub.Storage
{
    using UnityObject = UnityEngine.Object;
    public class AssetLoaderAdapter : IExternalAssetLoader
    {
        private static readonly Dictionary<string, int> m_refCounter = new Dictionary<string, int>();
        private static readonly Dictionary<object, string> m_objectToKey = new Dictionary<object, string>();
        private static readonly Dictionary<string, object> m_keyToExternalAsset = new Dictionary<string, object>();

        private readonly IExternalAssetLoader m_impl;
        public AssetLoaderAdapter(IExternalAssetLoader impl)
        {
            m_impl = impl;
        }

        public bool IsLoaded(string key)
        {
            return m_refCounter.ContainsKey(key);
        }

        public async Task<object> LoadAsync(string key, object root, IProgress<float> progress = null)
        {
            object obj;
            if (!m_refCounter.ContainsKey(key))
            {
                obj = await m_impl.LoadAsync(key, root, progress);
                if (obj == null)
                {
                    return null;
                }
                m_objectToKey[obj] = key;
                m_keyToExternalAsset[key] = obj;
                m_refCounter[key] = 1;
            }
            else
            {
                obj = m_keyToExternalAsset[key];
            }

            return obj;
        }

        public object Instantiate(string key, object root)
        {
            UnityObject externalAsset = (UnityObject)m_keyToExternalAsset[key];
            UnityObject instance = UnityObject.Instantiate(externalAsset, root as Transform);
            instance.name = externalAsset.name;

            m_objectToKey[instance] = key;
            m_refCounter[key]++;
            
            return instance;
        }

        public void Release(object obj)
        {
            if (m_objectToKey.TryGetValue(obj, out var key))
            {
                m_objectToKey.Remove(obj);
                object externalAsset = m_keyToExternalAsset[key];

                int refCounter = m_refCounter[key];
                refCounter--;
                if (refCounter == 0)
                {
                    m_refCounter.Remove(key);
                    m_impl.Release(externalAsset);
                    m_keyToExternalAsset.Remove(key);
                }
                else
                {
                    m_refCounter[key] = refCounter;
                }

                if (obj != externalAsset)
                {
                    UnityObject uo = obj as UnityObject;
                    if (uo != null)
                    {
                        UnityObject.Destroy(uo);
                    }
                }
            }
        }

    
    }
}