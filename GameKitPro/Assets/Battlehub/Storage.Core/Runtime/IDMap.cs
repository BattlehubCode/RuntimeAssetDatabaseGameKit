using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.Storage
{
    public abstract class IDMap<TID> : IIDMap<TID> where TID : IEquatable<TID>
    {
        public bool IsReadOnly
        {
            get;
            set;
        }

        private IIDMap<TID> m_parentMap;
        public IIDMap<TID> ParentMap
        {
            get { return m_parentMap; }
            set { m_parentMap = value; }
        }

        public abstract TID NullID { get; }

        public abstract TID CreateID();

        private Dictionary<TID, object> m_idToObject = new Dictionary<TID, object>();
        public IReadOnlyDictionary<object, TID> ObjectToID
        {
            get { return m_objectToId; }
        }

        private Dictionary<object, TID> m_objectToId = new Dictionary<object, TID>();
        public IReadOnlyDictionary<TID, object> IDToObject
        {
            get { return m_idToObject; }
        }

        public bool TryGetID(object obj, out TID id)
        {
            if (m_objectToId.TryGetValue(obj, out id))
            {
                return true;
            }

            if (m_parentMap != null && m_parentMap.TryGetID(obj, out id))
            {
                return true;
            }

            return false;
        }

        public bool TryGetObject<T>(TID id, out T obj)
        {
            if (m_idToObject.TryGetValue(id, out object o))
            {
                if (o is T)
                {
                    obj = (T)o;
                }
                else
                {
                    obj = default;
                }

                return true;
            }

            if (m_parentMap != null)
            {
                if (m_parentMap.TryGetObject(id, out obj))
                {
                    return true;
                }
            }
            else
            {
                obj = default;
            }

            return false;
        }

        TID IIDMap<TID>.GetOrCreateID(object obj)
        {
            if (obj == null)
            {
                return NullID;
            }

            if (m_objectToId.TryGetValue(obj, out TID id))
            {
                return id;
            }
            else
            {
                if (m_parentMap != null && m_parentMap.TryGetID(obj, out id))
                {
                    return id;
                }
            }

            if (IsReadOnly)
            {
                return NullID;
            }

            id = CreateID();
            m_objectToId.Add(obj, id);
            m_idToObject.Add(id, obj);
            return id;
        }

        void IIDMap<TID>.AddObject(object obj, TID id)
        {
            if (IsReadOnly)
            {
                Debug.LogWarning($"Can't add object. Is Read Only = {IsReadOnly}");
                return;
            }

            if (obj == null)
            {
                //Debug.LogWarning("Can't set id obj == null");
                m_idToObject[id] = null;
                return;
            }

            if (m_parentMap == null || !m_parentMap.TryGetID(obj, out _))
            {
                /*
                if (m_parentMap != null && m_parentMap.TryGetObject(id, out object _))
                {
                    Debug.LogWarning($"Object with id {id} already exists");
                }
                */

                m_idToObject[id] = obj;
                m_objectToId[obj] = id;
            }
        }

        public T GetObject<T>(TID id)
        {
            T obj = default;
            if (EqualityComparer<TID>.Default.Equals(NullID, id))
            {
                return obj;
            }

            if (m_idToObject.TryGetValue(id, out object o))
            {
                if (o is T)
                {
                    obj = (T)o;
                }
                return obj;
            }

            if (m_parentMap != null)
            {
                obj = m_parentMap.GetObject<T>(id);
            }

            return obj;
        }

        public T GetOrCreateObject<T>(TID id) where T : new()
        {
            T obj = GetObject<T>(id);
            if (!EqualityComparer<T>.Default.Equals(obj, default))
            {
                return obj;
            }

            if (IsReadOnly)
            {
                Debug.LogWarning($"Can't create object. Is Read Only = {IsReadOnly}");
                return obj;
            }

            if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
            {
                obj = (T)Convert.ChangeType(ScriptableObject.CreateInstance(typeof(T)), typeof(T));
            }
            else
            {
                obj = new T();
            }

            IIDMap<TID> idmap = this;
            idmap.AddObject(obj, id);

            return obj;
        }

        public bool Remove(TID id)
        {
            if (IsReadOnly)
            {
                Debug.LogWarning($"Can't remove object. Is Read Only = {IsReadOnly}");
                return false;
            }

            bool remove = false;

            if (TryGetObject(id, out object obj))
            {
                m_objectToId.Remove(obj);
                m_idToObject.Remove(id);
            }

            return remove;
        }

        public void Reset()
        {
            m_parentMap = null;
            m_idToObject.Clear();
            m_objectToId.Clear();
            IsReadOnly = false;
        }

        public void Commit()
        {
            if (IsReadOnly)
            {
                return;
            }

            if (m_parentMap == null)
            {
                return;
            }

            foreach (var kvp in m_idToObject)
            {
                TID id = kvp.Key;
                object obj = kvp.Value;
                if (obj == null)
                {
                    continue;
                }
                if (m_parentMap.TryGetObject(id, out object existingObject) && existingObject != obj)
                {
                    throw new IDMapCommitException($"Commit failed. Object {(existingObject != null ? existingObject.GetType().FullName : string.Empty)} with id {id} already added");
                }
                if (m_parentMap.TryGetID(obj, out TID existingID) && !existingID.Equals(id))
                {
                    throw new IDMapCommitException($"Commit failed. The same object with id {existingID} already added");
                }
            }

            foreach (var kvp in m_idToObject)
            {
                TID id = kvp.Key;
                object obj = kvp.Value;
                if (obj == null)
                {
                    continue;
                }

                m_parentMap.AddObject(obj, id);
            }
        }

        public void Rollback()
        {
            if (IsReadOnly)
            {
                return;
            }

            if (m_parentMap == null)
            {
                return;
            }

            foreach (var kvp in m_idToObject)
            {
                TID id = kvp.Key;
                m_parentMap.Remove(id);
            }
        }
    }

    public class IDMap : IDMap<Guid>
    {
        public override Guid NullID => Guid.Empty;

        public override Guid CreateID()
        {
            return Guid.NewGuid();
        }

        public string m_debugName;
        public IDMap()
        {
            m_debugName = Guid.NewGuid().ToString();
        }


    }
}

