using UnityEngine;

namespace Battlehub.Storage
{
    public class ObjectEnumerator<T> : BaseEnumerator
    {
        private T m_typedObject;
        protected T TypedObject
        {
            get { return m_typedObject; }
        }

        public override object Object
        {
            get { return m_typedObject; }
            set 
            { 
                if(value == null)
                {
                    m_typedObject = default;
                }
                else
                {
                    m_typedObject = (T)value;
                }
            }
        }

        private int m_currentKey = -1;

        public override int CurrentKey
        {
            get { return m_currentKey; }
        }

        /// key field from surrogate (persistent property identifier)

        protected bool MoveNext<TObj>(TObj obj, int key)
        {
            Current = obj;
            Index++;
            m_currentKey = key;
            return Current != null;
        }

        /// key field from surrogate (persistent property identifier)

        protected bool MoveNext(Component component, int key)
        {
            GameObject go = null;
            if (component != null)
            {
                go = component.gameObject;
            }

            return MoveNext(go, key);
        }

        public override void Reset()
        {
            base.Reset();
            Index = 0;
            m_currentKey = -1;
        }
    }
}
