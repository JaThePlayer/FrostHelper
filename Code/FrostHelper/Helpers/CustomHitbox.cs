namespace FrostHelper.Helpers;

internal static class CustomHitbox {
    private static readonly Dictionary<string, ColliderSource[]> Cache = new();

    public static Collider? Collider(this EntityData data, string key, float scale = 1f) {
        return CreateFrom(data.Attr(key, ""), scale);
    }
    
    public static Collider? CreateFrom(string txt, float scale) {
        if (string.IsNullOrWhiteSpace(txt))
            return null;
        
        if (Cache.TryGetValue(txt, out var colliders)) {
            return CreateFrom(colliders, scale);
        }

        var reader = new SpanParser(txt);
        var generators = new List<ColliderSource>();
        while (reader.SliceUntil(';').TryUnpack(out var entryParser)) {
            if (!entryParser.ReadUntil<char>(',').TryUnpack(out var type))
                type = '\0';

            switch (type) {
                case 'R': {
                    if (!entryParser.ReadUntil<int>(',').TryUnpack(out var w)) {
                        NotificationHelper.Notify($"Invalid rectangle width in {txt}");
                        return null;
                    }
                    if (!entryParser.ReadUntil<int>(',').TryUnpack(out var h)) {
                        NotificationHelper.Notify($"Invalid rectangle height in {txt}");
                        return null;
                    }
                    if (!entryParser.ReadUntil<int>(',').TryUnpack(out var x)) {
                        NotificationHelper.Notify($"Invalid rectangle x in {txt}");
                        return null;
                    }
                    if (!entryParser.ReadUntil<int>(',').TryUnpack(out var y)) {
                        NotificationHelper.Notify($"Invalid rectangle y in {txt}");
                        return null;
                    }
                    
                    generators.Add(new RectangleCollider(w, h, x, y));
                    break;
                }
                case 'C': {
                    if (!entryParser.ReadUntil<int>(',').TryUnpack(out var r)) {
                        NotificationHelper.Notify($"Invalid circle radius in {txt}");
                        return null;
                    }
                    if (!entryParser.ReadUntil<int>(',').TryUnpack(out var x)) {
                        NotificationHelper.Notify($"Invalid circle x in {txt}");
                        return null;
                    }
                    if (!entryParser.ReadUntil<int>(',').TryUnpack(out var y)) {
                        NotificationHelper.Notify($"Invalid circle y in {txt}");
                        return null;
                    }
                    
                    generators.Add(new CircleCollider(r, x, y));
                    break;
                }
                default:
                    NotificationHelper.Notify($"Invalid hitbox type in {txt}");
                    return null;
            }
        }
        
        colliders = generators.ToArray();
        Cache[txt] = colliders;
        return CreateFrom(colliders, scale);
    }

    private static Collider CreateFrom(ColliderSource[] sources, float scale) {
        if (sources is [var only])
            return only.Create(scale);

        Collider[] result = new Collider[sources.Length];
        for (int i = 0; i < result.Length; i++) {
            result[i] = sources[i].Create(scale);
        }
       
        return new ColliderList(result);
    }


    abstract class ColliderSource {
        public abstract Collider Create(float scale);
    }

    sealed class RectangleCollider(int w, int h, int x, int y) : ColliderSource {
        public override Collider Create(float scale) => new Hitbox(w * scale, h * scale, x * scale, y * scale);
    }
    
    sealed class CircleCollider(int r, int x, int y) : ColliderSource {
        public override Collider Create(float scale) => new Circle(r * scale, x * scale, y * scale);
    }
}