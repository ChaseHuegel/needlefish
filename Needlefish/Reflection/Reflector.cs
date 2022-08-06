using System.Linq;
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

        public static FieldInfo[] GetFields(Type type) => Fields.GetOrAdd(type, FieldInfoFactory);
        
        public static PropertyInfo[] GetProperties(Type type) => Properties.GetOrAdd(type, PropertyInfoFactory);
        
        public static void Load(Type type)
        {
            GetFields(type);
            GetProperties(type);
        }

        private static PropertyInfo[] PropertyInfoFactory(Type type)
        {
            return type.GetProperties().Where(IsPropertySerializable).OrderBy(GetMemberOrder).ToArray();
        }

        private static FieldInfo[] FieldInfoFactory(Type type)
        {
            return type.GetFields().Where(IsFieldSerializable).OrderBy(GetMemberOrder).ToArray();
        }

        private static object GetMemberOrder(MemberInfo info)
        {
            return info.GetCustomAttribute<DataFieldAttribute>()?.Index ?? int.MaxValue;
        }

        private static bool IsFieldSerializable(FieldInfo info)
        {
            return !info.IsStatic && (info.IsPublic || info.GetCustomAttribute<DataFieldAttribute>() != null);
        }

        private static bool IsPropertySerializable(PropertyInfo info)
        {
            return (!info.SetMethod?.IsStatic ?? false) && ((info.CanWrite && info.CanRead) || info.GetCustomAttribute<DataFieldAttribute>() != null);
        }
    }
}
