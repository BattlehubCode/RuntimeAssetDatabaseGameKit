using System;
using System.Collections.Generic;

namespace Battlehub.Storage
{
    public class AssetEnumerator<TID, TFID> : IEnumerator<object>
       where TID : IEquatable<TID>
       where TFID : IEquatable<TFID>
    {
        private IObjectTreeEnumerator m_enumerator;
        private ITypeMap m_typeMap;
        private IModuleDependencies<TID, TFID> m_deps;

        public object Current
        {
            get { return m_enumerator.Current; }
        }

        public string GetCurrentPath()
        {
            return m_enumerator.GetCurrentPath(m_typeMap);
        }

        public AssetEnumerator(IModuleDependencies<TID, TFID> deps, object obj)
        {
            m_deps = deps;

            m_enumerator = m_deps.AcquireEnumerator(obj);
            m_typeMap = m_deps.TypeMap;
        }

        ~AssetEnumerator()
        {
            DisposeEnumerator();
        }

        public bool MoveNext()
        {
            while(m_enumerator.MoveNext())
            {
                if (m_enumerator.Current == null)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public void Reset()
        {
            m_enumerator.Reset();
        }

        public void Dispose()
        {
            DisposeEnumerator();
            GC.SuppressFinalize(this);
        }

        private bool m_disposed;

        private void DisposeEnumerator()
        {
            if (!m_disposed)
            {
                m_deps.ReleaseEnumerator(m_enumerator);
                m_deps = null;
                m_disposed = true;
            }
        }
    }
}
