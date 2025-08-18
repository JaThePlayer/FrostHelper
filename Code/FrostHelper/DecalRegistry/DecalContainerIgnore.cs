using Celeste.Mod.Registry.DecalRegistryHandlers;
using System.Xml;

namespace FrostHelper.DecalRegistry;

internal sealed class DecalContainerIgnoreDecalRegistryHandler : DecalRegistryHandler {
    internal static HashSet<string> AllIgnored { get; } = [];
    
    public override string Name => "frosthelper.decalContainerIgnore";
    
    public override void Parse(XmlAttributeCollection xml) {
        
    }

    public override void ApplyTo(Decal decal) {
        AllIgnored.Add(decal.Name);
    }
    
    [OnLoad]
    public static void Load() {
        Celeste.Mod.DecalRegistry.AddPropertyHandler<DecalContainerIgnoreDecalRegistryHandler>();
    }
}