using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Battlehub.Storage
{
    public interface IAssetMap<TID>  where TID : IEquatable<TID>
    {
        IAssetMap<TID> ParentMap
        {
            set;
            get;
        }

        IEnumerable<object> RootInstances
        {
            get;
        }

        IEnumerable<object> RootAssets
        {
            get;
        }

        IEnumerable<object> GetDependencies(object obj);

        bool TryGetRootAssetByInstance(object instance, out object rootAsset);
        
        bool TryGetRootAssetByAsset(object asset, out object rootAsset);

        bool TryGetRootAssetIDByAsset(object asset, out TID rootAssetID);

        bool TryGetRootAssetIDByInstance(object instance, out TID rootAssetID);

        bool TryGetAssetIDByInstance(object instance, out TID assetID);

        bool TryGetAssetByInstance(object instance, out object asset);

        bool TryGetIDMapByRootAsset(object rootAsset, out IIDMap<TID> idmap);

        bool TryGetAssetsMarkedAsDestroyedByRootAsset(object rootAsset, out HashSet<object> assetsMarkedAsDestroyed);

        bool TryGetRootInstancesByRootAsset(object rootAsset, out IReadOnlyCollection<object> rootInstances);

        bool TryGetInstanceToAssetMapByRootInstance(object rootInstance, out IReadOnlyDictionary<object, object> instanceToAssetMap);

        bool TryGetAssetToInstanceMapByRootInstance(object rootInstance, out IReadOnlyDictionary<object, object> assetToInstanceMap);

        bool IsInstance(object obj);

        bool IsRootInstance(object obj);

        bool IsAsset(object obj);

        bool IsRootAsset(object obj);

        void AddRootAsset(object rootAsset, IIDMap<TID> idMap, HashSet<object> assetsMarkedAsDestroyed);

        void AddRootInstance(object rootInstance, object rootAsset, Dictionary<object, object> assetToInstance);

        void RemoveInstance(object instance);

        IEnumerable<object> GetDirty();

        bool IsDirty(object instance);

        void SetDirty(object instance);

        void ClearDirty(object instance, bool waitForCommit = false);
            
        void Commit();

        void Remove(object obj, bool waitForCommit = false);

        void Reset();
    }

    public interface IAssetMapInternal<TID> where TID : IEquatable<TID>
    {
        IReadOnlyDictionary<object, object> AssetToRootAsset
        {
            get;
        }

        IReadOnlyDictionary<object, IIDMap<TID>> RootAssetToIDMap
        {
            get;
        }

        IReadOnlyDictionary<object, HashSet<object>> RootAssetToRootInstances
        {
            get;
        }

        IReadOnlyDictionary<object, Dictionary<object, object>> RootInstanceToAssetInstanceMap
        {
            get;
        }

        IReadOnlyDictionary<object, object> InstanceToAsset
        {
            get;
        }

        IReadOnlyCollection<object> DirtyInstances
        {
            get;
        }
    }

    public class AssetMap<TID> : IAssetMap<TID>, IAssetMapInternal<TID> where TID : IEquatable<TID>
    {
        private AssetMap<TID> m_parentMap;

        public IAssetMap<TID> ParentMap
        {
            get { return m_parentMap; }
            set { m_parentMap = value as AssetMap<TID>; }
        }

        public IEnumerable<object> RootInstances
        {
            get { return m_rootInstanceToAssetInstanceMap.Keys; }
        }

        public IEnumerable<object> RootAssets
        {
            get { return m_rootAssetToAssetData.Keys; }
        }

        /// <summary>
        /// (child asset | root asset) -> root asset
        /// </summary>
        private readonly Dictionary<object, object> m_assetToRootAsset = new Dictionary<object, object>();

        private struct AssetData
        {
            public IIDMap<TID> IDMap
            {
                get;
            }

            public HashSet<object> AssetsMarkedAsDestroyed
            {
                get;
            }

            public AssetData(IIDMap<TID> idMap, HashSet<object> assetsMarkedAsDestroyed)
            {
                IDMap = idMap;
                AssetsMarkedAsDestroyed = assetsMarkedAsDestroyed;
            }
        }

        /// <summary>
        /// root asset id -> id map (asset <-> asset id)
        /// </summary>
        private readonly Dictionary<object, AssetData> m_rootAssetToAssetData = new Dictionary<object, AssetData>();

        /// <summary>
        /// root asset -> root instances list
        /// </summary>
        private readonly Dictionary<object, HashSet<object>> m_rootAssetToRootInstances = new Dictionary<object, HashSet<object>>();

        /// <summary>
        /// root instance -> ((child asset | root asset) -> (child instance | root instance))
        /// </summary>
        private readonly Dictionary<object, Dictionary<object, object>> m_rootInstanceToAssetInstanceMap = new Dictionary<object, Dictionary<object, object>>();

        /// <summary>
        /// (child instance | root instance) -> (child asset | root asset)
        /// </summary>
        private readonly Dictionary<object, object> m_instanceToAsset = new Dictionary<object, object>();

        private readonly HashSet<object> m_dirtyInstances = new HashSet<object>();

        private readonly HashSet<object> m_removeDirtyInstances = new HashSet<object>();

        private readonly List<object> m_removeObjects = new List<object>();

        IReadOnlyDictionary<object, object> IAssetMapInternal<TID>.AssetToRootAsset
        {
            get { return m_assetToRootAsset; }
        }

        IReadOnlyDictionary<object, IIDMap<TID>> IAssetMapInternal<TID>.RootAssetToIDMap
        {
            get { return m_rootAssetToAssetData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IDMap); }
        }

        IReadOnlyDictionary<object, HashSet<object>> IAssetMapInternal<TID>.RootAssetToRootInstances
        {
            get { return m_rootAssetToRootInstances; }
        }

        IReadOnlyDictionary<object, Dictionary<object, object>> IAssetMapInternal<TID>.RootInstanceToAssetInstanceMap
        {
            get { return m_rootInstanceToAssetInstanceMap; }
        }

        IReadOnlyDictionary<object, object> IAssetMapInternal<TID>.InstanceToAsset
        {
            get { return m_instanceToAsset; }
        }

        IReadOnlyCollection<object> IAssetMapInternal<TID>.DirtyInstances
        {
            get { return m_dirtyInstances; }
        }

        public void Reset()
        {
            m_dirtyInstances.Clear();
            m_removeDirtyInstances.Clear();
            m_assetToRootAsset.Clear();
            m_rootAssetToAssetData.Clear();
            m_rootAssetToRootInstances.Clear();
            m_rootInstanceToAssetInstanceMap.Clear();
            m_instanceToAsset.Clear();
            m_removeObjects.Clear();
        }

        public IEnumerable<object> GetDependencies(object obj)
        {
            var visited = new HashSet<object>();
            var depsList = new List<object>();

            GetDependencies(obj, visited, depsList);

            return depsList;
        }

        private void GetDependencies(object obj, HashSet<object> visited, List<object> depsList)
        {
            if (TryGetRootAssetByAsset(obj, out object rootAsset))
            {
                if (TryGetRootInstancesByRootAsset(rootAsset, out var rootInstances))
                {
                    foreach (object rootInstance in rootInstances)
                    {
                        GetDependencies(rootInstance, visited, depsList);
                    }
                }

                if (visited.Add(rootAsset))
                {
                    depsList.Add(rootAsset);
                }
            }
            
            if (rootAsset != obj && IsRootInstance(obj))
            {
                if (visited.Add(obj))
                {
                    depsList.Add(obj);
                }
            }
        }

        public bool TryGetRootAssetByInstance(object instance, out object rootAsset)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetRootAssetByInstance(instance, out rootAsset))
                {
                    return true;
                }
            }

            rootAsset = default;
            if (!TryGetAssetByInstance(instance, out object asset))
            {
                return false;
            }

            return TryGetRootAssetByAsset(asset, out rootAsset);
        }

        public bool TryGetRootAssetByAsset(object asset, out object rootAsset)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetRootAssetByAsset(asset, out rootAsset))
                {
                    return true;
                }
            }

            return m_assetToRootAsset.TryGetValue(asset, out rootAsset);
        }

        public bool TryGetRootAssetIDByAsset(object asset, out TID rootAssetID)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetRootAssetIDByAsset(asset, out rootAssetID))
                {
                    return true;
                }
            }

            rootAssetID = default;
            
            if (!m_assetToRootAsset.TryGetValue(asset, out object rootAsset))
            {
                return false;
            }

            if (!m_rootAssetToAssetData.TryGetValue(rootAsset, out var assetData))
            {
                return false;
            }

            return assetData.IDMap.TryGetID(rootAsset, out rootAssetID);
        }

        public bool TryGetRootAssetIDByInstance(object instance, out TID rootAssetID)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetRootAssetIDByInstance(instance, out rootAssetID))
                {
                    return true;
                }
            }

            rootAssetID = default;

            if (!TryGetAssetByInstance(instance, out object asset))
            {
                return false;
            }

            return TryGetRootAssetIDByAsset(asset, out rootAssetID);
        }

        public bool TryGetAssetIDByInstance(object instance, out TID assetID)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetAssetIDByInstance(instance, out assetID))
                {
                    return true;
                }
            }

            assetID = default;

            if (!TryGetAssetByInstance(instance, out object asset))
            {
                return false;
            }

            if (!TryGetRootAssetByAsset(asset, out object rootAsset))
            {
                return false;
            }

            if (!TryGetIDMapByRootAsset(rootAsset, out IIDMap<TID> idMap))
            {
                return false;
            }

            return idMap.TryGetID(asset, out assetID);
        }


        public bool TryGetAssetByInstance(object instance, out object asset)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetAssetByInstance(instance, out asset))
                {
                    return true;
                }
            }
            
            return m_instanceToAsset.TryGetValue(instance, out asset);
        }

        public bool TryGetIDMapByRootAsset(object rootAsset, out IIDMap<TID> idmap)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetIDMapByRootAsset(rootAsset, out idmap))
                {
                    return true;
                }
            }

            if(m_rootAssetToAssetData.TryGetValue(rootAsset, out var assetData))
            {
                idmap = assetData.IDMap;
                return true;
            }

            idmap = null;
            return false;
        }

        public bool TryGetAssetsMarkedAsDestroyedByRootAsset(object rootAsset, out HashSet<object> assetsMarkedAsDestroyed)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetAssetsMarkedAsDestroyedByRootAsset(rootAsset, out assetsMarkedAsDestroyed))
                {
                    return true;
                }
            }

            if (m_rootAssetToAssetData.TryGetValue(rootAsset, out var assetData))
            {
                assetsMarkedAsDestroyed = assetData.AssetsMarkedAsDestroyed;
                return assetsMarkedAsDestroyed != null;
            }

            assetsMarkedAsDestroyed = null;
            return false;

        }

        public bool TryGetRootInstancesByRootAsset(object rootAsset, out IReadOnlyCollection<object> instances)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetRootInstancesByRootAsset(rootAsset, out instances))
                {
                    return true;
                }
            }
            
            bool result = m_rootAssetToRootInstances.TryGetValue(rootAsset, out var instancesHs);
            instances = instancesHs;
            return result;
        }

        public bool TryGetInstanceToAssetMapByRootInstance(object rootInstance, out IReadOnlyDictionary<object, object> instanceToAssetMap)
        {
            if (TryGetAssetToInstanceMapByRootInstance(rootInstance, out var assetToInstanceMap))
            {
                instanceToAssetMap = assetToInstanceMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                return true;
            }

            instanceToAssetMap = default;
            return false;     
        }
        
        public bool TryGetAssetToInstanceMapByRootInstance(object rootInstance, out IReadOnlyDictionary<object, object> assetToInstanceMap)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetAssetToInstanceMapByRootInstance(rootInstance, out assetToInstanceMap))
                {
                    return true;
                }
            }

            bool result = m_rootInstanceToAssetInstanceMap.TryGetValue(rootInstance, out var assetToInstanceMapDict);
            assetToInstanceMap = assetToInstanceMapDict;
            return result;
        }

        public bool IsInstance(object obj)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.IsInstance(obj))
                {
                    return true;
                }
            }

            return m_instanceToAsset.ContainsKey(obj);
        }

        public bool IsRootInstance(object obj)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.IsRootInstance(obj))
                {
                    return true;
                }
            }

            return m_rootInstanceToAssetInstanceMap.ContainsKey(obj);
        }

        public bool IsAsset(object obj)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.IsAsset(obj))
                {
                    return true;
                }
            }

            return m_assetToRootAsset.ContainsKey(obj);
        }

        public bool IsRootAsset(object obj)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.IsRootAsset(obj))
                {
                    return true;
                }
            }

            return m_rootAssetToAssetData.ContainsKey(obj);
        }

        public void AddRootAsset(object rootAsset, IIDMap<TID> idMap, HashSet<object> assetsMarkedAsDestroyed)
        {
            if (!idMap.TryGetID(rootAsset, out var _))
            {
                throw new ArgumentException("rootAsset is not present in idMap", "rootAsset");
            }

            foreach (object asset in idMap.ObjectToID.Keys)
            {
                m_assetToRootAsset.Add(asset, rootAsset);
            }

            m_rootAssetToAssetData.Add(rootAsset, new AssetData(idMap, assetsMarkedAsDestroyed));
        }

        public void AddRootInstance(object rootInstance, object rootAsset, Dictionary<object, object> assetToInstance)
        {
            if (!m_rootAssetToRootInstances.TryGetValue(rootAsset, out var rootInstances))
            {
                rootInstances = new HashSet<object>();
                m_rootAssetToRootInstances.Add(rootAsset, rootInstances);
            }

            if (!rootInstances.Add(rootInstance))
            {
                throw new ArgumentException("rootInstance is already present in rootInstances", "rootInstance");
            }

            foreach (var kvp in assetToInstance)
            {
                object asset = kvp.Key;
                object instance = kvp.Value;
                if (m_instanceToAsset.TryGetValue(instance, out object existingAsset))
                {
                    if (existingAsset != asset)
                    {
                        throw new ArgumentException("instance is already present in instanceToAsset", "instance");
                    }
                }
                else
                {
                    m_instanceToAsset.Add(instance, asset);
                }
            }

            m_rootInstanceToAssetInstanceMap.Add(rootInstance, assetToInstance);
        }

        public void Commit()
        {
            //TODO: removal should be made after passing all checks.
            foreach (object obj in m_removeObjects)
            {
                m_parentMap.Remove(obj, false);
            }
            m_removeObjects.Clear();

            foreach (var kvp in m_assetToRootAsset)
            {
                var asset = kvp.Key;
                var rootAssetID = kvp.Value;

                if (m_parentMap.m_assetToRootAsset.ContainsKey(asset))
                {
                    throw new InvalidOperationException($"asset {asset} is already present in parent map");
                }
            }

            foreach (var kvp in m_rootAssetToAssetData)
            {
                var rootAsset = kvp.Key;
                var idMap = kvp.Value;

                if (m_parentMap.m_rootAssetToAssetData.ContainsKey(rootAsset))
                {
                    throw new InvalidOperationException("rootAsset is already present in parent map");
                }
            }

            foreach (var kvp in m_rootAssetToRootInstances)
            {
                var rootAsset = kvp.Key;
                var rootInstances = kvp.Value;

                if (!m_parentMap.m_rootAssetToRootInstances.TryGetValue(rootAsset, out var parentRootInstances))
                {
                    continue;
                }

                foreach (var rootInstance in rootInstances)
                {
                    if (parentRootInstances.Contains(rootInstance))
                    {
                        throw new InvalidOperationException("rootInstance is already present in parentRootInstances");
                    }
                }
            }

            foreach (var kvp in m_rootInstanceToAssetInstanceMap)
            {
                var rootInstance = kvp.Key;
                var assetToInstance = kvp.Value;

                if (!m_parentMap.m_rootInstanceToAssetInstanceMap.TryGetValue(rootInstance, out var parentAssetToInstance))
                {
                    continue;
                }

                foreach (var kvp2 in assetToInstance)
                {
                    var asset = kvp2.Key;
                    var instance = kvp2.Value;

                    if (parentAssetToInstance.ContainsKey(asset))
                    {
                        throw new InvalidOperationException("asset is already present in parentAssetToInstance");
                    }
                }
            }

            foreach (var kvp in m_instanceToAsset)
            {
                var instance = kvp.Key;
                var asset = kvp.Value;

                if (m_parentMap.m_instanceToAsset.ContainsKey(instance))
                {
                    throw new InvalidOperationException("instance is already present in parent map");
                }
            }

            foreach (var kvp in m_assetToRootAsset)
            {
                var asset = kvp.Key;
                var rootAssetID = kvp.Value;

                m_parentMap.m_assetToRootAsset.Add(asset, rootAssetID);
            }

            foreach (var kvp in m_rootAssetToAssetData)
            {
                var rootAsset = kvp.Key;
                var idMap = kvp.Value;

                m_parentMap.m_rootAssetToAssetData.Add(rootAsset, idMap);
            }

            foreach (var kvp in m_rootAssetToRootInstances)
            {
                var rootAsset = kvp.Key;
                var rootInstances = kvp.Value;

                if (!m_parentMap.m_rootAssetToRootInstances.TryGetValue(rootAsset, out var parentRootInstances))
                {
                    parentRootInstances = new HashSet<object>();
                    m_parentMap.m_rootAssetToRootInstances.Add(rootAsset, parentRootInstances);
                }

                foreach (var rootInstance in rootInstances)
                {
                    parentRootInstances.Add(rootInstance);
                }
            }

            foreach (var kvp in m_rootInstanceToAssetInstanceMap)
            {
                var rootInstance = kvp.Key;
                var assetToInstance = kvp.Value;

                if (!m_parentMap.m_rootInstanceToAssetInstanceMap.TryGetValue(rootInstance, out var parentAssetToInstance))
                {
                    parentAssetToInstance = new Dictionary<object, object>();
                    m_parentMap.m_rootInstanceToAssetInstanceMap.Add(rootInstance, parentAssetToInstance);
                }

                foreach (var kvp2 in assetToInstance)
                {
                    var asset = kvp2.Key;
                    var instance = kvp2.Value;

                    parentAssetToInstance.Add(asset, instance);
                }
            }

            foreach (var kvp in m_instanceToAsset)
            {
                var instance = kvp.Key;
                var asset = kvp.Value;

                m_parentMap.m_instanceToAsset.Add(instance, asset);
            }

            foreach (object dirtyInstance in m_removeDirtyInstances)
            {
                m_parentMap.m_dirtyInstances.Remove(dirtyInstance);
            }

            foreach (object dirtyInstance in m_dirtyInstances)
            {
                if (m_parentMap.IsInstance(dirtyInstance))
                {
                    m_parentMap.m_dirtyInstances.Add(dirtyInstance);
                }
            }
        }

        public void Remove(object obj, bool waitForCommit)
        {
            if (waitForCommit)
            {
                if (!m_removeObjects.Contains(obj))
                {
                    m_removeObjects.Add(obj);
                }
                return;
            }

            RemoveInstance(obj);
            RemoveAsset(obj);
        }

        private void RemoveAsset(object obj)
        {
            if (!m_assetToRootAsset.TryGetValue(obj, out object rootAsset))
            {
                return;
            }

            if (!m_rootAssetToAssetData.TryGetValue(rootAsset, out var assetData))
            {
                return;
            }

            if (rootAsset != obj)
            {
                // Sometimes rootAsset != obj is a valid case.
                // For example, when you import an addressable referencing some material,
                // before importing that material.
                rootAsset = obj;
                Debug.LogWarning($"obj {obj} is not a root asset");
                //return;
            }
            else
            {
                m_assetToRootAsset.Remove(rootAsset);
                foreach (object asset in assetData.IDMap.ObjectToID.Keys)
                {
                    m_assetToRootAsset.Remove(asset);
                }
                assetData.IDMap.Rollback();
            }
          
            if (m_rootAssetToRootInstances.TryGetValue(rootAsset, out var rootInstances))
            {
                if (rootInstances.Count > 0)
                {
                    throw new InvalidOperationException("Can't remove asset while instances still exist");
                }

                m_rootAssetToRootInstances.Remove(rootAsset);
            }

            m_rootAssetToAssetData.Remove(rootAsset);
        }

        public void RemoveInstance(object instance)
        {
            if (!m_instanceToAsset.TryGetValue(instance, out object asset))
            {
                return;
            }

            if (!m_assetToRootAsset.TryGetValue(asset, out object rootAsset))
            {
                throw new KeyNotFoundException($"Root Asset ID not found. Asset {asset}");
            }

            if (rootAsset != asset)
            {
                throw new ArgumentException("instance is not a root instance", "instance");
            }

            if (!m_rootAssetToRootInstances.TryGetValue(rootAsset, out var rootInstances))
            {
                throw new KeyNotFoundException($"Root Instances not found. Asset {rootAsset}");
            }

            if (!rootInstances.Contains(instance))
            {
                throw new ArgumentException("!rootInstances.Contains(instance)", "instance");
            }

            if (!m_rootInstanceToAssetInstanceMap.TryGetValue(instance, out var assetToInstance))
            {
                throw new ArgumentException("Asset To Instance Map not found");
            }

            rootInstances.Remove(instance);
            if (rootInstances.Count == 0)
            {
                m_rootAssetToRootInstances.Remove(rootAsset);
            }

            foreach (var kvp in assetToInstance)
            {
                object rootOrChildInstance = kvp.Value;
                m_instanceToAsset.Remove(rootOrChildInstance);
                m_dirtyInstances.Remove(rootOrChildInstance);
            }
            m_rootInstanceToAssetInstanceMap.Remove(instance);
        }

        public IEnumerable<object> GetDirty()
        {
            var dirtyInstances = new HashSet<object>();
            if (m_parentMap != null)
            {
                foreach(object dirty in m_parentMap.m_dirtyInstances)
                {
                    dirtyInstances.Add(dirty);
                }
            }

            foreach (object dirty in m_dirtyInstances)
            {
                dirtyInstances.Add(dirty);
            }

            return dirtyInstances;
        }

        public bool IsDirty(object instance)
        {
            if (m_parentMap != null)
            {
                if (m_parentMap.IsDirty(instance))
                {
                    return true;
                }
            }

            return m_dirtyInstances.Contains(instance);
        }

        public void SetDirty(object instance)
        {
            if (IsInstance(instance))
            {
                m_dirtyInstances.Add(instance);
            }
        }

        public void ClearDirty(object instance, bool waitForCommit)
        {
            if (waitForCommit)
            {
                m_removeDirtyInstances.Add(instance);
            }
            else
            {
                m_dirtyInstances.Remove(instance);
            }
        }
    }

}
