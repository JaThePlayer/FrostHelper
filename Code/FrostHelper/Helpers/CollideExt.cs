using System.Runtime.InteropServices;

namespace FrostHelper.Helpers;

internal static class CollideExt {
    public static void CollideInto<TList, TEntity, TFilter>(Scene scene, Rectangle rect, List<TList> hits, TFilter filter = default) 
        where TList : Entity
        where TEntity : TList
        where TFilter : struct, IFunc<TEntity, bool> {
        foreach (TEntity t in scene.Tracker.SafeGetEntitiesSpan<TEntity>())
        {
            if (t.Collidable && filter.Invoke(t) && t.CollideRect(rect))
                hits.Add(t);
        }
    }
    
    public static void CollideIntoComponents<TList, TColliderGetter>(List<Component> src, Rectangle rect, List<TList> hits, TColliderGetter colliderGetter = default) 
        where TList : Component
        where TColliderGetter : struct, IFunc<TList, Collider?> {
        foreach (TList t in src)
        {
            if (!t.Entity.Collidable)
                continue;
            var collider = colliderGetter.Invoke(t);
            if (collider != null && collider.Collide(rect))
                hits.Add(t);
        }
    }
    
    public static void CollideIntoBroadPhase(List<Entity> src, Rectangle rect, List<Entity> hits) {
        foreach (var t in src) {
            if (!t.Collidable || t.Collider is not {} c) continue;

            // if (t.Collider is Grid) {
            //     hits.Add(t);
            //     continue;
            // }

            if (c.Collide(rect))
                hits.Add(t);
        }
    }
    
    /// <summary>
    /// Finds all possible collisions within a rectangle for a broad phase collision check.
    /// </summary>
    public static void CollideIntoBroadPhase<T>(this Scene scene, Rectangle rect, List<Entity> hits) where T: Entity {
        foreach (var t in scene.Tracker.SafeGetEntitiesSpan<T>()) {
            if (!t.Collidable || t.Collider is not {} c) continue;

           // if (t.Collider is Grid) {
           //     hits.Add(t);
           //     continue;
           // }

            if (c.Collide(rect))
                hits.Add(t);
        }
    }
    
    /// <summary>
    /// Assumes that `hits` only stores collidable `T` elements with colliders (due to using CollideIntoBroadPhase earlier)
    /// </summary>
    public static T? CollideFirst<T>(this Entity entity, Vector2 at, List<Entity> hits) where T : Entity
    {
        Vector2 position = entity.Position;
        entity.Position = at;
        var c = entity.Collider;

        if (c.GetType() == typeof(Hitbox)) {
            var ch = (Hitbox) c;
            foreach (var e in CollectionsMarshal.AsSpan(hits)) {
                if (e.Collider.Collide(ch)) {
                    entity.Position = position;
                    return e as T;
                }
            }
        } else {
            foreach (var e in CollectionsMarshal.AsSpan(hits)) {
                if (c.Collide(e.Collider)) {
                    entity.Position = position;
                    return e as T;
                }
            }
        }
        
        entity.Position = position;
        return null;
    }
    
    /// <summary>
    /// CollideFirst, but all input elements in hits are assumed to be Collideable and have a not-null Collider.
    /// </summary>
    public static T? CollideFirstAssumeCollideable<T>(Rectangle rect, List<T> hits) where T : Entity {
        foreach (var e in CollectionsMarshal.AsSpan(hits))
        {
            if (e.Collider.Collide(rect))
                return e;
        }

        return null;
    }
    
    public static T? CollideFirstComponent<T, TColliderGetter>(Rectangle rect, List<T> hits, TColliderGetter colliderGetter = default) 
        where T : Component
        where TColliderGetter : struct, IFunc<T, Collider?>{
        foreach (var e in CollectionsMarshal.AsSpan(hits))
        {
            if (colliderGetter.Invoke(e)?.Collide(rect) ?? false)
                return e;
        }

        return null;
    }
    
    /// <summary>
    /// Assumes that `hits` only stores `T` elements (due to using CollideIntoBroadPhase earlier)
    /// </summary>
    public static T? CollideFirstOutside<T>(this Entity entity, Vector2 at, List<Entity> hits) where T : Entity
    {
        foreach (Entity b in hits)
        {
            if (!Collide.Check(entity, b) && Collide.Check(entity, b, at))
                return b as T;
        }
        return null;
    }

    public class MoveFastData(Entity entity) {
        public readonly List<Entity> Broadest = [];
        private readonly List<Entity> _temp = [];
        private readonly List<Entity> _jumpthrus = [];
        
        public Rectangle HitboxRect;

        public Vector2 MovementCounter;

