using FrostHelper.Effects;

namespace FrostHelper.Backdrops;

/// <summary>
/// Responsible for loading Frost Helper custom backdrops
/// </summary>
public static class BackdropLoader {
    [OnLoad]
    public static void Load() {
        On.Celeste.Mod.Everest.Events.Level.LoadBackdrop += Level_LoadBackdrop;
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Mod.Everest.Events.Level.LoadBackdrop -= Level_LoadBackdrop;
    }

    private static Backdrop Level_LoadBackdrop(On.Celeste.Mod.Everest.Events.Level.orig_LoadBackdrop orig, MapData map, BinaryPacker.Element child, BinaryPacker.Element above) {
        return child.Name switch {
            "FrostHelper/EntityBackdrop" => new EntityBackdrop(child),
            "FrostHelper/ShaderWrapper" => new ShaderWrapperBackdrop(child),
            "FrostHelper/ShaderFolder" => new ShaderFolder(map, child),
            _ => orig(map, child, above),
        };
    }
}
