using FrostHelper.Effects;

namespace FrostHelper.Backdrops;

/// <summary>
/// Responsible for loading Frost Helper custom backdrops
/// </summary>
public static class BackdropLoader {
    [OnLoad]
    public static void Load() {
        Everest.Events.Level.OnLoadBackdrop += Level_LoadBackdrop;
    }

    [OnUnload]
    public static void Unload() {
        Everest.Events.Level.OnLoadBackdrop -= Level_LoadBackdrop;
    }

    private static Backdrop Level_LoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above) {
        return child.Name switch {
            "FrostHelper/EntityBackdrop" => new EntityBackdrop(child),
            "FrostHelper/ShaderWrapper" => new ShaderWrapperBackdrop(child),
            "FrostHelper/ShaderFolder" => ShaderFolder.CreateWithInnerStyles(map, child),
            "FrostHelper/ShaderWrapperColorList" => new ColorListShaderWrapper(child),
            "FrostHelper/ColorgradeWrapper" => new ColorgradeWrapper(child),
            _ => null!,
        };
    }
}
