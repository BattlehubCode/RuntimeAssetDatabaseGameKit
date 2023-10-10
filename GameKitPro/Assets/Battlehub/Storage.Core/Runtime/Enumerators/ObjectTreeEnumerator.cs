using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Battlehub.Storage
{
    public interface IObjectTreeEnumerator : IEnumerator
    {
        object Root
        {
            get;
            set;
        }

        object Parent
        {
            get;
        }

        string GetCurrentPath(ITypeMap typeMap);

        // Trims object subtree at current hierarchy level
        bool Trim();
    }

    public class FallbackEnumerator : BaseEnumerator
    {
        public override bool MoveNext()
        {
            Current = Object;
            return 0 == Index++;
        }

        public override void Reset()
        {
            base.Reset();
            Index = 0;
        }
    }

    public class ObjectTreeEnumerator : IObjectTreeEnumerator
    {
        private const int k_stackSize = 10;
        private IObjectEnumeratorFactory m_enumeratorFactory;
        private IObjectEnumerator m_currentEnumerator;
        private Stack<IObjectEnumerator> m_enumeratorsStack = new Stack<IObjectEnumerator>(k_stackSize);
        private HashSet<object> m_activeEnumerators = new HashSet<object>();
        
        private Dictionary<Type, ObjectEnumeratorPool> m_enumeratorPools = new Dictionary<Type, ObjectEnumeratorPool>();
        private HashSet<Type> m_nonEnumerableTypes = new HashSet<Type>();
        private HashSet<object> m_visited = new HashSet<object>();
        
        private StringBuilder m_sb;

        private object m_root;
        public object Root
        {
            get { return m_root; }
            set 
            {
                if(m_currentEnumerator != null)
                {
                    Reset();
                }

                m_root = value;
                m_currentEnumerator = AcquireEnumerator(m_root, true);
            }
        }

        public ObjectTreeEnumerator(object root, IObjectEnumeratorFactory factory)
        {
            m_enumeratorFactory = factory;
            Root = root;
        }

        public ObjectTreeEnumerator(IObjectEnumeratorFactory factory)
        {
            m_enumeratorFactory = factory;
        }

        public object Current
        {
            get;
            private set;
        }

        public object Parent
        {
            get { return m_currentEnumerator.Object; }
        }

        private IEnumeratorState CurrentState
        {
            get { return m_currentEnumerator; }
        }

        private IReadOnlyCollection<IEnumeratorState> CurrentPath
        {
            get { return m_enumeratorsStack; }
        }

        public string GetCurrentPath(ITypeMap typeMap)
        {
            // This method may create a path that may differ from what is expected.
            // For example, if the "tr5" transform is referenced by a custom component, the path to tr1 might look like this: /root/custom component/tr5
            // instead of /root/tr0/tr1/tr2/tr3/tr4/tr5

            if (m_sb == null)
            {
                m_sb = new StringBuilder();
            }
            else
            {
                m_sb.Clear();
            }
            
            var currentState = CurrentState;
            if (!currentState.IsTerminal)
            {
                var type = currentState.CurrentType;
                int key = currentState.CurrentKey;

                AppendPathSegment(typeMap, type, key);
            }

            var currentPath = CurrentPath;
            foreach (var state in currentPath)
            {
                var type = state.CurrentType;
                int key = state.CurrentKey;

                AppendPathSegment(typeMap, type, key);
            }

            return m_sb.ToString();
        }

        private void AppendPathSegment(ITypeMap typeMap, Type type, int key)
        {
            m_sb.Append(key);
            m_sb.Append(":");
            
            if (typeMap.TryGetID(type, out int id))
            {
                m_sb.Append(id);
            }
            else
            {
                m_sb.Append(-1);
            }
            m_sb.Append("/");
        }

        private IObjectEnumerator AcquireEnumerator(object obj, bool useFallback)
        {
            Type type = obj.GetType();
            IObjectEnumerator enumerator;
            if (m_enumeratorPools.TryGetValue(type, out ObjectEnumeratorPool pool))
            {
                enumerator = pool.Acquire();
                if (enumerator == null)
                {
                    enumerator = m_enumeratorFactory.Create(obj, type);
                }
            }
            else
            {
                if (m_nonEnumerableTypes.Contains(type))
                {
                    return null;
                }
                
                enumerator = m_enumeratorFactory.Create(obj, type);
            }

            if(enumerator != null)
            {
                enumerator.Object = obj;
            }
            else
            {
                if(useFallback)
                {
                    enumerator = new FallbackEnumerator();
                    enumerator.Object = obj;
                }
                else
                {
                    m_nonEnumerableTypes.Add(type);
                }
            }

            return enumerator;
        }

        private void ReleaseEnumerator(IObjectEnumerator enumerator)
        {
            if (enumerator is FallbackEnumerator)
            {
                return;
            }

            if (enumerator.Object == null)
            {
                Debug.Log("Can't return enumerator to pool");
                return;
            }

            Type type = enumerator.Object.GetType();
            if (!m_enumeratorPools.TryGetValue(type, out ObjectEnumeratorPool pool))
            {
                pool = new ObjectEnumeratorPool();
                m_enumeratorPools.Add(type, pool);
            }

            enumerator.Reset();
            enumerator.Object = null;
            pool.Release(enumerator);
        }

        public bool MoveNext()
        {
            do
            {
                if (m_currentEnumerator.MoveNext())
                {
                    object current = m_currentEnumerator.Current;

                    IObjectEnumerator enumerator = null;

                    if (current != null && current != m_currentEnumerator.Object)
                    {
                        Type currentType = current.GetType();
                        if (currentType.IsValueType)
                        {
                            //#warning Check if this code needed
                            Type objectType = m_currentEnumerator.Object.GetType();
                            if (currentType != objectType) //recursive struct is not possible
                            {
                                enumerator = AcquireEnumerator(current, false);
                            }
                        }
                        else
                        {
                            enumerator = AcquireEnumerator(current, false);
                        }
                    }

                    if (enumerator != null)
                    {
                        if (m_activeEnumerators.Add(current))
                        {
                            m_enumeratorsStack.Push(m_currentEnumerator);
                            m_currentEnumerator = enumerator;
                        }    
                    }
                    else
                    {
                        if (m_visited.Add(current)) // prevent visiting the same object twice (possible with arrays of transforms for example)
                        {
                            Current = current;
                            return true;
                        }
                    }
                }
                else
                {
                    if (!Trim())
                    {
                        return false;
                    }
                }
            }
            while (true);
        }

        public bool Trim()
        {
            if (m_currentEnumerator != null)
            {
                m_activeEnumerators.Remove(m_currentEnumerator.Object);
                ReleaseEnumerator(m_currentEnumerator);
            }

            if (m_enumeratorsStack.Count == 0)
            {
                Current = null;
                return false;
            }

            m_currentEnumerator = m_enumeratorsStack.Pop();
            return true;
        }
        
        public void Dispose()
        {
            Reset();
        }

        public void Reset()
        {
            m_enumeratorsStack.Clear();
            m_currentEnumerator.Reset();
            m_visited.Clear();
            m_activeEnumerators.Clear();
            m_nonEnumerableTypes.Clear();
        }
    }
}
