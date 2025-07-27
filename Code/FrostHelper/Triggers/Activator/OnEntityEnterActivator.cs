using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnEntityEnterActivator")]
internal sealed class OnEntityEnterActivator : BaseActivator {
    private readonly bool _cacheEntities;
    private readonly EntityFilter _filter;
    private List<Entity>? _cachedEntities;

    // see comment in Update
    //private Entity? lastCollidedEntity;

    private readonly HashSet<Entity> _lastCollided = [];

    public OnEntityEnterActivator(EntityData data, Vector2 offset) : base(data, offset) {
        _cacheEntities = data.Bool("cache", true);
        _filter = EntityFilter.CreateFrom(data); //API.API.GetTypes(data.Attr("types", ""));

        if (_filter.Empty) {
            NotificationHelper.Notify("An On Entity Enter Activator with an empty 'types' list will DO NOTHING!");
        }
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        if (_cacheEntities)
            _cachedEntities = scene.Entities.Where(IsValid).ToList();
    }

    public override void Update() {
        var hitbox = ((Hitbox) Collider)!;
        var player = Scene.Tracker.SafeGetEntity<Player>();

        if (player is null && !ActivateAfterDeath)
            return;

        /*
        TODO: More considerations
        The below approach works nicely when only supporting one entity at once - then you get nice OnStay and OnLeave calls.
        However, this gets quite weird when having multiple entities
        For now, no activator will call OnStay and OnLeave, for consistency and simplicity

        // check if the last entity we collided with is still in the trigger.
        // If yes, then we don't want to activate again.
        if (lastCollidedEntity is { Collidable: true, Collider: { } lastC } last 
            && last.Scene == Scene 
            && hitbox.Collide(lastC)) {
            CallOnStay(player);
            return;
        } else {
            // doesn't collide anymore, we can forget about the entity now
            lastCollidedEntity = null;
            CallOnLeave(player);
        }*/

        if (_cachedEntities is { } cache) {
            foreach (var entity in cache) {
                if (HandleEntity(hitbox, entity, player))
                    break;
            }
        } else {
            foreach (var entity in Scene.Entities.entities) {
                if (IsValid(entity) && HandleEntity(hitbox, entity, player))
                    break;
            }
        }
    }

    private bool IsValid(Entity e) => _filter.Matches(e);

    private bool HandleEntity(Hitbox hitbox, Entity entity, Player? player) {
        var ret = false;
    
        if (entity is { Collidable: true, Collider: { } c }) {
            var collided = hitbox.Collide(c);
            if (collided) {
                if (_lastCollided.Add(entity))
                    ActivateAll(player!);
            } else {
                if (_lastCollided.Remove(entity)) {

                }
            }
        }

        return ret;
        /*
         * See comment in Update.
            if (entity is { Collidable: true, Collider: { } c } && hitbox.Collide(c)) {
                lastCollidedEntity = entity;
                ActivateAll(player);
                return true;
            }

            return false;
             */
    }
}
