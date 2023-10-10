using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Battlehub.Storage
{
    public struct SerializerOptions
    {
        public static readonly SerializerOptions Default = new SerializerOptions(false, loadImagesAsync:true);

        public bool SupportsMultidimensionalArrays
        {
            get;
            set;
        }

        public bool LoadImagesAsync
        {
            get;
            set;
        }

        public SerializerOptions(bool supportsMultimimensionalArrays, bool loadImagesAsync)
        {
            SupportsMultidimensionalArrays = supportsMultimimensionalArrays;
            LoadImagesAsync = loadImagesAsync;
        }
    }

    public interface ISerializationContext<TID> where TID : IEquatable<TID>
    {
        IIDMap<TID> IDMap
        {
            get;
            set;
        }

        IShaderUtil ShaderUtil
        {
            get;
            set;
        }

        object TempRoot
        {
            get;
            set;
        }

        SerializerOptions Options
        {
            get;
            set;
        }

        void Reset();
    }

    public class SerializationContext<TID> : ISerializationContext<TID> where TID : IEquatable<TID>
    {
        public IIDMap<TID> IDMap
        {
            get;
            set;
        }

        public IShaderUtil ShaderUtil
        {
            get;
            set;
        }

        public object TempRoot
        {
            get;
            set;
        }

        public SerializerOptions Options
        {
            get;
            set;
        }

        public SerializationContext()
        {
            Options = SerializerOptions.Default;
        }

        //Other properties required for serialization/deserialization
        //..

        public virtual void Reset()
        {
            IDMap = null;
            ShaderUtil = null;
            TempRoot = null;
            Options = SerializerOptions.Default;
        }
    }

    public interface ISurrogatesSerializer<TID> where TID : IEquatable<TID>
    {
        ValueTask<bool> Enqueue(object obj, ISerializationContext<TID> context);

        bool SerializeToStream(Stream stream);

        bool CopyToDeserializationQueue();

        Task DeserializeFromStream(Stream stream);

        ValueTask<object> Dequeue(ISerializationContext<TID> context);

        void Reset();
    }
}
