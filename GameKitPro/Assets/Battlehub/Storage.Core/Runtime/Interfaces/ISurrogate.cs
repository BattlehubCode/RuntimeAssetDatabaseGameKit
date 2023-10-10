using System;
using System.Threading.Tasks;

namespace Battlehub.Storage
{
    public class SurrogateAttribute : Attribute
    {
        public Type Type
        {
            get;
            private set;
        }

        public int PropertyIndex
        {
            get;
            private set;
        }

        public int TypeIndex
        {
            get;
            private set;
        }

        public string FilePath
        {
            get;
            private set;
        }

        public bool EnableUpdates
        {
            get;
            private set;
        }

        public bool Enabled
        {
            get;
            private set;
        }

        public SurrogateAttribute(Type type, int propertyIndex, int typeIndex, bool enabled = true, bool enableUpdates = true,  [System.Runtime.CompilerServices.CallerFilePath] string filePath = null)
        {
            Type = type;
            PropertyIndex = propertyIndex;
            TypeIndex = typeIndex;
            Enabled = enabled;
            EnableUpdates = enableUpdates;
            FilePath = filePath;
        }
    }

    public interface IValueTypeSurrogate<TValue, TID> where TID : IEquatable<TID>
    {
        void Serialize(in TValue value, ISerializationContext<TID> ctx);
        TValue Deserialize(ISerializationContext<TID> ctx);
    }

    public interface ISurrogate<TID> where TID : IEquatable<TID>
    {
        ValueTask Serialize(object obj, ISerializationContext<TID> ctx);

        ValueTask<object> Deserialize(ISerializationContext<TID> ctx);
    }
}
