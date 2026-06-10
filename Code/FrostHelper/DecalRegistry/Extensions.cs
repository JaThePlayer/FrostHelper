using System.Xml;

namespace FrostHelper.DecalRegistry;

internal static class Extensions {
    extension(XmlAttributeCollection xml) {
        public T GetEnum<T>(string attr, T def) where T : struct, Enum {
            var xmlAttribute = xml[attr];
            if (xmlAttribute is null)
                return def;
            
            return Enum.TryParse(xmlAttribute.Value, true, out T value) ? value : def;
        }
    }
}
