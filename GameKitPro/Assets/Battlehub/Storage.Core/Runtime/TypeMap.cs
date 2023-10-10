using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.Storage
{
    public interface ITypeMap
    {
        IReadOnlyCollection<Type> Types { get; }

        bool TryGetID(Type type, out int id);

        bool TryGetType(int id, out Type type);

        void Register(Type type, int id);
    }

    public class TypeMap : ITypeMap
    {
        private const int k_GameObjectTypeID = -2;
        private const int k_IListTypeID = -3;

        private readonly Dictionary<int, Type> m_idToType;
        private readonly Dictionary<Type, int> m_typeToId = new Dictionary<Type, int>()
        {
            { typeof(GameObject), k_GameObjectTypeID },
        };

        public IReadOnlyCollection<Type> Types
        {
            get { return m_typeToId.Keys; }
        }

        public TypeMap()
        {
            m_idToType = new Dictionary<int, Type>();
            foreach (var kvp in m_typeToId)
            {
                m_idToType.Add(kvp.Value, kvp.Key);
            }
        }

        public bool TryGetID(Type type, out int id)
        {
            if(m_typeToId.TryGetValue(type, out id))
            {
                return true;
            }

            if (typeof(IList).IsAssignableFrom(type)) 
            {
                id = k_IListTypeID;
                return true;
            }

            return false;
        }

        public bool TryGetType(int id, out Type type)
        {
            if(m_idToType.TryGetValue(id, out type))
            {
                return true;
            }

            if (id == k_IListTypeID)
            {
                type = typeof(IList);
            }

            return false;
        }

        public void Register(Type type, int id)
        {
            m_typeToId.Add(type, id);
            m_idToType.Add(id, type);
        } 
    }
}
