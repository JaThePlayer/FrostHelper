﻿using Celeste.Mod.Registry.DecalRegistryHandlers;
using System.Runtime.CompilerServices;
using System.Xml;

namespace FrostHelper.DecalRegistry;

internal sealed class RainbowDecalRegistryHandler : DecalRegistryHandler {
    public override string Name => "frosthelper.rainbow";
    
    [OnLoad]
    public static void Load() {
        Celeste.Mod.DecalRegistry.AddPropertyHandler<RainbowDecalRegistryHandler>();
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