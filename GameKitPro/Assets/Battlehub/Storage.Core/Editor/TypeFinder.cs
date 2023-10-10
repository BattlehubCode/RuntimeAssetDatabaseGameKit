using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Battlehub.Storage
{
    public abstract class BaseTypeFinder
    {
        private Func<Assembly, bool> m_assemblyFilter;
        private Func<Type, bool> m_typeFilter;

        public static readonly Func<Assembly, bool> DefaultAssemblyFilter = assembly =>
                !assembly.FullName.Contains("mscorlib") &&
                !assembly.FullName.Contains("UnityEditor") &&
                !assembly.FullName.Contains("Battlehub.Storage.Tests") &&
                !assembly.FullName.Contains("Battlehub.Storage.Runtime") &&
                !assembly.FullName.Contains("Battlehub.Storage.Editor") &&
                !assembly.FullName.Contains("Battlehub.Storage") &&
                !assembly.FullName.Contains("Battlehub.Storage.Editor") &&
                !assembly.FullName.Contains("UnityWeld");

        public static readonly Func<Type, bool> DefaultTypeFilter = type =>
                //type.IsSubclassOf(typeof(UnityEngine.Object)) &&
                type.IsPublic &&
                !type.IsGenericType &&
                !type.IsEnum &&
                !type.IsAbstract &&
                !type.IsInterface &&
                (type.Namespace == null || !type.Namespace.StartsWith("UnityEditor"));
        public BaseTypeFinder()
        {
            m_assemblyFilter = DefaultAssemblyFilter;
            m_typeFilter = DefaultTypeFilter;
        }

        public BaseTypeFinder(Func<Assembly, bool> assemblyFilter, Func<Type, bool> typeFilter)
        {
            m_assemblyFilter = assemblyFilter;
            m_typeFilter = typeFilter;
        }

        protected bool AssemblyFilter(Assembly assembly)
        {
            return m_assemblyFilter(assembly);
        }

        protected bool TypeFilter(Type type)
        {
            return m_typeFilter(type);
        }

        public Assembly[] Assemblies
        {
            get;
            protected set;
        }

        public Type[] Types
        {
            get;
            protected set;
        }

        public abstract void Find();
    }

    public class TypeFinder : BaseTypeFinder
    {
        private static readonly HashSet<Type> s_primitiveTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(short),
            typeof(ushort),
            typeof(string),
        };

        public static IEnumerable<Type> PrimitiveTypes
        {
            get { return s_primitiveTypes; }
        }

        public TypeFinder() : base()
        {
        }

        public TypeFinder(Func<Assembly, bool> assemblyFilter, Func<Type, bool> typeFilter) : base(assemblyFilter, typeFilter)
        {
        }

        public override void Find()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(AssemblyFilter).OrderBy(a => a.FullName).ToArray();

            List<Type> typesList = new List<Type>();
            List<Assembly> assembliesList = new List<Assembly>();

            for (int i = 0; i < assemblies.Length; ++i)
            {
                Assembly assembly = assemblies[i];
                try
                {
                    Type[] uoTypes = assembly.GetTypes().Where(TypeFilter).ToArray();
                    if (uoTypes.Length > 0)
                    {
                        assembliesList.Add(assembly);
                        typesList.AddRange(uoTypes);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to process :" + assembly.FullName + Environment.NewLine + e.ToString());
                }
            }

            Types = typesList.OrderBy(t => t.Name).ToArray();
            Assemblies = assembliesList.OrderBy(a => a.FullName).ToArray();
        }
    }
}

