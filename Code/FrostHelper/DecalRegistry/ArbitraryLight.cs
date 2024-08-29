using Celeste.Mod.Registry.DecalRegistryHandlers;
using FrostHelper.Entities;
using System.Xml;

namespace FrostHelper.DecalRegistry;
// TODO: consider how to implement
/*
internal sealed class ArbitraryLightDecalRegistryHandler : DecalRegistryHandler {
    public override string Name => "frosthelper.arbitraryLight";
    
    [OnLoad]
    public static void Load() {
        Celeste.Mod.DecalRegistry.AddPropertyHandler<ArbitraryLightDecalRegistryHandler>();
    }
    
    public override void Parse(XmlAttributeCollection xml) {
    }

    public override void ApplyTo(Decal decal) {
        decal.Add(new ArbitraryLight(decal.Position, Color.White, 1f, 16, 32, [], false, 24, 0f));
    }
}
*/