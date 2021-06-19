using Monocle;
using System;
using System.Reflection;

namespace FrostHelper
{
    public class EaseHelper
    {
        public static Ease.Easer GetEase(string name)
        {
            foreach (var propertyInfo in easeProps)
            {
                if (name.Equals(propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return (Ease.Easer)propertyInfo.GetValue(default(Ease));
                }
            }
            return Ease.Linear;
        }

        private static readonly FieldInfo[] easeProps = typeof(Ease).GetFields(BindingFlags.Static | BindingFlags.Public);
    }
}
