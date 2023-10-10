using System.IO;
using System.Threading.Tasks;

namespace Battlehub.Storage
{
    public struct Pack<T>
    {
        public bool IsEmpty
        {
            get;
            private set;
        }

        public T Data
        {
            get;
            private set;
        }

        public Pack(bool isEmpty, T data = default(T))
        {
            IsEmpty = isEmpty;
            Data = data;
        }
    }
    
    public interface ISerializer
    {
        void Serialize<T>(Stream stream, T obj);

        ValueTask<Pack<T>> Deserialize<T>(Stream stream);
    }
}
