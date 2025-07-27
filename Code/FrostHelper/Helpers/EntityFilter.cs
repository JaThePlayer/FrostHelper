using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrostHelper.Helpers;

/// <summary>
/// Helper class which allows for checking entity types against a mapper-defined list of entity types
/// </summary>
internal class EntityFilter(HashSet<Type> types, bool isBlacklist, HashSet<int> ids) {
    private static readonly Type[] DefaultBlacklistTypes = [
        typeof(Player),
        typeof(SolidTiles),
        typeof(BackgroundTiles),
        typeof(SpeedrunTimerDisplay),
        typeof(StrawberriesCounter)
    ];

    /// <summary>
    /// If true, no entity can match this filter ever.
    /// </summary>
    public bool Empty => !isBlacklist && types.Count == 0 && ids.Count == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Matches(Entity entity) => (ids.Contains(entity.SourceId.ID) || types.Contains(entity.GetType())) != isBlacklist;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Matches(Backdrop backdrop) => types.Contains(backdrop.GetType()) != isBlacklist;

    public static EntityFilter CreateFrom(ReadOnlySpan<char> str, bool isBlacklist, Type[]? blacklistTypes = null) {
        var types = new HashSet<Type>();
        var ids = new HashSet<int>();
        
        var parser = new SpanParser(str.Trim());
        while (parser.SliceUntil(',').TryUnpack(out var inner)) {
            var remaining = inner.Remaining.Trim();
            if (int.TryParse(remaining, out var id)) {
                ids.Add(id);
            } else if (TypeHelper.EntityNameToTypeSafe(inner.Remaining.ToString()) is {} type) {
                types.Add(type);
            }
        }
        
        if (isBlacklist) {
            // Some basic types we don't want to move
            foreach (Type type in blacklistTypes ?? DefaultBlacklistTypes)
                types.Add(type);
        }
            
        return new(types, isBlacklist, ids);
    }
    
    public static EntityFilter CreateFrom(EntityData data, string typesKey = "types", string blacklistKey = "blacklist", Type[]? blacklistTypes = null) {
        var str = data.Attr(typesKey, "");
        var isBlacklist = data.Bool(blacklistKey);
        
        return CreateFrom(str, isBlacklist, blacklistTypes);
    }

    public EntityFilterEnumerable Filter(Scene scene) 
        => new(scene, this);
}

internal readonly struct EntityFilterEnumerable(Scene scene, EntityFilter filter) {
    public Enumerator GetEnumerator() {
        return new(scene, filter);
    }

    internal ref struct Enumerator(Scene scene, EntityFilter filter) {
        private Span<Entity> _entities = CollectionsMarshal.AsSpan(scene.Entities.entities);
        private int _i = -1;

        public bool MoveNext() {
            var entities = _entities;

            while (true) {
                _i++;
                if (_i >= entities.Length) {
                    return false;
                }

                var e = entities[_i];
                if (filter.Matches(e)) {
                    Current = e;
                    return true;
                }
            }
        }

        public void Reset() {
            _i = -1;
        }

        public Entity Current { get; private set; }

        public void Dispose() {
        }
    }
}