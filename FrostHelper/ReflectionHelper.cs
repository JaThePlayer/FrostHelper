using System;
using System.Collections.Generic;
using System.Reflection;

namespace FrostHelper
{
    public static class ReflectionHelper
    {
        public static Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static FieldInfo GetField(this object obj, string fieldName)
        {
            Type type = obj.GetType();
            if (!FieldCache.TryGetValue(type, out var fieldCache))
            {
                fieldCache = FillCache(type);
            }

            return fieldCache[fieldName];
        }

        private static Dictionary<string, FieldInfo> FillCache(Type type)
        {
            var entry = new Dictionary<string, FieldInfo>();

            foreach (var item in type.BaseType?.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                entry[item.Name] = item;
            }
            foreach (var item in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                entry[item.Name] = item;
            }

            FieldCache.Add(type, entry);

            return entry;
        }

        public static void SetValue(this object obj, string fieldName, object value)
        {
            obj.GetField(fieldName).SetValue(obj, value);
        }

        public static object GetValue(this object obj, string fieldName)
        {
            return obj.GetField(fieldName).GetValue(obj);
        }

        public static T GetValue<T>(this object obj, string fieldName)
        {
            return (T)obj.GetField(fieldName).GetValue(obj);
        }
    }
}
