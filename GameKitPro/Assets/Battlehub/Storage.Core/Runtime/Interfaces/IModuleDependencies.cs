using System;

namespace Battlehub.Storage
{
    public class SafeRef<T> : IDisposable
        where T : class
    {
        private T m_target;
        private Action<T> m_dispose;

        public SafeRef(T target, Action<T> dispose)
        {
            m_target = target;
            m_dispose = dispose;
        }

        public T Get()
        {
            if (m_dispose == null)
            {
                throw new ObjectDisposedException("SafeRef");
            }

            return m_target;
        }

        public T Detach()
        {
            T detached = m_target;

            m_dispose = null;
            m_target = null;

            return detached;
        }

        public void Dispose()
        {
            if (m_dispose != null)
            {
                m_dispose.Invoke(m_target);
                m_dispose = null;
                m_target = null;
            }
        }
    }

    public static class IModuleDependencies
    {
        public static SafeRef<IIDMap<TID>> AcquireIDMapRef<TID, TFID>(this IModuleDependencies<TID, TFID> storageDependenciess, IIDMap<TID> parent = null) 
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return new SafeRef<IIDMap<TID>>(storageDependenciess.AcquireIDMap(parent), storageDependenciess.ReleaseIDMap);
        }

        public static SafeRef<IAssetMap<TID>> AcquireAssetMapRef<TID, TFID>(this IModuleDependencies<TID, TFID> storageDependenciess, IAssetMap<TID> parent = null)
          where TID : IEquatable<TID>
          where TFID : IEquatable<TFID>
        {
            return new SafeRef<IAssetMap<TID>>(storageDependenciess.AcquireAssetMap(parent), storageDependenciess.ReleaseAssetMap);
        }

        public static SafeRef<ISurrogatesSerializer<TID>> AcquireSerializerRef<TID, TFID>(this IModuleDependencies<TID, TFID> storageDependenciess)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return new SafeRef<ISurrogatesSerializer<TID>>(storageDependenciess.AcquireSerializer(), storageDependenciess.ReleaseSerializer);
        }

        public static SafeRef<IObjectTreeEnumerator> AcquireEnumeratorRef<TID, TFID>(this IModuleDependencies<TID, TFID> storageDependenciess, object obj)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return new SafeRef<IObjectTreeEnumerator>(storageDependenciess.AcquireEnumerator(obj), storageDependenciess.ReleaseEnumerator);
        }

        public static SafeRef<ISerializationContext<TID>> AcquireContextRef<TID, TFID>(this IModuleDependencies<TID, TFID> storageDependenciess)
            where TID : IEquatable<TID>
            where TFID : IEquatable<TFID>
        {
            return new SafeRef<ISerializationContext<TID>>(storageDependenciess.AcquireContext(), storageDependenciess.ReleaseContext);
        }
    }

    public interface IModuleDependencies<TID, TFID> : IDisposable
       where TID : IEquatable<TID>
       where TFID : IEquatable<TFID>
    {
        object AssetsRoot
        {
            get;
        }

        IShaderUtil ShaderUtil
        {
            get;
        }

        ITypeMap TypeMap
        {
            get;
        }

        IDataLayer<TFID> DataLayer
        {
            get;
        }

        ISerializer Serializer
        {
            get;
        }

        IWorkloadController WorkloadController
        {
            get;
        }

        IIDMap<TID> AcquireIDMap(IIDMap<TID> parent = null);
        void ReleaseIDMap(IIDMap<TID> idmap);

        IAssetMap<TID> AcquireAssetMap(IAssetMap<TID> parent = null);
        void ReleaseAssetMap(IAssetMap<TID> assetmap);
        
        ISurrogatesSerializer<TID> AcquireSerializer();
        void ReleaseSerializer(ISurrogatesSerializer<TID> serializer);

        IObjectTreeEnumerator AcquireEnumerator(object obj);
        void ReleaseEnumerator(IObjectTreeEnumerator enumerator);

        ISerializationContext<TID> AcquireContext();
        void ReleaseContext(ISerializationContext<TID> context);

        void Clear();
    }
}
