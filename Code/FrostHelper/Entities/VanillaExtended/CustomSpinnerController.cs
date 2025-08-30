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

    internal float TimeUnpaused;

    /// <summary>
    /// Level name this controller comes from, used to avoid using the controller in new rooms on room transition.
    /// </summary>
    internal readonly string? Level = FrostModule.TryGetCurrentLevel()?.Session.Level;

    public CustomSpinnerController() {
    }

    public CustomSpinnerController(EntityData data, Vector2 offset) {
        NoCycles = !data.Bool("cycles", true);

        OutlineShader = ShaderHelperIntegration.TryGetEffect(data.Attr("outlineShader", ""));
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        Player = scene.Tracker.SafeGetEntity<Player>();
    }

    public override void Update() {
        TimeUnpaused += Engine.DeltaTime;
    }
}

internal sealed class CustomSpinnerSpriteSource : ISavestatePersisted {
    private static readonly object _lock = new();
    private static readonly Dictionary<(string dir, string suffix, bool animated), CustomSpinnerSpriteSource> Cache = new();
    
    private static void OnContentChanged(ModAsset from, ReadOnlySpan<char> spritePathSpan) {
        var spritePath = spritePathSpan.ToString();
        
        if (FrostModule.TryGetCurrentLevel() is { } lvl) {
            Everest.Events.AssetReload.ReloadHandler dele = null!;
            dele = x => {
                // using a lock instead of ConcurrentDictionary because we need to atomically remove multiple entries,
                // which would require a lock anyway.
                lock (_lock)
                    foreach (var (k, v) in Cache.ToList()) {
                        if (spritePath.StartsWith(k.dir) || spritePath.Contains("fhAnimation")) {
                            Cache.Remove(k, out _);
                        }
                    }
                
                // make sure spinners are using new textures
                foreach (CustomSpinner s in lvl.Tracker.SafeGetEntities<CustomSpinner>()) {
                    s.ClearSprites();
                    s.ResetSpriteSource();
                }
                
                Everest.Events.AssetReload.OnAfterReload -= dele;
            };
            Everest.Events.AssetReload.OnAfterReload += dele;

        }
    }
    
    public static CustomSpinnerSpriteSource Get(string dir, string suffix, bool animated = false) {
        var origDirStr = dir;
        
        if (dir.EndsWith('!')) {
            animated = true;
            dir = dir[..^1];
        }
        
        var subDirIdx = dir.IndexOf('>', StringComparison.Ordinal);
        if (subDirIdx >= 0) {
            suffix = dir[(subDirIdx + 1)..];
            dir = dir[..subDirIdx];
        }
        
        var key = (dir, suffix, animated);
        lock (_lock) {
            if (Cache.Count == 0)
                FrostModule.OnSpriteChanged += OnContentChanged;

            if (Cache.TryGetValue(key, out var cached))
                return cached;

            return Cache[key] = new(dir, suffix, animated) {
                OrigDirectoryString = origDirStr
            };
        }
    }
    
    public string OrigDirectoryString { get; init; }
    public string Directory { get; }
    public string SpritePathSuffix { get; }
    
    public bool Animated { get; }
    
    internal bool HasDeco { get; }
    
    private string BgSpritePath { get; }
    private string FgSpritePath { get; }
    private string HotBgSpritePath { get; }
    private string HotFgSpritePath { get; }

    private List<MTexture> FgTextures { get; set; }
    private List<MTexture>? FgHotTextures { get; set; }
    private List<MTexture>? FgDecoTextures { get; set; }
    
    private List<MTexture> BgTextures { get; set; }

    private List<MTexture>? BgHotTextures { get; set; }
    private List<MTexture>? BgDecoTextures { get; set; }

    private VirtualTexture? PackedTexture;
    private VirtualTexture? PackedDecoTexture;
    
    public NumVector2 CullingDistance { get; private set; }
    
    public float ConnectionWidth { get; private set; }

    ~CustomSpinnerSpriteSource() {
        PackedTexture?.Dispose();
        PackedTexture = null;
        
        PackedDecoTexture?.Dispose();
        PackedDecoTexture = null;
    }
    
