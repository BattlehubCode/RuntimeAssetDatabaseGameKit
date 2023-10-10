using System.Collections;

namespace Battlehub.Storage
{
    public class IListEnumerator : BaseEnumerator
    {
        private IList m_root;

        public override object Object
        {
            get { return m_root; }
            set { m_root = (IList)value; }
        }

        public override int CurrentKey
        {
            get { return Index - 1; }
        }

        public override bool MoveNext()
        {
            if (Index >= m_root.Count)
            {
                Current = null;
                return false;
            }

            Current = m_root[Index];
            Index++;
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            Index = 0;
        }
    }
}
