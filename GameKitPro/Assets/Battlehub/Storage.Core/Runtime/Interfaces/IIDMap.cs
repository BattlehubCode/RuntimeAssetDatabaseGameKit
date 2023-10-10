using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.Storage
{
    public static class IIDMapExtensions
    {
        public static List<TID> GetOrCreateIDs<TID>(this IIDMap<TID> idmap, IList list) where TID : IEquatable<TID>
        {
            if (list == null)
            {
                return null;
            }

            List<TID> idsList = new List<TID>(list.Count);
            foreach (object obj in list)
            {
                idsList.Add(idmap.GetOrCreateID(obj));
            }
            return idsList;
        }

        public static TID[] GetOrCreateIDs<TID>(this IIDMap<TID> idmap, Array array) where TID : IEquatable<TID>
        {
            if (array == null)
            {
                return null;
            }

            TID[] ids = new TID[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                ids[i] = idmap.GetOrCreateID(array.GetValue(i));
            }
            return ids;

        }

        public static T[] GetObjects<T, TID>(this IIDMap<TID> idmap, TID[] ids, bool updateRefCounter = true) where TID : IEquatable<TID> 
        {
            T[] objects = null;
            if(ids != null)
            {
                objects = new T[ids.Length];
                for (int i = 0; i < ids.Length; ++i)
                {
                    T obj = idmap.GetObject<T>(ids[i]);
                    objects[i] = obj;
                }
            }
            return objects;
        }

        public static List<T> GetObjects<T, TID>(this IIDMap<TID> idmap, List<TID> ids, bool updateRefCounter = true) where TID : IEquatable<TID>
        {
            List<T> objects = null;
            if (ids != null)
            {
                objects = new List<T>(ids.Count);
                for (int i = 0; i < ids.Count; ++i)
                {
                    T obj = idmap.GetObject<T>(ids[i]);
                    objects.Add(obj);
                }
            }
            return objects;
        }

        public static Transform GetComponent<TID>(this IIDMap<TID> idmap, TID componentID, TID gameObjectID, Transform tempRoot) where TID : IEquatable<TID>
        {
            Debug.Assert(!idmap.NullID.Equals(componentID));
            Transform transform = idmap.GetObject<Transform>(componentID);
            if (transform == null)
            {
                Debug.Assert(!idmap.NullID.Equals(gameObjectID));
                GameObject gameObject = idmap.GetObject<GameObject>(gameObjectID);
                if (gameObject == null)
                {
                    //if ID is not points to prefab that create new GameObject();
                    //if ID points to prefab then load it and populate idmap with ids from it.
                    //then get game object by id,

                    gameObject = new GameObject();
                    gameObject.transform.SetParent(tempRoot);
                    idmap.AddObject(gameObject, gameObjectID);
                }
                transform = gameObject.GetComponent<Transform>();
                idmap.AddObject(transform, componentID);
            }
            return transform;
        }

        public static T GetComponent<T, TID>(this IIDMap<TID> idmap, TID componentID, TID gameObjectID) where TID : IEquatable<TID> where T : Component
        {
            Debug.Assert(!idmap.NullID.Equals(componentID));
            T component = idmap.GetObject<T>(componentID);
            if (component == null)
            {
                Debug.Assert(!idmap.NullID.Equals(gameObjectID));
                GameObject gameObject = idmap.GetObject<GameObject>(gameObjectID);
                if (gameObject == null)
                {
                    gameObject = new GameObject();
                    idmap.AddObject(gameObject, gameObjectID);
                }
                component = gameObject.AddComponent<T>();
                idmap.AddObject(component, componentID);
            }
            return component;
        }
    }


    [Serializable]
    public class IDMapCommitException : Exception
    {
        public IDMapCommitException() { }
        public IDMapCommitException(string message) : base(message) { }
        public IDMapCommitException(string message, Exception inner) : base(message, inner) { }
        protected IDMapCommitException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public interface IIDMap<TID> where TID : IEquatable<TID>
    {
        bool IsReadOnly
        {
            get;
            set;
        }

        IIDMap<TID> ParentMap
        {
            get;
            set;
        }

        IReadOnlyDictionary<object, TID> ObjectToID
        {
            get;
        }

        IReadOnlyDictionary<TID, object> IDToObject
        {
            get;
        }

        TID NullID { get; }

        TID CreateID();

        bool TryGetID(object obj, out TID id);

        bool TryGetObject<T>(TID id, out T obj);

        /// <summary>
        /// This method will create id for the object and add it to the map (or return NullID) 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public TID GetOrCreateID(object obj);

        void AddObject(object obj, TID id);

        T GetObject<T>(TID id);

        T GetOrCreateObject<T>(TID id) where T : new();

        bool Remove(TID id);

        //Commit to parent
        void Commit();

        //Remove from parent
        void Rollback();

        void Reset();
    }
}
