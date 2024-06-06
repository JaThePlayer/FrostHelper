namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/DustSprite")]
[Tracked]
[TrackedAs(typeof(DustStaticSpinner))]
internal sealed class CustomDustBunny : DustStaticSpinner {
    private static readonly Color[] DefaultEdgeColors = [ Calc.HexToColor("f25a10"), Calc.HexToColor("ff0000"), Calc.HexToColor("f21067") ];

    private static readonly Dictionary<string, DustEdgeColors> ColorCache = new(StringComparer.OrdinalIgnoreCase);

    public CustomDustBunny(EntityData data, Vector2 offset) : base(data, offset) {
        CustomDustGraphic.LoadHooksIfNeeded();

        var edgeColorsString = data.Attr("edgeColors", "f25a10,ff0000,f21067");
        if (!ColorCache.TryGetValue(edgeColorsString, out var edgeColors)) {
            edgeColors = new(data.GetColors("edgeColors", DefaultEdgeColors).Select(c => c.ToVector3()).ToArray());

            ColorCache[edgeColorsString] = edgeColors;
        }

        var attachGroup = data.Int("attachGroup", -1);
        bool attachToSolid = attachGroup != -1 || data.Bool("attachToSolid", false);

        Remove(Get<DustGraphic>());
        Add(Sprite = new CustomDustGraphic(
            edgeColors,
            DustGraphicsDirectory.Get(data.Attr("directory", "danger/dustcreature")),
            data.GetColor("tint", "ffffff"),
            data.GetColor("eyeColor", "ff0000"),
            false, true, true) {
            Rainbow = data.Bool("rainbow", false),
            RainbowEyes = data.Bool("rainbowEyes", false),
            //HasEdges = data.Bool("hasEdges", true)
        });

        if (attachToSolid) {
            if (Get<StaticMover>() is { } oldMover) {
                Remove(oldMover);
            }

            var mover = attachGroup switch {
                -1 => new StaticMover(),
                _ => new GroupedStaticMover(attachGroup, true)
            };

            mover.OnShake = OnShake;
            mover.SolidChecker = IsRiding;
            mover.OnDestroy = RemoveSelf;

            Add(mover);
        }
    }

    public override void Update() {
        // instead of base.Update to remove enumeration through the ComponentList 
        Sprite.Update();

        if (Sprite.Estableshed && Scene.OnInterval(0.05f, offset)) {
            if (Scene.Tracker.GetEntity<Player>() is { } player) {
                Collidable = Math.Abs(player.X - X) < 128f && Math.Abs(player.Y - Y) < 128f;
            }
        }
    }

    public override void Render() {
        // instead of base.Update to remove enumeration through the ComponentList 
        Sprite.Render();
    }
}

internal sealed class DustGraphicsDirectory {
    private static readonly Dictionary<string, DustGraphicsDirectory> Directories = new(StringComparer.Ordinal);

    public readonly string Base;
    public readonly string Overlay;
    public readonly string Center;
    public readonly string DeadEyes;
    public readonly string Eyes;

    private DustGraphicsDirectory(string directory) {
        Base = $"{directory}/base";
        Overlay = $"{directory}/overlay";
        Center = $"{directory}/center";
        DeadEyes = $"{directory}/deadEyes";
        Eyes = $"{directory}/eyes";
    }

    private static void OnContentChanged(ModAsset from, ReadOnlySpan<char> spritePath) {
        foreach (var (k, v) in Directories) {
            if (spritePath.StartsWith(k)) {
                Directories.Remove(k);
            }
        }
    }
    
    public static DustGraphicsDirectory Get(string directory) {
        if (Directories.Count == 0) {
            FrostModule.OnSpriteChanged += OnContentChanged;
        }
        
        if (Directories.TryGetValue(directory, out var cached))
            return cached;

        return Directories[directory] = new(directory);
    }
}

// attached to DustEdges, keeps track of which edges use which colors for more efficient rendering.
internal sealed class DustEdgesTracker {
    public readonly Dictionary<DustEdgeColors, List<CustomDustEdge>> EdgeColorCache = new();
}

internal sealed class DustEdgeColors(Vector3[] colors) : IEquatable<DustEdgeColors> {
    public Vector3[] Colors => colors;

    private int? _hash;
    
    public bool Equals(DustEdgeColors? other) {
        return ReferenceEquals(this, other) || (other is not null && Colors.AsSpan().SequenceEqual(other.Colors));
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DustEdgeColors otherColors)
            return false;
        
        return Equals(otherColors);
    }

    public override int GetHashCode() {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        if (_hash is { } precomputed) {
            return precomputed;
        }
        
        var hash = new HashCode();
        foreach (var c in Colors) {
            hash.Add(c); 
        }

        _hash = hash.ToHashCode();
        return _hash.Value;
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
