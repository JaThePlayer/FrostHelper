using FrostHelper.Helpers;

namespace FrostHelper;

[CustomEntity("FrostHelper/EntityMover")]
class EntityMover : Entity {
    private readonly EntityFilter entityFilter;

    private readonly Vector2 endNode;

    // For Tween
    private readonly Ease.Easer easer;
    private readonly float duration;
    private readonly float pauseTime;
    private readonly bool mustCollide;
    private readonly List<(Entity entity, Vector2 start)> entities = [];
    private readonly bool relativeMode;
    private readonly Vector2 distance;
    private readonly string onEndSfx;

    private float pauseTimer = 0f;
    private Tween tween;

    public EntityMover(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Collider = new Hitbox(data.Width, data.Height);

        entityFilter = EntityFilter.CreateFrom(data);

        pauseTime = data.Float("pauseTimeLength", 0f);
        pauseTimer = data.Float("startPauseTimeLength", 0f);
        relativeMode = data.Bool("relativeMovementMode", false);
        onEndSfx = data.Attr("onEndSFX", "");

        endNode = data.FirstNodeNullable(offset).GetValueOrDefault();
        easer = EaseHelper.GetEase(data.Attr("easing", "CubeInOut"));
        duration = data.Float("moveDuration", 1f);
        mustCollide = data.Bool("mustCollide", true);
        distance = new Vector2(endNode.X - Position.X, endNode.Y - Position.Y);
    }

    bool moveBack;

    public override void Awake(Scene scene) {
        base.Awake(scene);
        foreach (Entity entity in scene.Entities) {
            if ((!mustCollide || Collider.Collide(entity.Position)) && entityFilter.Matches(entity)) {
                entities.Add((entity, entity.Position));
            }
        }
        
        var t = Tween.Create(Tween.TweenMode.Looping, easer, duration, true);
        t.OnUpdate = tw => {
            foreach ((Entity? entity, Vector2 start) in entities) {
                Vector2 end = relativeMode ? start + distance : endNode;
                var to = moveBack ? Vector2.Lerp(end, start, tw.Eased) : Vector2.Lerp(start, end, tw.Eased);
                
                EntityMoveHelper.MoveEntity(entity, to);
            }
        };

        t.OnComplete = _ => {
            moveBack = !moveBack;
            pauseTimer = pauseTime;
            if (onEndSfx != "") {
                Audio.Play(onEndSfx);
            }
        };
        Add(tween = t);
    }

    public override void Update() {
        if (pauseTimer > 0f) {
            pauseTimer -= Engine.DeltaTime;
        } else {
            tween.Update();
        }
    }
}
