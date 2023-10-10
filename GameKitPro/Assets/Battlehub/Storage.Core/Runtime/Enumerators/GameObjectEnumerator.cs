using System;
using System.Collections.Generic;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Battlehub.Storage
{
    public class GameObjectEnumerator : BaseEnumerator
    {
        private GameObject m_root;
        private readonly List<UnityObject> m_objects = new List<UnityObject>();
        private readonly Dictionary<Type, int> m_componentTypeToIndex = new Dictionary<Type, int>();

        public override int CurrentKey
        {
            get 
            {
                var componentType = CurrentType;
                if (componentType == null)
                {
                    return -1;
                }

                return m_componentTypeToIndex[componentType];
            }
        }

        public override object Object
        {
            get { return m_root; }
            set
            {
                m_root = (GameObject)value;
                if (m_root != null)
                {
                    int childCount = m_root.transform.childCount;
                    Component[] components = m_root.GetComponents<Component>();

                    for (int i = 0; i < childCount; ++i)
                    {
                        m_objects.Add(m_root.transform.GetChild(i).gameObject);
                    }

                    for (int i = 0; i < components.Length; ++i)
                    {
                        m_objects.Add(components[i]);
                    }
                }
                else
                {
                    m_objects.Clear();
                    m_componentTypeToIndex.Clear();
                }
            }
        }

        public override bool MoveNext()
        {
            int count = m_objects.Count;
            if (Index < count)
            {
                Current = m_objects[Index];

                var componentType = CurrentType;
                if (componentType != null) 
                {
                    if(m_componentTypeToIndex.TryGetValue(componentType, out int index))
                    {
                        index++;
                    }
                    else
                    {
                        index = 0;
                    }

                    m_componentTypeToIndex[componentType] = index;
                }
            }
            else
            {
                Current = null;
                return false;
            }

            Index++;
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            Index = 0;
            m_componentTypeToIndex.Clear();
        }
    }
}
