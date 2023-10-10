using System;
using UnityEngine;

namespace Battlehub.Storage
{
    public static class StorageLayers
    {
        public const string IgnoreLayer = "StorageIgnore";
    }

    public abstract class DefaultModuleDependencies : ModuleDependencies<Guid, string>
    {
        private Pool<object> m_tempRootPool = new Pool<object>();
        private WorkloadController m_workloadController;
        private bool m_destroyHostGo;

        private GameObject m_hostGo;
        private GameObject m_assetsRoot;

        public override object AssetsRoot
        {
            get { return m_assetsRoot.transform; }
        }

        protected abstract IObjectEnumeratorFactory EnumeratorFactory
        {
            get;
        }

        protected override IDataLayer<string> CreateDataLayer()
        {
            return new FileSystemDataLayer();
        }

        protected override IWorkloadController CreateWorkloadController()
        {
            m_workloadController = m_hostGo.AddComponent<WorkloadController>();
            return m_workloadController;
        }

        protected override void DestroyWorkloadController()
        {
            if (m_workloadController != null)
            {
                UnityEngine.Object.Destroy(m_workloadController);
            }
        }

        protected override IIDMap<Guid> CreateIDMap()
        {
            return new IDMap();
        }

        protected override IAssetMap<Guid> CreateAssetMap()
        {
            return new AssetMap<Guid>();
        }

        protected override IObjectTreeEnumerator CreateEnumerator(object obj)
        {
            return new ObjectTreeEnumerator(obj, EnumeratorFactory);
        }

      
        public DefaultModuleDependencies() : this(null)
        {
        }

        public DefaultModuleDependencies(GameObject hostGo = null)
        {
            if (hostGo == null)
            {
                hostGo = new GameObject("storageDependenciess");

                int ignoreLayer = LayerMask.NameToLayer(StorageLayers.IgnoreLayer);
                if (ignoreLayer != -1)
                {
                    hostGo.layer = ignoreLayer;
                }

                m_destroyHostGo = true;
            }
            m_hostGo = hostGo;
            
            m_assetsRoot = new GameObject("Assets Root");
            m_assetsRoot.SetActive(false);
            m_assetsRoot.transform.SetParent(m_hostGo.transform, false);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (m_workloadController != null)
            {
                if (Application.isEditor && !Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(m_workloadController);
                }
                else
                {
                    UnityEngine.Object.Destroy(m_workloadController);
                }
            }

            if (m_destroyHostGo)
            {
                if(Application.isEditor && !Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(m_hostGo);
                }
                else
                {
                    UnityEngine.Object.Destroy(m_hostGo);
                }   
            }
        }

        public override void Clear()
        {
            base.Clear();

            while (true)
            {
                object tempRoot = m_tempRootPool.Get();
                if (tempRoot == null)
                {
                    break;
                }

                Transform transform = tempRoot as Transform;
                if (transform != null)
                {
                    UnityEngine.Object.Destroy(transform.gameObject);
                }
            }
        }
    }

