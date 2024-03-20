using FrostHelper.Helpers;
using FrostHelper.ModIntegration;

namespace FrostHelper;

[CustomEntity("FrostHelper/CustomSpinnerController")]
[Tracked]
public class CustomSpinnerController : Entity {
    public readonly bool NoCycles;
    public readonly Effect? OutlineShader;

    /// <summary>
    /// The first player in the scene, used for optimising proximity checks.
    /// </summary>
    internal Player? Player;

    /// <summary>
    /// The border color of the first added spinner. Used for outline rendering optimisations
    /// </summary>
    internal Color? FirstBorderColor = null;

    /// <summary>
    /// Whether border rendering can use Render Targets
    /// </summary>
    internal bool CanUseRenderTargetRender;

    /// <summary>
    /// Whether rendering can be optimised further thanks to all spinners using black borders
    /// </summary>
    internal bool CanUseBlackOutlineRenderTargetOpt = true;

    public CustomSpinnerController() { }

    public CustomSpinnerController(EntityData data, Vector2 offset) : base() {
        NoCycles = !data.Bool("cycles", true);

        OutlineShader = ShaderHelperIntegration.TryGetEffect(data.Attr("outlineShader", ""));
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        Player = scene.Tracker.SafeGetEntity<Player>();
    }
}

internal sealed class CustomSpinnerSpriteSource {
    private static readonly Dictionary<(string, string), CustomSpinnerSpriteSource> Cache = new();

    public static CustomSpinnerSpriteSource Get(string dir, string suffix) {
        var key = (dir, suffix);

        if (Cache.TryGetValue(key, out var cached))
            return cached;

        return Cache[key] = new(dir, suffix);
    }
    
    public string Directory { get; }
    public string SpritePathSuffix { get; }
    
    internal bool HasDeco { get; }
    
    private string BgSpritePath { get; }
    private string FgSpritePath { get; }
    private string HotBgSpritePath { get; }
    private string HotFgSpritePath { get; }

    private List<MTexture> FgTextures { get; }
    private List<MTexture>? FgHotTextures { get; set; }
    private List<MTexture>? FgDecoTextures { get; set; }
    
    private List<MTexture> BgTextures { get; }
    
    private List<MTexture>? BgHotTextures { get; set; }
    private List<MTexture>? BgDecoTextures { get; set; }
        
    private CustomSpinnerSpriteSource(string dir, string suffix) {
        Directory = dir;
        SpritePathSuffix = suffix;

        BgSpritePath = $"{Directory}/bg{SpritePathSuffix}";
        HotBgSpritePath = $"{Directory}/hot/bg{SpritePathSuffix}";
        
        FgSpritePath = $"{Directory}/fg{SpritePathSuffix}";
        HotFgSpritePath = $"{Directory}/hot/fg{SpritePathSuffix}";
        
        if (GFX.Game.Has(GetBGSpritePath(false) + "Deco00")) {
            HasDeco = true;
        }
        
        // Atlas pack textures for better rendering perf
        
        // We're leaking packedVirt here, but since this is a cached singleton, we might as well let it live forever.
        // This is only a small memory leak when hot reloading code mods...
        var packed = TexturePackHelper.CreatePackedGroups([
                GFX.Game.GetAtlasSubtextures(GetFGSpritePath(false)),
                GFX.Game.GetAtlasSubtextures(GetBGSpritePath(false)),
            ], $"spinner.{dir}.{suffix}", out var packedVirt);

        (FgTextures, BgTextures) = (packed[0], packed[1]);

        if (HasDeco) {
            packed = TexturePackHelper.CreatePackedGroups([
                    GFX.Game.GetAtlasSubtextures(GetFGSpritePath(false) + "Deco"),
                    GFX.Game.GetAtlasSubtextures(GetBGSpritePath(false) + "Deco"),
                ], $"spinner.deco.{dir}.{suffix}", out var packedDecoVirt);

            (FgDecoTextures, BgDecoTextures) = (packed[0], packed[1]);
        }
    }

    private string GetBGSpritePath(bool hotCoreMode) {
        return hotCoreMode ? HotBgSpritePath : BgSpritePath;
    }

    private string GetFGSpritePath(bool hotCoreMode) {
        return hotCoreMode ? HotFgSpritePath : FgSpritePath;
    }
    
    internal List<MTexture> GetFgTextures(bool hotCoreMode) {
        if (hotCoreMode) {
            return FgHotTextures ??= GFX.Game.GetAtlasSubtextures(GetFGSpritePath(hotCoreMode));
        }

        return FgTextures;
    }
    
    internal List<MTexture> GetBgTextures(bool hotCoreMode) {
        if (hotCoreMode) {
            return BgHotTextures ??= GFX.Game.GetAtlasSubtextures(GetFGSpritePath(hotCoreMode));
        }

        return BgTextures;
    }
    
    internal List<MTexture> GetDecoTextures(bool hotCoreMode) {
        return FgDecoTextures ?? [];
    }
    
    internal List<MTexture> GetBgDecoTextures(bool hotCoreMode) {
        return BgDecoTextures ?? [];
    }
}
