﻿//#define DebugLog

using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/GroupedMoverAttacher")]
[Tracked]
internal sealed class GroupedMoverAttacher : Entity {
    public static readonly Type[] DefaultBlacklist = new[] {
        typeof(GroupedMoverAttacher),
    };

    public readonly int AttachGroup;
    public readonly Rectangle Rect;
    private readonly EntityFilter _filter;
    public readonly bool SpecialHandling;
    public readonly bool CanBeLeader;

    #region Hooks
    private static bool _loaded;
    public static void Load() {
        if (!_loaded) {
            _loaded = true;
            IL.Monocle.EntityList.UpdateLists += EntityList_UpdateLists;
        }
    }

    private static void EntityList_UpdateLists(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<EntityList>("adding") &&
                                                        instr.Next.MatchCallvirt(out var reference) && reference.Name == "Clear")) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.Emit(OpCodes.Ldfld, typeof(EntityList).GetField(nameof(EntityList.toAwake), BindingFlags.NonPublic | BindingFlags.Instance)!);
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitCall(PreAwake);
        }
    }

    public static void PreAwake(List<Entity> toAwake, EntityList entitiyList) {
        foreach (GroupedMoverAttacher item in entitiyList.Scene.Tracker.SafeGetEntities<GroupedMoverAttacher>()) {
            item.Check(toAwake);
        }
    }

    [OnUnload]
    public static void Unload() {
        IL.Monocle.EntityList.UpdateLists -= EntityList_UpdateLists;
    }
    #endregion

    public GroupedMoverAttacher(EntityData data, Vector2 offset) : base(data.Position + offset) {
        AttachGroup = data.Int("attachGroup", 0);
        
        _filter = EntityFilter.CreateFrom(data, "types", "isBlacklist", DefaultBlacklist);
        SpecialHandling = data.Bool("specialHandling", false);
        CanBeLeader = data.Bool("canBeLeader", true);

        Collider = new Hitbox(data.Width, data.Height);
        Collidable = true;

        Depth = int.MinValue;

        Load();
    }


    public void Check(List<Entity> entities) {
        foreach (Entity entity in entities) {
            if ((entity.Collider is not null ? CollideCheck(entity) : Collider.Collide(entity.Position))
                && _filter.Matches(entity)
                && (entity.Get<GroupedStaticMover>() is null)) {

                switch (entity) {
                    case Solid solid:
                        solid.AllowStaticMovers = false;
                        break;
                }

                var prevMover = entity.Get<StaticMover>();
                if (prevMover is not null) {
                    entity.Remove(prevMover);
                    var newMover = API.API.ToGroupedStaticMover(prevMover, AttachGroup, CanBeLeader) as GroupedStaticMover;
                    entity.Add(newMover);
                    continue;
                }

                Action<Vector2>? onMove = null;
                
                if (SpecialHandling) {
                    onMove = entity switch {
                        Platform p => (amt) => {
                            if (p.Scene is { }) {
                                p.MoveH(amt.X);
                                p.MoveV(amt.Y);
                            }
                        },
                        _ => null,
                    };
                }

                onMove ??= (amt) => entity.Position += amt;

                entity.Add(new GroupedStaticMover(AttachGroup, CanBeLeader) {
                    OnMove = onMove,
                    OnShake = onMove,
                    SolidChecker = (solid) => entity != solid && entity.CollideCheck(solid),
                    JumpThruChecker = (solid) => entity != solid && entity.CollideCheck(solid),
                    CanBeLeader = CanBeLeader,
                });

            }
        }
    }
}
