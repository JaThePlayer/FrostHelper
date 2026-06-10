using Celeste.Mod.Registry.DecalRegistryHandlers;
using System.Runtime.CompilerServices;
using System.Xml;

namespace FrostHelper.DecalRegistry;

internal sealed class RainbowDecalRegistryHandler : DecalRegistryHandler {
    public override string Name => "frosthelper.rainbow";

    private ColorHelper.BlendingMode DecalColorBlending { get; set; }

    private float Alpha { get; set; }

    internal static Color? PrevColor;
    
    [OnLoad]
    public static void Load() {
        Celeste.Mod.DecalRegistry.AddPropertyHandler<RainbowDecalRegistryHandler>();

        On.Celeste.Decal.CreateOverlay += DecalOnCreateOverlay;
        FrostModule.RegisterILHook(EasierILHook.CreatePostRetHook(typeof(Decal), nameof(Decal.Render), 
            c => c.Emit(OpCodes.Ldarg_0).EmitCall(DecalPostRender)));
    }

    private static void DecalPostRender(Decal decal) {
        // Revert decal.Color set by RainbowDecalMarker this frame, so other sources chainging decal.Color can see its original value for blending purposes.
        if (PrevColor is { } prevColor) {
            decal.Color = prevColor;
            PrevColor = null;
        }
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
        DecalColorBlending = xml.GetEnum("decalColorBlending", ColorHelper.BlendingMode.IgnoreSource);
        Alpha = Get(xml, "alpha", 1.0f);
    }

    public override void ApplyTo(Decal decal) {
        decal.Add(new RainbowDecalMarker(DecalColorBlending, Alpha));
    }
}

internal sealed class RainbowDecalMarker(ColorHelper.BlendingMode decalColorBlending, float alpha) : Component(active: false, visible: true) {
    private Decal _decal;
    
    public override void EntityAwake() {
        base.EntityAwake();
        _decal = (Decal)Entity;
        UpdateHue();
    }

    public override void Render() {
        UpdateHue();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateHue() {
        var col = ColorHelper.GetHue(_decal.Scene, _decal.Position) * alpha;

        RainbowDecalRegistryHandler.PrevColor = _decal.Color;
        _decal.Color = ColorHelper.Blend(decalColorBlending, _decal.Color, col);
    }
}
