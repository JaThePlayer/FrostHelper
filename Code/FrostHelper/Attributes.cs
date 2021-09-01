using Celeste.Mod;
using System;
using System.Reflection;

namespace FrostHelper
{
    /// <summary>
    /// Method gets called when FrostModule.Load() is called
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class OnLoad : Attribute { }

    /// <summary>
    /// Method gets called when FrostModule.LoadContent() is called
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class OnLoadContent : Attribute { }

    /// <summary>
    /// Method gets called when FrostModule.Unload() is called
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class OnUnload : Attribute { }

    public static class AttributeHelper
    {
        public static void InvokeAllWithAttribute(Type attributeType)
        {
            foreach (var type in typeof(FrostModule).Assembly.GetTypesSafe())
            {
                checkType(type);
            }

            void checkType(Type type)
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    foreach (var attr in method.CustomAttributes)
                    {
                        if (attr.AttributeType == attributeType)
                        {
                            method.Invoke(null, null);
                            return;
                        }
                    }
                }
            }
        }
    }
}