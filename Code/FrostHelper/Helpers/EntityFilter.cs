using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FrostHelper.Helpers;

/// <summary>
/// Helper class which allows for checking entity types against a mapper-defined list of entity types
/// </summary>
internal class EntityFilter {
    private static readonly Type[] DefaultBlacklistTypes = [
        typeof(Player),
        typeof(SolidTiles),
        typeof(BackgroundTiles),
        typeof(SpeedrunTimerDisplay),
        typeof(StrawberriesCounter)
    ];
    
    public readonly HashSet<Type> Types;
    public readonly bool IsBlacklist;

    public EntityFilter(HashSet<Type> types, bool isBlacklist) {
        Types = types;
        IsBlacklist = isBlacklist;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Matches(Entity entity) => Types.Contains(entity.GetType()) != IsBlacklist;

    public static EntityFilter CreateFrom(EntityData data, string typesKey = "types", string blacklistKey = "blacklist") {
        var str = data.Attr(typesKey, "");
        var isBlacklist = data.Bool(blacklistKey);
        
        var types = FrostModule.GetTypesAsHashSet(str);
        if (isBlacklist) {
            // Some basic types we don't want to move
            foreach (Type type in DefaultBlacklistTypes)
                types.Add(type);
        }
            
        return new(types, isBlacklist);
    }

    public EntityFilterEnumerable Filter(Scene scene) 
        => new(scene, this);
}

internal readonly struct EntityFilterEnumerable(Scene scene, EntityFilter filter) {
    public Enumerator GetEnumerator() {
        return new(scene, filter);
    }

    internal ref struct Enumerator {
        private Span<Entity> Entities;
        private EntityFilter Filter;
        private int i = -1;

        public Enumerator(Scene scene, EntityFilter filter) {
            Entities = CollectionsMarshal.AsSpan(scene.Entities.entities);
            Filter = filter;
        }

        public bool MoveNext() {
            var entities = Entities;
            var filter = Filter;
            
            while (true) {
                i++;
                if (i >= entities.Length) {
                    return false;
                }

                var e = entities[i];
                if (filter.Matches(e)) {
                    Current = e;
                    return true;
                }
            }
        }

        public void Reset() {
            i = -1;
        }

        public Entity Current { get; private set; }

        public void Dispose() {
        }
    }
}