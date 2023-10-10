using System;
using System.Collections;

namespace Battlehub.Storage
{
    public interface IEnumeratorState
    {
        Type ObjectType
        {
            get;
        }

        Type CurrentType
        {
            get;
        }

        int CurrentKey
        {
            get;
        }

        bool IsTerminal
        {
            get;
        }
    }

    public interface IObjectEnumerator : IEnumerator, IEnumeratorState
    {
        object Object
        {
            get;
            set;
        }
    }

    public class ObjectEnumeratorAttribute : Attribute
    {
        public Type[] Types
        {
            get;
            private set;
        }

        public string FilePath
        {
            get;
            private set;
        }

        public ObjectEnumeratorAttribute(Type type, [System.Runtime.CompilerServices.CallerFilePath] string filePath = null)
        {
            Types = new[] { type };
            FilePath = filePath;
        }

        public ObjectEnumeratorAttribute(Type type1, Type type2, [System.Runtime.CompilerServices.CallerFilePath] string filePath = null)
        {
            Types = new[] { type1, type2 };
            FilePath = filePath;
        }

        public ObjectEnumeratorAttribute(Type type1, Type type2, Type type3, [System.Runtime.CompilerServices.CallerFilePath] string filePath = null)
        {
            Types = new[] { type1, type2, type3 };
            FilePath = filePath;
        }

        public ObjectEnumeratorAttribute(Type[] types, [System.Runtime.CompilerServices.CallerFilePath] string filePath = null)
        {
            Types = types;
            FilePath = filePath;
        }
    }

    public class BaseEnumerator : IObjectEnumerator
    {
        protected int Index
        {
            get;
            set;
        }

        public virtual object Object
        {
            get;
            set;
        }

        public Type ObjectType
        {
            get { return Object != null ? Object.GetType() : null; }
        }

        public virtual object Current
        {
            get;
            protected set;
        }

        public Type CurrentType
        {
            get { return Current != null ? Current.GetType() : null; }
        }

        public virtual int CurrentKey
        {
            get { return Index; }
        }

        public bool IsTerminal
        {
            get { return Object == Current; }
        }

        object IEnumerator.Current => Current;

        public virtual bool MoveNext()
        {
            return false;
        }

        public virtual void Reset()
        {
            Current = null;
        }

        public virtual void Dispose()
        {
            Reset();
            Object = null;
        }
    }
}