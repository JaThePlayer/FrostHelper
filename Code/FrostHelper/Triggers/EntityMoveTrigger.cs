using FrostHelper.Helpers;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/EntityMoveTrigger")]
internal sealed class EntityMoveTrigger : Trigger {
    private readonly EntityFilter entityFilter;
    private readonly Rectangle entityGrabBounds;
    private readonly Vector2 moveBy;
    private readonly Ease.Easer easer;
    private readonly float duration;
    private readonly bool once;

    private Tween? tween;
    private List<Entity>? entities;
    
    public EntityMoveTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        entityFilter = EntityFilter.CreateFrom(data);
        moveBy = new(data.Float("moveByX"), data.Float("moveByY"));
        easer = EaseHelper.GetEase(data.Attr("easing", "CubeInOut"));
        duration = data.Float("moveDuration", 1f);
        once = data.Bool("once");
        
        entityGrabBounds = RectangleExt.FromPoints(data.Nodes[0] + offset, data.Nodes[1] + offset);
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        CacheEntities();
    }

    private void CacheEntities() {
        if (entities is not null)
            return;
                
        entities = [];
        var bounds = entityGrabBounds;
        foreach (Entity entity in Scene.Entities) {
            if (entityFilter.Matches(entity) && bounds.Intersects(new((int)entity.Left, (int)entity.Top, (int)entity.Width, (int)entity.Height))) {
                entities.Add(entity);
            }
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        CacheEntities();
        
        tween = EntityMoveHelper.CreateMoveTween(entities!, moveBy, easer, duration);
        // If we're one use, the RemoveSelf call below would break all tweens if they were attached to this trigger. We'll use a helper entity for those cases.
        Entity tweenHolder = once ? ControllerHelper<StylegroundMoveTrigger.TweenHolder>.AddToSceneIfNeeded(Scene) : this;
        tweenHolder.Add(tween);
        
        if (once)
            RemoveSelf();
    }
}