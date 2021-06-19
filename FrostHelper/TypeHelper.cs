using Celeste.Mod;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FrostHelper
{
    public static class TypeHelper
    {
        private static Dictionary<string, Type> entityNameToType = null;

        public static Type EntityNameToType(string entityName)
        {
            // see if this is just a type name
            Type ret = FakeAssembly.GetFakeEntryAssembly().GetType(entityName, false, true);
            if (ret != null)
                return ret;

            if (entityNameToType is null)
            {
                CreateCache();
            }

            if (entityNameToType.TryGetValue(entityName, out ret))
                return ret;

            throw new Exception($"Unknown entity name: {entityName}.");
        }

        private static void CreateCache()
        {
            entityNameToType = new Dictionary<string, Type>();

            foreach (var type in FakeAssembly.GetFakeEntryAssembly().GetTypesSafe())
            {
                checkType(type);
            }

            void checkType(Type type)
            {
                foreach (CustomEntityAttribute customEntityAttribute in type.GetCustomAttributes<CustomEntityAttribute>())
                {
                    foreach (string idFull in customEntityAttribute.IDs)
                    {
                        string id;
                        string[] split = idFull.Split('=');

                        if (split.Length == 1)
                        {
                            id = split[0];

                        }
                        else if (split.Length == 2)
                        {
                            id = split[0];
                        }
                        else
                        {
                            // invalid
                            continue;
                        }

                        //Logger.Log(id.Trim(), type.Name);
                        entityNameToType.Add(id.Trim(), type);
                    }
                }
            }
        }
    }
}
