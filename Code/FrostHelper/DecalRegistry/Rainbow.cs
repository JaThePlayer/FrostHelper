using Celeste.Mod.Registry.DecalRegistryHandlers;
using System.Runtime.CompilerServices;
using System.Xml;

namespace FrostHelper.DecalRegistry;

internal sealed class RainbowDecalRegistryHandler : DecalRegistryHandler {
    public override string Name => "frosthelper.rainbow";
    
    [OnLoad]
    public static void Load() {
        Celeste.Mod.DecalRegistry.AddPropertyHandler<RainbowDecalRegistryHandler>();

        On.Celeste.Decal.CreateOverlay += DecalOnCreateOverlay;
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Decal.CreateOverlay -= DecalOnCreateOverlay;
    }

    private static void DecalOnCreateOverlay(On.Celeste.Decal.orig_CreateOverlay orig, Decal self) {
        if (self.Get<RainbowDecalMarker>() is { }) {
            RainbowTilesetController.RainbowifyTexture(self.Scene, self.textures[0]);
        }
        
        orig(self);
    }

    public override void Parse(XmlAttributeCollection xml) {
        
    }

    public override void ApplyTo(Decal decal) {
        decal.Add(new RainbowDecalMarker());
    }
}

internal sealed class RainbowDecalMarker() : Component(active: false, visible: true) {
    public override void EntityAwake() {
        base.EntityAwake();
        UpdateHue();
    }

    public override void Render() {
        UpdateHue();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateHue() {
        if (Entity is Decal d) {
            d.Color = ColorHelper.GetHue(d.Scene, d.Position);
        }
    }
}