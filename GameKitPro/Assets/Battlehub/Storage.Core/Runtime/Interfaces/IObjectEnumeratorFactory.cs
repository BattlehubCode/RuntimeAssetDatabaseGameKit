using System;

namespace Battlehub.Storage
{
    public interface IObjectEnumeratorFactory
    {
        void Register(Type type, Type enumeratorType);

        IObjectEnumerator Create(object obj, Type type);
    }
}
