using System.Runtime.InteropServices;

namespace FrostHelper.Helpers;

internal static class EntityMoveHelper {
    public static Tween CreateMoveTween(List<Entity> entities, Vector2 by, Ease.Easer easer, float duration) {
        List<(Entity entity, Vector2 startPos)> entitiesAndPos = entities
            .Select(e => (e, e.Position))
            .ToList();
        
        bool begun = false;
        
        var t = Tween.Create(Tween.TweenMode.Oneshot, easer, duration, true);
        t.OnStart += tw => {
            if (!begun)
                return;
            begun = false;
            
            var entitiesSpan = CollectionsMarshal.AsSpan(entitiesAndPos);
            for (int i = 0; i < entitiesSpan.Length; i++) {
                ref var e = ref entitiesSpan[i];

                e.startPos = e.entity.Position;
            }
        };
        
        t.OnUpdate += tw => {
            begun = true;
            
            foreach ((Entity entity, Vector2 start) in entitiesAndPos) {
                if (entity.Scene is null)
                    continue;
                
                Vector2 end = start + by;
                var to = Vector2.Lerp(start, end, tw.Eased);

                MoveEntity(entity, to);
            }
        };
        
        return t;
    }

    /// <summary>
    /// Moves the given entity to the given position, using MoveTo on Solids
    /// </summary>
    public static void MoveEntity(Entity entity, Vector2 to) {
        if (entity is Solid solid) {
            try {
                solid.MoveTo(to);
            } catch { }
        } else {
            entity.Position = to;
        }
    }
}