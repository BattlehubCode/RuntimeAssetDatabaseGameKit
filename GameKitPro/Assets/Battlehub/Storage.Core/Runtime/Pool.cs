using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Battlehub.Storage
{
    public interface IPool<T>
    {
        T Get();
        void Release(T obj);

        void Clear();
    }

    public class Pool<T> : IPool<T>
    {
        private Stack<T> m_pool = new Stack<T>();
        
        public T Get()
        {
            if (m_pool.Count == 0)
            {
                return default(T);
            }

            return m_pool.Pop();
        }

        public void Release(T obj)
        {
            m_pool.Push(obj);
        }

        public void Clear()
        {
            m_pool.Clear();
        }
    }

    public class ConcurrentPool<T> : IPool<T>
    {
        private ConcurrentStack<T> m_pool = new ConcurrentStack<T>();

        public T Get()
        {
            if (!m_pool.TryPop(out T result))
            {
                return default(T);
            }

            return result;
        }

        public void Release(T obj)
        {
            m_pool.Push(obj);
        }

        public void Clear()
        {
            m_pool.Clear();
        }
    }
}
