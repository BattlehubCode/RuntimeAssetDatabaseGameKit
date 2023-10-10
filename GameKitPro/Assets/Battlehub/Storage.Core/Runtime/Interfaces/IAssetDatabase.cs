using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Battlehub.Storage
{
    /// <summary>
    /// Defines a standardized interface for managing and interacting with assets and asset-related data.
    /// </summary>
    /// <typeparam name="TID">The type for asset identifiers</typeparam>
    /// <typeparam name="TFID">The type for file identifiers</typeparam>
    public interface IAssetDatabase<TID, TFID>
        where TID : IEquatable<TID>
        where TFID : IEquatable<TFID>
    {
        /// <summary>
        /// Loads a project based on the provided project ID.
        /// </summary>
        /// <param name="projectID">The unique project identifier to load.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LoadProjectAsync(TFID projectID);

        /// <summary>
        /// Unloads the currently loaded project
        /// </summary>
        /// <param name="destroy">Determines whether to destroy assets or just unload them.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnloadProjectAsync(bool destroy = false);

        /// <summary>
        /// Gets a value indicating whether a project is currently loaded.
        /// </summary>
        bool IsProjectLoaded { get; }

        /// <summary>
        /// Gets the root identifier of the asset hierarchy.
        /// </summary>
        TID RootID { get; }

        /// <summary>
        /// Tries to get the type of the asset identified by the given ID.
        /// </summary>
        /// <param name="id">The unique identifier of the asset.</param>
        /// <param name="type">The output parameter to store the asset type if found.</param>
        /// <returns>True if the asset type was found, otherwise false.</returns>
        bool TryGetAssetType(TID id, out Type type);

        /// <summary>
        /// Tries to get the metadata associated with an asset identified by the given ID.
        /// </summary>
        /// <param name="id">The unique identifier of the asset.</param>
        /// <param name="meta">The output parameter to store the asset's metadata if found.</param>
        /// <returns>True if metadata was found for the asset, otherwise false.</returns>
        bool TryGetMeta(TID id, out IMeta<TID, TFID> meta);

        /// <summary>
        /// Tries to get the metadata associated with the given object.
        /// </summary>
        /// <param name="obj">The object for which to retrieve metadata.</param>
        /// <param name="meta">The output parameter to store the metadata if found.</param>
        /// <returns>True if metadata was found for the object, otherwise false.</returns>
        bool TryGetMeta(object obj, out IMeta<TID, TFID> meta);

        /// <summary>
        /// Tries to get the metadata associated with the asset identified by the given file ID.
        /// </summary>
        /// <param name="fileID">The unique identifier of the file associated with the asset.</param>
        /// <param name="meta">The output parameter to store the asset's metadata if found.</param>
        /// <returns>True if metadata was found for the asset, otherwise false.</returns>
        bool TryGetMeta(TFID fileID, out IMeta<TID, TFID> meta);

        /// <summary>
        /// Tries to get the thumbnail data for the asset identified by the given ID.
        /// </summary>
        /// <param name="id">The unique identifier of the asset.</param>
        /// <param name="thumbnail">The output parameter to store the thumbnail data if found.</param>
        /// <returns>True if a thumbnail was found for the asset, otherwise false.</returns>
        bool TryGetThumbnail(TID id, out byte[] thumbnail);

        /// <summary>
        /// Tries to get the parent identifier of the asset identified by the given ID.
        /// </summary>
        /// <param name="id">The unique identifier of the asset.</param>
        /// <param name="parentID">The output parameter to store the parent identifier if found.</param>
        /// <returns>True if the parent identifier was found, otherwise false.</returns>
        bool TryGetParent(TID id, out TID parentID);

        /// <summary>
        /// Tries to get the list of child identifiers for the asset identified by the given ID.
        /// </summary>
        /// <param name="id">The unique identifier of the asset.</param>
        /// <param name="children">The output parameter to store the list of child identifiers if found.</param>
        /// <returns>True if child identifiers were found, otherwise false.</returns>
        bool TryGetChildren(TID id, out IReadOnlyList<TID> children);

        /// <summary>
        /// Checks if the asset identified by the given ID is a folder.
        /// </summary>
        /// <param name="id">The unique identifier of the asset to check.</param>
        /// <returns>True if the asset is a folder, otherwise false.</returns>
        bool IsFolder(TID id);

        /// <summary>
        /// Creates a new folder with the specified folder ID and optional name.
        /// </summary>
        /// <param name="folderID">The unique identifier for the new folder.</param>
        /// <param name="name">The name of the new folder (optional).</param>
        /// <param name="parentID">The identifier of the parent folder (default is the root).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateFolderAsync(TFID folderID, string name = null, TID parentID = default);

        /// <summary>
        /// Moves a folder to a new parent folder.
        /// </summary>
        /// <param name="folderID">The unique identifier of the folder to move.</param>
        /// <param name="newFolderID">The unique identifier of the new parent folder.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MoveFolderAsync(TFID folderID, TFID newFolderID);

        /// <summary>
        /// Deletes a folder based on its unique identifier.
        /// </summary>
        /// <param name="folderID">The unique identifier of the folder to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFolderAsync(TFID folderID);

        /// <summary>
        /// Loads an asset asynchronously based on its unique identifier.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to load.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LoadAssetAsync(TID assetID);

        /// <summary>
        /// Loads the thumbnail of an asset identified by the given ID.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset for which to load the thumbnail.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LoadThumbnailAsync(TID assetID);

        /// <summary>
        /// Saves the thumbnail data for an asset identified by the given ID.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset for which to save the thumbnail.</param>
        /// <param name="thumbnail">The thumbnail data to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveThumbnailAsync(TID assetID, byte[] thumbnail);

        /// <summary>
        /// Checks if an object can be used to create an asset.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object can be used to create an asset, otherwise false.</returns>
        bool CanCreateAsset(object obj);

        /// <summary>
        /// Creates a new asset using the provided object and associated information.
        /// </summary>
        /// <param name="obj">The object representing the new asset.</param>
        /// <param name="fileID">The unique identifier of the file associated with the asset.</param>
        /// <param name="thumbnail">The thumbnail data for the asset (optional).</param>
        /// <param name="thumbnailID">The unique identifier of the thumbnail file (optional).</param>
        /// <param name="dataFileID">The unique identifier of the data file (optional).</param>
        /// <param name="parentID">The identifier of the parent folder (default is the root).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateAssetAsync(object obj, TFID fileID, byte[] thumbnail = null, TFID thumbnailID = default, TFID dataFileID = default, TID parentID = default);

        /// <summary>
        /// Saves an asset identified by the given ID, including its associated thumbnail data.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to save.</param>
        /// <param name="thumbnail">The updated thumbnail data for the asset (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveAssetAsync(TID assetID, byte[] thumbnail = null);

        /// <summary>
        /// Checks if an object has pending changes that need to be saved.
        /// </summary>
        /// <param name="obj">The object to check for changes.</param>
        /// <returns>True if the object has changes, otherwise false.</returns>
        bool HasChanges(object obj);

        /// <summary>
        /// Checks if changes to an object can be applied.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="afterApplyingChangesToRoot">Determines whether to check after applying changes to the root object (default is false).</param>
        /// <returns>True if changes can be applied, otherwise false.</returns>
        bool CanApplyChanges(object obj, bool afterApplyingChangesToRoot = false);

        /// <summary>
        /// Applies changes made to an instance to an asset, and propagates changes from the asset to related instances.
        /// </summary>
        /// <param name="obj">The object representing changes to apply.</param>
        /// <returns>A task that returns an array of asset identifiers affected by the changes.</returns>
        Task<TID[]> ApplyChangesAsync(object obj);

        /// <summary>
        /// Renames an asset identified by the given ID.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to rename.</param>
        /// <param name="name">The new name for the asset.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RenameAssetAsync(TID assetID, string name);

        /// <summary>
        /// Moves an asset identified by the given ID to a new file, data file, and parent.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to move.</param>
        /// <param name="newFileID">The unique identifier of the new file for the asset.</param>
        /// <param name="newDataFileID">The unique identifier of the new data file for the asset (optional).</param>
        /// <param name="newThumbnailID">The unique identifier of the new thumbnail for the asset (optional).</param>
        /// <param name="newParentID">The identifier of the new parent folder (default is the root).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MoveAssetAsync(TID assetID, TFID newFileID, TFID newDataFileID = default, TFID newThumbnailID = default, TID newParentID = default);

        /// <summary>
        /// Unloads an asset identified by the given ID, optionally destroying instances.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to unload.</param>
        /// <param name="destroy">Determines whether to destroy the instance or just unload the asset (default is false).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnloadAssetAsync(TID assetID, bool destroy = false);

        /// <summary>
        /// Unloads all assets, optionally destroying instance.
        /// </summary>
        /// <param name="destroy">Determines whether to destroy the instances or just unload assets (default is false).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnloadAllAssetsAsync(bool destroy = false);

        /// <summary>
        /// Deletes an asset identified by the given ID asynchronously.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAssetAsync(TID assetID);

        /// <summary>
        /// Unloads the thumbnail of an asset identified by the given ID.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset for which to unload the thumbnail.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnloadThumbnailAsync(TID assetID);

        /// <summary>
        /// Checks if the provided object represents the root of an asset.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is an asset root, otherwise false.</returns>
        bool IsAssetRoot(object obj);

        /// <summary>
        /// Checks if the provided object represents an asset.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is an asset, otherwise false.</returns>
        bool IsAsset(object obj);

        /// <summary>
        /// Tries to get the asset root object associated with the provided asset object.
        /// </summary>
        /// <param name="asset">The asset object for which to retrieve the asset root.</param>
        /// <param name="assetRoot">The output parameter to store the asset root object if found.</param>
        /// <returns>True if the asset root was found, otherwise false.</returns>
        bool TryGetAssetRoot(object asset, out object assetRoot);

        /// <summary>
        /// Tries to get the asset object associated with the provided asset identifier.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to retrieve.</param>
        /// <param name="asset">The output parameter to store the asset object if found.</param>
        /// <returns>True if the asset object was found, otherwise false.</returns>
        bool TryGetAsset(TID assetID, out object asset);

        /// <summary>
        /// Tries to get the asset identifier associated with the provided asset object.
        /// </summary>
        /// <param name="asset">The asset object for which to retrieve the asset identifier.</param>
        /// <param name="assetID">The output parameter to store the asset identifier if found.</param>
        /// <returns>True if the asset identifier was found, otherwise false.</returns>
        bool TryGetAssetID(object asset, out TID assetID);

        /// <summary>
        /// Tries to get the asset object associated with the provided instance object.
        /// </summary>
        /// <param name="instance">The instance object for which to retrieve the associated asset object.</param>
        /// <param name="asset">The output parameter to store the associated asset object if found.</param>
        /// <returns>True if the associated asset object was found, otherwise false.</returns>
        bool TryGetAssetByInstance(object instance, out object asset);

        /// <summary>
        /// Tries to get instances associated with the asset identified by the given asset identifier.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset for which to retrieve instances.</param>
        /// <param name="instances">The output parameter to store the instances if found.</param>
        /// <returns>True if instances were found, otherwise false.</returns>
        bool TryGetInstances(TID assetID, out IReadOnlyCollection<object> instances);

        /// <summary>
        /// Tries to get destroyed parts associated with the asset identified by the given asset identifier.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset for which to retrieve destroyed parts.</param>
        /// <param name="destroyed">The output parameter to store the destroyed parts if found.</param>
        /// <returns>True if destroyed parts were found, otherwise false.</returns>
        bool TryGetDestroyed(TID assetID, out IReadOnlyCollection<object> destroyed);

        /// <summary>
        /// Checks if changes to the provided asset can be reverted.
        /// </summary>
        /// <param name="asset">The asset for which to check if changes can be reverted.</param>
        /// <returns>True if changes to the asset can be reverted, otherwise false.</returns>
        bool CanRevertChanges(object asset);

        /// <summary>
        /// Reverts changes to an asset and returns the identifiers of affected assets.
        /// </summary>
        /// <param name="asset">The asset to revert changes for.</param>
        /// <returns>A task that returns an array of asset identifiers affected by the changes.</returns>
        public Task<TID[]> RevertChangesAsync(object asset);

        /// <summary>
        /// Checks if the provided object represents the root of an instance.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is an instance root, otherwise false.</returns>
        bool IsInstanceRoot(object obj);

        /// <summary>
        /// Tries to get the instance root object associated with the provided instance object.
        /// </summary>
        /// <param name="obj">The instance object for which to retrieve the instance root.</param>
        /// <param name="instanceRoot">The output parameter to store the instance root object if found.</param>
        /// <returns>True if the instance root was found, otherwise false.</returns>
        bool TryGetInstanceRoot(object obj, out object instanceRoot);

        /// <summary>
        /// Checks if the provided object represents an instance.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is an instance, otherwise false.</returns>
        bool IsInstance(object obj);

        /// <summary>
        /// Checks if an asset identified by the given ID can be instantiated.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to check for instantiation.</param>
        /// <returns>True if the asset can be instantiated, otherwise false.</returns>
        bool CanInstantiateAsset(TID assetID);

        /// <summary>
        /// Instantiates an asset identified by the given ID.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to instantiate.</param>
        /// <param name="parent">The parent object to attach the instantiated asset to (optional).</param>
        /// <param name="detachInstance">Determines whether to detach the instantiated instance from the asset (default is false).</param>
        /// <returns>A task that returns the instantiated object.</returns>
        Task<object> InstantiateAssetAsync(TID assetID, object parent = null, bool detachInstance = false);

        /// <summary>
        /// Instantiates an object.
        /// </summary>
        /// <param name="obj">The object to instantiate.</param>
        /// <param name="parent">The parent object to attach the instantiated object to (optional).</param>
        /// <param name="detachInstance">Determines whether to detach the instantiated instance from the asset (default is false).</param>
        /// <returns>A task that returns the instantiated object.</returns>
        Task<object> InstantiateAsync(object obj, object parent = null, bool detachInstance = false);

        /// <summary>
        /// Releases an instance asynchronously, freeing up any associated resources.
        /// </summary>
        /// <param name="instance">The instance to release.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ReleaseAsync(object instance);

        /// <summary>
        /// Flags an object as non-destructible by the asset database, allowing it to retain its lifetime control independently.
        /// </summary>
        /// <param name="obj">The object to flag.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetDontDestroyFlagAsync(object obj);

        /// <summary>
        /// Clears don't destroy flag
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearDontDestroyFlagAsync(object obj);

        /// <summary>
        /// Clears don't destoy flags
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearDontDestroyFlagsAsync();


        /// <summary>
        /// Checks if an instance can be detached from its asset.
        /// </summary>
        /// <param name="instance">The instance to check for detachment.</param>
        /// <returns>True if the instance can be detached, otherwise false.</returns>
        bool CanDetach(object instance);

        /// <summary>
        /// Detaches an instance from its asset, optionally detaches child instances.
        /// </summary>
        /// <param name="instance">The instance to detach.</param>
        /// <param name="completely">Determines whether to completely detach the instance (default is true).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DetachAsync(object instance, bool completely = true);

        /// <summary>
        /// Marks an instance as dirty, indicating that it has unsaved changes.
        /// </summary>
        /// <param name="instance">The instance to mark as dirty.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetDirtyAsync(object instance);

        /// <summary>
        /// Clears the dirty state of an instance, indicating that changes have been saved.
        /// </summary>
        /// <param name="instance">The instance to clear the dirty state for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearDirtyAsync(object instance);

        /// <summary>
        /// Checks if an instance is marked as dirty, indicating unsaved changes.
        /// </summary>
        /// <param name="instance">The instance to check for dirty state.</param>
        /// <returns>True if the instance is marked as dirty, otherwise false.</returns>
        bool IsDirty(object instance);

        /// This group of methods needed to register external assets so that they are not serialized but referenced instead

        /// <summary>
        /// Registers an external asset loader with the specified loader ID and loader instance.
        /// </summary>
        /// <param name="loaderID">The unique identifier for the external asset loader.</param>
        /// <param name="loader">The external asset loader instance to register.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RegisterExternalAssetLoaderAsync(string loaderID, IExternalAssetLoader loader);

        /// <summary>
        /// Clears all registered external asset loaders.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearExternalAssetLoadersAsync();

        /// <summary>
        /// Tries to get external asset loader by external loader id
        /// </summary>
        /// <param name="externalLoaderID">loader id</param>
        /// <param name="loader">The output parameter to store external asset loader if found.</param>
        /// <returns>True if external asset loader was found, otherwise false.</returns>
        bool TryGetExternalLoader(string externalLoaderID, out IExternalAssetLoader loader);

        /// <summary>
        /// Imports an external asset using the provided information.
        /// </summary>
        /// <param name="externalAssetKey">The key or identifier for the external asset.</param>
        /// <param name="externalLoaderID">The unique identifier of the external asset loader to use.</param>
        /// <param name="fileID">The unique identifier of the file associated with the imported asset.</param>
        /// <param name="thumbnail">The thumbnail data for the imported asset (optional).</param>
        /// <param name="thumbnailID">The unique identifier of the thumbnail file (optional).</param>
        /// <param name="dataFileID">The unique identifier of the data file (optional).</param>
        /// <param name="parentID">The identifier of the parent folder (default is the root).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ImportExternalAssetAsync(string externalAssetKey, string externalLoaderID, TFID fileID, byte[] thumbnail = null, TFID thumbnailID = default, TFID dataFileID = default, TID parentID = default);

        /// <summary>
        /// Imports an external asset using the provided external asset object and associated information.
        /// </summary>
        /// <param name="externalAsset">The external asset object to import.</param>
        /// <param name="externalAssetKey">The key or identifier for the external asset.</param>
        /// <param name="externalLoaderID">The unique identifier of the external asset loader to use.</param>
        /// <param name="fileID">The unique identifier of the file associated with the imported asset.</param>
        /// <param name="thumbnail">The thumbnail data for the imported asset (optional).</param>
        /// <param name="thumbnailID">The unique identifier of the thumbnail file (optional).</param>
        /// <param name="dataFileID">The unique identifier of the data file (optional).</param>
        /// <param name="parentID">The identifier of the parent folder (default is the root).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ImportExternalAssetAsync(object externalAsset, string externalAssetKey, string externalLoaderID, TFID fileID, byte[] thumbnail = null, TFID thumbnailID = default, TFID dataFileID = default, TID parentID = default);

        /// <summary>
        /// Checks if the asset identified by the given ID is an external asset root.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to check.</param>
        /// <returns>True if the asset is an external asset root, otherwise false.</returns>
        bool IsExternalAssetRoot(TID assetID);

        /// <summary>
        /// Checks if the provided object represents an external asset root.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is an external asset root, otherwise false.</returns>
        bool IsExternalAssetRoot(object obj);

        /// <summary>
        /// Checks if the asset identified by the given ID is an external asset.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset to check.</param>
        /// <returns>True if the asset is an external asset, otherwise false.</returns>
        bool IsExternalAsset(TID assetID);

        /// <summary>
        /// Checks if the provided object represents an external asset.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is an external asset, otherwise false.</returns>
        bool IsExternalAsset(object obj);

        /// <summary>
        /// Tries to get the external asset associated with the provided asset identifier.
        /// </summary>
        /// <param name="assetID">The unique identifier of the asset for which to retrieve the external asset.</param>
        /// <param name="externalAsset">The output parameter to store the external asset if found.</param>
        /// <returns>True if the external asset was found, otherwise false.</returns>
        bool TryGetExternalAsset(TID assetID, out object externalAsset);

        /// <summary>
        /// Tries to get the asset identifier associated with the provided external asset object.
        /// </summary>
        /// <param name="obj">The external asset object for which to retrieve the asset identifier.</param>
        /// <param name="assetID">The output parameter to store the asset identifier if found.</param>
        /// <returns>True if the asset identifier was found, otherwise false.</returns>
        bool TryGetExternalAssetID(object obj, out TID assetID);

        /// <summary>
        /// Registers external assets asynchronously using a dictionary of asset identifiers and external asset objects.
        /// </summary>
        /// <param name="externalAssets">A dictionary of asset identifiers and external asset objects to register.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RegisterExternalAssetsAsync(IDictionary<TID, object> externalAssets);

        /// <summary>
        /// Unregisters external assets asynchronously by their asset identifiers.
        /// </summary>
        /// <param name="assetIDs">The asset identifiers of the external assets to unregister.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnregisterExternalAssetAsync(params TID[] assetIDs);

        /// <summary>
        /// Unregisters external assets asynchronously by their external asset objects.
        /// </summary>
        /// <param name="externalAssets">The external asset objects to unregister.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnregisterExternalAssetsAsync(params object[] externalAssets);

        /// <summary>
        /// Clears all registered external assets asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearExternalAssetsAsync();
    }
}