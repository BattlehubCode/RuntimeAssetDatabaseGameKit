using System;
using System.Collections.Generic;

namespace Battlehub.Storage
{
    public static class RuntimeAssetDatabaseCoreExtensions
    {
        public static TID GetParent<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID id)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetParent(id, out var parentID))
            {
                return default;
            }

            return parentID;
        }

        public static IReadOnlyList<TID> GetChildren<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID id)
           where TID : IEquatable<TID>
           where TFID : IEquatable<TFID>
        {
            assetDatabase.TryGetChildren(id, out var children);
            return children;
        }

        public static IMeta<TID, TFID> GetMeta<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetMeta(assetID, out var meta))
            {
                throw new ArgumentException($"Meta with id {assetID} not found", nameof(assetID));
            }

            return meta;
        }

        public static IMeta<TID, TFID> GetMeta<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object asset)
           where TID : IEquatable<TID>
           where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetMeta(asset, out var meta))
            {
                throw new ArgumentException($"Meta for asset not found");
            }

            return meta;
        }

        public static IMeta<TID, TFID> GetMeta<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TFID fileID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetMeta(fileID, out var meta))
            {
                throw new ArgumentException($"Meta with file id {fileID} not found");
            }

            return meta;
        }

        public static Type GetAssetType<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID id)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetAssetType(id, out var type))
            {
                return null;
            }

            return type;
        }


        public static bool Exists<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object asset)
           where TID : IEquatable<TID>
           where TFID : IEquatable<TFID>
        {
            return assetDatabase.TryGetMeta(asset, out _);
        }

        public static bool Exists<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return assetDatabase.TryGetMeta(assetID, out _);
        }

        public static bool Exists<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TFID fileID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return assetDatabase.TryGetMeta(fileID, out _);
        }

        public static bool IsThumbnailLoaded<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return assetDatabase.TryGetThumbnail(assetID, out var _);
        }

        public static byte[] GetThumbnail<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetThumbnail(assetID, out var thumbnail))
            {
                throw new ArgumentException($"Thumbnail with id {assetID} not found");
            }

            return thumbnail;
        }

        public static byte[] GetThumbnail<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TFID fileID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            var meta = assetDatabase.GetMeta(fileID);
            return assetDatabase.GetThumbnail(meta.ID);
        }

        public static object GetAsset<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetAsset(assetID, out object asset))
            {
                return null;
            }

            return asset;
        }

        public static object GetAssetByInstance<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object instance)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetAssetByInstance(instance, out object asset))
            {
                return null;
            }

            return asset;
        }

        public static IReadOnlyCollection<object> GetInstances<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            assetDatabase.TryGetInstances(assetID, out var instances);
            return instances;
        }

        public static IReadOnlyCollection<object> GetDestroyed<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            assetDatabase.TryGetDestroyed(assetID, out var destroyed);
            return destroyed;
        }

        public static bool IsLoaded<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            object asset = assetDatabase.GetAsset(assetID);
            return asset != null;
        }

        public static TID GetAssetID<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object asset)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetAssetID(asset, out var assetID))
            {
                throw new ArgumentException($"assetID for {asset} not found");
            }

            return assetID;
        }

        public static TID GetAssetIDByInstance<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object instance)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            object asset = assetDatabase.GetAssetByInstance(instance);
            if (asset == null)
            {
                throw new ArgumentException($"{instance} is not an instance");
            }

            return assetDatabase.GetAssetID(asset);
        }

        public static TFID GetAssetFileID<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object asset)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return assetDatabase.GetMeta(asset).FileID;
        }

        public static IReadOnlyList<TID> GetAssets<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID parentID, bool recursive = false)
          where TID : IEquatable<TID>
          where TFID : IEquatable<TFID>
        {
            var result = new List<TID>();
            GetChildren(assetDatabase, parentID, recursive, includeFolders: false, includeAssets: true, result);
            return result;
        }

        public static IReadOnlyList<TID> GetFolders<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID parentID, bool recursive = false)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            var result = new List<TID>();
            GetChildren(assetDatabase, parentID, recursive, includeFolders: true, includeAssets: false, result);
            return result;
        }

        public static IReadOnlyList<TID> GetChildren<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID parentID, bool recursive = false)
           where TID : IEquatable<TID>
           where TFID : IEquatable<TFID>
        {
            var result = new List<TID>();
            GetChildren(assetDatabase, parentID, recursive, includeFolders: true, includeAssets: true, result);
            return result;
        }

        private static void GetChildren<TID, TFID>(IAssetDatabase<TID, TFID> assetDatabase, TID parentID, bool recursive, bool includeFolders, bool includeAssets, List<TID> list)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            var children = assetDatabase.GetChildren(parentID);
            for (int i = 0; i < children.Count; ++i)
            {
                TID tid = children[i];
                if (assetDatabase.IsFolder(tid))
                {
                    if (includeFolders)
                    {
                        list.Add(tid);
                    }
                }
                else
                {
                    if (includeAssets)
                    {
                        list.Add(tid);
                    }
                }


                if (recursive)
                {
                    GetChildren(assetDatabase, tid, recursive, includeFolders, includeAssets, list);
                }
            }
        }

        public static bool CanCreateAssetVariant<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object obj)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.CanCreateAsset(obj))
            {
                return false;
            }

            return assetDatabase.IsInstanceRoot(obj) || assetDatabase.IsInstanceRootRef(obj);
        }

        public static bool IsAssetVariant<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object obj)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return assetDatabase.IsAssetRoot(obj) && assetDatabase.IsInstanceRoot(obj);
        }

        public static bool IsInstanceOfAssetVariant<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object obj)
           where TID : IEquatable<TID>
           where TFID : IEquatable<TFID>
        {
            return assetDatabase.TryGetAssetByInstance(obj, out object asset) && assetDatabase.IsAssetVariant(asset);
        }

        public static bool IsInstanceOfAssetVariantRef<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object obj)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return assetDatabase.IsInstance(obj) && assetDatabase.IsInstanceOfAssetVariant(assetDatabase.GetAssetByInstance(obj));
        }

        public static bool IsInstanceRootRef<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object obj)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return assetDatabase.IsInstance(obj) && assetDatabase.IsInstanceRoot(assetDatabase.GetAssetByInstance(obj));
        }

        public static bool IsAddedObject<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object obj)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            object parentInstanceRoot = assetDatabase.GetInstanceRoot(obj);
            if (parentInstanceRoot == null)
            {
                return false;
            }

            object asset = assetDatabase.GetAssetByInstance(obj);
            if (asset == null || !assetDatabase.TryGetAssetRoot(asset, out object assetRoot))
            {
                return true;
            }

            var parentAssetID = assetDatabase.GetAssetIDByInstance(parentInstanceRoot);
            var assetRootID = assetDatabase.GetAssetID(assetRoot);

            return !EqualityComparer<TID>.Default.Equals(assetRootID, parentAssetID);
        }

        public static object GetInstanceRoot<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object obj)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if(!assetDatabase.TryGetInstanceRoot(obj, out var instanceRoot))
            {
                return null;
            }
            return instanceRoot;
        }

        public static bool IsExternalAssetRoot<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TFID fileID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            var meta = assetDatabase.GetMeta(fileID);
            return assetDatabase.IsExternalAssetRoot(meta.ID);
        }

        public static bool IsExternalAsset<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TFID fileID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            var meta = assetDatabase.GetMeta(fileID);
            return assetDatabase.IsExternalAssetRoot(meta.ID);
        }

        public static bool IsExternalAssetInstance<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, object instance, bool checkBaseAsset = false)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {

            object asset = instance;
            while(true)
            {
                asset = assetDatabase.GetAssetByInstance(asset);
                if (asset == null)
                {
                    return false;
                }

                if(assetDatabase.IsExternalAsset(asset))
                {
                    return true;
                }

                if (!checkBaseAsset)
                {
                    break;
                }
            }

            return false;
        }

        public static IExternalAssetLoader GetExternalAssetLoader<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, string externalLoaderID)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetExternalLoader(externalLoaderID, out IExternalAssetLoader loader))
            {
                throw new ArgumentException($"loader {externalLoaderID} not found", "externalLoaderID");
            }
            return loader;
        }

        public static ICollection<TID> GetAssetsAffectedBy<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, params TID[] assetsIDs)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            var result = new HashSet<TID>();

            foreach (var assetID in assetsIDs)
            {
                if (result.Add(assetID))
                {
                    assetDatabase.GetAssetsAffectedBy(assetID, result);
                }
            }

            foreach (var assetID in assetsIDs)
            {
                result.Remove(assetID);
            }

            return result;
        }

        private static void GetAssetsAffectedBy<TID, TFID>(this IAssetDatabase<TID, TFID> assetDatabase, TID assetID, HashSet<TID> outResult)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            if (!assetDatabase.TryGetMeta(assetID, out var meta))
            {
                return;
            }

            if (meta.InboundDependencies == null)
            {
                return;
            }

            foreach (TID dep in meta.InboundDependencies)
            {
                if (outResult.Add(dep))
                {
                    assetDatabase.GetAssetsAffectedBy(dep, outResult);
                }
            }
        }
    }
}