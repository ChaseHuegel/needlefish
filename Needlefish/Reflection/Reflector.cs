using System.Collections.Generic;
using System;
using System.Reflection;
using System.Collections.Concurrent;

namespace Needlefish.Reflection
{
    internal static class Reflector
    {
        private static ConcurrentDictionary<Type, FieldInfo[]> Fields = new ConcurrentDictionary<Type, FieldInfo[]>();
        private static ConcurrentDictionary<Type, PropertyInfo[]> Properties = new ConcurrentDictionary<Type, PropertyInfo[]>();

        public static void Load(Type type)
        {
            GetFields(type);
            GetProperties(type);
        }

        public static FieldInfo[] GetFields(Type type) => Fields.GetOrAdd(type, FieldInfoFactory);
        public static PropertyInfo[] GetProperties(Type type) => Properties.GetOrAdd(type, PropertyInfoFactory);

        private static PropertyInfo[] PropertyInfoFactory(Type type) => type.GetProperties();
        private static FieldInfo[] FieldInfoFactory(Type type) => type.GetFields();
    }
}