    public abstract class ModuleDependencies<TID, TFID> : IModuleDependencies<TID, TFID>
        where TID : IEquatable<TID>
        where TFID : IEquatable<TFID>
    {
        private readonly Pool<IIDMap<TID>> m_idMapsPool = new Pool<IIDMap<TID>>();
        private readonly Pool<IAssetMap<TID>> m_assetMapsPool = new Pool<IAssetMap<TID>>();
        private readonly Pool<ISurrogatesSerializer<TID>> m_serializersPool = new Pool<ISurrogatesSerializer<TID>>();
        private readonly Pool<IObjectTreeEnumerator> m_enumeratorsPool = new Pool<IObjectTreeEnumerator>();
        private readonly Pool<ISerializationContext<TID>> m_contextsPool = new Pool<ISerializationContext<TID>>();

        private IShaderUtil m_shaderUtil;
        private ITypeMap m_typeMap;
        private ISerializer m_serializer;
        private IWorkloadController m_workloadController;
        private IDataLayer<TFID> m_dataLayer;
   
        public virtual object AssetsRoot
        {
            get;
        }

        protected virtual IShaderUtil CreateShaderUtil()
        {
            return new RTShaderUtil();
        }

        protected virtual void DestroyShaderUtil()
        {
        }

        public IShaderUtil ShaderUtil
        {
            get
            {
                if (m_shaderUtil == null)
                {
                    m_shaderUtil = CreateShaderUtil();
                }

                return m_shaderUtil;
            }
        }

        protected virtual ITypeMap CreateTypeMap()
        {
            return new TypeMap();
        }

        protected virtual void DestroyTypeMap()
        {
        }

        public ITypeMap TypeMap
        {
            get
            {
                if (m_typeMap == null)
                {
                    m_typeMap = CreateTypeMap();
                }

                return m_typeMap;
            }
        }

        protected abstract ISerializer CreateSerializer();

        protected virtual void DestroyMessagePackSerializer()
        {
        }

        public ISerializer Serializer
        {
            get
            {
                if (m_serializer == null)
                {
                    m_serializer = CreateSerializer();
                }

                return m_serializer;
            }
        }

        protected abstract IWorkloadController CreateWorkloadController();

        protected virtual void DestroyWorkloadController()
        {
        }

        public IWorkloadController WorkloadController
        {
            get
            {
                if (m_workloadController == null)
                {
                    m_workloadController = CreateWorkloadController();
                }

                return m_workloadController;
            }
        }

        protected abstract IDataLayer<TFID> CreateDataLayer();

        protected virtual void DestroyDataLayer()
        {
        }

        public IDataLayer<TFID> DataLayer
        {
            get
            {
                if (m_dataLayer == null)
                {
                    m_dataLayer = CreateDataLayer();
                }

                return m_dataLayer;
            }
        }

        protected abstract IIDMap<TID> CreateIDMap();

        public IIDMap<TID> AcquireIDMap(IIDMap<TID> parent = null)
        {
            IIDMap<TID> idmap = m_idMapsPool.Get();
            if (idmap == null)
            {
                idmap = CreateIDMap();
            }

            idmap.ParentMap = parent;
            return idmap;
        }

        public void ReleaseIDMap(IIDMap<TID> idmap)
        {
            idmap.ParentMap = null;
            idmap.Reset();
            m_idMapsPool.Release(idmap);
        }
        protected abstract IAssetMap<TID> CreateAssetMap();

        public IAssetMap<TID> AcquireAssetMap(IAssetMap<TID> parent = null)
        {
            IAssetMap<TID> assetmap = m_assetMapsPool.Get();
            if (assetmap == null)
            {
                assetmap = CreateAssetMap();
            }

            assetmap.ParentMap = parent;
            return assetmap;
        }

        public void ReleaseAssetMap(IAssetMap<TID> assetmap)
        {
            assetmap.ParentMap = null;
            assetmap.Reset();
            m_assetMapsPool.Release(assetmap);
        }


        protected abstract ISurrogatesSerializer<TID> CreateSurrogatesSerializer();

        public ISurrogatesSerializer<TID> AcquireSerializer()
        {
            ISurrogatesSerializer<TID> serializer = m_serializersPool.Get();
            if (serializer == null)
            {
                serializer = CreateSurrogatesSerializer();
            }
            return serializer;
        }

        public void ReleaseSerializer(ISurrogatesSerializer<TID> serializer)
        {
            serializer.Reset();
            m_serializersPool.Release(serializer);
        }

        protected abstract IObjectTreeEnumerator CreateEnumerator(object obj);

        public IObjectTreeEnumerator AcquireEnumerator(object obj)
        {
            IObjectTreeEnumerator enumerator = m_enumeratorsPool.Get();
            if (enumerator == null)
            {
                enumerator = CreateEnumerator(obj);
            }
            else
            {
                enumerator.Root = obj;
            }
            return enumerator;
        }

        public void ReleaseEnumerator(IObjectTreeEnumerator enumerator)
        {
            enumerator.Reset();
            m_enumeratorsPool.Release(enumerator);
        }

        protected virtual ISerializationContext<TID> CreateContext()
        {
            return new SerializationContext<TID>();
        }

        public ISerializationContext<TID> AcquireContext()
        {
            ISerializationContext<TID> context = m_contextsPool.Get();
            if (context == null)
            {
                context = CreateContext();
            }
            return context;
        }

        public void ReleaseContext(ISerializationContext<TID> context)
        {
            context.Reset();
            m_contextsPool.Release(context);
        }

        //public abstract object AcquireTempRoot();

        //public abstract void ReleaseTempRoot(object tempRoot);

        public virtual void Clear()
        {
            m_idMapsPool.Clear();
            m_assetMapsPool.Clear();
            m_serializersPool.Clear();
            m_enumeratorsPool.Clear();
            m_contextsPool.Clear();

            DestroyTypeMap();
            m_typeMap = null;

            DestroyShaderUtil();
            m_shaderUtil = null;

            DestroyDataLayer();
            m_dataLayer = null;

            DestroyMessagePackSerializer();
            m_serializer = null;

            DestroyWorkloadController();
            m_workloadController = null;
        }

        public virtual void Dispose()
        {
            Clear();
        }
    }
}