    private CustomSpinnerSpriteSource(string dir, string suffix, bool animated) {
        Directory = dir;
        SpritePathSuffix = suffix;
        Animated = animated;

        BgSpritePath = $"{Directory}/bg{SpritePathSuffix}";
        HotBgSpritePath = $"{Directory}/hot/bg{SpritePathSuffix}";
        
        FgSpritePath = $"{Directory}/fg{SpritePathSuffix}";
        HotFgSpritePath = $"{Directory}/hot/fg{SpritePathSuffix}";
        
        if (GFX.Game.Has(GetBGSpritePath(false) + "Deco00")) {
            HasDeco = true;
        }
        
        // Atlas pack textures for better rendering perf
        if (Animated) {
            List<(List<MTexture>, (bool bg, AnimationMetaYaml yaml))> groups = [];
            var atlas = GFX.Game;
            var fgSpriteAmt = 0;
            var bgSpriteAmt = 0;
            var i = 0;
            while (true) {
                var anyFound = false;

                var nextDir = $"{Directory}/{(i <= 9 ? $"0{i}" : i)}";
                var fallbackPath = $"{nextDir}/";
                var suffixedFallbackPath = $"{nextDir}/{SpritePathSuffix}";
                
                if (atlas.Has($"{nextDir}/fg{SpritePathSuffix}00")) {
                    fgSpriteAmt++;
                    anyFound = true;
                    var fgSpritePath = $"{nextDir}/fg{SpritePathSuffix}";
                    groups.Add((GFX.Game.GetAtlasSubtextures(fgSpritePath), (false, AnimationMetaYaml.GetForTexture(fgSpritePath, $"{nextDir}/fg", suffixedFallbackPath, fallbackPath))));
                }
                if (atlas.Has($"{nextDir}/bg{SpritePathSuffix}00")) {
                    bgSpriteAmt++;
                    anyFound = true;
                    var bgSpritePath = $"{nextDir}/bg{SpritePathSuffix}";
                    groups.Add((GFX.Game.GetAtlasSubtextures(bgSpritePath), (true, AnimationMetaYaml.GetForTexture(bgSpritePath, $"{nextDir}/bg", suffixedFallbackPath, fallbackPath))));
                }

                if (!anyFound)
                    break;
                i++;
            }

            if (fgSpriteAmt == 0) {
                NotificationHelper.Notify($"No fg Animated Spinner sprites found, searched at:\n{Directory}/00/{SpritePathSuffix}fg00.png");
                groups.Add(([GFX.Game.DefaultFallback], (false, AnimationMetaYaml.Default)));
            }
            if (bgSpriteAmt == 0) {
                NotificationHelper.Notify($"No bg Animated Spinner sprites found, searched at:\n{Directory}/00/{SpritePathSuffix}bg00.png");
                groups.Add(([GFX.Game.DefaultFallback], (true, AnimationMetaYaml.Default)));
            }
            
            var packed = TexturePackHelper.CreatePackedGroupsWithData(groups, $"spinner.{dir}.{suffix}", out PackedTexture);

            FgTextures = [];
            BgTextures = [];
            foreach (var (textures, (bg, yaml)) in packed) {
                (bg ? BgTextures : FgTextures).Add(new AnimatedMTexture(textures, yaml));
            }
        } else {
            var packed = TexturePackHelper.CreatePackedGroups([
                    GFX.Game.GetAtlasSubtexturesWithNotif(GetFGSpritePath(false)),
                    GFX.Game.GetAtlasSubtexturesWithNotif(GetBGSpritePath(false)),
                ], $"spinner.{dir}.{suffix}", out PackedTexture);

            (FgTextures, BgTextures) = (packed[0], packed[1]);
        }

        if (HasDeco) {
            var packed = TexturePackHelper.CreatePackedGroups([
                    GFX.Game.GetAtlasSubtextures(GetFGSpritePath(false) + "Deco"),
                    GFX.Game.GetAtlasSubtextures(GetBGSpritePath(false) + "Deco"),
                ], $"spinner.deco.{dir}.{suffix}", out PackedDecoTexture);

            (FgDecoTextures, BgDecoTextures) = (packed[0], packed[1]);
        }

        (CullingDistance, ConnectionWidth) = FgTextures is [var first, ..]
            ? (new NumVector2(first.Width + 8f, first.Height + 8f) / 2f, (first.Width/12*12) + (first.Width%12>0 ? 12 : 0))
            : (new NumVector2(16f), 24);
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
