using System.Runtime.InteropServices;
using YamlDotNet.Serialization;

namespace FrostHelper.Helpers;

internal sealed class AnimationMetaYaml {
    public float Speed { get; set; } = 12f;

    public bool RandomizeStartFrame { get; set; } = true;

    private string? _frames;
    public string? Frames {
        get => _frames;
        set {
            _frames = value;
            _framesArr = null;
        }
    }

    [YamlIgnore]
    private int[]? _framesArr;
    
    [YamlIgnore]
    public int[]? FramesArray => Frames is {} ? _framesArr ??= Calc.ReadCSVIntWithTricks(Frames) : null;
    
    public static readonly AnimationMetaYaml Default = new();

    public static AnimationMetaYaml GetForTexture(params Span<string> paths) {
        foreach (var path in paths) {
            var full = $"Graphics/Atlases/Gameplay/{path}{(path.EndsWith('/') ? "" : ".")}fhAnimation";
            if (Everest.Content.Get<AssetTypeYaml>(full) is {} yaml) {
                if (yaml.TryDeserialize<AnimationMetaYaml>(out var meta)) {
                    return meta;
                }
            
                Logger.Log(LogLevel.Warn, "FrostHelper.AnimationMeta", $"Failed to load animation .yaml from '{full}'.");
            }
        }

        return Default;
    }
}

internal sealed class AnimatedMTexture(List<MTexture> sourceTextures, AnimationMetaYaml yaml) 
    : MTexture(sourceTextures[0], new(0, 0, sourceTextures[0].ClipRect.Width, sourceTextures[0].ClipRect.Height)) {

    public AnimatedMTexture(AnimatedMTexture parent, int x, int y, int width, int height)
    : this(parent.SourceTextures.Select(t => new MTexture(t, x, y, width, height)).ToList(), parent.Meta) {
        
    }

    private Dictionary<(int, int, int, int), AnimatedMTexture>? _subtextureCache;
    
    public AnimationMetaYaml Meta => yaml;

    public List<MTexture> SourceTextures => sourceTextures;
    
    public readonly List<MTexture> Textures = CreateTexturesArray(sourceTextures, yaml);

    private static List<MTexture> CreateTexturesArray(List<MTexture> textures, AnimationMetaYaml yaml) {
        if (yaml.FramesArray is not { } indexes)
            return textures;
        
        var res = new List<MTexture>(indexes.Length);
        foreach (var idx in indexes) {
            if (idx < 0 || idx >= textures.Count)
                continue;
            res.Add(textures[idx]);
        }

        return res;
    }
    
    public float Speed => yaml.Speed;
    
    public MTexture GetAnim(float time, float offset) {
        var idx = (int)(time * Speed + offset) % Textures.Count;

        return Textures[int.Max(idx, 0)];
    }
    
    public AnimatedMTexture GetSubtexture(int x, int y, int width, int height)
    {
        return new AnimatedMTexture(this, x, y, width, height);
    }
    
    public AnimatedMTexture GetSubtextureCached(int x, int y, int width, int height)
    {
        ref var cache = ref CollectionsMarshal.GetValueRefOrAddDefault(_subtextureCache ??= [], (x, y, width, height), out _);
        
        return cache ??= new AnimatedMTexture(this, x, y, width, height);
    }
}