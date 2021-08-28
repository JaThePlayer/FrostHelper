using System;
using System.Collections.Generic;
using System.Reflection;

namespace FrostHelper
{
    public static class ReflectionHelper
    {
        public static Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        public static Dictionary<Type, Dictionary<string, MethodInfo>> MethodCache = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        public static FieldInfo GetField(this object obj, string fieldName)
        {
            Type type = obj.GetType();
            if (!FieldCache.TryGetValue(type, out var fieldCache))
            {
                fieldCache = FillCache(type);
            }

            return fieldCache[fieldName];
        }

        public static MethodInfo GetMethod(this object obj, string methodName)
        {
            Type type = obj.GetType();
            if (!MethodCache.TryGetValue(type, out var methodCache))
            {
                methodCache = FillMethodCache(type);
            }

            return methodCache[methodName];
        }
        private static Dictionary<string, MethodInfo> FillMethodCache(Type type)
        {
            var entry = new Dictionary<string, MethodInfo>();

            foreach (var item in type.BaseType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                entry[item.Name] = item;
            }
            foreach (var item in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                entry[item.Name] = item;
            }

            MethodCache.Add(type, entry);

            return entry;
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

        public static object Invoke(this object obj, string functionName, params object[] args)
        {
            return GetMethod(obj, functionName).Invoke(obj, args);
        }

        public static T Invoke<T>(this object obj, string functionName, params object[] args)
        {
            return (T)GetMethod(obj, functionName).Invoke(obj, args);
        }
    }
}
