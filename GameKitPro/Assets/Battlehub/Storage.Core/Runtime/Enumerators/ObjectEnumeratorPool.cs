using System.Collections.Generic;

namespace Battlehub.Storage
{
    public class ObjectEnumeratorPool
    {
        private const int k_stackSize = 100;
        private IObjectEnumerator m_first;
        private readonly Stack<IObjectEnumerator> m_pool = new Stack<IObjectEnumerator>(k_stackSize);

        public IObjectEnumerator Acquire()
        {
            if (m_first != null)
            {
                IObjectEnumerator enumerator = m_first;
                m_first = m_pool.Count > 0 ? m_pool.Pop() : null;
                return enumerator;
            }
            return null;
        }

        public void Release(IObjectEnumerator enumerator)
        {
            if (m_first != null)
            {
                m_pool.Push(m_first);
            }
            m_first = enumerator;
        }

        public void Clear()
        {
            m_pool.Clear();
            m_first = null;
        }
    }
}