        public bool MoveBoth(Vector2 move, Collision? collideH = null, Collision? collideV = null) {
            var r = false;
            entity.Scene.CollideIntoBroadPhase<Solid>(RectangleExt.Merge(HitboxRect, HitboxRect.MovedBy(move)), Broadest);
            
            r |= MoveH(move.X, out var movedH, collideH);
            HitboxRect.X += movedH;
            r |= MoveV(move.Y, collideV);
            
            Broadest.Clear();
            _temp.Clear();
            _jumpthrus.Clear();

            return r;
        }
        
        public bool MoveH(float moveH, out int moved, Collision? onCollide = null, Solid? pusher = null) {
            moved = 0;
            MovementCounter.X += moveH;
            int moveH1 = (int) Math.Round(MovementCounter.X, MidpointRounding.ToEven);
            if (moveH1 == 0)
                return false;
            MovementCounter.X -= moveH1;
            return MoveHExact(moveH1, out moved, onCollide, pusher);
        }
        
        public bool MoveHExact(int moveH, out int moved, Collision? onCollide = null, Solid? pusher = null) {
            moved = 0;
            var e = entity;
            var possibleCollisions = _temp;
            
            possibleCollisions.Clear();
            var hitboxRect = HitboxRect;
            CollideIntoBroadPhase(Broadest, RectangleExt.Merge(hitboxRect, hitboxRect.MovedBy(moveH, 0)), possibleCollisions);

            if (possibleCollisions.Count == 0) {
                e.X += moveH;
                return false;
            }
            
            Vector2 target = e.Position + Vector2.UnitX * moveH;
            int dir = Math.Sign(moveH);

            while (moveH != 0)
            {
                var solid = e.CollideFirst<Solid>(e.Position + Vector2.UnitX * dir, possibleCollisions);
                if (solid != null)
                {
                    MovementCounter.X = 0.0f;
                    onCollide?.Invoke(new CollisionData {
                        Direction = Vector2.UnitX * dir,
                        Moved = Vector2.UnitX * moved,
                        TargetPosition = target,
                        Hit = solid,
                        Pusher = pusher
                    });
                    return true;
                }
                

                moved += dir;
                moveH -= dir;
                e.X += dir;
            }

            //Console.WriteLine($"Found possible collisions, but nothing happened: {string.Join("\n", possibleCollisions.Select(x => x.collider))}");
            return false;
        }

        public bool MoveV(float moveV, Collision? onCollide = null, Solid? pusher = null)
        {
            MovementCounter.Y += moveV;
            int moveV1 = (int) Math.Round(MovementCounter.Y, MidpointRounding.ToEven);
            if (moveV1 == 0)
                return false;
            MovementCounter.Y -= moveV1;
            return MoveVExact(moveV1, onCollide, pusher);
        }
    
        public bool MoveVExact(int moveV, Collision? onCollide = null, Solid? pusher = null)
        {
            var e = entity;
            var hitboxRect = HitboxRect;
            
            var possibleSolids = _temp;
            var possibleJumpthrus = _jumpthrus;
            
            var useJumpthrus = moveV > 0 /*&& !this.IgnoreJumpThrus*/;
            
            possibleSolids.Clear();
            CollideExt.CollideIntoBroadPhase(Broadest, RectangleExt.Merge(hitboxRect, hitboxRect.MovedBy(0, moveV)), possibleSolids);
            if (useJumpthrus) {
                possibleJumpthrus.Clear();
                e.Scene.CollideIntoBroadPhase<JumpThru>(RectangleExt.Merge(hitboxRect, hitboxRect.MovedBy(0, moveV)), possibleJumpthrus);
            }
            
            if (possibleSolids.Count == 0 && possibleJumpthrus.Count == 0) {
                e.Y += moveV;
                return false;
            }
            
            Vector2 vector2 = e.Position + Vector2.UnitY * moveV;
            int dir = Math.Sign(moveV);
            int moved = 0;
            
            while (moveV != 0)
            {
                if (e.CollideFirst<Solid>(e.Position + Vector2.UnitY * dir, possibleSolids) is {} solid)
                {
                    MovementCounter.Y = 0.0f;
                    onCollide?.Invoke(new CollisionData
                    {
                        Direction = Vector2.UnitY * dir,
                        Moved = Vector2.UnitY * moved,
                        TargetPosition = vector2,
                        Hit = solid,
                        Pusher = pusher
                    });
                    return true;
                }
                if (useJumpthrus)
                {
                    if (e.CollideFirstOutside<JumpThru>(e.Position + Vector2.UnitY * dir, possibleJumpthrus) is {} jumpthru)
                    {
                        MovementCounter.Y = 0.0f;
                        onCollide?.Invoke(new CollisionData
                        {
                            Direction = Vector2.UnitY * dir,
                            Moved = Vector2.UnitY * moved,
                            TargetPosition = vector2,
                            Hit = jumpthru,
                            Pusher = pusher
                        });
                        return true;
                    }
                }
                moved += dir;
                moveV -= dir;
                e.Y += dir;
            }
            return false;
        }
    }
}