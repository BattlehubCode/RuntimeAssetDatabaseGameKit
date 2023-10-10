using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.Storage
{
    public class ObjectEnumeratorFactoryBase : IObjectEnumeratorFactory
    {
        private readonly Dictionary<Type, Type> m_typeToEnumerator = new Dictionary<Type, Type>();
        private readonly IShaderUtil m_shaderUtil;

        public ObjectEnumeratorFactoryBase(IShaderUtil shaderUtil)
        {
            m_shaderUtil = shaderUtil;
        }

        public void Register(Type type, Type enumeratorType)
        {
            m_typeToEnumerator[type] = enumeratorType;
        }

        public virtual IObjectEnumerator Create(object obj, Type type)
        {
            if (m_typeToEnumerator.TryGetValue(type, out Type enumeratorType))
            {
                return (IObjectEnumerator)Activator.CreateInstance(enumeratorType);
            }
            else if (obj is GameObject)
            {
                return new GameObjectEnumerator();
            }
            else if (obj is IList)
            {
                return new IListEnumerator();
            }
            return null;
        }
    }
}